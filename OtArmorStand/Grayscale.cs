using UnityEngine;
using UnityEngine.Rendering;

namespace OtArmorStand
{
    // Monochrome copies of vanilla icons for the slot placeholders. An Image tint can only multiply
    // colors, not drop them, so the icon's pixels are converted instead. Vanilla icons sit in
    // atlases that aren't CPU-readable, so the icon is copied through the GPU first (Blit into a
    // RenderTexture, ReadPixels back), as OtMegingjord's Recolor does.
    internal static class Grayscale
    {
        internal static Sprite Sprite(Sprite source)
        {
            if (source == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
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
                    Color pixel = pixels[i];
                    float luminance = pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f;
                    pixels[i] = new Color(luminance, luminance, luminance, pixel.a);
                }
                copy.SetPixels(pixels);
                copy.Apply();

                var sprite = UnityEngine.Sprite.Create(copy, new Rect(0, 0, copy.width, copy.height), new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
                sprite.name = source.name + "_otarmorstand";
                return sprite;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
