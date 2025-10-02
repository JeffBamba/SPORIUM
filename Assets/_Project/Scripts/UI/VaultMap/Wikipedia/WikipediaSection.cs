using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Project.Wikipedia
{
    public class WikipediaSection : MonoBehaviour
    {
        [SerializeField] private WikipediaItemData.Sections _section;
        [SerializeField] private WikipediaItemUI _itemUIPrefab;
        
        private readonly List<WikipediaItemUI> _itemUIs = new();
        
        public void Init(List<WikipediaItemData> items, WikipediaUI ui)
        {
            foreach (var item in items.Where(item => item.Section == _section))
            {
                var itemUI = Instantiate(_itemUIPrefab, transform);
                itemUI.SetData(item);
                itemUI.OnSeeDetails += () =>
                {
                    ui.ShowDetails(item);
                };
                
                _itemUIs.Add(itemUI);
            }
        }

        private void OnDestroy()
        {
            foreach (var item in _itemUIs)
                item.OnSeeDetails = null;
        }
    }
}