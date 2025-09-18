using TMPro;
using UnityEngine;

namespace _Project
{
    public class HUDInventoryItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _scoreLabel;

        public void SetItem(string itemName, int score)
        {
            _nameLabel.text = itemName;
            _scoreLabel.text = score.ToString();
        }
    }
}