using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;                    // TextMeshPro

public class SceneLoadingUI : MonoBehaviour
{
    [Header("Target scene (must be in Build Settings)")]
    public string firstScene = "SCN_Dome_Main";

    [Header("UI refs")]
    public TextMeshProUGUI loadingText;                   // assegna TMP Text
    public UnityEngine.UI.Image loadingBarFill;           // <- tipo ESPPLICITO UI (non UIElements!)

    private void Start()
    {
        StartCoroutine(LoadMainSceneAsync());
    }

    private IEnumerator LoadMainSceneAsync()
    {
        yield return null;

        var op = SceneManager.LoadSceneAsync(firstScene, LoadSceneMode.Single);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            if (loadingText != null) loadingText.text = $"Loading… {Mathf.RoundToInt(progress * 100f)}%";
            if (loadingBarFill != null) loadingBarFill.fillAmount = progress;

            if (progress >= 1f)
            {
                yield return new WaitForSeconds(0.15f);
                op.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}
