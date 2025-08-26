using UnityEngine;

public class EndDayButton : MonoBehaviour
{
    public int dailyPowerCost = 20;   // costo corrente giornaliero

    public void EndDay()
    {
        if (GameManager.I == null) return;
        GameManager.I.EndDay(dailyPowerCost);
        Debug.Log($"[EndDay] Day={GameManager.I.CurrentDay}  Actions={GameManager.I.ActionsLeft}  CRY={GameManager.I.CurrentCRY}");
    }

}
