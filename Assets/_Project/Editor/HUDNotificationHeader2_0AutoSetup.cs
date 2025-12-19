#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

namespace _Project.UI.HUDNotifications2_0.Editor
{
    /// <summary>
    /// Script editor per configurare automaticamente tutti i componenti e layout di HUDNotificationHeader2.0
    /// </summary>
    public static class HUDNotificationHeader2_0AutoSetup
    {
        [MenuItem("Tools/HUD Notifications 2.0/Auto Setup Header")]
        public static void AutoSetupHeader()
        {
            // Trova il GameObject HUDNotificationHeader2.0 nella scena
            HUDNotificationHeader2_0 headerComponent = Object.FindObjectOfType<HUDNotificationHeader2_0>();
            
            if (headerComponent == null)
            {
                EditorUtility.DisplayDialog("Errore", 
                    "Nessun componente HUDNotificationHeader2_0 trovato nella scena.\n\n" +
                    "Assicurati di avere il GameObject con il componente nella scena.", 
                    "OK");
                return;
            }
            
            // Trova anche il manager per collegare i riferimenti
            HUDNotificationFeedManager2_0 manager = Object.FindObjectOfType<HUDNotificationFeedManager2_0>();
            
            GameObject headerGO = headerComponent.gameObject;
            RectTransform headerRect = headerGO.GetComponent<RectTransform>();
            
            if (headerRect == null)
            {
                EditorUtility.DisplayDialog("Errore", 
                    "Il GameObject non ha un RectTransform.\n\n" +
                    "Assicurati che sia un elemento UI.", 
                    "OK");
                return;
            }
            
            Undo.RecordObject(headerGO, "Auto Setup HUDNotificationHeader2.0");
            
            // Carica la config per ottenere i valori
            HUDNotificationConfig2_0 config = Resources.Load<HUDNotificationConfig2_0>("Configs/HUDNotificationConfig2.0");
            
            if (config == null)
            {
                EditorUtility.DisplayDialog("Avviso", 
                    "Config non trovata. Userò valori di default.\n\n" +
                    "Assicurati di avere HUDNotificationConfig2.0 in Resources/Configs/", 
                    "OK");
            }
            
            // 1. Configura RectTransform principale
            SetupRootRectTransform(headerRect, config);
            
            // 2. Setup Background
            SetupBackground(headerGO, config);
            
            // 3. Setup Border
            SetupBorder(headerGO, config);
            
            // 4. Setup Content (HorizontalLayoutGroup)
            GameObject contentGO = SetupContent(headerGO, config);
            
            // 5. Setup InfoIcon
            SetupInfoIcon(contentGO, config);
            
            // 6. Setup HeaderText
            SetupHeaderText(contentGO, config);
            
            // 7. Setup BadgeContainer e BadgeText
            GameObject badgeContainerGO = SetupBadgeContainer(contentGO, config);
            
            // 8. Setup ChevronIcon
            SetupChevronIcon(contentGO, config);
            
            // 9. Setup ToggleButton
            SetupToggleButton(headerGO);
            
            // 10. Collega automaticamente i riferimenti nel componente header
            AutoConnectReferences(headerComponent, headerGO);
            
            // 11. Collega l'header al manager se presente
            if (manager != null)
            {
                ConnectHeaderToManager(manager, headerComponent);
            }
            
            EditorUtility.SetDirty(headerComponent);
            EditorUtility.SetDirty(headerGO);
            if (manager != null)
                EditorUtility.SetDirty(manager);
            
            string successMessage = "Setup completato!\n\n" +
                "Tutti i componenti sono stati configurati automaticamente.\n" +
                "I riferimenti sono stati collegati automaticamente.";
            
            if (manager != null)
            {
                successMessage += "\n\nL'header è stato collegato al Manager.";
            }
            else
            {
                successMessage += "\n\n⚠️ Manager non trovato. Collega manualmente l'header al Manager nell'Inspector.";
            }
            
            EditorUtility.DisplayDialog("Successo", successMessage, "OK");
        }
        
        private static void AutoConnectReferences(HUDNotificationHeader2_0 headerComponent, GameObject headerGO)
        {
            SerializedObject serializedObject = new SerializedObject(headerComponent);
            
            // Trova i componenti nella gerarchia
            Transform toggleButtonTransform = headerGO.transform.Find("ToggleButton");
            Transform backgroundTransform = headerGO.transform.Find("Background");
            Transform borderTransform = headerGO.transform.Find("Border");
            Transform contentTransform = headerGO.transform.Find("Content");
            Transform infoIconTransform = contentTransform != null ? contentTransform.Find("InfoIcon") : null;
            Transform headerTextTransform = contentTransform != null ? contentTransform.Find("HeaderText") : null;
            Transform badgeContainerTransform = contentTransform != null ? contentTransform.Find("BadgeContainer") : null;
            Transform badgeTextTransform = badgeContainerTransform != null ? badgeContainerTransform.Find("BadgeText") : null;
            Transform chevronIconTransform = contentTransform != null ? contentTransform.Find("ChevronIcon") : null;
            
            // Collega i riferimenti usando SerializedProperty
            SerializedProperty toggleButtonProp = serializedObject.FindProperty("_toggleButton");
            SerializedProperty headerBackgroundProp = serializedObject.FindProperty("_headerBackground");
            SerializedProperty borderImageProp = serializedObject.FindProperty("_borderImage");
            SerializedProperty infoIconProp = serializedObject.FindProperty("_infoIcon");
            SerializedProperty headerTextProp = serializedObject.FindProperty("_headerText");
            SerializedProperty badgeContainerProp = serializedObject.FindProperty("_badgeContainer");
            SerializedProperty badgeTextProp = serializedObject.FindProperty("_badgeText");
            SerializedProperty chevronIconProp = serializedObject.FindProperty("_chevronIcon");
            SerializedProperty chevronTransformProp = serializedObject.FindProperty("_chevronTransform");
            
            if (toggleButtonProp != null && toggleButtonTransform != null)
                toggleButtonProp.objectReferenceValue = toggleButtonTransform.GetComponent<Button>();
            
            if (headerBackgroundProp != null && backgroundTransform != null)
                headerBackgroundProp.objectReferenceValue = backgroundTransform.GetComponent<Image>();
            
            if (borderImageProp != null && borderTransform != null)
                borderImageProp.objectReferenceValue = borderTransform.GetComponent<Image>();
            
            if (infoIconProp != null && infoIconTransform != null)
                infoIconProp.objectReferenceValue = infoIconTransform.GetComponent<Image>();
            
            if (headerTextProp != null && headerTextTransform != null)
                headerTextProp.objectReferenceValue = headerTextTransform.GetComponent<TextMeshProUGUI>();
            
            if (badgeContainerProp != null && badgeContainerTransform != null)
                badgeContainerProp.objectReferenceValue = badgeContainerTransform.gameObject;
            
            if (badgeTextProp != null && badgeTextTransform != null)
                badgeTextProp.objectReferenceValue = badgeTextTransform.GetComponent<TextMeshProUGUI>();
            
            if (chevronIconProp != null && chevronIconTransform != null)
                chevronIconProp.objectReferenceValue = chevronIconTransform.GetComponent<Image>();
            
            if (chevronTransformProp != null && chevronIconTransform != null)
                chevronTransformProp.objectReferenceValue = chevronIconTransform.GetComponent<RectTransform>();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private static void SetupRootRectTransform(RectTransform rect, HUDNotificationConfig2_0 config)
        {
            float width = config != null ? config.ContainerWidth : 306f;
            
            rect.anchorMin = new Vector2(1f, 1f); // Top-right
            rect.anchorMax = new Vector2(1f, 1f); // Top-right
            rect.pivot = new Vector2(1f, 1f); // Top-right
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, 50f); // Height temporaneo, si aggiusterà
        }
        
        private static void SetupBackground(GameObject parent, HUDNotificationConfig2_0 config)
        {
            // Trova o crea Background
            Transform backgroundTransform = parent.transform.Find("Background");
            GameObject backgroundGO;
            
            if (backgroundTransform == null)
            {
                backgroundGO = new GameObject("Background");
                backgroundGO.transform.SetParent(parent.transform, false);
                backgroundGO.AddComponent<Image>();
            }
            else
            {
                backgroundGO = backgroundTransform.gameObject;
            }
            
            RectTransform bgRect = backgroundGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; // (0,0)
            bgRect.anchorMax = Vector2.one; // (1,1)
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgRect.sizeDelta = Vector2.zero;
            
            Image bgImage = backgroundGO.GetComponent<Image>();
            Color bgColor = config != null ? config.BackgroundColor : new Color(0.11f, 0.16f, 0.16f, 0.9f);
            bgImage.color = bgColor;
            bgImage.raycastTarget = true; // Per hover effect
            
            // Assicurati che sia il primo child (renderizzato sotto)
            backgroundGO.transform.SetAsFirstSibling();
        }
        
        private static void SetupBorder(GameObject parent, HUDNotificationConfig2_0 config)
        {
            // Trova o crea Border
            Transform borderTransform = parent.transform.Find("Border");
            GameObject borderGO;
            
            if (borderTransform == null)
            {
                borderGO = new GameObject("Border");
                borderGO.transform.SetParent(parent.transform, false);
                borderGO.AddComponent<Image>();
            }
            else
            {
                borderGO = borderTransform.gameObject;
            }
            
            RectTransform borderRect = borderGO.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            borderRect.sizeDelta = Vector2.zero;
            
            Image borderImage = borderGO.GetComponent<Image>();
            if (config != null && config.BorderSprite != null)
            {
                borderImage.sprite = config.BorderSprite;
                borderImage.type = Image.Type.Sliced; // Per 9-sliced
            }
            Color borderColor = config != null ? config.ColorIdle : new Color32(93, 182, 227, 255);
            borderImage.color = borderColor;
            
            // Assicurati che sia dopo Background (renderizzato sopra)
            borderGO.transform.SetSiblingIndex(1);
        }
        
        private static GameObject SetupContent(GameObject parent, HUDNotificationConfig2_0 config)
        {
            // Trova o crea Content
            Transform contentTransform = parent.transform.Find("Content");
            GameObject contentGO;
            
            if (contentTransform == null)
            {
                contentGO = new GameObject("Content");
                contentGO.transform.SetParent(parent.transform, false);
            }
            else
            {
                contentGO = contentTransform.gameObject;
            }
            
            RectTransform contentRect = contentGO.GetComponent<RectTransform>();
            if (contentRect == null)
                contentRect = contentGO.AddComponent<RectTransform>();
            
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            float padding = config != null ? config.HeaderPadding : 8f;
            contentRect.offsetMin = new Vector2(padding, padding);
            contentRect.offsetMax = new Vector2(-padding, -padding);
            contentRect.sizeDelta = Vector2.zero;
            
            // Aggiungi HorizontalLayoutGroup
            HorizontalLayoutGroup layoutGroup = contentGO.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
                layoutGroup = contentGO.AddComponent<HorizontalLayoutGroup>();
            
            layoutGroup.spacing = 8f;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            
            return contentGO;
        }
        
        private static void SetupInfoIcon(GameObject contentParent, HUDNotificationConfig2_0 config)
        {
            // Trova o crea InfoIcon
            Transform iconTransform = contentParent.transform.Find("InfoIcon");
            GameObject iconGO;
            
            if (iconTransform == null)
            {
                iconGO = new GameObject("InfoIcon");
                iconGO.transform.SetParent(contentParent.transform, false);
                iconGO.AddComponent<Image>();
            }
            else
            {
                iconGO = iconTransform.gameObject;
            }
            
            // LayoutElement per dimensioni fisse
            LayoutElement layoutElement = iconGO.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = iconGO.AddComponent<LayoutElement>();
            
            float iconSize = config != null ? config.HeaderIconSize : 14f;
            layoutElement.preferredWidth = iconSize;
            layoutElement.preferredHeight = iconSize;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;
            
            Image iconImage = iconGO.GetComponent<Image>();
            if (config != null && config.InfoIcon != null)
            {
                iconImage.sprite = config.InfoIcon;
            }
            Color iconColor = config != null ? config.ColorIdle : new Color32(93, 182, 227, 255);
            iconImage.color = iconColor;
            
            // Assicurati che sia il primo nel content
            iconGO.transform.SetAsFirstSibling();
        }
        
        private static void SetupHeaderText(GameObject contentParent, HUDNotificationConfig2_0 config)
        {
            // Trova o crea HeaderText
            Transform textTransform = contentParent.transform.Find("HeaderText");
            GameObject textGO;
            
            if (textTransform == null)
            {
                textGO = new GameObject("HeaderText");
                textGO.transform.SetParent(contentParent.transform, false);
                textGO.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                textGO = textTransform.gameObject;
            }
            
            TextMeshProUGUI textComponent = textGO.GetComponent<TextMeshProUGUI>();
            textComponent.text = "NOTIFICATIONS";
            textComponent.font = config != null ? config.MonospacedFont : null;
            float fontSize = config != null ? config.HeaderFontSize : 10f;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = FontStyles.UpperCase;
            textComponent.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
            Color textColor = config != null ? config.ColorIdle : new Color32(93, 182, 227, 255);
            textComponent.color = textColor;
            
            // LayoutElement opzionale (lasciamo che il layout group gestisca le dimensioni)
            LayoutElement layoutElement = textGO.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = textGO.AddComponent<LayoutElement>();
            
            layoutElement.flexibleWidth = 1f; // Prende spazio rimanente
            layoutElement.flexibleHeight = 0;
        }
        
        private static GameObject SetupBadgeContainer(GameObject contentParent, HUDNotificationConfig2_0 config)
        {
            // Trova o crea BadgeContainer
            Transform badgeTransform = contentParent.transform.Find("BadgeContainer");
            GameObject badgeContainerGO;
            
            if (badgeTransform == null)
            {
                badgeContainerGO = new GameObject("BadgeContainer");
                badgeContainerGO.transform.SetParent(contentParent.transform, false);
            }
            else
            {
                badgeContainerGO = badgeTransform.gameObject;
            }
            
            RectTransform badgeRect = badgeContainerGO.GetComponent<RectTransform>();
            if (badgeRect == null)
                badgeRect = badgeContainerGO.AddComponent<RectTransform>();
            
            // LayoutElement per dimensioni preferite
            LayoutElement layoutElement = badgeContainerGO.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = badgeContainerGO.AddComponent<LayoutElement>();
            
            Vector2 badgePadding = config != null ? config.HeaderBadgePadding : new Vector2(6f, 2f);
            float badgeFontSize = config != null ? config.HeaderBadgeFontSize : 10f;
            float badgeWidth = badgePadding.x * 2f + badgeFontSize * 2f; // Stima larghezza
            float badgeHeight = badgePadding.y * 2f + badgeFontSize;
            
            layoutElement.preferredWidth = badgeWidth;
            layoutElement.preferredHeight = badgeHeight;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;
            
            // Trova o crea BadgeText
            Transform badgeTextTransform = badgeContainerGO.transform.Find("BadgeText");
            GameObject badgeTextGO;
            
            if (badgeTextTransform == null)
            {
                badgeTextGO = new GameObject("BadgeText");
                badgeTextGO.transform.SetParent(badgeContainerGO.transform, false);
                badgeTextGO.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                badgeTextGO = badgeTextTransform.gameObject;
            }
            
            RectTransform badgeTextRect = badgeTextGO.GetComponent<RectTransform>();
            badgeTextRect.anchorMin = Vector2.zero;
            badgeTextRect.anchorMax = Vector2.one;
            badgeTextRect.offsetMin = Vector2.zero;
            badgeTextRect.offsetMax = Vector2.zero;
            badgeTextRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI badgeTextComponent = badgeTextGO.GetComponent<TextMeshProUGUI>();
            badgeTextComponent.text = "0";
            badgeTextComponent.font = config != null ? config.MonospacedFont : null;
            badgeTextComponent.fontSize = badgeFontSize;
            badgeTextComponent.alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
            badgeTextComponent.color = Color.white; // O nero se lo sfondo è chiaro
            
            return badgeContainerGO;
        }
        
        private static void SetupChevronIcon(GameObject contentParent, HUDNotificationConfig2_0 config)
        {
            // Trova o crea ChevronIcon
            Transform chevronTransform = contentParent.transform.Find("ChevronIcon");
            GameObject chevronGO;
            
            if (chevronTransform == null)
            {
                chevronGO = new GameObject("ChevronIcon");
                chevronGO.transform.SetParent(contentParent.transform, false);
                chevronGO.AddComponent<Image>();
            }
            else
            {
                chevronGO = chevronTransform.gameObject;
            }
            
            // LayoutElement per dimensioni fisse
            LayoutElement layoutElement = chevronGO.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = chevronGO.AddComponent<LayoutElement>();
            
            float chevronSize = config != null ? config.HeaderChevronSize : 16f;
            layoutElement.preferredWidth = chevronSize;
            layoutElement.preferredHeight = chevronSize;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;
            
            Image chevronImage = chevronGO.GetComponent<Image>();
            if (config != null && config.ChevronIcon != null)
            {
                chevronImage.sprite = config.ChevronIcon;
            }
            chevronImage.color = Color.white;
            
            // Il RectTransform del chevron è usato per la rotazione
            RectTransform chevronRect = chevronGO.GetComponent<RectTransform>();
            // Lo script userà questo per la rotazione
        }
        
        private static void SetupToggleButton(GameObject parent)
        {
            // Trova o crea ToggleButton
            Transform buttonTransform = parent.transform.Find("ToggleButton");
            GameObject buttonGO;
            
            if (buttonTransform == null)
            {
                buttonGO = new GameObject("ToggleButton");
                buttonGO.transform.SetParent(parent.transform, false);
                buttonGO.AddComponent<Button>();
                buttonGO.AddComponent<Image>();
            }
            else
            {
                buttonGO = buttonTransform.gameObject;
            }
            
            RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.one;
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            buttonRect.sizeDelta = Vector2.zero;
            
            Button buttonComponent = buttonGO.GetComponent<Button>();
            // Il button non ha transizione visiva (o può essere None)
            ColorBlock colors = buttonComponent.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            buttonComponent.colors = colors;
            
            Image buttonImage = buttonGO.GetComponent<Image>();
            buttonImage.color = new Color(1f, 1f, 1f, 0f); // Trasparente (solo per intercettare click)
            
            // Assicurati che sia l'ultimo child (renderizzato sopra tutto)
            buttonGO.transform.SetAsLastSibling();
        }
        
        private static void ConnectHeaderToManager(HUDNotificationFeedManager2_0 manager, HUDNotificationHeader2_0 header)
        {
            SerializedObject managerSerialized = new SerializedObject(manager);
            SerializedProperty headerProp = managerSerialized.FindProperty("_header");
            
            if (headerProp != null)
            {
                headerProp.objectReferenceValue = header;
                managerSerialized.ApplyModifiedProperties();
            }
        }
    }
}
#endif

