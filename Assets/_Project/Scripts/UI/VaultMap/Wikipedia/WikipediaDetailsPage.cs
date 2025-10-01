using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Wikipedia
{
    public class WikipediaDetailsPage : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private TextMeshProUGUI _descriptionLabel;
        [SerializeField] private Image _image;
        
        public void Show(WikipediaItemData item)
        {
            _titleLabel.text = item.Title;
            _descriptionLabel.text = item.Description;
            _image.sprite = item.Sprite;
        }
    }
}