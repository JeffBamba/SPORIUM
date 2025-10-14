using UnityEngine;
using UnityEngine.UI;

namespace _Project
{
    public class UILineRenderer : Graphic
    {
        [SerializeField] private float _thickness;
        [SerializeField] private Vector2[] _points;
        
        public float Thickness
        {
            get => _thickness;
            set
            {
                _thickness = value;
                SetVerticesDirty();
            }
        }

        public Vector2[] Points
        {
            get => _points;
            set { 
                _points = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_points == null || _points.Length < 2)
                return;

            for (var i = 0; i < _points.Length - 1; i++)
            {
                var start = _points[i];
                var end = _points[i + 1];
                
                DrawLine(vh, start, end, Thickness);
            }
        }

        private void DrawLine(VertexHelper vh, Vector2 start, Vector2 end, float width)
        {
            var dir = (end - start).normalized;
            var normal = new Vector2(-dir.y, dir.x) * (width / 2f);

            var v0 = UIVertex.simpleVert; v0.color = color; v0.position = start - normal;
            var v1 = UIVertex.simpleVert; v1.color = color; v1.position = start + normal;
            var v2 = UIVertex.simpleVert; v2.color = color; v2.position = end + normal;
            var v3 = UIVertex.simpleVert; v3.color = color; v3.position = end - normal;
            
            var index = vh.currentVertCount;
            vh.AddVert(v0); vh.AddVert(v1); vh.AddVert(v2); vh.AddVert(v3);
            vh.AddTriangle(index + 0, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index + 0);
        }
    }
}