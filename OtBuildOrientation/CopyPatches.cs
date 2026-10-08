using System;
using HarmonyLib;

namespace OtBuildOrientation
{
    // Pick block. Vanilla already has it - Player.CopyPiece, bound to AltPlace (LeftShift) + Remove
    // (middle mouse): selects the hovered piece's type in the build menu and copies its spin, or says
    // "missing requirement" if it isn't buildable. This adds CopyModifierKey + Remove as a second
    // binding that calls the same method, and teaches CopyPiece (both bindings) to copy the piece's
    // resting face too.
    [HarmonyPatch(typeof(Player), "UpdatePlacement")]
    internal static class CopyInputPatch
    {
        private static readonly Func<Player, bool> CopyPiece =
            AccessTools.MethodDelegate<Func<Player, bool>>(AccessTools.Method(typeof(Player), "CopyPiece"));

        private static readonly AccessTools.FieldRef<Player, bool> BlockRemove =
            AccessTools.FieldRefAccess<Player, bool>("m_blockRemove");

        private static bool _suppressingRemove;

        // Without the AltPlace key held, vanilla treats the Remove button's *release* as "remove the
        // hovered piece". m_blockRemove skips that, but vanilla clears it every frame AltPlace isn't
        // held - after the release check, though, so setting it here, before vanilla runs, on every
        // frame from the press through the release frame keeps a copy from also deleting the piece.
        private static void Prefix(Player __instance, bool takeInput)
        {
            if (__instance != Player.m_localPlayer || !takeInput || !__instance.InPlaceMode() ||
                __instance.IsDead() || Hud.IsPieceSelectionVisible() || ZInput.IsGamepadActive())
            {
                _suppressingRemove = false;
                return;
            }

            // With AltPlace also held, vanilla's own binding copies this frame; don't copy twice.
            if (ZInput.GetButtonDown("Remove") && Plugin.IsCopyModifierHeld() && !ZInput.GetButton("AltPlace"))
            {
                _suppressingRemove = true;
                CopyPiece(__instance);
            }

            if (_suppressingRemove)
            {
                BlockRemove(__instance) = true;
                if (!ZInput.GetButton("Remove") && !ZInput.GetButtonUp("Remove"))
                {
                    _suppressingRemove = false;
                }
            }
        }
    }

    // CopyPiece doesn't hand back the piece it copied, so catch it on its way through
    // SetSelectedPiece(Piece) - only while CopyPiece is running - and match its orientation once
    // CopyPiece has finished overwriting the spin.
    [HarmonyPatch(typeof(Player), "CopyPiece")]
    internal static class CopyPiecePatch
    {
        internal static bool Copying;
        internal static Piece Copied;

        private static void Prefix()
        {
            Copying = true;
            Copied = null;
        }

        private static void Postfix(Player __instance, bool __result)
        {
            Copying = false;
            if (__result && Copied != null && __instance == Player.m_localPlayer)
            {
                Orientation.MatchCopiedPiece(__instance, Copied);
            }
            Copied = null;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetSelectedPiece), typeof(Piece))]
    internal static class SetSelectedPiecePatch
    {
        private static void Postfix(Piece p, bool __result)
        {
            if (CopyPiecePatch.Copying && __result)
            {
                CopyPiecePatch.Copied = p;
            }
        }
    }
}
