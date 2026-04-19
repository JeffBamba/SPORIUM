using UnityEngine;

namespace _Project.Player
{
    /// <summary>
    /// Varianti outfit (tinta sprite) per Task Armadio — stesso binario demo / Nuova partita.
    /// Assegna tinte in Inspector o usa i default a runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerOutfitController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Color[] _outfitTints;

        private int _index;

        public int CurrentIndex => _index;
        public int OutfitCount => _outfitTints != null ? _outfitTints.Length : 0;

        private void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (_outfitTints == null || _outfitTints.Length == 0)
            {
                _outfitTints = new[]
                {
                    Color.white,
                    new Color(0.75f, 1f, 1f, 1f),
                    new Color(1f, 0.92f, 0.65f, 1f)
                };
            }

            Apply(_index);
        }

        public void Cycle(int delta)
        {
            if (_outfitTints == null || _outfitTints.Length == 0)
                return;
            int n = _outfitTints.Length;
            _index = ((_index + delta) % n + n) % n;
            Apply(_index);
        }

        public void Apply(int index)
        {
            if (_spriteRenderer == null || _outfitTints == null || _outfitTints.Length == 0)
                return;
            _index = Mathf.Clamp(index, 0, _outfitTints.Length - 1);
            _spriteRenderer.color = _outfitTints[_index];
        }

        public string GetCurrentLabel() => $"Outfit {_index + 1}/{_outfitTints.Length}";
    }
}
