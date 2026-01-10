using UnityEngine;

public class DeselectHandler : MonoBehaviour
{
    [SerializeField] private Transform followObject;

    // DEBUG_SAFE_FIX: Ensure this handler never blocks the player via physics collisions.
    // It exists only to receive mouse clicks, so its collider should be a trigger.
    [SerializeField] private bool forceTriggerCollider = true; // DEBUG_SAFE_FIX

    private Collider2D _col;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        if (forceTriggerCollider && _col != null && !_col.isTrigger)
            _col.isTrigger = true;
    }

    void OnMouseDown()
    {
        // PotHUDWidget rimosso - le HUD sono sempre visibili e non c'è più selezione interattiva
        // Se necessario, implementare logica di deselezione qui
    }

    void Update()
    {
        transform.position = new Vector3(followObject.position.x, followObject.position.y, transform.position.z);
    }
}
