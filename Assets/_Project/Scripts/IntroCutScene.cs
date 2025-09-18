using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project
{
    public class IntroCutScene : MonoBehaviour
    {
        [SerializeField] private string _sceneToLoad;
        [SerializeField] private float _timeToLoad;
        
        private void Start()
        {
            StartCoroutine(LoadRoutine());
        }

        private IEnumerator LoadRoutine()
        {
            yield return new WaitForSeconds(_timeToLoad);
            SceneManager.LoadScene(_sceneToLoad);
        }
    }
}