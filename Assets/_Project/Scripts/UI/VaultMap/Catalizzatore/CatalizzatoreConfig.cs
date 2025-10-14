using UnityEngine;

namespace _Project
{
    [CreateAssetMenu(menuName = "Catalizzatore/CatalizzatoreConfig", fileName = "CatalizzatoreConfig")]
    public class CatalizzatoreConfig : ScriptableObject
    {
        [field: SerializeField] public float MinInterval { get; private set; }
        [field: SerializeField] public float MaxInterval { get; private set; }
        [field: SerializeField] public float MinDuration { get; private set; }
        [field: SerializeField] public float MaxDuration { get; private set; }
        [field: SerializeField] public float Session { get; private set; }

        [field: SerializeField] public Vector2 FieldSize { get; private set; }
    }
}