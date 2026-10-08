using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace OtBuildOrientation
{
    // Tracks the current resting face and turns it into a rotation. Faces are applied in the
    // piece's own (pre-spin) space, i.e. vanilla's rotation becomes spin * face: "on its left side"
    // means the piece's own left, whichever way vanilla's scroll has spun it to face.
    internal static class Orientation
    {
        private static readonly string[] Names =
        {
            "Upright",
            "On left side",
            "Upside down",
            "On right side",
            "Face down",
            "Face up",
        };

        // Unity is left-handed with Y up: +90° about Z rolls the piece's right side (+X) up onto its
        // left side, and +90° about X tips its front (+Z) down.
        private static readonly Quaternion[] Rotations =
        {
            Quaternion.identity,
            Quaternion.Euler(0f, 0f, 90f),
            Quaternion.Euler(0f, 0f, 180f),
            Quaternion.Euler(0f, 0f, -90f),
            Quaternion.Euler(90f, 0f, 0f),
            Quaternion.Euler(-90f, 0f, 0f),
        };

        private static readonly AccessTools.FieldRef<Player, GameObject> PlacementGhost =
            AccessTools.FieldRefAccess<Player, GameObject>("m_placementGhost");

        private static readonly AccessTools.FieldRef<Player, PieceTable> BuildPieces =
            AccessTools.FieldRefAccess<Player, PieceTable>("m_buildPieces");

        private static readonly AccessTools.FieldRef<Player, int> PlaceRotation =
            AccessTools.FieldRefAccess<Player, int>("m_placeRotation");

        private static readonly AccessTools.FieldRef<Player, float> PlaceRotationDegrees =
            AccessTools.FieldRefAccess<Player, float>("m_placeRotationDegrees");

        private static int _index;
        private static float _scrollAccumulated;
        private static string _lastSelectedPrefab;

        // Same eligibility vanilla uses for its own scroll spin (Piece.m_canRotate), minus ground
        // pieces (hoe/cultivator terrain ops), which are flat stamps where a tipped orientation
        // makes no sense.
        private static bool CanOrient(Player player)
        {
            var ghost = PlacementGhost(player);
            if (ghost == null)
            {
                return false;
            }
            var piece = ghost.GetComponent<Piece>();
            return piece != null && piece.m_canRotate && !piece.m_groundPiece;
        }

        // Spliced into Player.UpdatePlacement in place of its one ZInput.GetMouseScrollWheel() call,
        // which vanilla only reaches for a rotatable ghost. With the modifier held, the scroll is
        // consumed here (vanilla sees 0 and doesn't spin) and steps the resting face instead, using
        // vanilla's own scroll threshold so one notch feels the same as one spin step.
        internal static float FilterScroll()
        {
            float scroll = ZInput.GetMouseScrollWheel();
            var player = Player.m_localPlayer;
            if (player == null || !Plugin.IsModifierHeld() || !CanOrient(player))
            {
                _scrollAccumulated = 0f;
                return scroll;
            }

            _scrollAccumulated += scroll;
            if (_scrollAccumulated > player.m_scrollAmountThreshold)
            {
                _scrollAccumulated = 0f;
                Step(player, 1);
            }
            else if (_scrollAccumulated < -player.m_scrollAmountThreshold)
            {
                _scrollAccumulated = 0f;
                Step(player, -1);
            }
            return 0f;
        }

        private static void Step(Player player, int direction)
        {
            _index = (_index + direction + Rotations.Length) % Rotations.Length;
            player.Message(MessageHud.MessageType.Center, Names[_index]);
        }

        // Spliced into Player.UpdatePlacementGhost right after it builds its spin-only rotation
        // (Quaternion.Euler(0, 22.5 * m_placeRotation, 0)); everything after that point - ghost
        // positioning, manual snap-point offsets, snap-point matching, and the rotation PlacePiece
        // eventually reads off the ghost - uses the returned value.
        internal static Quaternion Apply(Quaternion spin, Player player)
        {
            if (_index == 0 || !CanOrient(player))
            {
                return spin;
            }
            return spin * Rotations[_index];
        }

        // Vanilla's CopyPiece recovers only the spin, from the copied piece's yaw - wrong for a
        // flipped piece, whose euler Y no longer means "spin". Instead find the (spin, face) pair
        // whose combined rotation is closest to the piece's actual rotation. Runs after CopyPiece has
        // selected the piece (and SetupPlacementGhost has reset the face), so it has the final say.
        internal static void MatchCopiedPiece(Player player, Piece piece)
        {
            if (!CanOrient(player))
            {
                return;
            }

            float degrees = PlaceRotationDegrees(player);
            int spins = Mathf.Max(1, Mathf.RoundToInt(360f / degrees));
            var target = piece.transform.rotation;
            float best = float.MaxValue;
            for (int face = 0; face < Rotations.Length; face++)
            {
                for (int spin = 0; spin < spins; spin++)
                {
                    float angle = Quaternion.Angle(Quaternion.Euler(0f, degrees * spin, 0f) * Rotations[face], target);
                    if (angle < best)
                    {
                        best = angle;
                        _index = face;
                        PlaceRotation(player) = spin;
                    }
                }
            }
        }

        // SetupPlacementGhost reruns whenever the available piece list refreshes, not only when a
        // different piece is chosen, so the reset keys on the selected prefab actually changing.
        internal static void OnPlacementGhostSetup(Player player)
        {
            var buildPieces = BuildPieces(player);
            var prefab = buildPieces != null ? buildPieces.GetSelectedPrefab() : null;
            var name = prefab != null ? prefab.name : null;
            if (name != _lastSelectedPrefab)
            {
                _lastSelectedPrefab = name;
                _index = 0;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "UpdatePlacement")]
    internal static class UpdatePlacementPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel));
            var replacement = AccessTools.Method(typeof(Orientation), nameof(Orientation.FilterScroll));
            return Splice.ReplaceSingleCall(instructions, target, "Player.UpdatePlacement",
                call => new[] { new CodeInstruction(OpCodes.Call, replacement).MoveLabelsFrom(call) });
        }
    }

    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    internal static class UpdatePlacementGhostPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(Quaternion), nameof(Quaternion.Euler), new[] { typeof(float), typeof(float), typeof(float) });
            var apply = AccessTools.Method(typeof(Orientation), nameof(Orientation.Apply));
            return Splice.ReplaceSingleCall(instructions, target, "Player.UpdatePlacementGhost",
                call => new[]
                {
                    call,
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, apply),
                });
        }
    }

    [HarmonyPatch(typeof(Player), "SetupPlacementGhost")]
    internal static class SetupPlacementGhostPatch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                Orientation.OnPlacementGhostSetup(__instance);
            }
        }
    }

    // Before accepting a snap, vanilla rejects it if an identically-named piece already sits within
    // 5cm of the snapped position - and, unless the piece has m_allowRotatedOverlap (stone stairs,
    // beams and most others don't), regardless of rotation. Vanilla can only ever produce two same
    // pieces on one pivot by spinning a copy in place, so that's a fair duplicate guard there. Once
    // pieces can be flipped it isn't: a stair is pivoted at the center of its flat bottom face, and
    // flipping it upside down leaves that face (and the pivot) on top, so an upright stair snapped
    // flush onto it lands on exactly the same pivot and every one of its snaps gets thrown away.
    // Pieces whose up directions differ are never stacked duplicates, so they no longer count; when
    // the up directions match (every vanilla-only pair, or two pieces flipped the same way) vanilla's
    // verdict stands.
    [HarmonyPatch(typeof(Player), "IsOverlappingOtherPiece")]
    internal static class IsOverlappingOtherPiecePatch
    {
        private const float MaxUpAngle = 10f;

        private static void Postfix(Vector3 p, Quaternion rotation, string pieceName, List<Piece> pieces, bool allowRotatedOverlap, ref bool __result)
        {
            if (!__result)
            {
                return;
            }

            var up = rotation * Vector3.up;
            __result = pieces.Any(other =>
                other != null &&
                Vector3.Distance(p, other.transform.position) < 0.05f &&
                (!allowRotatedOverlap || Quaternion.Angle(other.transform.rotation, rotation) <= 10f) &&
                other.gameObject.name.CustomStartsWith(pieceName) &&
                Vector3.Angle(other.transform.up, up) <= MaxUpAngle);
        }
    }

    internal static class Splice
    {
        // Both transpilers rely on vanilla making exactly one call to their target method. If a game
        // update changes that, patching a guessed call site could silently misbehave, so leave the
        // method untouched and say so in the log instead.
        internal static IEnumerable<CodeInstruction> ReplaceSingleCall(
            IEnumerable<CodeInstruction> instructions, MethodInfo target, string where,
            System.Func<CodeInstruction, IEnumerable<CodeInstruction>> replace)
        {
            var codes = instructions.ToList();
            var matches = codes.Where(c => c.Calls(target)).ToList();
            if (matches.Count != 1)
            {
                Plugin.Log.LogError($"Expected 1 call to {target.DeclaringType.Name}.{target.Name} in {where}, found {matches.Count} - leaving it unpatched, orientation won't work.");
                return codes;
            }

            var result = new List<CodeInstruction>(codes.Count + 2);
            foreach (var code in codes)
            {
                if (code == matches[0])
                {
                    result.AddRange(replace(code));
                }
                else
                {
                    result.Add(code);
                }
            }
            return result;
        }
    }
}
