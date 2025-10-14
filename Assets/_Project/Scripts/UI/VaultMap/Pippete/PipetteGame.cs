using System;
using UnityEngine;

namespace _Project
{
    public class PipetteGame : MonoBehaviour
    {
        [SerializeField] private UILineRenderer _path;
        [SerializeField] private RectTransform _pipette;
        [SerializeField] private float _tolerance;
        [SerializeField] private float _stability ;
        [SerializeField] private float _timer;
        [SerializeField] private PipetteController _pipetteComponent;
        
        private Vector2 _pipetteInitPosition;
        private float _progress; 
        
        private Vector2 _lastPos;
        private bool _isCompleted;
        
        public event Action OnComplete;
        
        public float Timer => _timer;
        public float Stability => _stability;
        public float Progress => _progress;

        private void Start()
        {
            _pipetteInitPosition = _pipette.anchoredPosition;
        }
        
        public void Run()
        {
            _isCompleted = false;
            _pipette.anchoredPosition = _pipetteInitPosition;
        }
        
        private void Update()
        {
            if (_isCompleted)
                return;
            
            _timer -= Time.deltaTime;
            
            var pipettePos = _pipette.anchoredPosition;
            var closest = GetClosestPoint(pipettePos);
            var distance = Vector2.Distance(pipettePos, closest);

            if (distance > _tolerance)
            {
                _path.color = Color.red;
                _stability -= 40f * Time.deltaTime;
            }
            else
            {
                _path.color = Color.green;
                _stability += 2f * Time.deltaTime;
            }

            _stability = Mathf.Clamp(_stability, 0, 100);
            _lastPos = pipettePos;
            
            _progress = GetPathProgress(pipettePos);

            if (_progress >= 0.98f) 
            {
                _isCompleted = true;
                _pipetteComponent.OnPointerUp(null);
                OnComplete?.Invoke();
            }
        }

        private Vector2 GetClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = Vector2.Dot(p - a, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            
            return a + ab * t;
        }

        private Vector2 GetClosestPoint(Vector2 point)
        {
            var minDist = float.MaxValue;
            var closest = Vector2.zero;

            for (var i = 0; i < _path.Points.Length - 1; i++)
            {
                var a = _path.Points[i];
                var b = _path.Points[i + 1];
                var candidate = GetClosestPointOnSegment(point, a, b);

                var d = Vector2.SqrMagnitude(point - candidate);
                if (!(d < minDist)) 
                    continue;
                
                minDist = d;
                closest = candidate;
            }
            
            return closest;
        }

        private float GetPathProgress(Vector2 pos)
        {
            float totalLength = 0,
                  traveled = 0;

            for (var i = 0; i < _path.Points.Length - 1; i++)
            {
                var segLength = Vector2.Distance(_path.Points[i], _path.Points[i + 1]);
                totalLength += segLength;
            }

            for (int i = 0; i < _path.Points.Length - 1; i++)
            {
                Vector2 a = _path.Points[i];
                Vector2 b = _path.Points[i + 1];
                Vector2 closest = GetClosestPointOnSegment(pos, a, b);

                var distToSegment = Vector2.Distance(pos, closest);
                var segmentLength = Vector2.Distance(a, b);

                var t = Vector2.Dot(closest - a, (b - a).normalized) / segmentLength;
                t = Mathf.Clamp01(t);

                if (!(distToSegment < _tolerance * 1.5f)) 
                    continue;
                
                for (var j = 0; j < i; j++)
                    traveled += Vector2.Distance(_path.Points[j], _path.Points[j + 1]);
                traveled += t * segmentLength;
                
                break;
            }

            return Mathf.Clamp01(traveled / totalLength);
        }
    }
}