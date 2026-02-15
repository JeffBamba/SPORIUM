using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Sporae.Dome.PotSystem.Growth;
using Sporae.UI.UIToolkit.PlantCard;
using Sporae.UI.UIToolkit.PlantCard.Components;
using Sporae.Dome;
using Sporae.Dome.UI;
using _Project;
using _Project.UI.HUDNotifications2_0;

namespace Sporae.UI.UIToolkit.PlantCard
{
    /// <summary>
    /// Controller principale per PlantCard V2.0.
    /// Coordina binding dati, tab switching, quick actions, rotary knobs, note, e event subscriptions.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PlantCardV2Controller : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlantCardV2Config _config;
        [SerializeField] private PotSlot _currentPotSlot;
        
        [Header("UI Elements")]
        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _overlay;
        private Button _closeButton;
        
        // Tab switching
        private Button _tabVitalParameters;
        private Button _tabNotes;
        private VisualElement _vitalParametersTab;
        private VisualElement _noteDiarioTab;
        
        // Quick Actions
        private Button _sprayButton;
        private Button _pruneButton;
        private Button _fertilizeButton;
        
        // Add Note
        private Button _addNoteButton;
        
        // Data Binder
        private PlantCardV2DataBinder _dataBinder;
        
        // Pot Actions reference
        private PotActions _potActions;
        
        // BUG FIX: Flag per saltare il prossimo RefreshData quando lo stato viene cambiato dall'utente
        private bool _skipNextRefresh = false;
        
        // DEBUG_SAFE_FIX: Flag per prevenire l'esecuzione di azioni durante il binding dell'UI
        private bool _isBindingUI = false;
        
        // BUG1 FIX: Flag per prevenire chiamate multiple a DoWater() nello stesso frame
        private bool _isProcessingIrrigationToggle = false;
        
        // BUG FIX: Riferimento al player mover per sospendere il movimento quando la HUD è aperta
        private PlayerClickMover2D _playerMover;
        
        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            
            // DEBUG_SAFE_FIX: Imposta sortingOrder sia su UIDocument che su Canvas parent (se presente)
            // PlantCard deve stare sopra HUD base (50) ma sotto selector modali (200)
            // Usiamo 300 per essere sicuri che stia sopra tutto tranne i selector modali
            if (_uiDocument != null)
            {
                _uiDocument.sortingOrder = 300;
                
                // Se c'è un Canvas parent, imposta anche il suo sortingOrder
                var canvas = _uiDocument.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.sortingOrder = 300;
                }
            }
            
            // BUG FIX: Trova il player mover per sospendere il movimento quando la HUD è aperta
            _playerMover = FindObjectOfType<PlayerClickMover2D>();
            
            if (_uiDocument == null)
            {
                Debug.LogError("PlantCardV2Controller: UIDocument non trovato!");
                return;
            }
            
            _root = _uiDocument.rootVisualElement;
            
            if (_root == null)
            {
                Debug.LogError("PlantCardV2Controller: rootVisualElement non trovato!");
                return;
            }
            
            InitializeUIElements();
            
            // Inizializza DataBinder
            if (_config != null)
            {
                _dataBinder = new PlantCardV2DataBinder(_root, _config);
            }
            else
            {
                Debug.LogWarning("PlantCardV2Controller: PlantCardV2Config non assegnato!");
            }
        }
        
        private void Start()
        {
            // DEBUG_SAFE_FIX: Ribadisce sortingOrder in Start() per assicurarsi che sia applicato dopo l'inizializzazione completa
            // Questo risolve problemi dove l'ordine nella Hierarchy o PanelSettings diversi causano conflitti
            if (_uiDocument != null)
            {
                _uiDocument.sortingOrder = 300;
                
                var canvas = _uiDocument.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.sortingOrder = 300;
                    
                    // DEBUG_SAFE_FIX: Forza PlantCard DOPO TopBar/BottomNav nella Hierarchy per garantire rendering sopra
                    // L'ordine nella Hierarchy può influenzare il rendering quando sortingOrder è uguale
                    var topBar = GameObject.Find("HUD_TopBar");
                    var bottomNav = GameObject.Find("HUD_BottomNavigation");
                    if (topBar != null && bottomNav != null)
                    {
                        // Sposta PlantCard dopo TopBar e BottomNav
                        int topBarIndex = topBar.transform.GetSiblingIndex();
                        int bottomNavIndex = bottomNav.transform.GetSiblingIndex();
                        int maxIndex = Mathf.Max(topBarIndex, bottomNavIndex);
                        transform.SetSiblingIndex(maxIndex + 1);
                    }
                }
            }
            
            // Nascondi overlay all'avvio
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.None;
                // CRITICAL FIX: Imposta pickingMode a Ignore quando nascosto per non bloccare i click del player
                _overlay.pickingMode = PickingMode.Ignore;
            }
            
            // CRITICAL FIX: Imposta anche root a Ignore quando l'overlay è nascosto
            if (_root != null)
            {
                _root.pickingMode = PickingMode.Ignore;
            }
            
            // BUG FIX: Disabilita solo il Canvas all'avvio quando l'overlay è nascosto per non bloccare i click su oggetti di gioco
            // NON disabilitiamo il GameObject perché questo rompe gli event handlers
            if (_uiDocument != null)
            {
                var canvas = _uiDocument.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.enabled = false;
                }
            }
        }
        
        private void OnEnable()
        {
            // Sottoscrivi eventi
            PotEvents.OnPotStateChanged += OnPotStateChanged;
            PotEvents.OnPotActionFailed += OnPotActionFailed;
            PotEvents.OnPlantStageChanged += OnPlantStageChanged;
        }
        
        private void OnDisable()
        {
            // Rimuovi sottoscrizioni
            PotEvents.OnPotStateChanged -= OnPotStateChanged;
            PotEvents.OnPotActionFailed -= OnPotActionFailed;
            PotEvents.OnPlantStageChanged -= OnPlantStageChanged;
        }
        
        private void InitializeUIElements()
        {
            // Overlay e close button
            _overlay = _root.Q<VisualElement>("plant-card-v2-overlay");
            _closeButton = _root.Q<Button>("close-button");
            
            // BUG FIX: Assicura che il close button possa ricevere click
            if (_closeButton != null)
            {
                _closeButton.pickingMode = PickingMode.Position;
            }
            
            // DEBUG_SAFE_FIX: Root deve essere Position per intercettare eventi, ma overlay deve essere Ignore
            // per non bloccare click sugli elementi interattivi dentro PlantCard (es. watering ON)
            if (_root != null)
            {
                _root.pickingMode = PickingMode.Position;
            }
            if (_overlay != null)
            {
                // Overlay deve essere Ignore per non bloccare click sugli elementi interni
                // Il close button e altri elementi interattivi hanno già PickingMode.Position
                _overlay.pickingMode = PickingMode.Ignore;
            }
            
            if (_closeButton != null)
            {
                _closeButton.clicked += OnCloseButtonClicked;
            }
            
            // Tab switching
            _tabVitalParameters = _root.Q<Button>("tab-vital-parameters");
            _tabNotes = _root.Q<Button>("tab-notes");
            _vitalParametersTab = _root.Q<VisualElement>("vital-parameters-tab");
            _noteDiarioTab = _root.Q<VisualElement>("note-diario-tab");
            
            if (_tabVitalParameters != null)
            {
                _tabVitalParameters.clicked += () => SwitchTab(true);
            }
            
            if (_tabNotes != null)
            {
                _tabNotes.clicked += () => SwitchTab(false);
            }
            
            // Quick Actions
            _sprayButton = _root.Q<Button>("spray-button");
            _pruneButton = _root.Q<Button>("prune-button");
            _fertilizeButton = _root.Q<Button>("fertilize-button");
            
            if (_sprayButton != null)
            {
                _sprayButton.clicked += OnSprayButtonClicked;
            }
            
            if (_pruneButton != null)
            {
                _pruneButton.clicked += OnPruneButtonClicked;
            }
            
            if (_fertilizeButton != null)
            {
                _fertilizeButton.clicked += OnFertilizeButtonClicked;
            }
            
            // Plant/Remove buttons
            var plantButton = _root.Q<Button>("plant-button");
            var removeButton = _root.Q<Button>("remove-button");
            
            if (plantButton != null)
            {
                // Plant è disabilitato: si pianta solo dal Pot Ops menu.
                plantButton.style.display = DisplayStyle.None;
                plantButton.SetEnabled(false);
            }
            
            // Plant action button sopra la live view
            var plantActionButton = _root.Q<Button>("plant-action-button");
            if (plantActionButton != null)
            {
                // Plant è disabilitato: si pianta solo dal Pot Ops menu.
                plantActionButton.style.display = DisplayStyle.None;
                plantActionButton.SetEnabled(false);
            }
            
            if (removeButton != null)
            {
                removeButton.clicked += OnRemoveButtonClicked;
            }
            
            // Add Note button
            _addNoteButton = _root.Q<Button>("add-note-button");
            if (_addNoteButton != null)
            {
                _addNoteButton.clicked += OnAddNoteButtonClicked;
            }
            
            // Rotary Knobs - Setup handlers dopo che DataBinder li ha creati
            // (verrà fatto in ShowForPot)
        }
        
        /// <summary>
        /// Mostra PlantCard per un pot specifico
        /// </summary>
        public void ShowForPot(PotSlot potSlot)
        {
            // CRITICAL FIX: Abilita il GameObject PRIMA di qualsiasi altra operazione
            // Questo è necessario perché se il GameObject è disabilitato, non possiamo avviare coroutine
            if (gameObject != null && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            
            if (potSlot == null)
            {
                Debug.LogWarning("PlantCardV2Controller: PotSlot null!");
                return;
            }
            
            _currentPotSlot = potSlot;

            // DEBUG_SAFE_FIX: ribadisce l'ordine in caso di override da Inspector/scene
            // Imposta sia UIDocument che Canvas parent (se presente)
            // E forza l'ordine nella Hierarchy per garantire rendering sopra TopBar/BottomNav
            if (_uiDocument != null)
            {
                _uiDocument.sortingOrder = 300;
                
                var canvas = _uiDocument.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.sortingOrder = 300;
                    
                    // DEBUG_SAFE_FIX: Forza PlantCard DOPO TopBar/BottomNav nella Hierarchy
                    // In Unity UI Toolkit, quando più UIDocument condividono lo stesso Canvas,
                    // l'ordine nella Hierarchy può influenzare il rendering anche con sortingOrder
                    var topBar = GameObject.Find("HUD_TopBar");
                    var bottomNav = GameObject.Find("HUD_BottomNavigation");
                    if (topBar != null && bottomNav != null)
                    {
                        int topBarIndex = topBar.transform.GetSiblingIndex();
                        int bottomNavIndex = bottomNav.transform.GetSiblingIndex();
                        int maxIndex = Mathf.Max(topBarIndex, bottomNavIndex);
                        transform.SetSiblingIndex(maxIndex + 1);
                    }
                }
            }
            
            // Ottieni PotActions dal pot
            _potActions = potSlot.GetComponent<PotActions>();
            if (_potActions == null)
            {
                Debug.LogWarning($"PlantCardV2Controller: PotActions non trovato su {potSlot.name}!");
            }
            
            // Mostra overlay
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.Flex;
                // DEBUG_SAFE_FIX: Overlay deve essere Ignore per non bloccare click sugli elementi interattivi
                // (es. watering ON, rotary knobs, buttons). Solo gli elementi interattivi hanno PickingMode.Position
                _overlay.pickingMode = PickingMode.Ignore;
                
                // CRITICAL FIX: Abilita il Canvas quando l'overlay è mostrato
                // (Il GameObject è già stato abilitato all'inizio del metodo)
                if (_uiDocument != null)
                {
                    var canvas = _uiDocument.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        canvas.enabled = true;
                    }
                }
            }
            
            // Binding dati
            RefreshData();
            
            // BUG FIX: Forza un refresh aggiuntivo dopo che la UI è stata mostrata per assicurarsi
            // che i valori visualizzati siano sempre sincronizzati con lo stato corrente.
            // Questo risolve il problema dove la HUD mostra valori vecchi quando viene aperta dopo un cambio giorno.
            // Il refresh viene fatto dopo un breve delay per assicurarsi che la UI sia completamente inizializzata.
            if (_root != null)
            {
                _root.schedule.Execute(() => {
                    RefreshData();
                }).ExecuteLater(50); // Delay di 50ms per assicurarsi che la UI sia completamente inizializzata
            }
            
            // Setup rotary knobs handlers
            SetupRotaryKnobsHandlers();
            
            // BUG FIX: Sospendi il movimento del player quando la HUD è aperta
            if (_playerMover != null)
            {
                _playerMover.SuspendMovement(true);
                _playerMover.StopMovement();
            }
        }
        
        /// <summary>
        /// Nasconde PlantCard
        /// </summary>
        public void Hide()
        {
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.None;
                // CRITICAL FIX: Imposta pickingMode a Ignore quando nascosto per non bloccare i click del player
                _overlay.pickingMode = PickingMode.Ignore;
            }
            
            // CRITICAL FIX: Imposta anche root a Ignore quando l'overlay è nascosto
            if (_root != null)
            {
                _root.pickingMode = PickingMode.Ignore;
            }
            
            // BUG FIX: Disabilita solo il Canvas quando l'overlay è nascosto per non bloccare i click su oggetti di gioco
            // NON disabilitiamo il GameObject perché questo rompe gli event handlers
            if (_uiDocument != null)
            {
                var canvas = _uiDocument.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.enabled = false;
                }
            }
            
            // BUG FIX: Riprendi il movimento del player quando la HUD è chiusa
            if (_playerMover != null)
            {
                _playerMover.SuspendMovement(false);
            }
            
            _currentPotSlot = null;
        }
        
        /// <summary>
        /// Aggiorna tutti i dati dalla UI
        /// </summary>
        public void RefreshData()
        {
            if (_skipNextRefresh)
            {
                _skipNextRefresh = false;
                return;
            }
            
            if (_currentPotSlot == null || _dataBinder == null)
                return;
            
            PotStateModel state = _potActions != null ? _potActions.PotState : null;
            if (state == null)
                return;
            
            // DEBUG_SAFE_FIX: Imposta flag per prevenire esecuzione azioni durante binding UI
            _isBindingUI = true;
            
            try
            {
                // Ottieni PlantData
                PlantData plantData = null;
                if (!string.IsNullOrEmpty(state.PlantCode))
                {
                    plantData = PlantDatabase.Instance?.GetPlantDataByCode(state.PlantCode);
                }
                
                // Ottieni sprite pianta (se disponibile)
                Sprite plantSprite = null; // TODO: Da implementare se necessario
                var potGrowthController = _currentPotSlot?.GetComponent<Sporae.Dome.PotSystem.Growth.PotGrowthController>();
                if (potGrowthController != null)
                {
                    var plantRenderer = potGrowthController.GetComponentInChildren<SpriteRenderer>();
                    if (plantRenderer != null && plantRenderer.sprite != null)
                    {
                        plantSprite = plantRenderer.sprite;
                    }
                }
                
                // BUG FIX: Imposta callback PRIMA di BindAllData così è disponibile quando vengono creati i tooltip
                _dataBinder.SetStateGetter(() => {
                    PotStateModel currentState = _potActions != null ? _potActions.PotState : null;
                    if (currentState == null) return null;
                    
                    PlantData currentPlantData = null;
                    if (!string.IsNullOrEmpty(currentState.PlantCode))
                    {
                        currentPlantData = PlantDatabase.Instance?.GetPlantDataByCode(currentState.PlantCode);
                    }
                    
                    if (currentPlantData == null) return null;
                    return (currentState, currentPlantData);
                });
                
                // Binding completo (dopo aver impostato SetStateGetter)
                _dataBinder.BindAllData(state, plantData, plantSprite);
            }
            finally
            {
                // DEBUG_SAFE_FIX: Reset flag dopo binding completato
                _isBindingUI = false;
            }
        }
        
        /// <summary>
        /// Setup handlers per rotary knobs
        /// </summary>
        private void SetupRotaryKnobsHandlers()
        {
            if (_dataBinder == null) return;
            
            // Irrigation Knob
            var irrigationKnob = _dataBinder.GetIrrigationKnob();
            if (irrigationKnob != null)
            {
                irrigationKnob.OnIrrigationStateChanged += OnIrrigationStateChanged;
            }
            
            // Illuminazione Knob
            var illuminazioneKnob = _dataBinder.GetIlluminazioneKnob();
            if (illuminazioneKnob != null)
            {
                illuminazioneKnob.OnLedStateChanged += OnLedStateChanged;
            }
        }
        
        /// <summary>
        /// Switch tab (true = Vital Parameters, false = Notes)
        /// </summary>
        private void SwitchTab(bool showVitalParameters)
        {
            // Aggiorna tab buttons
            if (_tabVitalParameters != null)
            {
                if (showVitalParameters)
                {
                    _tabVitalParameters.AddToClassList("tab-button-active");
                }
                else
                {
                    _tabVitalParameters.RemoveFromClassList("tab-button-active");
                }
            }
            
            if (_tabNotes != null)
            {
                if (!showVitalParameters)
                {
                    _tabNotes.AddToClassList("tab-button-active");
                }
                else
                {
                    _tabNotes.RemoveFromClassList("tab-button-active");
                }
            }
            
            // Mostra/nascondi tab content
            if (_vitalParametersTab != null)
            {
                _vitalParametersTab.style.display = showVitalParameters ? DisplayStyle.Flex : DisplayStyle.None;
                if (showVitalParameters)
                {
                    _vitalParametersTab.AddToClassList("tab-active");
                }
                else
                {
                    _vitalParametersTab.RemoveFromClassList("tab-active");
                }
            }
            
            if (_noteDiarioTab != null)
            {
                _noteDiarioTab.style.display = !showVitalParameters ? DisplayStyle.Flex : DisplayStyle.None;
                if (!showVitalParameters)
                {
                    _noteDiarioTab.AddToClassList("tab-active");
                }
                else
                {
                    _noteDiarioTab.RemoveFromClassList("tab-active");
                }
            }
        }
        
        // ============================================
        // QUICK ACTIONS HANDLERS
        // ============================================
        
        private void OnSprayButtonClicked()
        {
            if (_potActions == null || _currentPotSlot == null)
                return;
            
            OpenAdditiveSelector();
        }

        private void OpenAdditiveSelector()
        {
            // Cerca AdditiveSelectorController nella scena
            var selector = FindObjectOfType<Sporae.UI.UIToolkit.AdditiveSelector.AdditiveSelectorController>();
            if (selector == null)
            {
                Debug.LogWarning("PlantCardV2Controller: AdditiveSelectorController non trovato nella scena!");
                return;
            }

            // Sottoscrivi eventi
            selector.OnAdditiveSelected -= OnAdditiveSelected;
            selector.OnAdditiveSelected += OnAdditiveSelected;
            selector.OnCancelled -= OnAdditiveSelectionCancelled;
            selector.OnCancelled += OnAdditiveSelectionCancelled;

            selector.Show();
        }

        private void OnAdditiveSelected(string additiveTypeId)
        {
            if (_potActions == null || _currentPotSlot == null)
                return;

            _potActions.DoApplyAdditive(additiveTypeId);

            // Refresh UI dopo applicazione additivo
            if (_root != null)
            {
                _root.schedule.Execute(() => { RefreshData(); }).ExecuteLater(10);
            }
            else
            {
                RefreshData();
            }
        }

        private void OnAdditiveSelectionCancelled()
        {
            // Nessuna azione necessaria
        }
        
        private void OnPruneButtonClicked()
        {
            if (_potActions == null || _currentPotSlot == null)
                return;
            
            // Apri pruning dialog
            OpenPruningDialog(_currentPotSlot);
        }
        
        /// <summary>
        /// Apre il dialog di potatura per il vaso corrente
        /// </summary>
        private void OpenPruningDialog(PotSlot targetPot)
        {
            if (targetPot == null) return;
            var dayActivityLog = ServiceContainer.Instance.Get<DayActivityLog>(suppressWarning: true);
            if (dayActivityLog != null)
                dayActivityLog.RecordDomeActionStarted(targetPot.PotId);
            // Cerca PruningDialog nella scena o crea istanza
            var pruningDialog = FindObjectOfType<Sporae.Dome.UI.PruningDialog>();
            
            // Se non esiste, cerca prefab e istanzia
            if (pruningDialog == null)
            {
                // Cerca prefab nelle Resources
                var dialogPrefab = Resources.Load<GameObject>("Prefabs/UI/PruningDialog");
                if (dialogPrefab != null)
                {
                    // Trova Canvas per istanziare
                    Canvas canvas = FindObjectOfType<Canvas>();
                    if (canvas != null)
                    {
                        var dialogInstance = Instantiate(dialogPrefab, canvas.transform);
                        pruningDialog = dialogInstance.GetComponent<Sporae.Dome.UI.PruningDialog>();
                    }
                }
                
                if (pruningDialog == null)
                {
                    Debug.LogWarning("PlantCardV2Controller: PruningDialog non trovato nella scena e prefab non disponibile!");
                    return;
                }
            }
            
            // Verifica disponibilità spray
            bool hasSpray = _potActions?.HasSprayAntifungal() ?? false;
            
            // Sottoscrivi eventi
            pruningDialog.OnDialogResult -= OnPruningDialogResult;
            pruningDialog.OnDialogResult += OnPruningDialogResult;
            
            // Mostra dialog
            pruningDialog.Show(hasSpray);
        }
        
        /// <summary>
        /// Gestisce il risultato del dialog di potatura
        /// </summary>
        private void OnPruningDialogResult(bool confirmed, bool useSpray)
        {
            if (!confirmed || _potActions == null || _currentPotSlot == null)
                return;
            
            // Esegui potatura con opzione spray
            _potActions.DoPruning(useSpray);
        }
        
        private void OnFertilizeButtonClicked()
        {
            if (_potActions == null || _currentPotSlot == null)
                return;
            
            // Apri fertilizer selector
            OpenFertilizerSelector(_currentPotSlot);
        }
        
        /// <summary>
        /// Apre il selettore fertilizzanti per il vaso corrente
        /// </summary>
        private void OpenFertilizerSelector(PotSlot targetPot)
        {
            if (targetPot == null) return;
            var dayActivityLog = ServiceContainer.Instance.Get<DayActivityLog>(suppressWarning: true);
            if (dayActivityLog != null)
                dayActivityLog.RecordDomeActionStarted(targetPot.PotId);
            // Cerca UIFertilizerSelector nella scena
            var fertilizerSelector = FindObjectOfType<_Project.UIFertilizerSelector>();
            if (fertilizerSelector == null)
            {
                Debug.LogWarning("PlantCardV2Controller: UIFertilizerSelector non trovato nella scena!");
                return;
            }
            
            // DEBUG_SAFE_FIX: Rimuovi tutte le sottoscrizioni esistenti prima di sottoscrivere
            // Questo previene che PotDetailsWidget gestisca eventi quando viene aperto da PlantCardV2Controller
            fertilizerSelector.ClearAllSubscribers();
            
            // Sottoscrivi eventi per PlantCardV2Controller
            fertilizerSelector.OnFertilizerSelected += OnFertilizerSelected;
            fertilizerSelector.OnCancelled += OnFertilizerSelectionCancelled;
            
            // Mostra selettore (gestisce il sorting order come UISeedSelector)
            fertilizerSelector.Show(targetPot);
        }
        
        /// <summary>
        /// Gestisce la selezione di un fertilizzante
        /// </summary>
        private void OnFertilizerSelected(string fertilizerTypeId)
        {
            if (_potActions == null || _currentPotSlot == null)
                return;
            
            // Applicare il fertilizzante selezionato
            _potActions.DoFertilize(fertilizerTypeId);
        }
        
        /// <summary>
        /// Gestisce l'annullamento della selezione fertilizzante
        /// </summary>
        private void OnFertilizerSelectionCancelled()
        {
            // Nessuna azione necessaria
        }
        
        private void OnPlantButtonClicked()
        {
            if (_potActions == null || _currentPotSlot == null)
                return;
            
            // Apri seed selector
            OpenSeedSelector(_currentPotSlot);
        }
        
        /// <summary>
        /// Apre il selettore semi per il vaso corrente
        /// </summary>
        private void OpenSeedSelector(PotSlot targetPot)
        {
            if (targetPot == null) return;
            var dayActivityLog = ServiceContainer.Instance.Get<DayActivityLog>(suppressWarning: true);
            if (dayActivityLog != null)
                dayActivityLog.RecordDomeActionStarted(targetPot.PotId);
            // Cerca UISeedSelector nella scena
            var seedSelector = FindObjectOfType<_Project.UISeedSelector>();
            if (seedSelector == null)
            {
                Debug.LogWarning("PlantCardV2Controller: UISeedSelector non trovato nella scena!");
                return;
            }
            
            // Sottoscrivi eventi
            seedSelector.OnSeedSelected -= OnSeedSelected;
            seedSelector.OnSeedSelected += OnSeedSelected;
            seedSelector.OnCancelled -= OnSeedSelectionCancelled;
            seedSelector.OnCancelled += OnSeedSelectionCancelled;
            
            // Mostra selettore
            seedSelector.Show(targetPot);
        }
        
        /// <summary>
        /// Gestisce la selezione di un seme
        /// </summary>
        private void OnSeedSelected(string seedTypeId)
        {
            if (_potActions == null || _currentPotSlot == null)
                return;
            
            // Piantare il seme selezionato
            _potActions.DoPlant(seedTypeId);
            
            // DEBUG_SAFE_FIX: Assicura che il root e l'overlay possano ancora ricevere click dopo DoPlant
            if (_root != null)
            {
                _root.pickingMode = PickingMode.Position;
            }
            if (_overlay != null)
            {
                _overlay.pickingMode = PickingMode.Position;
            }
        }
        
        /// <summary>
        /// Gestisce l'annullamento della selezione seme
        /// </summary>
        private void OnSeedSelectionCancelled()
        {
            // Nessuna azione necessaria
        }
        
        private void OnRemoveButtonClicked()
        {
            if (_potActions == null || _currentPotSlot == null)
                return;
            
            _potActions.DoUproot();
        }
        
        // ============================================
        // ROTARY KNOBS HANDLERS
        // ============================================
        
        private void OnIrrigationStateChanged(bool isOn)
        {
            // BUG1 FIX: Prevenire chiamate multiple nello stesso frame
            if (_isProcessingIrrigationToggle)
            {
                return;
            }
            
            // DEBUG_SAFE_FIX: Ignora eventi durante binding UI per prevenire azioni automatiche
            if (_isBindingUI)
            {
                return;
            }
            
            if (_potActions == null || _currentPotSlot == null)
            {
                return;
            }
            var dayActivityLogWater = ServiceContainer.Instance.Get<DayActivityLog>(suppressWarning: true);
            if (dayActivityLogWater != null)
                dayActivityLogWater.RecordDomeActionStarted(_currentPotSlot.PotId);
            // DEBUG_SAFE_FIX: Verifica lo stato corrente PRIMA di chiamare DoWater()
            bool currentState = _potActions.IsWateringSystemOn();
            
            // BUG1 FIX: DoWater() è un toggle. Chiamiamolo solo se lo stato desiderato è diverso da quello corrente.
            // Se lo stato è già quello desiderato, significa che è stato aggiornato da RefreshData() e non dobbiamo fare nulla.
            if (isOn == currentState)
            {
                return;
            }
            
            // BUG1 FIX: Imposta flag per prevenire chiamate multiple
            _isProcessingIrrigationToggle = true;
            
            try
            {
                // DEBUG_SAFE_FIX: DoWater() è un toggle, quindi chiamiamolo solo se lo stato è diverso
                bool success = _potActions.DoWater();
                
                // Se DoWater() fallisce, ripristina lo stato del toggle
                if (!success)
                {
                    // DEBUG_SAFE_FIX: Ripristina lo stato del toggle allo stato corrente reale
                    if (_dataBinder != null)
                    {
                        var irrigationKnob = _dataBinder.GetIrrigationKnob();
                        if (irrigationKnob != null)
                        {
                            // Ripristina allo stato corrente reale (che non è cambiato perché DoWater() è fallito)
                            irrigationKnob.SetIrrigationState(currentState);
                        }
                    }
                }
                else
                {
                    // BUG FIX: Temporaneamente disabilita RefreshData per evitare che ripristini lo stato
                    _skipNextRefresh = true;
                }
            }
            finally
            {
                // BUG1 FIX: Reset flag nel prossimo frame per permettere nuove chiamate
                StartCoroutine(ResetIrrigationToggleFlag());
            }
        }
        
        /// <summary>
        /// Reset del flag di irrigation toggle nel prossimo frame
        /// </summary>
        private System.Collections.IEnumerator ResetIrrigationToggleFlag()
        {
            yield return null; // Aspetta un frame
            _isProcessingIrrigationToggle = false;
        }
        
        private void OnLedStateChanged(LedSystemState state)
        {
            // DEBUG_SAFE_FIX: Ignora eventi durante binding UI per prevenire azioni automatiche
            if (_isBindingUI)
            {
                return;
            }
            
            if (_potActions == null || _currentPotSlot == null)
            {
                return;
            }
            
            // BUG FIX: Temporaneamente disabilita RefreshData per evitare che ripristini lo stato
            // Lo stato verrà aggiornato quando PotEvents.OnPotStateChanged viene emesso
            _skipNextRefresh = true;
            
            // Cambia stato LED
            _potActions.DoLight(state);
        }
        
        // ============================================
        // DIARY NOTES HANDLERS
        // ============================================
        
        private void OnAddNoteButtonClicked()
        {
            if (_dataBinder == null) return;
            
            var diaryNotes = _dataBinder.GetDiaryNotes();
            diaryNotes?.ShowAddNotePanel();
        }
        
        // ============================================
        // EVENT HANDLERS
        // ============================================
        
        private void OnPotStateChanged(PotSlot pot)
        {
            // Aggiorna UI se è il pot corrente
            if (_currentPotSlot != null && pot != null && pot.PotId == _currentPotSlot.PotId)
            {
                // BUG FIX: Forza refresh anche se PlantCardV2 è già aperta
                // Questo assicura che i tooltip e tutti i dati vengano aggiornati quando
                // i valori vengono modificati dalla console debug o da altri sistemi
                RefreshData();
            }
        }
        
        private void OnPotActionFailed(PotEvents.PotActionType actionType, PotSlot pot, string reason)
        {
            // Mostra feedback errore (es. toast notification)
            Debug.LogWarning($"PlantCardV2Controller: Azione {actionType} fallita su {pot?.PotId ?? "unknown"}: {reason}");
            
            // TODO: Mostrare feedback UI (es. label temporaneo con messaggio errore)
        }
        
        private void OnPlantStageChanged(string potId, PlantStage stage)
        {
            // Aggiorna UI se è il pot corrente
            if (_currentPotSlot != null && potId == _currentPotSlot.PotId)
            {
                RefreshData();
            }
        }
        
        private void OnCloseButtonClicked()
        {
            Hide();
        }
        
    }
}


