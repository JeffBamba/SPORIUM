using UnityEngine;
using UnityEngine.UI;

namespace _Project.Wikipedia
{
    public class WikipediaToggle : MonoBehaviour
    {
        [SerializeField] private WikipediaUI _wikipediaUI;
        
        private Button _button;

        private void Start()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(ToggleWikipedia);
        }

        private void ToggleWikipedia()
        {
            if (_wikipediaUI.gameObject.activeSelf)
                _wikipediaUI.Hide();
            else
                _wikipediaUI.Show();   
        }
    }
}