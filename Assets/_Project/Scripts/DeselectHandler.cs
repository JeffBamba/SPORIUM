using UnityEngine;

public class DeselectHandler : MonoBehaviour
{
    [SerializeField] private Transform followObject;

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
