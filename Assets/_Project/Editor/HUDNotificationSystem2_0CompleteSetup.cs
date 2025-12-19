#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

namespace _Project.UI.HUDNotifications2_0.Editor
{
    /// <summary>
    /// Script editor per creare e configurare automaticamente TUTTO il sistema HUD Notifications 2.0
    /// Crea: root, manager, pool, header completo, container e collega tutti i riferimenti
    /// </summary>
    public static class HUDNotificationSystem2_0CompleteSetup
    {
        [MenuItem("Tools/HUD Notifications 2.0/Complete System Setup (Create Everything)")]
        public static void CreateCompleteSystem()
        {
            // Verifica se esiste già
            HUDNotificationFeedManager2_0 existingManager = Object.FindObjectOfType<HUDNotificationFeedManager2_0>();
            if (existingManager != null)
            {
                if (!EditorUtility.DisplayDialog("Sistema già esistente", 
                    "È stato trovato un HUDNotificationFeedManager2_0 esistente.\n\n" +
                    "Vuoi ricreare tutto il sistema da zero?\n\n" +
                    "⚠️ Questo eliminerà il sistema esistente!", 
                    "Sì, Ricrea", "Annulla"))
                {
                    return;
                }
                
                // Elimina il sistema esistente
                GameObject existingRoot = existingManager.gameObject;
                Object.DestroyImmediate(existingRoot);
            }
            
            Undo.SetCurrentGroupName("Create HUD Notifications 2.0 Complete System");
            int undoGroup = Undo.GetCurrentGroup();
            
            try
            {
                // 1. Trova o crea Canvas
                Canvas canvas = FindOrCreateCanvas();
                if (canvas == null)
                {
                    EditorUtility.DisplayDialog("Errore", "Impossibile trovare o creare il Canvas!", "OK");
                    return;
                }
                
                // 2. Carica config
                HUDNotificationConfig2_0 config = Resources.Load<HUDNotificationConfig2_0>("Configs/HUDNotificationConfig2.0");
                if (config == null)
                {
                    EditorUtility.DisplayDialog("Avviso", 
                        "Config non trovata in Resources/Configs/HUDNotificationConfig2.0\n\n" +
                        "Userò valori di default. Crea la config per usare valori personalizzati.", 
                        "OK");
                }
                
                // 3. Crea root GameObject
                GameObject rootGO = CreateRootGameObject(canvas.transform, config);
                
                // 4. Crea Manager
                HUDNotificationFeedManager2_0 manager = CreateManager(rootGO);
                
                // 5. Crea Pool
                HUDNotificationPool2_0 pool = CreatePool(rootGO);
                
                // 6. Crea Header completo
                HUDNotificationHeader2_0 header = CreateHeader(rootGO, config);
                
                // 7. Crea NotificationContainer
                RectTransform notificationContainer = CreateNotificationContainer(rootGO, config);
                
                // 8. Collega tutti i riferimenti
                ConnectAllReferences(manager, pool, header, notificationContainer, rootGO);
                
                // 9. Salva scena
                if (!Application.isPlaying)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                }
                
                Undo.CollapseUndoOperations(undoGroup);
                
                EditorUtility.DisplayDialog("Successo!", 
                    "Sistema HUD Notifications 2.0 creato con successo!\n\n" +
                    "Creato:\n" +
                    "• Root GameObject\n" +
                    "• Manager con config\n" +
                    "• Pool (richiede prefab)\n" +
                    "• Header completo con tutti i componenti\n" +
                    "• NotificationContainer\n" +
                    "• Tutti i riferimenti collegati\n\n" +
                    "⚠️ Ricorda di assegnare:\n" +
                    "• Prefab HUDNotificationItem2.0 al Pool\n" +
                    "• Sprites e Font alla Config", 
                    "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Errore", 
                    $"Errore durante la creazione:\n\n{e.Message}\n\nControlla la Console per dettagli.", 
                    "OK");
                Debug.LogError($"[HUDNotificationSystem2_0CompleteSetup] Errore: {e}\n{e.StackTrace}");
            }
        }
        
        private static Canvas FindOrCreateCanvas()
        {
            // Cerca Canvas nella scena
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            
            if (canvas != null)
                return canvas;
            
            // Crea Canvas se non esiste
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Aggiungi EventSystem se non esiste
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            return canvas;
        }
        
        private static GameObject CreateRootGameObject(Transform parent, HUDNotificationConfig2_0 config)
        {
            GameObject rootGO = new GameObject("HUDNotificationSystem2.0");
            rootGO.transform.SetParent(parent, false);
            
            RectTransform rootRect = rootGO.AddComponent<RectTransform>();
            float width = config != null ? config.ContainerWidth : 306f;
            float topOffset = config != null ? config.ContainerTopOffset : 96f;
            float rightOffset = config != null ? config.ContainerRightOffset : 24f;
            
            rootRect.anchorMin = new Vector2(1f, 1f); // Top-right
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.anchoredPosition = new Vector2(-rightOffset, -topOffset);
            rootRect.sizeDelta = new Vector2(width, 0f);
            
            Undo.RegisterCreatedObjectUndo(rootGO, "Create Root");
            return rootGO;
        }
        
        private static HUDNotificationFeedManager2_0 CreateManager(GameObject parent)
        {
            HUDNotificationFeedManager2_0 manager = parent.AddComponent<HUDNotificationFeedManager2_0>();
            Undo.RegisterCreatedObjectUndo(manager, "Create Manager");
            return manager;
        }
        
        private static HUDNotificationPool2_0 CreatePool(GameObject parent)
        {
            HUDNotificationPool2_0 pool = parent.AddComponent<HUDNotificationPool2_0>();
            Undo.RegisterCreatedObjectUndo(pool, "Create Pool");
            return pool;
        }
        
        private static HUDNotificationHeader2_0 CreateHeader(GameObject parent, HUDNotificationConfig2_0 config)
        {
            GameObject headerGO = new GameObject("Header");
            headerGO.transform.SetParent(parent.transform, false);
            
            RectTransform headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 50f); // Height temporaneo
            
            HUDNotificationHeader2_0 headerComponent = headerGO.AddComponent<HUDNotificationHeader2_0>();
            SetupHeaderComplete(headerGO, config, headerComponent);
            
            Undo.RegisterCreatedObjectUndo(headerGO, "Create Header");
            return headerComponent;
        }
        
        private static void SetupHeaderComplete(GameObject headerGO, HUDNotificationConfig2_0 config, HUDNotificationHeader2_0 headerComponent)
        {
            // Setup Background
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(headerGO.transform, false);
            bgGO.transform.SetAsFirstSibling();
            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = config != null ? config.BackgroundColor : new Color(0.11f, 0.16f, 0.16f, 0.9f);
            bgImage.raycastTarget = true;
            
            // Setup Border
            GameObject borderGO = new GameObject("Border");
            borderGO.transform.SetParent(headerGO.transform, false);
            borderGO.transform.SetSiblingIndex(1);
            RectTransform borderRect = borderGO.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            Image borderImage = borderGO.AddComponent<Image>();
            if (config != null && config.BorderSprite != null)
            {
                borderImage.sprite = config.BorderSprite;
                borderImage.type = Image.Type.Sliced;
            }
            borderImage.color = config != null ? config.ColorIdle : new Color32(93, 182, 227, 255);
            
            // Setup Content (HorizontalLayoutGroup)
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(headerGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            float padding = config != null ? config.HeaderPadding : 8f;
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(padding, padding);
            contentRect.offsetMax = new Vector2(-padding, -padding);
            HorizontalLayoutGroup layoutGroup = contentGO.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 8f;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            
            // Setup InfoIcon
            GameObject infoIconGO = new GameObject("InfoIcon");
            infoIconGO.transform.SetParent(contentGO.transform, false);
            infoIconGO.transform.SetAsFirstSibling();
            Image infoIconImage = infoIconGO.AddComponent<Image>();
            if (config != null && config.InfoIcon != null)
                infoIconImage.sprite = config.InfoIcon;
            infoIconImage.color = config != null ? config.ColorIdle : new Color32(93, 182, 227, 255);
            LayoutElement infoIconLayout = infoIconGO.AddComponent<LayoutElement>();
            float iconSize = config != null ? config.HeaderIconSize : 14f;
            infoIconLayout.preferredWidth = iconSize;
            infoIconLayout.preferredHeight = iconSize;
            infoIconLayout.flexibleWidth = 0;
            infoIconLayout.flexibleHeight = 0;
            
            // Setup HeaderText
            GameObject headerTextGO = new GameObject("HeaderText");
            headerTextGO.transform.SetParent(contentGO.transform, false);
            TextMeshProUGUI headerText = headerTextGO.AddComponent<TextMeshProUGUI>();
            headerText.text = "NOTIFICATIONS";
            headerText.font = config != null ? config.MonospacedFont : null;
            headerText.fontSize = config != null ? config.HeaderFontSize : 10f;
            headerText.fontStyle = FontStyles.UpperCase;
            headerText.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
            headerText.color = config != null ? config.ColorIdle : new Color32(93, 182, 227, 255);
            LayoutElement headerTextLayout = headerTextGO.AddComponent<LayoutElement>();
            headerTextLayout.flexibleWidth = 1f;
            headerTextLayout.flexibleHeight = 0;
            
            // Setup BadgeContainer
            GameObject badgeContainerGO = new GameObject("BadgeContainer");
            badgeContainerGO.transform.SetParent(contentGO.transform, false);
            Vector2 badgePadding = config != null ? config.HeaderBadgePadding : new Vector2(6f, 2f);
            float badgeFontSize = config != null ? config.HeaderBadgeFontSize : 10f;
            LayoutElement badgeContainerLayout = badgeContainerGO.AddComponent<LayoutElement>();
            badgeContainerLayout.preferredWidth = badgePadding.x * 2f + badgeFontSize * 2f;
            badgeContainerLayout.preferredHeight = badgePadding.y * 2f + badgeFontSize;
            badgeContainerLayout.flexibleWidth = 0;
            badgeContainerLayout.flexibleHeight = 0;
            
            // Setup BadgeText
            GameObject badgeTextGO = new GameObject("BadgeText");
            badgeTextGO.transform.SetParent(badgeContainerGO.transform, false);
            RectTransform badgeTextRect = badgeTextGO.AddComponent<RectTransform>();
            badgeTextRect.anchorMin = Vector2.zero;
            badgeTextRect.anchorMax = Vector2.one;
            badgeTextRect.offsetMin = Vector2.zero;
            badgeTextRect.offsetMax = Vector2.zero;
            TextMeshProUGUI badgeText = badgeTextGO.AddComponent<TextMeshProUGUI>();
            badgeText.text = "0";
            badgeText.font = config != null ? config.MonospacedFont : null;
            badgeText.fontSize = badgeFontSize;
            badgeText.alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
            badgeText.color = Color.white;
            
            // Setup ChevronIcon
            GameObject chevronGO = new GameObject("ChevronIcon");
            chevronGO.transform.SetParent(contentGO.transform, false);
            Image chevronImage = chevronGO.AddComponent<Image>();
            if (config != null && config.ChevronIcon != null)
                chevronImage.sprite = config.ChevronIcon;
            chevronImage.color = Color.white;
            LayoutElement chevronLayout = chevronGO.AddComponent<LayoutElement>();
            float chevronSize = config != null ? config.HeaderChevronSize : 16f;
            chevronLayout.preferredWidth = chevronSize;
            chevronLayout.preferredHeight = chevronSize;
            chevronLayout.flexibleWidth = 0;
            chevronLayout.flexibleHeight = 0;
            
            // Setup ToggleButton
            GameObject toggleButtonGO = new GameObject("ToggleButton");
            toggleButtonGO.transform.SetParent(headerGO.transform, false);
            toggleButtonGO.transform.SetAsLastSibling();
            RectTransform toggleButtonRect = toggleButtonGO.AddComponent<RectTransform>();
            toggleButtonRect.anchorMin = Vector2.zero;
            toggleButtonRect.anchorMax = Vector2.one;
            toggleButtonRect.offsetMin = Vector2.zero;
            toggleButtonRect.offsetMax = Vector2.zero;
            Button toggleButton = toggleButtonGO.AddComponent<Button>();
            Image toggleButtonImage = toggleButtonGO.AddComponent<Image>();
            toggleButtonImage.color = new Color(1f, 1f, 1f, 0f); // Trasparente (solo per intercettare click)
            
            // Collega riferimenti header usando SerializedObject
            SerializedObject headerSerialized = new SerializedObject(headerComponent);
            headerSerialized.FindProperty("_toggleButton").objectReferenceValue = toggleButton;
            headerSerialized.FindProperty("_headerBackground").objectReferenceValue = bgImage;
            headerSerialized.FindProperty("_borderImage").objectReferenceValue = borderImage;
            headerSerialized.FindProperty("_infoIcon").objectReferenceValue = infoIconImage;
            headerSerialized.FindProperty("_headerText").objectReferenceValue = headerText;
            headerSerialized.FindProperty("_badgeContainer").objectReferenceValue = badgeContainerGO;
            headerSerialized.FindProperty("_badgeText").objectReferenceValue = badgeText;
            headerSerialized.FindProperty("_chevronIcon").objectReferenceValue = chevronImage;
            headerSerialized.FindProperty("_chevronTransform").objectReferenceValue = chevronGO.GetComponent<RectTransform>();
            headerSerialized.ApplyModifiedProperties();
        }
        
        private static RectTransform CreateNotificationContainer(GameObject parent, HUDNotificationConfig2_0 config)
        {
            GameObject containerGO = new GameObject("NotificationContainer");
            containerGO.transform.SetParent(parent.transform, false);
            
            RectTransform containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 1f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.pivot = new Vector2(0.5f, 1f);
            containerRect.sizeDelta = new Vector2(0f, 0f);
            
            // ContentSizeFitter
            ContentSizeFitter sizeFitter = containerGO.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // VerticalLayoutGroup
            VerticalLayoutGroup layoutGroup = containerGO.AddComponent<VerticalLayoutGroup>();
            float gap = config != null ? config.ToastGap : 6f;
            layoutGroup.spacing = gap;
            layoutGroup.childAlignment = TextAnchor.UpperRight;
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            
            // Inizialmente nascosto (header chiuso di default)
            containerGO.SetActive(false);
            
            Undo.RegisterCreatedObjectUndo(containerGO, "Create NotificationContainer");
            return containerRect;
        }
        
        private static void ConnectAllReferences(
            HUDNotificationFeedManager2_0 manager, 
            HUDNotificationPool2_0 pool,
            HUDNotificationHeader2_0 header,
            RectTransform notificationContainer,
            GameObject rootGO)
        {
            SerializedObject managerSerialized = new SerializedObject(manager);
            
            // Collega pool
            managerSerialized.FindProperty("_pool").objectReferenceValue = pool;
            
            // Collega header
            managerSerialized.FindProperty("_header").objectReferenceValue = header;
            
            // Collega notification container
            managerSerialized.FindProperty("_notificationContainer").objectReferenceValue = notificationContainer;
            
            // Collega root rect transform
            managerSerialized.FindProperty("_rootRectTransform").objectReferenceValue = rootGO.GetComponent<RectTransform>();
            
            managerSerialized.ApplyModifiedProperties();
        }
    }
}
#endif

