using UnityEngine;
using UnityEngine.UI;

namespace AirStrikeKit
{
    public class CanvasCameraFeed : MonoBehaviour
    {
        public Camera SourceCamera;
        public RawImage TargetImage;
        public RenderTexture TargetTextureAsset;
        public int Width = 2048;
        public int Height = 2048;
        public int Depth = 24;
        public int AntiAliasing = 8;
        public bool ForceRuntimeTexture = true;

        private RenderTexture runtimeTexture;

        void Awake()
        {
            ApplyFeed();
        }

        void OnEnable()
        {
            ApplyFeed();
        }

        void Start()
        {
            ApplyFeed();
        }

        void OnDisable()
        {
            if (SourceCamera != null && SourceCamera.targetTexture == runtimeTexture)
            {
                SourceCamera.targetTexture = null;
            }

            if (TargetImage != null && TargetImage.texture == runtimeTexture)
            {
                TargetImage.texture = null;
            }

            if (runtimeTexture != null)
            {
                runtimeTexture.Release();
                Destroy(runtimeTexture);
                runtimeTexture = null;
            }
        }

        private void ApplyFeed()
        {
            if (SourceCamera == null || TargetImage == null)
            {
                return;
            }

            int targetWidth = Mathf.Max(Width, 1);
            int targetHeight = Mathf.Max(Height, 1);
            RenderTexture textureToUse = ForceRuntimeTexture ? null : TargetTextureAsset;
            if (textureToUse == null)
            {
                if (runtimeTexture == null || runtimeTexture.width != targetWidth || runtimeTexture.height != targetHeight)
                {
                    if (runtimeTexture != null)
                    {
                        runtimeTexture.Release();
                        Destroy(runtimeTexture);
                    }

                    runtimeTexture = new RenderTexture(targetWidth, targetHeight, Depth, RenderTextureFormat.ARGB32);
                    runtimeTexture.name = "MainCanvasCameraFeed_Runtime";
                    runtimeTexture.antiAliasing = Mathf.Clamp(AntiAliasing, 1, 8);
                    runtimeTexture.filterMode = FilterMode.Bilinear;
                    runtimeTexture.wrapMode = TextureWrapMode.Clamp;
                    runtimeTexture.useMipMap = false;
                    runtimeTexture.autoGenerateMips = false;
                    runtimeTexture.Create();
                }

                textureToUse = runtimeTexture;
            }

            if (SourceCamera.targetTexture != textureToUse)
            {
                SourceCamera.targetTexture = textureToUse;
            }

            if (TargetImage.texture != textureToUse)
            {
                TargetImage.texture = textureToUse;
            }
        }
    }
}
