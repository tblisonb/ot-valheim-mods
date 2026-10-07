using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OtInventoryLayout
{
    // Repacks the container grid's slots into a configured column count after vanilla finishes
    // laying them out at their native (real inventory) positions. Only the on-screen position of
    // each already-built InventoryElement changes - nothing about how elements are looked up by
    // real grid coordinate (clicking, dragging, hovering, gamepad highlight) is touched, so mouse
    // interaction keeps working unmodified. Gamepad d-pad/stick navigation still steps through
    // slots in the *native* column order, since that logic (InventoryGrid.UpdateGamepad) isn't
    // patched - fine for now since this is a mouse-driven feature, but worth knowing if gamepad
    // play surfaces an issue.
    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
    internal static class InventoryGrid_UpdateGui_Patch
    {
        private static readonly FieldInfo ElementsField = AccessTools.Field(typeof(InventoryGrid), "m_elements");
        private static readonly FieldInfo CurrentContainerField = AccessTools.Field(typeof(InventoryGui), "m_currentContainer");

        // Vanilla's shared container panel is sized to fit this many rows of its native column
        // layout without scrolling (e.g. the blackmetal chest's 8x4 layout fits exactly) -
        // confirmed from the panel's measured height via LogContainerUiHierarchy. Captured
        // relative to this, rather than hardcoded in pixels, so it keeps working even if element
        // spacing ever changes.
        private const float VanillaFitRows = 4f;

        private static bool _baselineCaptured;
        private static float _baselineContainerWidth;
        private static float _baselineContainerHeight;
        private static float _baselineSunkenHeight;
        private static Vector2 _baselineSunkenAnchoredPos;
        private static readonly HashSet<string> WidthWarnedPrefabs = new HashSet<string>();

        private static void Postfix(InventoryGrid __instance, Player player)
        {
            if (player != null)
            {
                return; // only the container grid is ever updated with a null player.
            }

            var gui = InventoryGui.instance;
            if (gui == null || __instance != gui.ContainerGrid)
            {
                return;
            }

            var container = (Container)CurrentContainerField.GetValue(gui);
            if (container == null)
            {
                return;
            }

            var inventory = __instance.GetInventory();
            var realWidth = inventory.GetWidth();
            var realHeight = inventory.GetHeight();
            var totalCells = realWidth * realHeight;
            if (totalCells <= 0)
            {
                return;
            }

            var prefabName = Utils.GetPrefabName(container.gameObject);
            var displayWidth = realWidth;
            if (Plugin.DisplayColumnOverrides.TryGetValue(prefabName, out var configuredWidth) && configuredWidth > 0)
            {
                displayWidth = configuredWidth;
            }

            var displayHeight = Mathf.CeilToInt((float)totalCells / displayWidth);

            var elements = (List<InventoryElement>)ElementsField.GetValue(__instance);
            RepositionElements(__instance, elements, displayWidth, displayHeight);
            ResizePanel(gui, prefabName, displayWidth, displayHeight, __instance.m_elementSpace);
        }

        private static void RepositionElements(InventoryGrid grid, List<InventoryElement> elements, int displayWidth, int displayHeight)
        {
            var elementSpace = grid.m_elementSpace;
            var selfRect = grid.transform as RectTransform;
            var widgetSize = new Vector2(displayWidth * elementSpace, displayHeight * elementSpace);
            var origin = new Vector2(selfRect.rect.width / 2f, 0f) - new Vector2(widgetSize.x, 0f) * 0.5f;

            for (var i = 0; i < elements.Count; i++)
            {
                var dx = i % displayWidth;
                var dy = i / displayWidth;
                var offset = new Vector2(dx * elementSpace, (0f - dy) * elementSpace);
                elements[i].GetElementRectTransform().anchoredPosition = origin + offset;
            }

            grid.m_gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, displayHeight * elementSpace);
        }

        private static void ResizePanel(InventoryGui gui, string prefabName, int displayWidth, int displayHeight, float elementSpace)
        {
            var panel = gui.m_container;
            var sunken = panel.Find("sunken") as RectTransform;
            if (panel == null || sunken == null)
            {
                return;
            }

            if (!_baselineCaptured)
            {
                _baselineContainerWidth = panel.rect.width;
                _baselineContainerHeight = panel.rect.height;
                _baselineSunkenHeight = sunken.rect.height;
                _baselineSunkenAnchoredPos = sunken.anchoredPosition;
                _baselineCaptured = true;
            }

            if (displayWidth * elementSpace > _baselineContainerWidth && WidthWarnedPrefabs.Add(prefabName))
            {
                Plugin.Log.LogWarning(
                    $"DisplayColumnOverrides: {prefabName}'s configured width ({displayWidth} columns) is wider than " +
                    "the container panel - slots past the right edge will render outside the visible panel. This " +
                    "mod only grows the panel's height, not its width.");
            }

            var heightPadding = _baselineContainerHeight - VanillaFitRows * elementSpace;

            var targetContainerHeight = Mathf.Max(_baselineContainerHeight, displayHeight * elementSpace + heightPadding);
            var heightDelta = targetContainerHeight - _baselineContainerHeight;

            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetContainerHeight);

            var targetSunkenHeight = _baselineSunkenHeight + heightDelta;
            sunken.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetSunkenHeight);
            sunken.anchoredPosition = _baselineSunkenAnchoredPos + new Vector2(0f, -heightDelta / 2f);
        }
    }
}
