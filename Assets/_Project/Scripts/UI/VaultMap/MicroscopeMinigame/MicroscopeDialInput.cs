using UnityEngine;

namespace _Project
{
    public class MicroscopeDialInput : MonoBehaviour
    {
        [SerializeField] private MicroscopeConfig _config;
        
        [field: SerializeField] public float CurrentAngle { get; set; }
        
        private void Update()
        {
            var horiz = Input.GetAxisRaw("Horizontal"); 
            if (horiz < 0)
                horiz *= _config.RightMultiplier;
            
            var delta = -horiz * _config.Sensitivity * Time.deltaTime;

            delta += _config.AutoDriftSpeed * Time.deltaTime;

            CurrentAngle += delta;
            CurrentAngle = _config.WrapAngle ?
                Mathf.Repeat(CurrentAngle, 360f) :
                Mathf.Clamp(CurrentAngle, 0f, 360f);
        }

        public float AngleDeltaTo(float target)
        {
            var diff = Mathf.DeltaAngle(CurrentAngle, target);
            return Mathf.Abs(diff);
        }
    }
}