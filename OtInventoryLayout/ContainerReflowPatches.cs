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

        // Vanilla's shared container panel is sized to fit this many rows/columns of its native
        // layout without scrolling (e.g. the blackmetal chest's 8x4 layout fits exactly, in both
        // directions) - confirmed from the panel's measured size via LogContainerUiHierarchy.
        // Captured relative to this, rather than hardcoded in pixels, so it keeps working even if
        // element spacing ever changes.
        private const float VanillaFitColumns = 8f;
        private const float VanillaFitRows = 4f;

        private static bool _baselineCaptured;
        private static float _baselineContainerWidth;
        private static float _baselineContainerHeight;

        // Every direct child of the panel that's anchored to its own center (rather than
        // stretched or pinned to a corner) needs repositioning when the panel resizes, since its
        // anchor point moves but its offset from that point doesn't. Covers the TakeAll/StackAll/
        // SortAll buttons, the scrollbar track, and the "sunken" backdrop art - keyed generically
        // by child name rather than listing each one, so a game update adding/renaming a button
        // doesn't need a code change here.
        private static readonly Dictionary<string, (Vector2 pos, Vector2 size)> BaselineCenterChildren =
            new Dictionary<string, (Vector2 pos, Vector2 size)>();

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
            ResizePanel(gui, displayWidth, displayHeight, __instance.m_elementSpace);
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

        private static bool IsCenterAnchored(RectTransform rt)
        {
            return Vector2.Distance(rt.anchorMin, new Vector2(0.5f, 0.5f)) < 0.01f
                   && Vector2.Distance(rt.anchorMax, new Vector2(0.5f, 0.5f)) < 0.01f;
        }

        private static void ResizePanel(InventoryGui gui, int displayWidth, int displayHeight, float elementSpace)
        {
            var panel = gui.m_container;
            if (panel == null)
            {
                return;
            }

            if (!_baselineCaptured)
            {
                _baselineContainerWidth = panel.rect.width;
                _baselineContainerHeight = panel.rect.height;
                for (var i = 0; i < panel.childCount; i++)
                {
                    var child = panel.GetChild(i) as RectTransform;
                    if (child != null && IsCenterAnchored(child))
                    {
                        BaselineCenterChildren[child.name] = (child.anchoredPosition, child.rect.size);
                    }
                }

                _baselineCaptured = true;
            }

            var widthPadding = _baselineContainerWidth - VanillaFitColumns * elementSpace;
            var heightPadding = _baselineContainerHeight - VanillaFitRows * elementSpace;

            var targetContainerWidth = Mathf.Max(_baselineContainerWidth, displayWidth * elementSpace + widthPadding);
            var targetContainerHeight = Mathf.Max(_baselineContainerHeight, displayHeight * elementSpace + heightPadding);
            var widthDelta = targetContainerWidth - _baselineContainerWidth;
            var heightDelta = targetContainerHeight - _baselineContainerHeight;

            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetContainerWidth);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetContainerHeight);

            // No X correction: the grid's own slots are horizontally *centered* within the panel
            // (RepositionElements' origin formula), so the grid's visual center already coincides
            // exactly with the same center-anchor point these children use - meaning a child's X
            // offset from that point needs no adjustment to keep pace with the widening grid. (An
            // earlier version corrected X the same way as Y - keeping a fixed distance from the
            // panel's left edge - which instead held buttons in their old screen position while
            // the grid visibly widened out from under them.)
            //
            // Y does need correcting: the grid is *top*-anchored, not centered (it only grows
            // downward, for scrolling), so the fixed structural reference is the top edge, not
            // the center - but the center-anchor point these children use drifts away from that
            // fixed top edge as height grows. Pushing the Y offset out by half the height growth
            // cancels that drift and keeps each child's distance from the top edge constant.
            // Worked out by hand from Unity's RectTransform.rect formula (rect = (-size*pivot,
            // size)); confirm against LogContainerUiHierarchy if this ever looks off again.
            var correction = new Vector2(0f, heightDelta / 2f);
            for (var i = 0; i < panel.childCount; i++)
            {
                var child = panel.GetChild(i) as RectTransform;
                if (child == null || !BaselineCenterChildren.TryGetValue(child.name, out var baseline))
                {
                    continue;
                }

                if (child.name == "sunken")
                {
                    child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, baseline.size.x + widthDelta);
                    child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, baseline.size.y + heightDelta);
                }

                child.anchoredPosition = baseline.pos + correction;
            }
        }
    }
}
