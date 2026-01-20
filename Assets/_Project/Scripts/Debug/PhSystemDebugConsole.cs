using System.Collections.Generic;
using UnityEngine;
using _Project;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem;
using Sporae.Dome.PotSystem.Growth;

namespace Sporae.DevTools
{
    /// <summary>
    /// Console di debug per il sistema pH
    /// Tasto Z per aprire/chiudere
    /// Solo per Editor/Development build
    /// </summary>
    public class PhSystemDebugConsole : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugConsole = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.Z;
        [SerializeField] private bool showOnStart = false;
        
        /// <summary>
        /// Configura la console dopo la creazione (per auto-setup)
        /// </summary>
        public void Configure(bool enable, KeyCode key, bool showOnStartValue = false)
        {
            enableDebugConsole = enable;
            toggleKey = key;
            showOnStart = showOnStartValue;
        }

        [Header("References")]
        [SerializeField] private GameManager gameManager;
        
        private PhSystem _phSystem;
        private DayCycleSystem _dayCycleSystem;
        private GameManager _gameManager;
        private CondensationSystem _condensationSystem;
        private bool _isConsoleOpen = false;
        private string _phInputValue = "0";
        private string _cryInputValue = "250";
        private string _actionsInputValue = "4";
        private string _dayInputValue = "1";
        private string _condensationInputValue = "0";
        private string _condensationDailyProductionInput = "0";
        private string _condensationCapInput = "8";
        private bool _condensationExtendedBasin = false;
        private Vector2 _scrollPosition;
        private Vector2 _mainScrollPosition; // Scroll per l'intera console
        private List<string> _debugLog = new List<string>();
        private const int MAX_LOG_ENTRIES = 50;

        // Valori rapidi per test
        private readonly Dictionary<string, float> _quickValues = new Dictionary<string, float>
        {
            { "Ultra Acid", -100f },
            { "Stable Acid", -50f },
            { "Neutral", 0f },
            { "Stable Basic", +50f },
            { "Ultra Basic", +100f }
        };

        private void Awake()
        {
            // Crea sistema pH se non esiste - sempre inizializzato a 0.0
            _phSystem = new PhSystem(0f);
            // Reset esplicito per assicurarsi che tutto sia a 0.0 (pH base, oscillazione, contributi)
            _phSystem.Reset();
            _phSystem.OnPhChanged += OnPhChanged;

            _isConsoleOpen = showOnStart;
            
            #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enableDebugConsole = false;
            #endif
            
            SporiumLogger.LogDebug(LogCategory.Ph, $"Awake - enableDebugConsole: {enableDebugConsole}, toggleKey: {toggleKey}, showOnStart: {showOnStart}");
            SporiumLogger.LogInfo(LogCategory.Ph, "pH inizializzato a 0.0 (Reset completo)");
        }

        private void Start()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }
            
            _gameManager = gameManager;

            // Ottieni DayCycleSystem per pulsante End of Day
            TryGetDayCycleSystem();
            
            // Ottieni CondensationSystem
            TryGetCondensationSystem();

            // Registra nel ServiceContainer se disponibile (dopo che è stato inizializzato)
            TryRegisterPhSystem();

            AddLog("=== pH System Debug Console ===");
            AddLog("Premi Z per aprire/chiudere la console");
            AddLog($"pH iniziale: {_phSystem.CurrentPh:F2} ({_phSystem.GetBandName()})");
        }
        
        private void TryGetCondensationSystem()
        {
            try
            {
                if (_gameManager != null)
                {
                    _condensationSystem = _gameManager.CondensationSystem;
                    if (_condensationSystem != null)
                    {
                        AddLog("CondensationSystem trovato");
                        // Inizializza valori input con valori correnti
                        _condensationInputValue = _condensationSystem.CurrentAccumulation.ToString("F1");
                        _condensationDailyProductionInput = _condensationSystem.DailyProduction.ToString("F1");
                        _condensationCapInput = _condensationSystem.GetCollectionCap().ToString();
                        _condensationExtendedBasin = _condensationSystem.HasExtendedBasin;
                    }
                }
            }
            catch
            {
                // CondensationSystem non disponibile
            }
        }
        
        private void TryGetDayCycleSystem()
        {
            try
            {
                var serviceContainer = ServiceContainer.Instance;
                if (serviceContainer != null)
                {
                    _dayCycleSystem = serviceContainer.Get<DayCycleSystem>(suppressWarning: true);
                    if (_dayCycleSystem != null)
                    {
                        AddLog("DayCycleSystem trovato - pulsante End of Day disponibile");
                    }
                }
            }
            catch
            {
                // DayCycleSystem non ancora disponibile
            }
        }

        private void TryRegisterPhSystem()
        {
            try
            {
                var serviceContainer = ServiceContainer.Instance;
                if (serviceContainer == null)
                {
                    AddLog("ServiceContainer non ancora inizializzato");
                    return;
                }

                // Controlla sia locale che globale (con sicurezza per null)
                bool isRegistered = serviceContainer.Contains(typeof(PhSystem));
                try
                {
                    isRegistered = isRegistered || serviceContainer.ContainsGlobal(typeof(PhSystem));
                }
                catch
                {
                    // Se ContainsGlobal fallisce (globalInstance null), usa solo Contains
                }

                if (!isRegistered)
                {
                    serviceContainer.Register<PhSystem>(_phSystem);
                    AddLog("Sistema pH registrato nel ServiceContainer");
                }
                else
                {
                    // Se già registrato, recuperalo
                    try
                    {
                        _phSystem = serviceContainer.Get<PhSystem>();
                        _phSystem.OnPhChanged += OnPhChanged;
                        AddLog("Sistema pH recuperato dal ServiceContainer");
                    }
                    catch (System.Exception)
                    {
                        // Se fallisce il recupero, usa quello locale
                        AddLog("Sistema pH già registrato, uso istanza locale");
                    }
                }
            }
            catch (System.Exception ex)
            {
                // ServiceContainer non ancora inizializzato o non disponibile
                AddLog($"ServiceContainer non disponibile: {ex.Message}");
            }
        }

        private void Update()
        {
            if (!enableDebugConsole)
            {
                // Debug: verifica perché non funziona
                if (Input.GetKeyDown(toggleKey))
                {
                    SporiumLogger.LogWarning(LogCategory.Ph, "Tasto Z premuto ma enableDebugConsole è FALSE!");
                }
                return;
            }

            if (Input.GetKeyDown(toggleKey))
            {
                _isConsoleOpen = !_isConsoleOpen;
                AddLog(_isConsoleOpen ? "Console aperta" : "Console chiusa");
                SporiumLogger.LogDebug(LogCategory.Ph, $"Console {( _isConsoleOpen ? "aperta" : "chiusa")} - Tasto {toggleKey} premuto");
            }

            // Hotkeys rapide (solo se console aperta)
            if (_isConsoleOpen)
            {
                // 1-5: Valori rapidi
                if (Input.GetKeyDown(KeyCode.Alpha1)) SetQuickPh("Ultra Acid");
                if (Input.GetKeyDown(KeyCode.Alpha2)) SetQuickPh("Stable Acid");
                if (Input.GetKeyDown(KeyCode.Alpha3)) SetQuickPh("Neutral");
                if (Input.GetKeyDown(KeyCode.Alpha4)) SetQuickPh("Stable Basic");
                if (Input.GetKeyDown(KeyCode.Alpha5)) SetQuickPh("Ultra Basic");

                // +/-: Modifiche incrementali
                if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals))
                {
                    _phSystem.ApplyInstantDelta(+5f, "Debug_Manual");
                }
                if (Input.GetKeyDown(KeyCode.Minus))
                {
                    _phSystem.ApplyInstantDelta(-5f, "Debug_Manual");
                }

                // R: Reset a neutro
                if (Input.GetKeyDown(KeyCode.R))
                {
                    _phSystem.Reset();
                    AddLog("pH resettato a neutro");
                }

                // D: Simula drift giornaliero
                if (Input.GetKeyDown(KeyCode.D))
                {
                    SimulateDailyDrift();
                }
            }
        }

        private void OnPhChanged(float newPh, float delta)
        {
            AddLog($"pH cambiato: {newPh:F2} (Δ {delta:+#0.0;-#0.0}) - Banda: {_phSystem.GetBandName()}");
        }

        private void OnGUI()
        {
            if (!enableDebugConsole || !_isConsoleOpen) return;

            // Stile della console
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.9f));

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 20; // Aumentato da 16 a 20 per migliore visibilità

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 18; // Aumentato da 14 a 18 per migliore visibilità

            // Area console principale - adattiva all'altezza dello schermo
            float consoleWidth = 780f; // Scalato del 30% (600 * 1.3)
            float consoleHeight = Mathf.Min(Screen.height - 26, 900f); // Altezza adattiva, max 900px
            Rect consoleRect = new Rect(Screen.width - consoleWidth - 13, 13, consoleWidth, consoleHeight); // Margini scalati del 30%

            GUILayout.BeginArea(consoleRect, boxStyle);
            
            // BLK-02.07: Scroll view per l'intera console per adattarsi al contenuto
            _mainScrollPosition = GUILayout.BeginScrollView(_mainScrollPosition, false, true);
            GUILayout.BeginVertical();

            // Header
            GUILayout.Label("🧪 pH SYSTEM DEBUG CONSOLE", labelStyle);
            GUILayout.Space(7); // Scalato del 30% (5 * 1.3)

            // Stato corrente pH (escludendo drift simulato dal display principale)
            var contribHeader = _phSystem.GetContributions();
            float phDisplay = _phSystem.CurrentPh - contribHeader.DailyDrift; // esclude drift simulato
            var bandColor = _phSystem.GetBandColor();
            var oldColor = GUI.color;
            GUI.color = bandColor;
            GUILayout.Label($"pH Corrente: {phDisplay:F2}", labelStyle);
            GUI.color = oldColor;
            GUILayout.Label($"Banda: {_phSystem.GetBandName()}", labelStyle);
            GUILayout.Space(13); // Scalato del 30% (10 * 1.3)

            // Input manuale pH
            GUILayout.BeginHorizontal();
            GUILayout.Label("Imposta pH:", labelStyle, GUILayout.Width(130)); // Scalato del 30% (100 * 1.3)
            _phInputValue = GUILayout.TextField(_phInputValue, GUILayout.Width(130)); // Scalato del 30% (100 * 1.3)
            if (GUILayout.Button("Applica", buttonStyle, GUILayout.Width(104))) // Scalato del 30% (80 * 1.3)
            {
                if (float.TryParse(_phInputValue, out float newPh))
                {
                    _phSystem.SetPh(newPh);
                    AddLog($"pH impostato manualmente a {newPh:F2}");
                }
                else
                {
                    AddLog("ERRORE: Valore non valido!");
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(7); // Scalato del 30% (5 * 1.3)

            // Valori rapidi
            GUILayout.Label("Valori Rapidi:", labelStyle);
            GUILayout.BeginHorizontal();
            foreach (var kvp in _quickValues)
            {
                if (GUILayout.Button(kvp.Key, buttonStyle, GUILayout.Width(143))) // Scalato del 30% (110 * 1.3)
                {
                    SetQuickPh(kvp.Key);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(7); // Scalato del 30% (5 * 1.3)

            // Modifiche incrementali
            GUILayout.Label("Modifiche Incrementali:", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-10", buttonStyle, GUILayout.Width(78))) _phSystem.ApplyInstantDelta(-10f, "Debug_Manual"); // Scalato del 30% (60 * 1.3)
            if (GUILayout.Button("-5", buttonStyle, GUILayout.Width(78))) _phSystem.ApplyInstantDelta(-5f, "Debug_Manual"); // Scalato del 30% (60 * 1.3)
            if (GUILayout.Button("+5", buttonStyle, GUILayout.Width(78))) _phSystem.ApplyInstantDelta(+5f, "Debug_Manual"); // Scalato del 30% (60 * 1.3)
            if (GUILayout.Button("+10", buttonStyle, GUILayout.Width(78))) _phSystem.ApplyInstantDelta(+10f, "Debug_Manual"); // Scalato del 30% (60 * 1.3)
            if (GUILayout.Button("Reset", buttonStyle, GUILayout.Width(104))) _phSystem.Reset(); // Scalato del 30% (80 * 1.3)
            GUILayout.EndHorizontal();
            GUILayout.Space(7); // Scalato del 30% (5 * 1.3)

            // Simulazioni
            GUILayout.Label("Simulazioni:", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Drift Giornaliero", buttonStyle, GUILayout.Width(156))) // Scalato del 30% (120 * 1.3)
            {
                SimulateDailyDrift();
            }
            if (GUILayout.Button("Overwatering", buttonStyle, GUILayout.Width(156))) // Scalato del 30% (120 * 1.3)
            {
                _phSystem.ApplyInstantDelta(-5f, "Action_Watering_Over");
                AddLog("Simulato Overwatering: pH -5");
            }
            if (GUILayout.Button("LED Blu", buttonStyle, GUILayout.Width(156))) // Scalato del 30% (120 * 1.3)
            {
                _phSystem.ApplyInstantDelta(+5f, "Action_LED_Blue");
                AddLog("Simulato LED Blu: pH +5");
            }
            if (GUILayout.Button("LED Rosso", buttonStyle, GUILayout.Width(156))) // Scalato del 30% (120 * 1.3)
            {
                _phSystem.ApplyInstantDelta(-5f, "Action_LED_Red");
                AddLog("Simulato LED Rosso: pH -5");
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(7); // Scalato del 30% (5 * 1.3)
            
            // === SEZIONE GAME STATE ===
            GUILayout.Label("=== Game State ===", labelStyle);
            if (_gameManager != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"CRY: {_gameManager.CurrentCRY}", labelStyle, GUILayout.Width(130));
                _cryInputValue = GUILayout.TextField(_cryInputValue, GUILayout.Width(100));
                if (GUILayout.Button("Set CRY", buttonStyle, GUILayout.Width(104)))
                {
                    if (int.TryParse(_cryInputValue, out int newCry))
                    {
                        _gameManager.EconomySystem.SetCRY(newCry);
                        AddLog($"CRY impostato a {newCry}");
                    }
                }
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Actions: {_gameManager.ActionsLeft}", labelStyle, GUILayout.Width(130));
                _actionsInputValue = GUILayout.TextField(_actionsInputValue, GUILayout.Width(100));
                if (GUILayout.Button("Set Actions", buttonStyle, GUILayout.Width(104)))
                {
                    if (int.TryParse(_actionsInputValue, out int newActions))
                    {
                        _gameManager.ActionSystem.RestoreState(newActions, _gameManager.ActionSystem.MaxActions);
                        AddLog($"Actions impostate a {newActions}");
                    }
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("GameManager non disponibile", labelStyle);
            }
            GUILayout.Space(7);
            
            // === SEZIONE DAY CYCLE (Espansa) ===
            GUILayout.Label("=== Day Cycle System ===", labelStyle);
            GUILayout.BeginHorizontal();
            if (_dayCycleSystem != null)
            {
                int currentDay = _dayCycleSystem.CurrentDay;
                bool canEndDay = _dayCycleSystem.CanEndDay();
                
                GUILayout.Label($"Giorno Corrente: {currentDay}", labelStyle, GUILayout.Width(200));
                GUILayout.Label($"Prossimo Giorno: {currentDay + 1}", labelStyle, GUILayout.Width(200));
                
                if (!canEndDay)
                {
                    GUILayout.Label("(CRY insufficienti)", new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.red } });
                }
            }
            else
            {
                GUILayout.Label("DayCycleSystem non disponibile", labelStyle);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (_dayCycleSystem != null)
            {
                int currentDay = _dayCycleSystem.CurrentDay;
                bool canEndDay = _dayCycleSystem.CanEndDay();
                
                GUI.enabled = canEndDay;
                if (GUILayout.Button($"End of Day (Giorno {currentDay})", buttonStyle, GUILayout.Width(260)))
                {
                    if (_dayCycleSystem.EndDay())
                    {
                        AddLog($"✅ End of Day attivato! Nuovo giorno: {_dayCycleSystem.CurrentDay}");
                    }
                    else
                    {
                        AddLog("❌ End of Day fallito - CRY insufficienti");
                    }
                }
                GUI.enabled = true;
                
                // Pulsante per impostare giorno manualmente
                _dayInputValue = GUILayout.TextField(_dayInputValue, GUILayout.Width(100));
                if (GUILayout.Button("Set Day", buttonStyle, GUILayout.Width(104)))
                {
                    if (int.TryParse(_dayInputValue, out int newDay))
                    {
                        // Nota: DayCycleSystem potrebbe non avere un metodo SetDay pubblico
                        // In questo caso, loggiamo solo
                        AddLog($"⚠️ Set Day non implementato direttamente (Giorno corrente: {currentDay})");
                    }
                }
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Button("End of Day (N/A)", buttonStyle, GUILayout.Width(260));
                GUI.enabled = true;
                
                // Prova a recuperare DayCycleSystem se non disponibile
                if (GUILayout.Button("Retry", buttonStyle, GUILayout.Width(78)))
                {
                    TryGetDayCycleSystem();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(7);
            
            // === SEZIONE CONDENSATION ===
            GUILayout.Label("=== Condensation System ===", labelStyle);
            if (_condensationSystem != null)
            {
                float currentCondensation = _condensationSystem.CurrentAccumulation;
                float dailyProduction = _condensationSystem.DailyProduction;
                int collectionCap = _condensationSystem.GetCollectionCap();
                bool hasExtendedBasin = _condensationSystem.HasExtendedBasin;
                var configValues = _condensationSystem.GetConfigValues();
                
                // Accumulo corrente (0-100%)
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Accumulo: {currentCondensation:F1}%", labelStyle, GUILayout.Width(200));
                _condensationInputValue = GUILayout.TextField(_condensationInputValue, GUILayout.Width(80));
                if (GUILayout.Button("Set", buttonStyle, GUILayout.Width(60)))
                {
                    if (float.TryParse(_condensationInputValue, out float newValue))
                    {
                        _condensationSystem.SetCurrentAccumulation(newValue);
                        if (_gameManager != null)
                        {
                            _gameManager.NotifyCondensationChanged();
                        }
                        AddLog($"✓ Accumulo condensazione impostato a {newValue:F1}%");
                    }
                    else
                    {
                        AddLog("✗ Valore non valido per accumulo");
                    }
                }
                if (GUILayout.Button("Reset", buttonStyle, GUILayout.Width(60)))
                {
                    _condensationSystem.Reset();
                    if (_gameManager != null)
                    {
                        _gameManager.NotifyCondensationChanged();
                    }
                    AddLog("✓ Condensazione resettata a 0%");
                }
                GUILayout.EndHorizontal();
                
                // Produzione giornaliera
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Produzione/giorno: {dailyProduction:F1}", labelStyle, GUILayout.Width(200));
                _condensationDailyProductionInput = GUILayout.TextField(_condensationDailyProductionInput, GUILayout.Width(80));
                if (GUILayout.Button("Set", buttonStyle, GUILayout.Width(60)))
                {
                    if (float.TryParse(_condensationDailyProductionInput, out float newValue))
                    {
                        _condensationSystem.SetDailyProduction(newValue);
                        AddLog($"✓ Produzione giornaliera impostata a {newValue:F1}");
                    }
                    else
                    {
                        AddLog("✗ Valore non valido per produzione");
                    }
                }
                GUILayout.EndHorizontal();
                
                // Cap di raccolta
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Cap raccolta: {collectionCap} (Base: {configValues.baseCap}, Extended: {configValues.extendedCap})", labelStyle, GUILayout.Width(300));
                _condensationCapInput = GUILayout.TextField(_condensationCapInput, GUILayout.Width(80));
                if (GUILayout.Button("Set", buttonStyle, GUILayout.Width(60)))
                {
                    if (int.TryParse(_condensationCapInput, out int newValue))
                    {
                        _condensationSystem.SetCollectionCap(newValue);
                        AddLog($"✓ Cap raccolta impostato a {newValue}");
                    }
                    else
                    {
                        AddLog("✗ Valore non valido per cap");
                    }
                }
                GUILayout.EndHorizontal();
                
                // Upgrade Bacino Esteso
                GUILayout.BeginHorizontal();
                bool newExtendedBasin = GUILayout.Toggle(hasExtendedBasin, "Bacino Esteso (Cap 12)", GUILayout.Width(200));
                if (newExtendedBasin != hasExtendedBasin)
                {
                    _condensationSystem.SetExtendedBasin(newExtendedBasin);
                    AddLog($"✓ Bacino esteso: {(newExtendedBasin ? "ATTIVO" : "DISATTIVO")}");
                }
                GUILayout.EndHorizontal();
                
                // Valori Config (solo visualizzazione)
                GUILayout.BeginVertical("box");
                GUILayout.Label("Config Values (read-only):", labelStyle);
                GUILayout.Label($"  Base Sana: {configValues.baseSana:F1}", labelStyle);
                GUILayout.Label($"  Base Stressata: {configValues.baseStressata:F1}", labelStyle);
                GUILayout.Label($"  LED Bonus: {configValues.ledBonus:F1}", labelStyle);
                GUILayout.EndVertical();
                
                // Aggiorna valori input quando cambiano (solo se non sono in editing)
                if (float.TryParse(_condensationInputValue, out float parsedCondensation))
                {
                    if (Mathf.Abs(parsedCondensation - currentCondensation) > 0.1f)
                    {
                        _condensationInputValue = currentCondensation.ToString("F1");
                    }
                }
                else if (_condensationInputValue == "")
                {
                    _condensationInputValue = currentCondensation.ToString("F1");
                }
                
                if (float.TryParse(_condensationDailyProductionInput, out float parsedProduction))
                {
                    if (Mathf.Abs(parsedProduction - dailyProduction) > 0.1f)
                    {
                        _condensationDailyProductionInput = dailyProduction.ToString("F1");
                    }
                }
                else if (_condensationDailyProductionInput == "")
                {
                    _condensationDailyProductionInput = dailyProduction.ToString("F1");
                }
                
                if (int.TryParse(_condensationCapInput, out int parsedCap))
                {
                    if (parsedCap != collectionCap)
                    {
                        _condensationCapInput = collectionCap.ToString();
                    }
                }
                else if (_condensationCapInput == "")
                {
                    _condensationCapInput = collectionCap.ToString();
                }
            }
            else
            {
                GUILayout.Label("CondensationSystem non disponibile", labelStyle);
                if (GUILayout.Button("Retry", buttonStyle, GUILayout.Width(78)))
                {
                    TryGetCondensationSystem();
                }
            }
            GUILayout.Space(7);

            // Effetti su piante
            GUILayout.Label("Effetti su Piante:", labelStyle);
            ShowPlantEffects();
            GUILayout.Space(7); // Scalato del 30% (5 * 1.3)
            
            // pH Affinity - Piante Attive
            GUILayout.Label("=== pH Affinity - Piante Attive ===", labelStyle);
            ShowPhAffinityInfo();
            GUILayout.Space(7); // Scalato del 30% (5 * 1.3)
            
            // BLK-02.07: Breakdown calcoli pH (sequenza step-by-step)
            GUILayout.Label("📊 Breakdown Calcoli pH:", labelStyle);
            ShowCalculationBreakdown();
            GUILayout.Space(7); // Scalato del 30% (5 * 1.3)

            // Log debug
            GUILayout.Label("Log:", labelStyle);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(195)); // Scalato del 30% (150 * 1.3)
            foreach (var logEntry in _debugLog)
            {
                GUILayout.Label(logEntry, labelStyle);
            }
            GUILayout.EndScrollView();

            // Hotkeys info
            GUILayout.Space(7); // Scalato del 30% (5 * 1.3)
            GUILayout.Label("Hotkeys: 1-5=Valori rapidi | +/- = ±5 | R=Reset | D=Drift", 
                new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.gray } }); // Aumentato da 13 a 16

            GUILayout.EndVertical();
            GUILayout.EndScrollView(); // Fine scroll view principale
            GUILayout.EndArea();
        }

        private void SetQuickPh(string key)
        {
            if (_quickValues.TryGetValue(key, out float value))
            {
                _phSystem.SetPh(value);
                AddLog($"pH impostato a {key}: {value:F2}");
            }
        }

        private void SimulateDailyDrift()
        {
            // Simula drift da piante (esempio: 2 Pure + 1 Evil = +4 -2 = +2)
            float simulatedDrift = 0f;
            
            // Cerca piante nella scena
            PotSlot[] allPots = FindObjectsOfType<PotSlot>();
            int pureCount = 0;
            int evilCount = 0;
            int standardCount = 0;

            foreach (var pot in allPots)
            {
                if (pot != null && pot.PotActions != null && pot.PotActions.HasPlant)
                {
                    // TODO: Quando il sistema piante sarà completo, leggere famiglia reale
                    // Per ora simuliamo
                    var state = pot.PotActions.GetCurrentState();
                    if (state != null && state.Stage != (int)PlantStage.Empty)
                    {
                        // Placeholder: assumiamo distribuzione casuale per test
                        standardCount++;
                    }
                }
            }

            // Calcolo drift simulato
            simulatedDrift = (pureCount * 2f) - (evilCount * 2f) + (standardCount * 0f);
            
            if (simulatedDrift == 0f)
            {
                simulatedDrift = Random.Range(-2f, +2f); // Drift casuale se nessuna pianta
            }

            _phSystem.RegisterDailyDrift(simulatedDrift);
            AddLog($"Drift giornaliero simulato: {simulatedDrift:+#0.0;-#0.0} (Pure:{pureCount} Evil:{evilCount} Std:{standardCount})");
        }

        private void ShowPlantEffects()
        {
            var band = _phSystem.EvaluateState();
            var bandName = _phSystem.GetBandName();

            string effects = band switch
            {
                PhSystem.PhBand.UltraAcid => "PURE: Collapsing | EVIL: Thriving",
                PhSystem.PhBand.StableAcid => "PURE: Weakening | EVIL: Thriving",
                PhSystem.PhBand.Neutral => "Tutte: Stable",
                PhSystem.PhBand.StableBasic => "PURE: Thriving | EVIL: Weakening",
                PhSystem.PhBand.UltraBasic => "PURE: Thriving | EVIL: Collapsing",
                _ => "Unknown"
            };

            GUILayout.Label($"  {effects}", new GUIStyle(GUI.skin.label) { fontSize = 14 }); // Scalato del 30% (11 * 1.3)
        }
        
        /// <summary>
        /// Mostra informazioni pH Affinity per tutte le piante attive
        /// </summary>
        private void ShowPhAffinityInfo()
        {
            if (_phSystem == null)
            {
                GUILayout.Label("PhSystem non disponibile", new GUIStyle(GUI.skin.label) { fontSize = 14 });
                return;
            }
            
            float currentPh = _phSystem.CurrentPh;
            PhSystem.PhBand phBand = _phSystem.EvaluateState();
            
            // Cerca tutte le piante attive
            PotSlot[] allPots = FindObjectsOfType<PotSlot>();
            int activePlantCount = 0;
            
            foreach (var pot in allPots)
            {
                if (pot != null && pot.PotActions != null && pot.PotActions.HasPlant)
                {
                    var potState = pot.PotActions.GetCurrentState();
                    if (potState != null && potState.HasPlant && !string.IsNullOrEmpty(potState.PlantCode))
                    {
                        activePlantCount++;
                        var plantData = potState.GetPlantData();
                        if (plantData != null)
                        {
                            bool inRange = plantData.IsPhInOptimalRange(currentPh);
                            float phDistance = plantData.GetPhDistanceFromOptimal(currentPh);
                            
                            // PlantCode e Famiglia
                            string familyName = plantData.Family.ToString();
                            GUILayout.BeginHorizontal();
                            GUILayout.Label($"  {potState.PlantCode} ({familyName}):", 
                                new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white } });
                            GUILayout.EndHorizontal();
                            
                            // Range Ottimale
                            GUILayout.Label($"    Range: {plantData.OptimalPhMin:F1} - {plantData.OptimalPhMax:F1}", 
                                new GUIStyle(GUI.skin.label) { fontSize = 13 });
                            
                            // In Range
                            Color inRangeColor = inRange ? Color.green : Color.red;
                            GUILayout.Label($"    In Range: {(inRange ? "Sì" : "No")}", 
                                new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = inRangeColor } });
                            
                            // Distanza dal Range
                            GUILayout.Label($"    Distanza: {phDistance:F2}", 
                                new GUIStyle(GUI.skin.label) { fontSize = 13 });
                            
                            // Countdown Morte
                            if (potState.ExtremePhDeathCountdown >= 0)
                            {
                                GUILayout.Label($"    ⚠️ Countdown: {potState.ExtremePhDeathCountdown} giorni", 
                                    new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = Color.red } });
                            }
                            
                            GUILayout.Space(5);
                        }
                    }
                }
            }
            
            if (activePlantCount == 0)
            {
                GUILayout.Label("  Nessuna pianta attiva", 
                    new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.gray } });
            }
        }
        
        /// <summary>
        /// BLK-02.07: Mostra breakdown dettagliato dei calcoli pH (sequenza step-by-step)
        /// </summary>
        private void ShowCalculationBreakdown()
        {
            if (_phSystem == null) return;
            
            string breakdown = _phSystem.GetCalculationBreakdown();
            
            // Rimuovi tag HTML per GUI (mantieni solo testo)
            breakdown = breakdown.Replace("<b>", "").Replace("</b>", "");
            breakdown = breakdown.Replace("<color=#FFFF00>", "").Replace("<color=#00FFFF>", "");
            breakdown = breakdown.Replace("<color=#FF00FF>", "").Replace("<color=#FFA500>", "");
            breakdown = breakdown.Replace("<color=#808080>", "").Replace("</color>", "");
            
            // Mostra pulsante per pulire il drift simulato se presente
            var contrib = _phSystem.GetContributions();
            float simulatedDrift = contrib.DailyDrift;
            if (Mathf.Abs(simulatedDrift) > 0.01f)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Drift simulato: {simulatedDrift:+#0.0;-#0.0}", new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = Color.yellow } });
                if (GUILayout.Button("Pulisci drift simulato", GUILayout.Width(210)))
                {
                    _phSystem.ClearDailyDrift();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            // BLK-02.07: Calcola altezza dinamica in base al numero di righe
            int lineCount = breakdown.Split('\n').Length;
            float dynamicHeight = Mathf.Clamp(lineCount * 20f + 20f, 100f, 400f); // Min 100px, max 400px, ~20px per riga
            
            // Mostra in area scrollabile con altezza adattiva
            var scrollStyle = new GUIStyle(GUI.skin.label) 
            { 
                fontSize = 15, // Aumentato da 12 a 15 per migliore visibilità
                normal = { textColor = Color.cyan },
                wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };
            
            var scrollRect = GUILayout.BeginScrollView(new Vector2(0, 0), false, true, GUILayout.Height(dynamicHeight));
            GUILayout.Label(breakdown, scrollStyle);
            GUILayout.EndScrollView();
            
            // Mostra anche contributi accodati (queued) se disponibili
            GUILayout.Space(3);
            GUILayout.Label($"Queued: Plants={contrib.PlantsDrift:+#0.0;-#0.0;0} Actions={contrib.ActionsDrift:+#0.0;-#0.0;0} Events={contrib.EventsDrift:+#0.0;-#0.0;0}", 
                new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.yellow } }); // Aumentato da 11 a 14

            // pH corrente separato dal drift simulato
            float phWithoutSim = _phSystem.CurrentPh - simulatedDrift;
            GUILayout.Space(3);
            GUILayout.Label($"pH Corrente (senza drift simulato): {phWithoutSim:F1}", new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = Color.white } });
            if (Mathf.Abs(simulatedDrift) > 0.01f)
            {
                GUILayout.Label($"Drift simulato: {simulatedDrift:+#0.0;-#0.0}", new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = Color.yellow } });
                GUILayout.Label($"pH Corrente (con drift simulato): {_phSystem.CurrentPh:F1}", new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = Color.white } });
            }
        }

        private void AddLog(string message)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            _debugLog.Add($"[{timestamp}] {message}");
            
            if (_debugLog.Count > MAX_LOG_ENTRIES)
            {
                _debugLog.RemoveAt(0);
            }
            
            SporiumLogger.LogDebug(LogCategory.Ph, message);
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public PhSystem GetPhSystem() => _phSystem;
    }
}

