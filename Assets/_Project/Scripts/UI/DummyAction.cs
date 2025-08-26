using UnityEngine;

public class DummyAction : MonoBehaviour
{
    public ActionCost cost;  // riferimento al componente sullo stesso bottone

    public void DoAction()
    {
        if (cost != null && cost.TryPerform())
            Debug.Log($"[DummyAction] OK → ActionsLeft={GameManager.I.ActionsLeft}, CRY={GameManager.I.CurrentCRY}");
        else
            Debug.LogWarning("[DummyAction] KO → Azioni finite o CRY insufficiente");
    }
}
