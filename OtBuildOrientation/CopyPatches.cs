using HarmonyLib;

namespace OtBuildOrientation
{
    // Vanilla already has pick block: Player.CopyPiece, bound to AltPlace (LeftShift) + Remove
    // (middle mouse), selects the hovered piece's type in the build menu and recovers its spin from
    // the piece's yaw - which is wrong for a flipped piece, and ignores which face rests downward.
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
            if (__result && Copied != null && __instance == Player.m_localPlayer && Plugin.CopyOrientation.Value)
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
