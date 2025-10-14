using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Project
{
    public class CatalizzatoreCircle : MonoBehaviour, IPointerClickHandler
    {
        public event Action OnFailed;
        public event Action OnSuccess;
        
        public void Init(float duration)
        {
            StartCoroutine(LifeRoutine(duration));
        }

        private IEnumerator LifeRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            OnFailed?.Invoke();
            Destroy(gameObject);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            StopAllCoroutines();
            OnSuccess?.Invoke();
            Destroy(gameObject);
        }
    }
}