using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI actionsText;
    public TextMeshProUGUI cryText;

    void Start()
    {
        if (GameManager.I == null) return;

        GameManager.I.OnDayChanged += UpdateDay;
        GameManager.I.OnActionsChanged += UpdateActions;
        GameManager.I.OnCRYChanged += UpdateCRY;

        // init
        UpdateDay(GameManager.I.CurrentDay);
        UpdateActions(GameManager.I.ActionsLeft);
        UpdateCRY(GameManager.I.CurrentCRY);
    }

    void OnDestroy()
    {
        if (GameManager.I == null) return;
        GameManager.I.OnDayChanged -= UpdateDay;
        GameManager.I.OnActionsChanged -= UpdateActions;
        GameManager.I.OnCRYChanged -= UpdateCRY;
    }

    void UpdateDay(int d) => dayText.text = $"Giorno: {d}";
    void UpdateActions(int a) => actionsText.text = $"Azioni: {a}";
    void UpdateCRY(int c) => cryText.text = $"CRY: {c}";
}
