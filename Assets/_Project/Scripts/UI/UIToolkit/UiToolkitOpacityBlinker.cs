using UnityEngine.UIElements;

namespace Sporae.UI.UIToolkit
{
    /// <summary>
    /// Lampeggio opacità per label UITK (schedule su VisualElement, compatibile con render texture).
    /// </summary>
    public sealed class UiToolkitOpacityBlinker
    {
        private VisualElement _target;
        private IVisualElementScheduledItem _schedule;
        private bool _bright = true;

        public void Bind(VisualElement target)
        {
            Stop();
            _target = target;
        }

        public void SetActive(bool active, string activeClass = null)
        {
            Stop();

            if (_target == null || !active)
                return;

            if (!string.IsNullOrEmpty(activeClass))
                _target.AddToClassList(activeClass);

            _bright = true;
            _target.style.opacity = 1f;
            _schedule = _target.schedule.Execute(Tick).Every(420);
        }

        public void Stop(string activeClass = null)
        {
            if (_schedule != null)
            {
                _schedule.Pause();
                _schedule = null;
            }

            if (_target == null)
                return;

            if (!string.IsNullOrEmpty(activeClass))
                _target.RemoveFromClassList(activeClass);

            _target.style.opacity = 1f;
        }

        private void Tick()
        {
            if (_target == null)
                return;

            _bright = !_bright;
            _target.style.opacity = _bright ? 1f : 0.38f;
        }
    }
}
