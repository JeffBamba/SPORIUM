using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SceneLoadingUI : MonoBehaviour
{
    [Header("Target scene (must be in Build Settings)")]
    public string firstScene = "SCN_Dome_Main";

    [Header("UI refs")]
    public TextMeshProUGUI loadingText;                   // assegna TMP Text
    public UnityEngine.UI.Image loadingBarFill;           // UGUI Image (Image Type = Filled)

    [Header("Manual hold at 100%")]
    public bool holdForKey = true;                        // resta fermo a 100% finché non premi un tasto
    public KeyCode continueKey = KeyCode.Return;          // tasto per proseguire (di default: Invio)
    public bool showHint = true;                          // mostra suggerimento "(premi INVIO)"

    private void Start()
    {
        StartCoroutine(LoadMainSceneAsync());
    }

    private IEnumerator LoadMainSceneAsync()
    {
        // piccolo delay per assicurare la visibilità dell'overlay almeno 1 frame
        yield return null;

        var op = SceneManager.LoadSceneAsync(firstScene, LoadSceneMode.Single);
        op.allowSceneActivation = false;

        // Avanza finché Unity arriva a ~90% (op.progress ~0.9)
        while (op.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            UpdateUI(progress);
            yield return null;
        }

        // Mostra 100% e blocca finché non premi il tasto (se richiesto)
        UpdateUI(1f);

        if (holdForKey)
        {
            if (loadingText != null && showHint)
                loadingText.text = "Loading... 100% (premi INVIO)";

            // attende finché non viene premuto il tasto scelto
            while (!Input.GetKeyDown(continueKey))
                yield return null;
        }

        // ora consenti l'attivazione della scena
        op.allowSceneActivation = true;
    }

    private void UpdateUI(float progress01)
    {
        if (loadingText != null)
            // uso "..." per evitare problemi di encoding del carattere ellissi
            loadingText.text = $"Loading... {Mathf.RoundToInt(progress01 * 100f)}%";

        if (loadingBarFill != null)
            loadingBarFill.fillAmount = progress01; // richiede Image Type = Filled
    }
}
