using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace OtBuildOrientation
{
    // Turns an already-built piece in place: with a build tool out, look at a piece, hold
    // RotatePieceKey and scroll. Vanilla has no such thing - a piece's rotation is read from its ZDO
    // once, when the object is created, and nothing (no ZSyncTransform) keeps a built piece's
    // transform in step with its ZDO afterwards. So the rotation is written to the ZDO (which makes
    // it persistent and correct for anyone who loads the area later) and also announced with a
    // routed RPC, which other players running the mod apply to the copy they already have loaded.
    // Players without the mod keep seeing the old rotation until they reload the area.
    //
    // The turn is about the vertical axis only, like vanilla's spin, and about the horizontal center
    // of the piece's colliders rather than its pivot: pivots sit wherever the prefab's author put
    // them (often an edge or corner), and turning around one swings the piece sideways instead of
    // spinning it where it stands.
    internal static class BuiltRotation
    {
        private const string RpcName = "OtBuildOrientation_RotatePiece";

        private static readonly AccessTools.FieldRef<Player, float> PlaceRotationDegrees =
            AccessTools.FieldRefAccess<Player, float>("m_placeRotationDegrees");

        private static readonly System.Reflection.MethodInfo CheckCanRemovePiece =
            AccessTools.Method(typeof(Player), "CheckCanRemovePiece");
        private static readonly System.Reflection.MethodInfo SetupColliders =
            AccessTools.Method(typeof(WearNTear), "SetupColliders");
        private static readonly System.Reflection.MethodInfo ClearCachedSupport =
            AccessTools.Method(typeof(WearNTear), "ClearCachedSupport");

        // Same layers WearNTear looks for supporting pieces on.
        private static int _supportMask;

        private static float _scrollAccumulated;

        internal static void RegisterRpc(ZRoutedRpc routedRpc)
        {
            routedRpc.Register<ZDOID, Vector3, Quaternion>(RpcName, RPC_RotatePiece);
        }

        // While the key is held over a piece, the scroll belongs to that piece: Orientation.FilterScroll
        // hands vanilla 0 so it doesn't spin the placement ghost too.
        internal static bool OwnsScroll(Player player) =>
            Plugin.IsRotatePieceHeld() && player.GetHoveringPiece() != null;

        // Stands in for ZInput.GetMouseScrollWheel() in GameCamera.UpdateCamera.
        internal static float CameraScroll()
        {
            Player player = Player.m_localPlayer;
            return player != null && OwnsScroll(player) ? 0f : ZInput.GetMouseScrollWheel();
        }

        internal static void Update(Player player)
        {
            if (!OwnsScroll(player))
            {
                _scrollAccumulated = 0f;
                return;
            }

            // Same threshold vanilla spins the ghost by, so one notch is one step either way.
            _scrollAccumulated += ZInput.GetMouseScrollWheel();
            int direction = 0;
            if (_scrollAccumulated > player.m_scrollAmountThreshold)
            {
                direction = 1;
            }
            else if (_scrollAccumulated < -player.m_scrollAmountThreshold)
            {
                direction = -1;
            }
            if (direction != 0)
            {
                _scrollAccumulated = 0f;
                TryRotate(player, player.GetHoveringPiece(), direction);
            }
        }

        // The same permission checks vanilla's RemovePiece makes before letting a player take a
        // piece down (removable at all, outside no-build zones, ward access, workbench in range), plus
        // vanilla's own rotate flag. Things that move on their own (ships, carts) are physics bodies
        // whose transform their ZSyncTransform owns, so they're left alone.
        private static void TryRotate(Player player, Piece piece, int direction)
        {
            ZNetView nview = piece.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid() || !piece.m_canRotate || piece.m_groundPiece ||
                !piece.m_canBeRemoved || piece.GetComponent<ZSyncTransform>() != null)
            {
                player.Message(MessageHud.MessageType.Center, "This piece can't be rotated");
                return;
            }
            Rigidbody body = piece.GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic)
            {
                player.Message(MessageHud.MessageType.Center, "This piece can't be rotated");
                return;
            }
            if (Location.IsInsideNoBuildLocation(piece.transform.position))
            {
                player.Message(MessageHud.MessageType.Center, "$msg_nobuildzone");
                return;
            }
            if (!PrivateArea.CheckAccess(piece.transform.position))
            {
                player.Message(MessageHud.MessageType.Center, "$msg_privatezone");
                return;
            }
            if (!(bool)CheckCanRemovePiece.Invoke(player, new object[] { piece }))
            {
                return;
            }

            float step = Plugin.RotatePieceStep.Value;
            if (step <= 0f)
            {
                step = PlaceRotationDegrees(player);
            }
            Quaternion turn = Quaternion.AngleAxis(step * direction, Vector3.up);
            Transform t = piece.transform;
            Vector3 center = HorizontalCenter(piece);
            Vector3 position = center + turn * (t.position - center);
            Quaternion rotation = turn * t.rotation;

            // Pieces resting on or holding up this one cache which colliders support them and where
            // those were; tell the ones touching it now to drop that, and the ones touching it in the
            // new spot below, so support is worked out afresh on both sides.
            ClearNeighborSupport(piece);

            // Only the owner's position/rotation writes bump the ZDO's revision and get sent on.
            nview.ClaimOwnership();
            ZDO zdo = nview.GetZDO();
            zdo.SetPosition(position);
            zdo.SetRotation(rotation);

            Apply(piece.gameObject, position, rotation);
            ClearNeighborSupport(piece);

            piece.m_placeEffect.Create(position, rotation, t);
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RpcName, zdo.m_uid, position, rotation);
        }

        private static void RPC_RotatePiece(long sender, ZDOID id, Vector3 position, Quaternion rotation)
        {
            if (sender == ZDOMan.GetSessionID() || ZNetScene.instance == null)
            {
                return;
            }
            GameObject go = ZNetScene.instance.FindInstance(id);
            if (go != null)
            {
                Apply(go, position, rotation);
            }
        }

        // WearNTear computes its support boxes in world space once (SetupColliders, on its first
        // support check) and caches which neighbors held it up, so after moving the transform both
        // are stale. Rebuilding them here keeps whoever owns the piece now or later working from the
        // new placement.
        private static void Apply(GameObject go, Vector3 position, Quaternion rotation)
        {
            go.transform.SetPositionAndRotation(position, rotation);
            // Physics queries (support overlap checks, collider bounds) otherwise keep seeing the
            // colliders where they were until the next physics step.
            Physics.SyncTransforms();
            WearNTear wnt = go.GetComponent<WearNTear>();
            if (wnt != null)
            {
                SetupColliders.Invoke(wnt, null);
                ClearCachedSupport.Invoke(wnt, null);
            }
        }

        private static IEnumerable<Collider> SolidColliders(Piece piece)
        {
            foreach (Collider collider in piece.GetComponentsInChildren<Collider>())
            {
                if (collider.enabled && !collider.isTrigger && collider.attachedRigidbody == null)
                {
                    yield return collider;
                }
            }
        }

        private static Vector3 HorizontalCenter(Piece piece)
        {
            bool any = false;
            Bounds bounds = default;
            foreach (Collider collider in SolidColliders(piece))
            {
                if (!any)
                {
                    bounds = collider.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }
            Vector3 position = piece.transform.position;
            return any ? new Vector3(bounds.center.x, position.y, bounds.center.z) : position;
        }

        // Mirrors what WearNTear.UpdateSupport does for a newly placed piece (m_clearCachedSupport):
        // every piece overlapping this one's slightly padded colliders gets RPC_ClearCachedSupport,
        // sent to its owner (which may be this client).
        private static void ClearNeighborSupport(Piece piece)
        {
            if (_supportMask == 0)
            {
                _supportMask = LayerMask.GetMask("piece", "Default", "static_solid", "Default_small", "terrain");
            }
            var own = new HashSet<Collider>(piece.GetComponentsInChildren<Collider>(true));
            var cleared = new HashSet<WearNTear>();
            foreach (Collider collider in SolidColliders(piece))
            {
                Bounds b = collider.bounds;
                foreach (Collider other in Physics.OverlapBox(b.center, b.extents + Vector3.one * 0.15f, Quaternion.identity, _supportMask))
                {
                    if (own.Contains(other) || other.isTrigger || other.attachedRigidbody != null)
                    {
                        continue;
                    }
                    WearNTear neighbor = other.GetComponentInParent<WearNTear>();
                    if (neighbor == null || !cleared.Add(neighbor))
                    {
                        continue;
                    }
                    ZNetView view = neighbor.GetComponent<ZNetView>();
                    if (view != null && view.IsValid())
                    {
                        view.InvokeRPC(view.GetZDO().GetOwner(), "RPC_ClearCachedSupport");
                    }
                }
            }
        }
    }

    // In build mode vanilla zooms the camera with the scroll wheel whenever it isn't spinning the
    // ghost: the selected piece can't rotate, or the crosshair hits nothing to place against. Either
    // can be true while turning a built piece, so the camera's scroll reads 0 while that owns it.
    // UpdateCamera reads the wheel twice (free-fly debug camera and the zoom); both are swapped,
    // which is harmless for free-fly since it's never in build mode.
    [HarmonyPatch(typeof(GameCamera), "UpdateCamera")]
    internal static class CameraZoomPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel));
            var replacement = AccessTools.Method(typeof(BuiltRotation), nameof(BuiltRotation.CameraScroll));
            int count = 0;
            foreach (var code in instructions)
            {
                if (code.Calls(target))
                {
                    count++;
                    yield return new CodeInstruction(OpCodes.Call, replacement).MoveLabelsFrom(code);
                }
                else
                {
                    yield return code;
                }
            }
            if (count == 0)
            {
                Plugin.Log.LogWarning("No scroll-wheel read found in GameCamera.UpdateCamera; turning a built piece may also zoom the camera.");
            }
        }
    }

    // ZNet creates a fresh ZRoutedRpc for every session, so register on each.
    [HarmonyPatch(typeof(ZRoutedRpc), MethodType.Constructor, typeof(bool))]
    internal static class ZRoutedRpcCtorPatch
    {
        private static void Postfix(ZRoutedRpc __instance)
        {
            BuiltRotation.RegisterRpc(__instance);
        }
    }
}
