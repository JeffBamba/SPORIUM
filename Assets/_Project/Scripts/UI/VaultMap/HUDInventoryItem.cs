using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sporae.DevTools;

namespace _Project
{
    public class HUDInventoryItem : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _scoreLabel;

        [SerializeField] private Color _normalColor = new Color(0.15f, 0.15f, 0.15f, 0.95f); // Sfondo scuro opaco di default
        [SerializeField] private Color _selectedColor = new Color(0.3f, 0.6f, 0.3f, 0.95f); // Verde più chiaro quando selezionato

        private string _itemName;
        
        public string ItemName => _itemName;
        
        private Image _image;
        
        public event Action<HUDInventoryItem> OnClick;

        private void Awake()
        {
            _image = GetComponent<Image>();
            
            // Migliora leggibilità all'avvio
            ImproveReadability();
        }
        
        /// <summary>
        /// Migliora la leggibilità del testo e del background
        /// </summary>
        private void ImproveReadability()
        {
            // Migliora il testo del nome
            if (_nameLabel != null)
            {
                // Assicura font size minimo
                if (_nameLabel.fontSize < 16)
                {
                    _nameLabel.fontSize = 16;
                }
                
                // Colore bianco brillante per massimo contrasto
                _nameLabel.color = new Color(1f, 1f, 1f, 1f);
                
                // Aggiungi outline spesso per massimo contrasto (solo se materiale disponibile)
                if (_nameLabel.fontMaterial != null)
                {
                    try
                    {
                        _nameLabel.outlineWidth = 0.4f;
                        _nameLabel.outlineColor = new Color(0f, 0f, 0f, 1f);
                    }
                    catch (System.Exception ex)
                    {
                        SporiumLogger.LogWarning(LogCategory.UI, $"Impossibile impostare outline per _nameLabel in ImproveReadability: {ex.Message}");
                    }
                }
                
                // Assicura che il testo sia sempre visibile
                _nameLabel.enabled = true;
                _nameLabel.gameObject.SetActive(true);
            }
            
            // Migliora il testo della quantità
            if (_scoreLabel != null)
            {
                if (_scoreLabel.fontSize < 14)
                {
                    _scoreLabel.fontSize = 14;
                }
                
                // Colore giallo/arancione brillante per le quantità
                _scoreLabel.color = new Color(1f, 0.95f, 0.7f, 1f);
                
                // Outline spesso per quantità (solo se materiale disponibile)
                if (_scoreLabel.fontMaterial != null)
                {
                    try
                    {
                        _scoreLabel.outlineWidth = 0.4f;
                        _scoreLabel.outlineColor = new Color(0f, 0f, 0f, 1f);
                    }
                    catch (System.Exception ex)
                    {
                        SporiumLogger.LogWarning(LogCategory.UI, $"Impossibile impostare outline per _scoreLabel in ImproveReadability: {ex.Message}");
                    }
                }
                
                // Assicura che il testo sia sempre visibile
                _scoreLabel.enabled = true;
                _scoreLabel.gameObject.SetActive(true);
            }
            
            // Migliora il background - DEVE essere scuro per contrasto con testo bianco
            if (_image != null)
            {
                // Sfondo scuro opaco per massimo contrasto con testo bianco
                _normalColor = new Color(0.1f, 0.1f, 0.1f, 0.98f); // Molto scuro e opaco
                _image.color = _normalColor;
                _image.enabled = true;
            }
        }
        
        public void SetItem(string itemName, int quantity)
        {
            _itemName = itemName;
            
            // Assicurati che il GameObject sia attivo e visibile
            gameObject.SetActive(true);
            
            // Assicurati che CanvasGroup sia visibile se presente
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            
            // Assicurati che l'Image sia visibile
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }
            
            if (_image != null)
            {
                _image.enabled = true;
                Color imgColor = _image.color;
                imgColor.a = 1f; // Assicura opacità completa
                _image.color = imgColor;
            }
            
            // Auto-trova i label se non assegnati manualmente
            if (_nameLabel == null)
            {
                _nameLabel = GetComponentInChildren<TextMeshProUGUI>();
                // Se ci sono più TextMeshProUGUI, cerca quello con il nome più appropriato
                TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var text in allTexts)
                {
                    if (text != null && (text.name.ToLower().Contains("name") || text.name.ToLower().Contains("item")))
                    {
                        _nameLabel = text;
                        break;
                    }
                }
                // Se ancora null, usa il primo trovato
                if (_nameLabel == null && allTexts.Length > 0)
                {
                    _nameLabel = allTexts[0];
                }
            }
            
            if (_nameLabel != null)
            {
                _nameLabel.gameObject.SetActive(true);
                _nameLabel.text = itemName ?? "";
                _nameLabel.enabled = true;
                // Colore bianco brillante con outline nero spesso per massimo contrasto
                _nameLabel.color = new Color(1f, 1f, 1f, 1f);
                
                // Verifica che il materiale sia disponibile prima di impostare outline
                if (_nameLabel.fontMaterial != null)
                {
                    try
                    {
                        _nameLabel.outlineWidth = 0.4f;
                        _nameLabel.outlineColor = new Color(0f, 0f, 0f, 1f);
                    }
                    catch (System.Exception ex)
                    {
                        SporiumLogger.LogWarning(LogCategory.UI, $"Impossibile impostare outline per _nameLabel: {ex.Message}");
                    }
                }
                
                // Forza aggiornamento del rendering
                _nameLabel.SetAllDirty();
                _nameLabel.ForceMeshUpdate();
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"⚠️ _nameLabel è null per item: {itemName}. Assicurati che il TextMeshProUGUI sia assegnato nell'Inspector.");
            }
            
            // Auto-trova score label se non assegnato
            if (_scoreLabel == null)
            {
                TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var text in allTexts)
                {
                    if (text != null && text != _nameLabel && (text.name.ToLower().Contains("score") || text.name.ToLower().Contains("quantity") || text.name.ToLower().Contains("count")))
                    {
                        _scoreLabel = text;
                        break;
                    }
                }
                // Se ancora null e ci sono 2+ testi, usa il secondo
                if (_scoreLabel == null && allTexts.Length > 1)
                {
                    _scoreLabel = allTexts[1];
                }
            }
            
            if (_scoreLabel != null)
            {
                _scoreLabel.gameObject.SetActive(true);
                _scoreLabel.text = quantity == -1 ? "" : quantity.ToString();
                _scoreLabel.enabled = true;
                // Colore giallo brillante con outline nero spesso
                _scoreLabel.color = new Color(1f, 0.95f, 0.7f, 1f);
                
                // Verifica che il materiale sia disponibile prima di impostare outline
                if (_scoreLabel.fontMaterial != null)
                {
                    try
                    {
                        _scoreLabel.outlineWidth = 0.4f;
                        _scoreLabel.outlineColor = new Color(0f, 0f, 0f, 1f);
                    }
                    catch (System.Exception ex)
                    {
                        SporiumLogger.LogWarning(LogCategory.UI, $"Impossibile impostare outline per _scoreLabel: {ex.Message}");
                    }
                }
                
                // Forza aggiornamento del rendering
                _scoreLabel.SetAllDirty();
                _scoreLabel.ForceMeshUpdate();
            }
            
            // Assicurati che la scala sia corretta
            transform.localScale = Vector3.one;
            
            // Migliora leggibilità dopo aver impostato i valori
            ImproveReadability();
            
            // Forza aggiornamento del Canvas per assicurare rendering immediato
            Canvas.ForceUpdateCanvases();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke(this);
        }

        public void Deselect()
        {
            _image.color = _normalColor;    
        }

        public void Select()
        {
            _image.color = _selectedColor;
        }
    }
}