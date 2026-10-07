using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace OtInventoryLayout
{
    [HarmonyPatch(typeof(InventoryGui), "Show", typeof(Container), typeof(int))]
    internal static class InventoryGui_Show_Patch
    {
        private static readonly FieldInfo ContainerGridField = AccessTools.Field(typeof(InventoryGui), "m_containerGrid");

        private static void Postfix(InventoryGui __instance, Container container)
        {
            if (container == null)
            {
                return;
            }

            var grid = (InventoryGrid)ContainerGridField.GetValue(__instance);
            if (grid == null)
            {
                return;
            }

            if (Plugin.LogContainerUiHierarchy.Value)
            {
                DumpHierarchy(__instance.m_container);
            }

            ContainerPanelResizer.TryResize(__instance, grid, container);
        }

        private static void DumpHierarchy(RectTransform root)
        {
            if (root == null)
            {
                Plugin.Log.LogInfo("LogContainerUiHierarchy: m_container is null.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("LogContainerUiHierarchy: dump of container panel hierarchy follows.");
            Describe(root.transform, 0, sb);
            Plugin.Log.LogInfo(sb.ToString());
        }

        private static void Describe(Transform node, int depth, StringBuilder sb)
        {
            var indent = new string(' ', depth * 2);
            var rt = node as RectTransform;
            if (rt != null)
            {
                sb.AppendLine(
                    $"{indent}{node.name}  rect=({rt.rect.width:F1}x{rt.rect.height:F1}) " +
                    $"anchoredPos=({rt.anchoredPosition.x:F1},{rt.anchoredPosition.y:F1}) " +
                    $"sizeDelta=({rt.sizeDelta.x:F1},{rt.sizeDelta.y:F1}) " +
                    $"anchorMin=({rt.anchorMin.x:F2},{rt.anchorMin.y:F2}) " +
                    $"anchorMax=({rt.anchorMax.x:F2},{rt.anchorMax.y:F2}) " +
                    $"pivot=({rt.pivot.x:F2},{rt.pivot.y:F2})");
            }
            else
            {
                sb.AppendLine($"{indent}{node.name}");
            }

            foreach (var component in node.GetComponents<Component>())
            {
                if (component is Transform)
                {
                    continue;
                }

                var extra = "";
                if (component is ScrollRect scrollRect)
                {
                    extra = $"  viewport={(scrollRect.viewport ? scrollRect.viewport.name : "null")} content={(scrollRect.content ? scrollRect.content.name : "null")} " +
                            $"horizontal={scrollRect.horizontal} vertical={scrollRect.vertical}";
                }
                else if (component is Image image)
                {
                    extra = $"  sprite={(image.sprite ? image.sprite.name : "null")} type={image.type}";
                }
                else if (component is Mask)
                {
                    extra = "  (Mask)";
                }
                else if (component is RectMask2D)
                {
                    extra = "  (RectMask2D)";
                }
                else if (component is LayoutElement layoutElement)
                {
                    extra = $"  preferredHeight={layoutElement.preferredHeight} minHeight={layoutElement.minHeight}";
                }
                else if (component is ContentSizeFitter fitter)
                {
                    extra = $"  vertical={fitter.verticalFit} horizontal={fitter.horizontalFit}";
                }

                sb.AppendLine($"{indent}  [{component.GetType().Name}]{extra}");
            }

            for (int i = 0; i < node.childCount; i++)
            {
                Describe(node.GetChild(i), depth + 1, sb);
            }
        }
    }
}
