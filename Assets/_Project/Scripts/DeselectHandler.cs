using UnityEngine;

public class DeselectHandler : MonoBehaviour
{
    [SerializeField] private Transform followObject;

    void OnMouseDown()
    {
        var widget = FindObjectOfType<PotHUDWidget>();
        if (widget != null)
        {
            widget.DeselectPot();
        }
    }

    void Update()
    {
        transform.position = new Vector3(followObject.position.x, followObject.position.y, transform.position.z);
    }
}
