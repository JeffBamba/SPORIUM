#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace Sporae.Editor
{
    /// <summary>
    /// Editor script per creare automaticamente la struttura UI del Room Navigation System.
    /// Menu: Tools > Sporae > Setup Room Navigation UI
    /// </summary>
    public class RoomNavigationAutoSetup : EditorWindow
    {
        [MenuItem("Tools/Sporae/Setup Room Navigation UI")]
        public static void CreateRoomNavigationUIDirect()
        {
            if (!Application.isPlaying)
            {
                // Salva la scena corrente se modificata
                if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty)
                {
                    if (EditorUtility.DisplayDialog("Scena non salvata",
                        "La scena corrente ha modifiche non salvate.\nVuoi salvare prima di continuare?",
                        "Sì, Salva", "Continua senza salvare"))
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                    }
                }
            }

            try
            {
                Debug.Log("[RoomNavigationAutoSetup] ========================================");
                Debug.Log("[RoomNavigationAutoSetup] Avvio setup diretto Room Navigation UI...");
                Debug.Log("[RoomNavigationAutoSetup] Scena attiva: " + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name);
                CreateRoomNavigationUI();
                Debug.Log("[RoomNavigationAutoSetup] ========================================");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RoomNavigationAutoSetup] ERRORE durante setup: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Errore Setup",
                    $"Si è verificato un errore durante il setup:\n\n{e.Message}\n\nControlla la Console per dettagli.",
                    "OK");
            }
        }

        [MenuItem("Tools/Sporae/Setup Room Navigation UI (Window)")]
        public static void ShowWindow()
        {
            GetWindow<RoomNavigationAutoSetup>("Room Navigation Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Room Navigation UI Auto Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Questo tool crea automaticamente:\n" +
                "• Canvas_MainHUD (se non esiste)\n" +
                "• Panel_BottomNavigation con struttura completa\n" +
                "• 6 Bottoni UI con configurazione completa\n" +
                "\n⚠️ RoomNavigationManager, RoomButton e CameraController non ancora implementati\n" +
                "\nAssicurati di essere nella scena corretta!",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Create Room Navigation UI", GUILayout.Height(40)))
            {
                CreateRoomNavigationUI();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Cleanup Existing (Delete All)", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Confirm Cleanup",
                    "Vuoi eliminare tutti gli elementi del Room Navigation System?\n\n" +
                    "Questo eliminerà:\n" +
                    "• Panel_BottomNavigation e figli\n" +
                    "\n⚠️ RoomNavigationManager e CameraController non ancora implementati",
                    "Sì, Elimina", "Annulla"))
                {
                    CleanupExisting();
                }
            }
        }

        private static void CreateRoomNavigationUI()
        {
            Debug.Log("[RoomNavigationAutoSetup] Inizio creazione Room Navigation UI...");

            // FASE 1: Canvas
            Canvas canvas = FindOrCreateCanvas();
            if (canvas == null)
            {
                Debug.LogError("[RoomNavigationAutoSetup] Impossibile creare Canvas!");
                return;
            }

            // FASE 2: Panel Bottom Navigation
            GameObject bottomPanel = FindOrCreateBottomPanel(canvas.transform);
            if (bottomPanel == null)
            {
                Debug.LogError("[RoomNavigationAutoSetup] Impossibile creare Bottom Panel!");
                return;
            }

            // FASE 3: Panel Room Buttons
            GameObject buttonsPanel = FindOrCreateButtonsPanel(bottomPanel.transform);
            if (buttonsPanel == null)
            {
                Debug.LogError("[RoomNavigationAutoSetup] Impossibile creare Buttons Panel!");
                return;
            }

            // FASE 4: Creare RoomButtons
            CreateRoomButtons(buttonsPanel.transform);

            // FASE 5: RoomNavigationManager
            // TODO: Implementare RoomNavigationManager e RoomButton prima di abilitare questa fase
            // FindOrCreateRoomNavigationManager(buttonsPanel.transform);

            // FASE 6: CameraController
            // TODO: Implementare CameraController prima di abilitare questa fase
            // SetupCameraController();

            Debug.Log("[RoomNavigationAutoSetup] ✅ Setup completato con successo!");
            EditorUtility.DisplayDialog("Setup Completato",
                "Room Navigation UI creata con successo!\n\n" +
                "⚠️ Nota: RoomNavigationManager, RoomButton e CameraController non ancora implementati\n" +
                "I bottoni UI sono stati creati ma mancano i componenti script.\n\n" +
                "Prossimi passi:\n" +
                "1. Implementare RoomButton component\n" +
                "2. Implementare RoomNavigationManager\n" +
                "3. Implementare CameraController\n" +
                "4. Configurare riferimenti e testare in Play Mode",
                "OK");
        }

        private static Canvas FindOrCreateCanvas()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            
            if (canvas == null)
            {
                Debug.Log("[RoomNavigationAutoSetup] Creando Canvas_MainHUD...");
                GameObject canvasGO = new GameObject("Canvas_MainHUD");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                
                canvasGO.AddComponent<GraphicRaycaster>();
                
                // EventSystem se non esiste
                if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject eventSystem = new GameObject("EventSystem");
                    eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }
            else
            {
                Debug.Log("[RoomNavigationAutoSetup] Canvas esistente trovato: " + canvas.name);
            }
            
            return canvas;
        }

        private static GameObject FindOrCreateBottomPanel(Transform parent)
        {
            Transform existing = parent.Find("Panel_BottomNavigation");
            if (existing != null)
            {
                Debug.Log("[RoomNavigationAutoSetup] Panel_BottomNavigation già esistente, uso quello esistente.");
                return existing.gameObject;
            }

            Debug.Log("[RoomNavigationAutoSetup] Creando Panel_BottomNavigation...");
            GameObject panel = new GameObject("Panel_BottomNavigation");
            panel.transform.SetParent(parent, false);

            // RectTransform
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 120);

            // Image Background
            Image bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0.04f, 0.07f, 0.08f, 0.95f); // #0a1214 con alpha 95%
            bgImage.raycastTarget = true;

            // Top Border
            GameObject border = new GameObject("Image_TopBorder");
            border.transform.SetParent(panel.transform, false);
            RectTransform borderRect = border.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0, 1);
            borderRect.anchorMax = new Vector2(1, 1);
            borderRect.pivot = new Vector2(0.5f, 1);
            borderRect.anchoredPosition = Vector2.zero;
            borderRect.sizeDelta = new Vector2(0, 2);
            Image borderImage = border.AddComponent<Image>();
            borderImage.color = new Color(0.18f, 0.41f, 0.41f, 1f); // #2d6868

            return panel;
        }

        private static GameObject FindOrCreateButtonsPanel(Transform parent)
        {
            Transform existing = parent.Find("Panel_RoomButtons");
            if (existing != null)
            {
                Debug.Log("[RoomNavigationAutoSetup] Panel_RoomButtons già esistente.");
                return existing.gameObject;
            }

            Debug.Log("[RoomNavigationAutoSetup] Creando Panel_RoomButtons...");
            GameObject panel = new GameObject("Panel_RoomButtons");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(40, 10);
            rect.offsetMax = new Vector2(-40, -10);

            HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return panel;
        }

        private static void CreateRoomButtons(Transform parent)
        {
            string[] roomNames = { "Dome", "Laboratory", "Kitchen", "Dormitory", "BlackMarket", "SeedStorage" };
            string[] roomIcons = { "🏛️", "🔬", "🍽️", "🛏️", "💰", "📦" };

            // TODO: Implementare RoomButton prima di abilitare questa parte
            // List<RoomButton> roomButtons = new List<RoomButton>();

            for (int i = 0; i < roomNames.Length; i++)
            {
                GameObject buttonGO = CreateRoomButton(parent, roomNames[i], roomIcons[i], i);
                // TODO: Implementare RoomButton component
                // RoomButton roomButton = buttonGO.GetComponent<RoomButton>();
                // if (roomButton != null)
                // {
                //     roomButtons.Add(roomButton);
                // }
            }

            Debug.Log($"[RoomNavigationAutoSetup] Creati {roomNames.Length} bottoni (RoomButton component non ancora implementato)");
        }

        private static GameObject CreateRoomButton(Transform parent, string roomName, string icon, int roomIndex)
        {
            GameObject buttonGO = new GameObject($"Button_{roomName}");
            buttonGO.transform.SetParent(parent, false);

            // RectTransform
            RectTransform rect = buttonGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280, 100);

            // Layout Element
            LayoutElement layoutElement = buttonGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 280;
            layoutElement.preferredHeight = 100;

            // Button Component
            Button button = buttonGO.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f);
            button.colors = colors;

            // RoomButton Component
            // TODO: Implementare RoomButton prima di abilitare questa parte
            // RoomButton roomButton = buttonGO.AddComponent<RoomButton>();

            // Background Image
            GameObject bg = new GameObject("Image_Background");
            bg.transform.SetParent(buttonGO.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.14f, 0.15f, 1f); // #1a2325
            bgImage.raycastTarget = false;

            // Border Image
            GameObject border = new GameObject("Image_Border");
            border.transform.SetParent(buttonGO.transform, false);
            RectTransform borderRect = border.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = Vector2.zero;
            Image borderImage = border.AddComponent<Image>();
            borderImage.color = new Color(0.18f, 0.41f, 0.41f, 1f); // #2d6868
            borderImage.raycastTarget = false;
            Outline outline = border.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.41f, 0.41f, 1f);
            outline.effectDistance = new Vector2(2, 2);

            // Glow Image
            GameObject glow = new GameObject("Image_Glow");
            glow.transform.SetParent(buttonGO.transform, false);
            RectTransform glowRect = glow.AddComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.sizeDelta = Vector2.zero;
            Image glowImage = glow.AddComponent<Image>();
            glowImage.color = new Color(0.5f, 1f, 0.48f, 0f); // #7FFF7A alpha 0
            glowImage.raycastTarget = false;

            // Icon Text
            GameObject iconTextGO = new GameObject("Text_Icon");
            iconTextGO.transform.SetParent(buttonGO.transform, false);
            RectTransform iconRect = iconTextGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0, -15);
            iconRect.sizeDelta = new Vector2(100, 40);
            TextMeshProUGUI iconText = iconTextGO.AddComponent<TextMeshProUGUI>();
            iconText.text = icon;
            iconText.fontSize = 32;
            iconText.alignment = TextAlignmentOptions.Center;
            iconText.color = Color.white;

            // Label Text
            GameObject labelTextGO = new GameObject("Text_Label");
            labelTextGO.transform.SetParent(buttonGO.transform, false);
            RectTransform labelRect = labelTextGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0, 10);
            labelRect.sizeDelta = new Vector2(200, 30);
            TextMeshProUGUI labelText = labelTextGO.AddComponent<TextMeshProUGUI>();
            labelText.text = roomName.ToUpper();
            labelText.fontSize = 14;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(0.36f, 0.71f, 0.89f, 1f); // #5DB6E3

            // Active Indicator
            GameObject activeIndicator = new GameObject("Image_ActiveIndicator");
            activeIndicator.transform.SetParent(buttonGO.transform, false);
            RectTransform activeRect = activeIndicator.AddComponent<RectTransform>();
            activeRect.anchorMin = new Vector2(0f, 1f);
            activeRect.anchorMax = new Vector2(0f, 1f);
            activeRect.pivot = new Vector2(0.5f, 0.5f);
            activeRect.anchoredPosition = new Vector2(10, -10);
            activeRect.sizeDelta = new Vector2(8, 8);
            Image activeImage = activeIndicator.AddComponent<Image>();
            activeImage.color = new Color(0.5f, 1f, 0.48f, 1f); // #7FFF7A
            activeIndicator.SetActive(false);

            // Notification Badge
            GameObject badge = new GameObject("Badge_Notification");
            badge.transform.SetParent(buttonGO.transform, false);
            RectTransform badgeRect = badge.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(-8, -8);
            badgeRect.sizeDelta = new Vector2(24, 24);

            GameObject badgeBg = new GameObject("Image_BgCircle");
            badgeBg.transform.SetParent(badge.transform, false);
            RectTransform badgeBgRect = badgeBg.AddComponent<RectTransform>();
            badgeBgRect.anchorMin = Vector2.zero;
            badgeBgRect.anchorMax = Vector2.one;
            badgeBgRect.sizeDelta = Vector2.zero;
            Image badgeBgImage = badgeBg.AddComponent<Image>();
            badgeBgImage.color = new Color(0.83f, 0.37f, 0.37f, 1f); // #D35F5F

            GameObject badgeTextGO = new GameObject("Text_Count");
            badgeTextGO.transform.SetParent(badge.transform, false);
            RectTransform badgeTextRect = badgeTextGO.AddComponent<RectTransform>();
            badgeTextRect.anchorMin = Vector2.zero;
            badgeTextRect.anchorMax = Vector2.one;
            badgeTextRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI badgeText = badgeTextGO.AddComponent<TextMeshProUGUI>();
            badgeText.text = "99+";
            badgeText.fontSize = 11;
            badgeText.fontStyle = FontStyles.Bold;
            badgeText.alignment = TextAlignmentOptions.Center;
            badgeText.color = Color.white;

            badge.SetActive(false);

            // Assign references to RoomButton
            // TODO: Implementare RoomButton prima di abilitare questa parte
            // SerializedObject so = new SerializedObject(roomButton);
            // so.FindProperty("backgroundImage").objectReferenceValue = bgImage;
            // so.FindProperty("borderImage").objectReferenceValue = borderImage;
            // so.FindProperty("glowImage").objectReferenceValue = glowImage;
            // so.FindProperty("iconText").objectReferenceValue = iconText;
            // so.FindProperty("labelText").objectReferenceValue = labelText;
            // so.FindProperty("activeIndicator").objectReferenceValue = activeImage;
            // so.FindProperty("notificationBadge").objectReferenceValue = badge;
            // so.FindProperty("notificationCountText").objectReferenceValue = badgeText;
            // so.FindProperty("roomType").enumValueIndex = roomIndex;
            // so.FindProperty("roomLabel").stringValue = roomName.ToUpper();
            // so.FindProperty("iconPlaceholder").stringValue = icon;
            // so.ApplyModifiedProperties();

            return buttonGO;
        }

        // TODO: Implementare RoomNavigationManager e RoomButton prima di abilitare questi metodi
        /*
        private static void FindOrCreateRoomNavigationManager(Transform buttonsParent)
        {
            RoomNavigationManager existing = Object.FindObjectOfType<RoomNavigationManager>();
            if (existing != null)
            {
                Debug.Log("[RoomNavigationAutoSetup] RoomNavigationManager già esistente, aggiorno riferimenti.");
                UpdateRoomNavigationManagerReferences(existing, buttonsParent);
                return;
            }

            Debug.Log("[RoomNavigationAutoSetup] Creando RoomNavigationManager...");
            GameObject managerGO = new GameObject("RoomNavigationManager");
            RoomNavigationManager manager = managerGO.AddComponent<RoomNavigationManager>();

            UpdateRoomNavigationManagerReferences(manager, buttonsParent);
        }

        private static void UpdateRoomNavigationManagerReferences(RoomNavigationManager manager, Transform buttonsParent)
        {
            List<RoomButton> roomButtons = buttonsParent.GetComponentsInChildren<RoomButton>().ToList();
            
            SerializedObject so = new SerializedObject(manager);
            SerializedProperty buttonsProp = so.FindProperty("roomButtons");
            buttonsProp.ClearArray();
            buttonsProp.arraySize = roomButtons.Count;
            for (int i = 0; i < roomButtons.Count; i++)
            {
                buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = roomButtons[i];
            }
            so.ApplyModifiedProperties();

            Debug.Log($"[RoomNavigationAutoSetup] RoomNavigationManager configurato con {roomButtons.Count} bottoni");
        }
        */

        // TODO: Implementare CameraController prima di abilitare questo metodo
        /*
        private static void SetupCameraController()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = Object.FindObjectOfType<Camera>();
            }

            if (mainCamera == null)
            {
                Debug.LogWarning("[RoomNavigationAutoSetup] Nessuna camera trovata! CameraController non creato.");
                return;
            }

            Sporae.Core.CameraController existing = mainCamera.GetComponent<Sporae.Core.CameraController>();
            if (existing != null)
            {
                Debug.Log("[RoomNavigationAutoSetup] CameraController già presente su " + mainCamera.name);
                return;
            }

            Debug.Log("[RoomNavigationAutoSetup] Aggiungendo CameraController a " + mainCamera.name);
            Sporae.Core.CameraController controller = mainCamera.gameObject.AddComponent<Sporae.Core.CameraController>();
            
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("mainCamera").objectReferenceValue = mainCamera;
            so.ApplyModifiedProperties();
        }
        */

        private static void CleanupExisting()
        {
            // Elimina Panel_BottomNavigation
            GameObject bottomPanel = GameObject.Find("Panel_BottomNavigation");
            if (bottomPanel != null)
            {
                DestroyImmediate(bottomPanel);
                Debug.Log("[RoomNavigationAutoSetup] Eliminato Panel_BottomNavigation");
            }

            // Elimina RoomNavigationManager
            // TODO: Implementare RoomNavigationManager prima di abilitare questa parte
            // RoomNavigationManager manager = Object.FindObjectOfType<RoomNavigationManager>();
            // if (manager != null)
            // {
            //     DestroyImmediate(manager.gameObject);
            //     Debug.Log("[RoomNavigationAutoSetup] Eliminato RoomNavigationManager");
            // }

            // Rimuovi CameraController da Main Camera
            // TODO: Implementare CameraController prima di abilitare questa parte
            // Camera mainCamera = Camera.main ?? Object.FindObjectOfType<Camera>();
            // if (mainCamera != null)
            // {
            //     Sporae.Core.CameraController controller = mainCamera.GetComponent<Sporae.Core.CameraController>();
            //     if (controller != null)
            //     {
            //         DestroyImmediate(controller);
            //         Debug.Log("[RoomNavigationAutoSetup] Rimosso CameraController da " + mainCamera.name);
            //     }
            // }

            Debug.Log("[RoomNavigationAutoSetup] ✅ Cleanup completato!");
        }
    }
}
#endif

