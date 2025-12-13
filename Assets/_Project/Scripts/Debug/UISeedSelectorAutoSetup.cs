using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Project;
using Sporae.DevTools;

namespace _Project
{
    /// <summary>
    /// Script runtime per creare automaticamente la UI del selettore semi
    /// </summary>
    public class UISeedSelectorAutoSetup : MonoBehaviour
    {
        [Header("Auto Setup Settings")]
        [SerializeField] private bool createOnStart = true;
        [SerializeField] private bool showDebugLogs = true;
        
        [Header("UI Settings")]
        [SerializeField] private Vector2 panelSize = new Vector2(400, 300);
        [SerializeField] private Color panelBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        [SerializeField] private int maxSeedsPerRow = 3;
        
        private void Start()
        {
            if (createOnStart)
            {
                SetupSeedSelector();
            }
        }
        
        [ContextMenu("Create Seed Selector UI")]
        public void SetupSeedSelector()
        {
            // Cerca se esiste già
            UISeedSelector existing = FindObjectOfType<UISeedSelector>();
            if (existing != null)
            {
                if (showDebugLogs)
                {
                    SporiumLogger.LogDebug(LogCategory.UI, "UISeedSelector già presente nella scena");
                }
                return;
            }
            
            // Trova o crea Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                
                if (showDebugLogs)
                {
                    SporiumLogger.LogInfo(LogCategory.UI, "Creato Canvas per UISeedSelector");
                }
            }
            
            // Crea GameObject principale
            GameObject selectorGO = new GameObject("UISeedSelector");
            selectorGO.transform.SetParent(canvas.transform, false);
            
            UISeedSelector selector = selectorGO.AddComponent<UISeedSelector>();
            
            // Crea Panel principale
            GameObject panelGO = new GameObject("SelectorPanel");
            panelGO.transform.SetParent(selectorGO.transform, false);
            
            RectTransform panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = panelSize;
            panelRect.anchoredPosition = Vector2.zero;
            
            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = panelBackgroundColor;
            
            // Crea Container per pulsanti semi
            GameObject containerGO = new GameObject("SeedButtonContainer");
            containerGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 0f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.offsetMin = new Vector2(10, 50);
            containerRect.offsetMax = new Vector2(-10, -10);
            
            // Aggiungi GridLayoutGroup per organizzare i pulsanti
            GridLayoutGroup gridLayout = containerGO.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(120, 100);
            gridLayout.spacing = new Vector2(10, 10);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = maxSeedsPerRow;
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            
            // Crea Title
            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.sizeDelta = new Vector2(0, 40);
            titleRect.anchoredPosition = new Vector2(0, -5);
            
            TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "Seleziona Seme";
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 20;
            titleText.color = Color.white;
            
            // Crea No Seeds Text
            GameObject noSeedsGO = new GameObject("NoSeedsText");
            noSeedsGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform noSeedsRect = noSeedsGO.AddComponent<RectTransform>();
            noSeedsRect.anchorMin = new Vector2(0.5f, 0.5f);
            noSeedsRect.anchorMax = new Vector2(0.5f, 0.5f);
            noSeedsRect.sizeDelta = new Vector2(300, 50);
            noSeedsRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI noSeedsText = noSeedsGO.AddComponent<TextMeshProUGUI>();
            noSeedsText.text = "Nessun seme disponibile";
            noSeedsText.alignment = TextAlignmentOptions.Center;
            noSeedsText.fontSize = 16;
            noSeedsText.color = Color.yellow;
            noSeedsGO.SetActive(false);
            
            // Crea Close Button
            GameObject closeButtonGO = new GameObject("CloseButton");
            closeButtonGO.transform.SetParent(panelGO.transform, false);
            
            RectTransform closeButtonRect = closeButtonGO.AddComponent<RectTransform>();
            closeButtonRect.anchorMin = new Vector2(1f, 1f);
            closeButtonRect.anchorMax = new Vector2(1f, 1f);
            closeButtonRect.sizeDelta = new Vector2(30, 30);
            closeButtonRect.anchoredPosition = new Vector2(-5, -5);
            
            Image closeButtonImage = closeButtonGO.AddComponent<Image>();
            closeButtonImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            
            Button closeButton = closeButtonGO.AddComponent<Button>();
            closeButton.targetGraphic = closeButtonImage;
            
            // Testo X sul pulsante chiudi
            GameObject closeTextGO = new GameObject("Text");
            closeTextGO.transform.SetParent(closeButtonGO.transform, false);
            
            RectTransform closeTextRect = closeTextGO.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.sizeDelta = Vector2.zero;
            closeTextRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
            closeText.text = "X";
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.fontSize = 18;
            closeText.color = Color.white;
            
            // Usa reflection per assegnare i riferimenti (perché sono SerializeField privati)
            SetPrivateField(selector, "selectorPanel", panelGO);
            SetPrivateField(selector, "seedButtonContainer", containerGO.transform);
            SetPrivateField(selector, "closeButton", closeButton);
            SetPrivateField(selector, "titleText", titleText);
            SetPrivateField(selector, "noSeedsText", noSeedsText);
            
            if (showDebugLogs)
            {
                SporiumLogger.LogInfo(LogCategory.UI, "UISeedSelector creato con successo!");
                SporiumLogger.LogDebug(LogCategory.UI, $"Panel: {panelGO.name}, Container: {containerGO.name}");
            }
        }
        
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, $"Campo '{fieldName}' non trovato in UISeedSelector!");
            }
        }
    }
}

