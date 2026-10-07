using UnityEngine;
using UnityEngine.UI;

namespace OtInventoryLayout
{
    // Best-effort resize of a container's UI panel so more of its rows are visible at once.
    // Assumes the vanilla panel follows Unity's standard ScrollRect/Viewport/Content pattern
    // (InventoryGrid's m_gridRoot as Content). Confirm the real hierarchy with
    // LogContainerUiHierarchy before trusting this on a container it hasn't been checked against.
    internal static class ContainerPanelResizer
    {
        internal static void TryResize(InventoryGui gui, InventoryGrid containerGrid, Container container)
        {
            var prefabName = Utils.GetPrefabName(container.gameObject);
            if (!Plugin.VisibleRowOverrides.TryGetValue(prefabName, out var maxVisibleRows))
            {
                return;
            }

            var scrollRect = containerGrid.GetComponentInParent<ScrollRect>();
            if (scrollRect == null || scrollRect.viewport == null)
            {
                Plugin.Log.LogWarning(
                    $"VisibleRowOverrides: no ScrollRect/viewport found above {prefabName}'s container grid - " +
                    "panel not resized. Enable LogContainerUiHierarchy and check the log for the real structure.");
                return;
            }

            var actualRows = container.GetInventory().GetHeight();
            var visibleRows = Mathf.Min(maxVisibleRows, actualRows);
            var rowSpace = containerGrid.m_elementSpace;
            var newViewportHeight = visibleRows * rowSpace;

            var viewport = scrollRect.viewport;
            var delta = newViewportHeight - viewport.rect.height;
            if (Mathf.Approximately(delta, 0f))
            {
                return;
            }

            viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newViewportHeight);

            var panel = gui.m_container;
            if (panel != null && panel != viewport)
            {
                panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panel.rect.height + delta);
            }

            scrollRect.verticalNormalizedPosition = 1f;

            Plugin.Log.LogInfo(
                $"VisibleRowOverrides: {prefabName} panel grown to show {visibleRows}/{actualRows} rows " +
                $"(viewport height {viewport.rect.height:F1}).");
        }
    }
}
