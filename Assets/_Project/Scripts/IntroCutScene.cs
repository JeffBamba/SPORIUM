using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project
{
    public class IntroCutScene : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(LoadRoutine());
        }

        private IEnumerator LoadRoutine()
        {
            yield return new WaitForSeconds(5f);
            SceneManager.LoadScene(2);
        }
    }
}