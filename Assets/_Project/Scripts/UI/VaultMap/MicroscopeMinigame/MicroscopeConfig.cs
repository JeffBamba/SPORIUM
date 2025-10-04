using UnityEngine;

namespace _Project
{

    [CreateAssetMenu(menuName = "Microscope/MicroscopeConfig", fileName = "MicroscopeConfig")]
    public class MicroscopeConfig : ScriptableObject
    { 
        [field: SerializeField] public float AutoDriftSpeed { get; set; }
        [field: SerializeField] public float Sensitivity { get; set; }
        [field: SerializeField] public float RightMultiplier { get; set; }
        [field: SerializeField] public bool WrapAngle { get; set; }
    }
}