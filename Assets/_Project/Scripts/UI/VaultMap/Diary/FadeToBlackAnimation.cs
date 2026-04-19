using System;
using _Project.Sporae.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    public class FadeToBlackAnimation : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private float _duration;

        private DayCycleSystem _dayCycleSystem;
        
        private readonly Color k_blackColor = new(0, 0, 0, 1);
        private readonly Color k_transparentColor = new(0, 0, 0, 0);

        public event Action OnFaded;
        
        public void Show()
        {
            if (_image == null)
                return;
            DOTween.Kill(_image, false);
            DOTween.To(
                () => _image.color,
                x => _image.color = x,
                k_blackColor,
                _duration
            ).OnComplete(() => {
                OnFaded?.Invoke();
            });
        }

        public void Hide()
        {
            if (_image == null)
                return;
            DOTween.Kill(_image, false);
            DOTween.To(
                () => _image.color,
                x => _image.color = x,
                k_transparentColor,
                _duration
            );   
        }

        /// <summary>Schermo nero immediato poi dissolvenza verso trasparente (ingresso da menu).</summary>
        public void PlayEntryFadeInFromBlack()
        {
            if (_image == null)
                return;
            DOTween.Kill(_image, false);
            _image.color = k_blackColor;
            Hide();
        }

        private void Awake()
        {
            if (VaultMapEntryFade.RequestFadeInOnNextLoad && _image != null)
                _image.color = k_blackColor;
            _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
        }

        private void Start()
        {
            _dayCycleSystem.OnDayChanged += HandleDayChanged;

            if (VaultMapEntryFade.RequestFadeInOnNextLoad)
            {
                VaultMapEntryFade.RequestFadeInOnNextLoad = false;
                PlayEntryFadeInFromBlack();
            }
        }
        
        private void HandleDayChanged(int d)
        {
            Hide();
        }
    }
}