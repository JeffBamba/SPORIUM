using UnityEngine;

public class ActionCost : MonoBehaviour
{
    [Min(0)] public int cryCost = 0;

    public bool TryPerform()
    {
        return GameManager.I != null && GameManager.I.TrySpendAction(cryCost);
    }
}
