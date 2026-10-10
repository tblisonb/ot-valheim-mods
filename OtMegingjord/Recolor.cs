using UnityEngine;
using UnityEngine.Rendering;

namespace OtMegingjord
{
    // Placeholder art: each upgrade item is a vanilla item tinted toward its biome's color. Vanilla
    // icons sit in atlases that aren't CPU-readable, so the icon is copied through the GPU (Blit into
    // a RenderTexture, ReadPixels back) before tinting.
    internal static class Recolor
    {
        // How far each pixel moves from its own color toward the tint (keeping its brightness).
        private const float Strength = 0.75f;

        internal static Sprite Sprite(Sprite source, Color tint)
        {
            if (source == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                // Dedicated server: no GPU and nobody to look at icons.
                return source;
            }

            var texture = source.texture;
            var rect = source.textureRect;
            var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(texture, rt);
                RenderTexture.active = rt;
                var copy = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(rect, 0, 0);

                var pixels = copy.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Tint(pixels[i], tint);
                }
                copy.SetPixels(pixels);
                copy.Apply();

                var sprite = UnityEngine.Sprite.Create(copy, new Rect(0, 0, copy.width, copy.height), new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
                sprite.name = source.name + "_otmegingjord";
                return sprite;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        private static Color Tint(Color pixel, Color tint)
        {
            float luminance = pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f;
            // Scale so a mid-grey pixel lands on roughly the tint itself.
            var target = tint * Mathf.Clamp01(luminance * 1.8f);
            var result = Color.Lerp(pixel, target, Strength);
            result.a = pixel.a;
            return result;
        }

        // Tints the materials of the item's world model (copies, so the vanilla item is unchanged).
        internal static void Renderers(GameObject go, Color tint)
        {
            var color = Color.Lerp(Color.white, tint, Strength);
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || !materials[i].HasProperty("_Color"))
                    {
                        continue;
                    }
                    var material = new Material(materials[i]);
                    material.color = material.color * color;
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
        }
    }
}
