using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Wikipedia {
    public class WikipediaUI : MonoBehaviour
    {
      [SerializeField] private WikipediaCategoryTab _sporeTab;
      [SerializeField] private WikipediaCategoryTab _reagentsTab;
      [SerializeField] private WikipediaCategoryTab _plantsTab;
      [SerializeField] private WikipediaCategoryTab _hybridsTab;
      [SerializeField] private WikipediaCategoryTab _mutationsTab;
      [SerializeField] private WikipediaCategoryTab _foodWaterTab;

      [SerializeField] private WikipediaSection _sporeSection;
      [SerializeField] private WikipediaSection _reagentsSection;
      [SerializeField] private WikipediaSection _plantsSection;
      [SerializeField] private WikipediaSection _hybridsSection;
      [SerializeField] private WikipediaSection _mutationsSection;
      [SerializeField] private WikipediaSection _foodWaterSection;

      [SerializeField] private WikipediaDetailsPage _detailsPage;
      
      [SerializeField] private Button _closeButton;

      [SerializeField] private List<WikipediaItemData> _items;
      
      private WikipediaCategoryTab _currentTab;

      private void Awake()
      {
         _closeButton.onClick.AddListener(HandleClose);
         
         _sporeTab.OnClick += HandleSporeTab;
         _reagentsTab.OnClick += HandleReagentsTab;
         _plantsTab.OnClick += HandlePlantsTab;
         _hybridsTab.OnClick += HandleHybridsTab;
         _mutationsTab.OnClick += HandleMutationsTab;
         _foodWaterTab.OnClick += HandleFoodWaterTab;
         
         _sporeSection.Init(_items, this);
         _reagentsSection.Init(_items, this);
         _plantsSection.Init(_items, this);
         _hybridsSection.Init(_items, this);
         _mutationsSection.Init(_items, this);
         _foodWaterSection.Init(_items, this);
      }

      private void OnDestroy()
      {
         _sporeTab.OnClick -= HandleSporeTab;
         _reagentsTab.OnClick -= HandleReagentsTab;
         _plantsTab.OnClick -= HandlePlantsTab;
         _hybridsTab.OnClick -= HandleHybridsTab;
         _mutationsTab.OnClick -= HandleMutationsTab;
         _foodWaterTab.OnClick -= HandleFoodWaterTab;
      }

      private void Start()
      {
         HandlePlantsTab();
      }

      private void HandleClose()
      {
         Hide();
      }

      private void HandleSporeTab() => SelectTab(_sporeTab, _sporeSection);
      private void HandleReagentsTab() => SelectTab(_reagentsTab, _reagentsSection);
      private void HandlePlantsTab() => SelectTab(_plantsTab, _plantsSection);
      private void HandleHybridsTab() => SelectTab(_hybridsTab, _hybridsSection);
      private void HandleMutationsTab() => SelectTab(_mutationsTab, _mutationsSection);
      private void HandleFoodWaterTab() => SelectTab(_foodWaterTab, _foodWaterSection);

      private void SelectTab(WikipediaCategoryTab tab, WikipediaSection section)
      {
         DisableAll();
         section.gameObject.SetActive(true);
         
         _currentTab?.Deselect();
         _currentTab = tab;
         _currentTab.Select();
      }

      public void Show()
      {
         gameObject.SetActive(true);
      }

      public void Hide()
      {
         gameObject.SetActive(false);
      }

      public void ShowDetails(WikipediaItemData config)
      {
         DisableAll();
         _detailsPage.gameObject.SetActive(true);
         _detailsPage.Show(config);
      }
      
      private void DisableAll()
      {
         _detailsPage.gameObject.SetActive(false);
         _sporeSection.gameObject.SetActive(false);
         _reagentsSection.gameObject.SetActive(false);
         _plantsSection.gameObject.SetActive(false);
         _hybridsSection.gameObject.SetActive(false);
         _mutationsSection.gameObject.SetActive(false); 
         _foodWaterSection.gameObject.SetActive(false);
      }
    }
}