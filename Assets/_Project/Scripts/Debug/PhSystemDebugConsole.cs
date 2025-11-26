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
        private bool _isConsoleOpen = false;
        private string _phInputValue = "0";
        private Vector2 _scrollPosition;
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
            // Crea sistema pH se non esiste
            _phSystem = new PhSystem(0f);
            _phSystem.OnPhChanged += OnPhChanged;

            _isConsoleOpen = showOnStart;
            
            #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enableDebugConsole = false;
            #endif
            
            Debug.Log($"[pH Debug Console] Awake - enableDebugConsole: {enableDebugConsole}, toggleKey: {toggleKey}, showOnStart: {showOnStart}");
        }

        private void Start()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            // Registra nel ServiceContainer se disponibile (dopo che è stato inizializzato)
            TryRegisterPhSystem();

            AddLog("=== pH System Debug Console ===");
            AddLog("Premi Z per aprire/chiudere la console");
            AddLog($"pH iniziale: {_phSystem.CurrentPh:F2} ({_phSystem.GetBandName()})");
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
                    Debug.LogWarning("[pH Debug Console] Tasto Z premuto ma enableDebugConsole è FALSE!");
                }
                return;
            }

            if (Input.GetKeyDown(toggleKey))
            {
                _isConsoleOpen = !_isConsoleOpen;
                AddLog(_isConsoleOpen ? "Console aperta" : "Console chiusa");
                Debug.Log($"[pH Debug Console] Console {( _isConsoleOpen ? "aperta" : "chiusa")} - Tasto {toggleKey} premuto");
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
            labelStyle.fontSize = 12;

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 11;

            // Area console principale
            float consoleWidth = 600f;
            float consoleHeight = 500f;
            Rect consoleRect = new Rect(Screen.width - consoleWidth - 10, 10, consoleWidth, consoleHeight);

            GUILayout.BeginArea(consoleRect, boxStyle);
            GUILayout.BeginVertical();

            // Header
            GUILayout.Label("🧪 pH SYSTEM DEBUG CONSOLE", labelStyle);
            GUILayout.Space(5);

            // Stato corrente pH
            var bandColor = _phSystem.GetBandColor();
            var oldColor = GUI.color;
            GUI.color = bandColor;
            GUILayout.Label($"pH Corrente: {_phSystem.CurrentPh:F2}", labelStyle);
            GUI.color = oldColor;
            GUILayout.Label($"Banda: {_phSystem.GetBandName()}", labelStyle);
            GUILayout.Space(10);

            // Input manuale pH
            GUILayout.BeginHorizontal();
            GUILayout.Label("Imposta pH:", labelStyle, GUILayout.Width(100));
            _phInputValue = GUILayout.TextField(_phInputValue, GUILayout.Width(100));
            if (GUILayout.Button("Applica", buttonStyle, GUILayout.Width(80)))
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
            GUILayout.Space(5);

            // Valori rapidi
            GUILayout.Label("Valori Rapidi:", labelStyle);
            GUILayout.BeginHorizontal();
            foreach (var kvp in _quickValues)
            {
                if (GUILayout.Button(kvp.Key, buttonStyle, GUILayout.Width(110)))
                {
                    SetQuickPh(kvp.Key);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            // Modifiche incrementali
            GUILayout.Label("Modifiche Incrementali:", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-10", buttonStyle, GUILayout.Width(60))) _phSystem.ApplyInstantDelta(-10f, "Debug_Manual");
            if (GUILayout.Button("-5", buttonStyle, GUILayout.Width(60))) _phSystem.ApplyInstantDelta(-5f, "Debug_Manual");
            if (GUILayout.Button("+5", buttonStyle, GUILayout.Width(60))) _phSystem.ApplyInstantDelta(+5f, "Debug_Manual");
            if (GUILayout.Button("+10", buttonStyle, GUILayout.Width(60))) _phSystem.ApplyInstantDelta(+10f, "Debug_Manual");
            if (GUILayout.Button("Reset", buttonStyle, GUILayout.Width(80))) _phSystem.Reset();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            // Simulazioni
            GUILayout.Label("Simulazioni:", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Drift Giornaliero", buttonStyle, GUILayout.Width(120)))
            {
                SimulateDailyDrift();
            }
            if (GUILayout.Button("Overwatering", buttonStyle, GUILayout.Width(120)))
            {
                _phSystem.ApplyInstantDelta(-5f, "Action_Watering_Over");
                AddLog("Simulato Overwatering: pH -5");
            }
            if (GUILayout.Button("LED Blu", buttonStyle, GUILayout.Width(120)))
            {
                _phSystem.ApplyInstantDelta(+5f, "Action_LED_Blue");
                AddLog("Simulato LED Blu: pH +5");
            }
            if (GUILayout.Button("LED Rosso", buttonStyle, GUILayout.Width(120)))
            {
                _phSystem.ApplyInstantDelta(-5f, "Action_LED_Red");
                AddLog("Simulato LED Rosso: pH -5");
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            // Effetti su piante
            GUILayout.Label("Effetti su Piante:", labelStyle);
            ShowPlantEffects();
            GUILayout.Space(5);

            // Log debug
            GUILayout.Label("Log:", labelStyle);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));
            foreach (var logEntry in _debugLog)
            {
                GUILayout.Label(logEntry, labelStyle);
            }
            GUILayout.EndScrollView();

            // Hotkeys info
            GUILayout.Space(5);
            GUILayout.Label("Hotkeys: 1-5=Valori rapidi | +/- = ±5 | R=Reset | D=Drift", 
                new GUIStyle(GUI.skin.label) { fontSize = 10, normal = { textColor = Color.gray } });

            GUILayout.EndVertical();
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

            GUILayout.Label($"  {effects}", new GUIStyle(GUI.skin.label) { fontSize = 11 });
        }

        private void AddLog(string message)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            _debugLog.Add($"[{timestamp}] {message}");
            
            if (_debugLog.Count > MAX_LOG_ENTRIES)
            {
                _debugLog.RemoveAt(0);
            }
            
            Debug.Log($"[pH Debug] {message}");
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

