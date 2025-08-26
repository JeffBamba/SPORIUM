using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;                    // TextMeshPro

public class SceneLoadingUI : MonoBehaviour
{
    [Header("Target scene (must be in Build Settings)")]
    public string firstScene = "SCN_VaultMap";   // <-- CAMBIATO da SCN_Dome_Main a SCN_VaultMap

    [Header("UI refs")]
    public TextMeshProUGUI loadingText;          // assegna TMP Text
    public UnityEngine.UI.Image loadingBarFill;  // tipo esplicito UI

    private void Start()
    {
        StartCoroutine(LoadMainSceneAsync());
    }

    private IEnumerator LoadMainSceneAsync()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(firstScene);

        while (!op.isDone)
        {
            if (loadingBarFill != null)
                loadingBarFill.fillAmount = Mathf.Clamp01(op.progress / 0.9f);

            if (loadingText != null)
                loadingText.text = $"Caricamento... {op.progress * 100f:0}%";

            yield return null;
        }
    }
}
