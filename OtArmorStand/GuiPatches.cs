using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OtArmorStand
{
    // The stand's panel is vanilla's container panel. Its Place stacks button does nothing for a
    // stand, so while one is open it's swapped for a Swap button cloned from it.
    [HarmonyPatch(typeof(InventoryGui))]
    internal static class GuiPatches
    {
        private static readonly AccessTools.FieldRef<InventoryGui, Container> CurrentContainer =
            AccessTools.FieldRefAccess<InventoryGui, Container>("m_currentContainer");
        private static readonly MethodInfo SetupDragItem = AccessTools.Method(typeof(InventoryGui), "SetupDragItem");

        private static Button _swapButton;
        private static TMP_Text _swapLabel;

        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        private static void AwakePostfix(InventoryGui __instance)
        {
            Button source = __instance.m_stackAllButton;
            if (source == null)
            {
                return;
            }
            GameObject go = Object.Instantiate(source.gameObject, source.transform.parent);
            go.name = "OtArmorStand_SwapButton";
            // Localize would put "Place stacks" back on language or input-layout changes.
            foreach (var localize in go.GetComponentsInChildren<Localize>(true))
            {
                Object.DestroyImmediate(localize);
            }
            _swapButton = go.GetComponent<Button>();
            _swapButton.onClick.RemoveAllListeners();
            _swapButton.onClick.AddListener(() => OnSwap(__instance));
            _swapLabel = go.GetComponentInChildren<TMP_Text>(true);
            go.SetActive(false);
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        private static void UpdatePostfix(InventoryGui __instance)
        {
            Container shown = CurrentContainer(__instance);
            StandSession.Tick(shown);

            bool standOpen = StandSession.Current != null && shown == StandSession.Current.Stub;
            if (__instance.m_stackAllButton != null && __instance.m_stackAllButton.gameObject.activeSelf == standOpen)
            {
                __instance.m_stackAllButton.gameObject.SetActive(!standOpen);
            }
            if (_swapButton != null)
            {
                bool showSwap = standOpen && Plugin.ShowSwapButton.Value;
                if (_swapButton.gameObject.activeSelf != showSwap)
                {
                    _swapButton.gameObject.SetActive(showSwap);
                    if (showSwap && _swapLabel != null)
                    {
                        _swapLabel.text = "Swap armor";
                    }
                }
            }
        }

        private static void OnSwap(InventoryGui gui)
        {
            var session = StandSession.Current;
            Player player = Player.m_localPlayer;
            if (session == null || player == null || player.IsTeleporting() || CurrentContainer(gui) != session.Stub)
            {
                return;
            }
            // Same as vanilla's Take all: drop whatever is being dragged first.
            SetupDragItem.Invoke(gui, new object[] { null, null, 1 });
            session.Swap(player);
            session.Sync();
        }
    }

    // Holding Use right after opening a container places stacks, which for a regular chest goes
    // through Container.StackAll's RPCs. The panel's stand-in Container never registered them.
    [HarmonyPatch(typeof(Container), nameof(Container.StackAll))]
    internal static class ContainerStackAllPatch
    {
        private static bool Prefix(Container __instance)
        {
            var session = StandSession.Current;
            return session == null || __instance != session.Stub;
        }
    }
}
