using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace Sporae.UI.UIToolkit.HUD.Components
{
    public sealed class UiGlowFrameGenerator
    {
        private readonly VisualElement _target;
        private readonly Material _material;
        private RenderTexture _rt;
        private bool _isDisposed;

        public UiGlowFrameGenerator(VisualElement target, Material material)
        {
            _target = target;
            _material = material;

            if (_target != null)
            {
                _target.RegisterCallback<GeometryChangedEvent>(_ => Render());
                _target.RegisterCallback<AttachToPanelEvent>(_ => Render());
            }
        }

        public void Render()
        {
            if (_isDisposed || _target == null || _material == null)
                return;

            int w = Mathf.Max(2, Mathf.RoundToInt(_target.resolvedStyle.width));
            int h = Mathf.Max(2, Mathf.RoundToInt(_target.resolvedStyle.height));
            if (w <= 0 || h <= 0)
                return;

            if (_rt == null || _rt.width != w || _rt.height != h)
            {
                Release();
                _rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                _rt.Create();
            }

            _material.SetVector("_Size", new Vector4(w, h, 0f, 0f));
            Graphics.Blit(null, _rt, _material);

            _target.style.backgroundImage = Background.FromRenderTexture(_rt);
            _target.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
            _target.MarkDirtyRepaint();
        }

        public void Dispose()
        {
            _isDisposed = true;
            Release();
        }

        private void Release()
        {
            if (_rt != null)
            {
                _rt.Release();
                UnityEngine.Object.Destroy(_rt);
                _rt = null;
            }
        }
    }
}
