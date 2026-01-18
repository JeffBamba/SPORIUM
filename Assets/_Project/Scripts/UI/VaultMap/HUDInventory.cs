using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.UI.UIToolkit.SeedInventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sporae.DevTools;

namespace _Project
{
    [RequireComponent(typeof(HUDItemContainer))]
    public class HUDInventory : MonoBehaviour
    {
        [SerializeField] private GameObject _inventoryPage;
        
        [SerializeField] private Button _showInventoryButton;
        
        [Header("Leggibilità")]
        [SerializeField] private bool improveReadability = true;
        [SerializeField] private int canvasSortingOrder = 150; // Sopra la HUD della pianta (100)
        
        [Header("Chiusura")]
        [SerializeField] private bool closeOnESC = true;
        [SerializeField] private bool closeOnClickOutside = true;
        [SerializeField] private Image _backgroundBlocker; // Immagine trasparente dietro l'inventory per intercettare click fuori
        
        private GameManager _gameManager;
        private Inventory _inventory;
        private HUDItemContainer _hudItemContainer;
        private Canvas _inventoryCanvas;
        
        public event Action OnClose;
        
        private void Awake()
        {
            _hudItemContainer = GetComponent<HUDItemContainer>();
            // Usa ServiceContainer invece di FindObjectOfType
            _gameManager = ServiceContainer.Instance?.Get<GameManager>();
            if (_gameManager != null)
            {
                _inventory = _gameManager.PlayerInventory;
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "GameManager non disponibile via ServiceContainer. Tentativo late binding...");
                if (ServiceContainer.Instance != null)
                {
                    ServiceContainer.Instance.OnServiceRegistered += OnGameManagerRegistered;
                }
            }
        }
        
        /// <summary>
        /// Late binding per GameManager quando viene registrato
        /// </summary>
        private void OnGameManagerRegistered(object service)
        {
            if (service is GameManager gameManager && _gameManager == null)
            {
                _gameManager = gameManager;
                _inventory = _gameManager.PlayerInventory;
                
                if (_inventory != null)
                {
                    _inventory.OnInventoryChanged += UpdateInventory;
                }
                
                if (ServiceContainer.Instance != null)
                {
                    ServiceContainer.Instance.OnServiceRegistered -= OnGameManagerRegistered;
                }
            }
        }

        private void Start()
        {
            // Pulsante Inventory ora gestito dal Player Status Panel UI Toolkit - nascondi il pulsante vecchio
            if (_showInventoryButton != null)
            {
                _showInventoryButton.gameObject.SetActive(false); // Nascondi completamente il pulsante
            }
            
            if (_inventory != null)
                _inventory.OnInventoryChanged += UpdateInventory;
            
            // Configura background blocker per click fuori
            SetupBackgroundBlocker();
            
            // Configura Canvas e leggibilità
            if (improveReadability)
            {
                SetupCanvasAndReadability();
            }
        }
        
        private void Update()
        {
            // Gestione ESC per chiudere l'inventory
            if (closeOnESC && _inventoryPage != null && _inventoryPage.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Close();
                }
            }
        }

        private void OnDestroy()
        {
            if (_inventory != null)
                _inventory.OnInventoryChanged -= UpdateInventory;
            
            // Cleanup ServiceContainer subscriptions
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered -= OnGameManagerRegistered;
            }
        }

        public void Toggle()
        {
            if (_inventoryPage.activeSelf)
                Close();
            else 
                Show();
        }
        
        private void Close()
        {
            OnClose?.Invoke();
            Hide();
        }

        /// <summary>
        /// Configura il background blocker per intercettare click fuori dall'inventory
        /// </summary>
        private void SetupBackgroundBlocker()
        {
            if (_backgroundBlocker == null && closeOnClickOutside && _inventoryPage != null)
            {
                // Trova il Canvas corretto (quello dell'inventory o creane uno nuovo)
                Canvas targetCanvas = _inventoryPage.GetComponentInParent<Canvas>();
                if (targetCanvas == null)
                {
                    // Cerca il Canvas nel GameObject stesso
                    targetCanvas = _inventoryPage.GetComponent<Canvas>();
                    if (targetCanvas == null)
                    {
                        // Cerca ricorsivamente nei parent
                        Transform parent = _inventoryPage.transform.parent;
                        while (parent != null && targetCanvas == null)
                        {
                            targetCanvas = parent.GetComponent<Canvas>();
                            parent = parent.parent;
                        }
                    }
                }
                
                // Se non trovato, crea un Canvas dedicato per il blocker
                if (targetCanvas == null)
                {
                    GameObject canvasGO = new GameObject("InventoryBlockerCanvas");
                    targetCanvas = canvasGO.AddComponent<Canvas>();
                    targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    targetCanvas.sortingOrder = canvasSortingOrder - 1; // Dietro l'inventory ma sopra il resto
                }
                
                // Crea background blocker come figlio diretto del Canvas
                GameObject blockerGO = new GameObject("InventoryBackgroundBlocker");
                blockerGO.transform.SetParent(targetCanvas.transform, false);
                
                _backgroundBlocker = blockerGO.AddComponent<Image>();
                _backgroundBlocker.color = new Color(0f, 0f, 0f, 0.5f); // Nero semi-trasparente
                _backgroundBlocker.raycastTarget = true;
                
                // Imposta per coprire TUTTO lo schermo
                RectTransform blockerRect = blockerGO.GetComponent<RectTransform>();
                blockerRect.anchorMin = Vector2.zero; // Angolo in basso a sinistra
                blockerRect.anchorMax = Vector2.one;   // Angolo in alto a destra
                blockerRect.offsetMin = Vector2.zero;  // Nessun offset minimo
                blockerRect.offsetMax = Vector2.zero; // Nessun offset massimo
                blockerRect.sizeDelta = Vector2.zero;  // Nessuna dimensione aggiuntiva
                blockerRect.anchoredPosition = Vector2.zero; // Centrato
                
                // Assicurati che il RectTransform copra tutto lo schermo
                blockerRect.localScale = Vector3.one;
                
                // Imposta come primo figlio (dietro l'inventory)
                blockerGO.transform.SetAsFirstSibling();
                
                // Aggiungi listener per click
                Button blockerButton = blockerGO.AddComponent<Button>();
                blockerButton.onClick.AddListener(Close);
                blockerButton.transition = Selectable.Transition.None; // Nessuna transizione visiva
                
                blockerGO.SetActive(false); // Inizia nascosto
                
                SporiumLogger.LogInfo(LogCategory.UI, $"Background blocker creato e configurato per coprire tutto lo schermo (Canvas: {targetCanvas.name}, Sorting Order: {targetCanvas.sortingOrder})");
            }
        }
        
        public void Show()
        {
            _inventoryPage.SetActive(true);
            
            // Mostra background blocker se presente
            if (_backgroundBlocker != null)
            {
                _backgroundBlocker.gameObject.SetActive(true);
            }
            
            // Assicurati che il Canvas sia configurato correttamente quando viene mostrato
            if (improveReadability)
            {
                SetupCanvasAndReadability();
            }
            
            UpdateInventory();
            
            // Migliora leggibilità DOPO aver aggiornato gli item
            if (improveReadability)
            {
                ImproveReadability();
            }
        }
        
        public void Hide()
        {
            _inventoryPage.SetActive(false);
            
            // Nascondi background blocker se presente
            if (_backgroundBlocker != null)
            {
                _backgroundBlocker.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Configura il Canvas dell'inventario per renderlo sopra la HUD della pianta
        /// </summary>
        private void SetupCanvasAndReadability()
        {
            if (_inventoryPage == null) return;
            
            // Trova il Canvas dell'inventario
            _inventoryCanvas = _inventoryPage.GetComponentInParent<Canvas>();
            if (_inventoryCanvas == null)
            {
                // Cerca nel GameObject stesso o nei parent
                _inventoryCanvas = _inventoryPage.GetComponent<Canvas>();
                if (_inventoryCanvas == null)
                {
                    Transform parent = _inventoryPage.transform.parent;
                    while (parent != null && _inventoryCanvas == null)
                    {
                        _inventoryCanvas = parent.GetComponent<Canvas>();
                        parent = parent.parent;
                    }
                }
            }
            
            // Se trovato, imposta sorting order più alto
            if (_inventoryCanvas != null)
            {
                _inventoryCanvas.sortingOrder = canvasSortingOrder;
                
                // Assicurati che il Canvas sia Screen Space Overlay o Camera per il sorting order
                if (_inventoryCanvas.renderMode != RenderMode.ScreenSpaceOverlay && 
                    _inventoryCanvas.renderMode != RenderMode.ScreenSpaceCamera)
                {
                    SporiumLogger.LogWarning(LogCategory.UI, $"Canvas render mode è {_inventoryCanvas.renderMode}. Il sorting order funziona solo con ScreenSpaceOverlay o ScreenSpaceCamera!");
                }
                
                SporiumLogger.LogInfo(LogCategory.UI, $"Canvas sorting order impostato a {canvasSortingOrder} (render mode: {_inventoryCanvas.renderMode})");
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "Canvas non trovato per l'inventario. Assicurati che _inventoryPage sia dentro un Canvas.");
            }
        }
        
        /// <summary>
        /// Migliora la leggibilità dell'inventario aumentando contrasto, font size, etc.
        /// </summary>
        private void ImproveReadability()
        {
            if (_hudItemContainer == null) return;
            
            // Migliora solo gli item attivi dell'inventario
            foreach (var item in _hudItemContainer.Items)
            {
                if (item == null || !item.gameObject.activeSelf) continue;
                
                // Assicurati che l'item sia visibile e sopra altri elementi
                Canvas itemCanvas = item.GetComponentInParent<Canvas>();
                if (itemCanvas != null && itemCanvas.sortingOrder < canvasSortingOrder)
                {
                    itemCanvas.sortingOrder = canvasSortingOrder;
                    SporiumLogger.LogDebug(LogCategory.UI, $"Canvas item sorting order aggiornato a {canvasSortingOrder}");
                }
                
                // Migliora i testi - trova TUTTI i TextMeshProUGUI nell'item
                TextMeshProUGUI[] allLabels = item.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (TextMeshProUGUI label in allLabels)
                {
                    if (label == null) continue;
                    
                    // Assicura che il label sia attivo e visibile
                    label.gameObject.SetActive(true);
                    label.enabled = true;
                    
                    // Aumenta font size se troppo piccolo
                    if (label.fontSize < 16)
                    {
                        label.fontSize = 16;
                    }
                    
                    // Colore bianco brillante per massimo contrasto
                    label.color = new Color(1f, 1f, 1f, 1f);
                    
                    // Applica outline spesso per massima leggibilità
                    label.outlineWidth = 0.4f;
                    label.outlineColor = new Color(0f, 0f, 0f, 1f);
                    
                    // Forza aggiornamento rendering
                    label.SetAllDirty();
                    label.ForceMeshUpdate();
                }
                
                // Migliora il background dell'item - DEVE essere scuro per contrasto con testo bianco
                Image itemImage = item.GetComponent<Image>();
                if (itemImage != null)
                {
                    itemImage.enabled = true;
                    // Sfondo molto scuro e opaco per massimo contrasto con testo bianco
                    Color bgColor = new Color(0.1f, 0.1f, 0.1f, 0.98f); // Molto scuro e opaco
                    itemImage.color = bgColor;
                }
            }
            
            // Migliora il background del pannello inventario se presente
            Image panelImage = _inventoryPage.GetComponent<Image>();
            if (panelImage != null)
            {
                Color panelColor = panelImage.color;
                panelColor.a = 0.95f; // Più opaco
                // Sfondo più scuro per maggiore contrasto
                if (panelColor.r > 0.3f || panelColor.g > 0.3f || panelColor.b > 0.3f)
                {
                    panelColor.r = Mathf.Min(panelColor.r * 0.7f, 0.25f);
                    panelColor.g = Mathf.Min(panelColor.g * 0.7f, 0.25f);
                    panelColor.b = Mathf.Min(panelColor.b * 0.7f, 0.25f);
                }
                panelImage.color = panelColor;
            }
        } 

        private void UpdateInventory()
        {
            if (_inventory == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "Inventory è null! Impossibile aggiornare inventario.");
                return;
            }
            
            if (_hudItemContainer == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "HUDItemContainer è null! Impossibile aggiornare inventario.");
                return;
            }
            
            _hudItemContainer.DisableAllSlots();

            int index = 0;
            int uniqueItems = _inventory.UniqueItems;
            int maxCapacity = _hudItemContainer.Capacity;
            
            SporiumLogger.LogDebug(LogCategory.UI, $"Aggiornamento inventario: {uniqueItems} item unici trovati, capacità container: {maxCapacity}");
            
            if (maxCapacity == 0)
            {
                SporiumLogger.LogError(LogCategory.UI, "HUDItemContainer non ha slot disponibili! Assicurati che gli item siano assegnati nell'Inspector.");
                return;
            }
            
            for (var i = 0; i < uniqueItems && index < maxCapacity; i++)
            {
                var slot = _inventory.Items.ElementAt(i);

                if (slot.Items.Count > 0 && slot.Items.ElementAt(0).ItemConfig.CanStack)
                {
                    if (index < maxCapacity)
                    {
                        // Converti TypeId in nome leggibile se è un seed
                        string displayName = SeedInventoryMenu.GetSeedDisplayName(slot.TypeId);
                        _hudItemContainer.SetItemData(index++, displayName, slot.Quantity);
                        SporiumLogger.LogDebug(LogCategory.UI, $"Item stackabile aggiunto all'indice {index-1}: {displayName} x{slot.Quantity}");
                    }
                    else
                    {
                        SporiumLogger.LogWarning(LogCategory.UI, $"Capacità inventario raggiunta! Saltato item: {slot.TypeId}");
                        break;
                    }
                }
                else 
                {
                    foreach (var item in slot.Items)
                    {
                        if (index < maxCapacity)
                        {
                            // Converti TypeId in nome leggibile se è un seed
                            string displayName = SeedInventoryMenu.GetSeedDisplayName(item.TypeId);
                            _hudItemContainer.SetItemData(index++, displayName, -1);
                            SporiumLogger.LogDebug(LogCategory.UI, $"Item non-stackabile aggiunto all'indice {index-1}: {displayName}");
                        }
                        else
                        {
                            SporiumLogger.LogWarning(LogCategory.UI, $"Capacità inventario raggiunta! Saltato item: {item.TypeId}");
                            break;
                        }
                    }
                    
                    if (index >= maxCapacity) break;
                }
            }
            
            SporiumLogger.LogInfo(LogCategory.UI, $"Inventario aggiornato: {index} slot popolati su {maxCapacity} disponibili");
        }
    }
}