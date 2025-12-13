using UnityEngine;
using _Project.Sporae.Core;
using Sporae.DevTools;

namespace _Project
{
    /// <summary>
    /// Componente che simula l'oscillazione organica di un sensore pH reale
    /// Oscilla casualmente tra valori intorno al valore base, simulando la precisione limitata del sensore
    /// </summary>
    public class PhSystemIdleOscillation : MonoBehaviour
    {
        [Header("Oscillation Settings")]
        [Tooltip("Abilita/disabilita oscillazione")]
        [SerializeField] private bool enableOscillation = true;
        
        [Tooltip("Velocità di oscillazione (più basso = più lento, movimento molto graduale)")]
        [SerializeField] private float oscillationSpeed = 0.15f; // Molto lento per movimento organico e graduale
        
        [Tooltip("Range minimo oscillazione rispetto al valore base (-2)")]
        [SerializeField] private float minOscillation = -2f;
        
        [Tooltip("Range massimo oscillazione rispetto al valore base (+1)")]
        [SerializeField] private float maxOscillation = 1f;
        
        [Tooltip("Intervallo tra cambi casuali di oscillazione (secondi) - più basso = cambi più frequenti e random")]
        [SerializeField] private float randomChangeInterval = 0.5f; // Cambi molto frequenti per movimento completamente casuale
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;
        
        private PhSystem _phSystem;
        private float _basePhValue; // Valore base pH senza oscillazione
        private float _targetOscillation; // Target oscillazione corrente (casuale)
        private float _currentOscillation; // Oscillazione attuale
        private float _lastRandomChangeTime;
        private float _smoothVelocity; // Per SmoothDamp
        
        private void Awake()
        {
            // Cerca PhSystem nel ServiceContainer
            TryGetPhSystem();
            
            // Sottoscrivi all'evento OnServiceRegistered per late binding
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered += OnServiceRegistered;
            }
        }
        
        private void Start()
        {
            // Riprova a ottenere PhSystem se non trovato in Awake
            if (_phSystem == null)
            {
                TryGetPhSystem();
            }
            
            if (_phSystem != null)
            {
                InitializeOscillation();
            }
            else
            {
                if (showDebugLogs)
                {
                    SporiumLogger.LogWarning(LogCategory.Ph, "PhSystem non trovato in Start(). Riproverà periodicamente.");
                }
            }
        }
        
        private void InitializeOscillation()
        {
            // Inizializza con il valore base del pH (senza oscillazione)
            _basePhValue = GetBasePhValue();
            // Inizia sempre con oscillazione a 0.0 quando si preme PLAY
            _currentOscillation = 0f;
            _targetOscillation = Random.Range(minOscillation, maxOscillation);
            _lastRandomChangeTime = Time.time;
            
            // Assicura che il sistema pH sia a 0.0
            if (_phSystem != null)
            {
                _phSystem.SetIdleOscillation(0f);
            }
            
            if (showDebugLogs)
            {
                SporiumLogger.LogInfo(LogCategory.Ph, $"✅ Inizializzato. Base pH: {_basePhValue:F2}, Oscillazione iniziale: 0.00 (sempre da 0.0)");
            }
        }
        
        private void OnServiceRegistered(object service)
        {
            if (service is PhSystem phSystem && _phSystem == null)
            {
                _phSystem = phSystem;
                InitializeOscillation();
                
                if (showDebugLogs)
                {
                    SporiumLogger.LogInfo(LogCategory.Ph, "✅ PhSystem registrato! Oscillazione attivata.");
                }
            }
        }
        
        /// <summary>
        /// Ottiene il valore base del pH (senza oscillazione) usando reflection
        /// </summary>
        private float GetBasePhValue()
        {
            try
            {
                var field = typeof(PhSystem).GetField("_currentPh", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    return (float)field.GetValue(_phSystem);
                }
            }
            catch
            {
                // Fallback: usa CurrentPh se reflection fallisce
            }
            
            return _phSystem.CurrentPh;
        }
        
        private void Update()
        {
            if (!enableOscillation)
                return;
            
            // Se PhSystem non è ancora disponibile, riprova ogni secondo
            if (_phSystem == null)
            {
                if (Time.time % 1f < 0.1f) // Ogni secondo circa
                {
                    TryGetPhSystem();
                    if (_phSystem != null)
                    {
                        InitializeOscillation();
                    }
                }
                return;
            }
            
            // Ottiene il valore base del pH (senza oscillazione)
            // Usiamo reflection per accedere al campo privato _currentPh
            float basePh = GetBasePhValue();
            
            // Aggiorna valore base se pH è stato modificato esternamente
            // (tramite azioni, drift giornaliero, ecc.)
            if (Mathf.Abs(basePh - _basePhValue) > 0.1f)
            {
                _basePhValue = basePh;
                
                if (showDebugLogs)
                {
                    SporiumLogger.LogInfo(LogCategory.Ph, $"Valore base aggiornato: {_basePhValue:F2}");
                }
            }
            
            // Simula oscillazione completamente casuale del sensore
            // Genera nuovi target casuali molto frequentemente per movimento completamente random
            if (Time.time - _lastRandomChangeTime >= randomChangeInterval)
            {
                // Genera sempre un nuovo valore completamente casuale nel range
                // Questo garantisce che il movimento non vada sempre nella stessa direzione
                _targetOscillation = Random.Range(minOscillation, maxOscillation);
                _lastRandomChangeTime = Time.time;
                
                if (showDebugLogs)
                {
                    SporiumLogger.LogDebug(LogCategory.Ph, $"Nuovo target casuale: {_targetOscillation:F2}");
                }
            }
            
            // Smooth movement molto lento verso il target casuale che cambia continuamente
            // Il target cambia molto frequentemente, quindi il movimento è sempre verso direzioni casuali
            float smoothTime = 5f / oscillationSpeed; // Tempo di smoothing molto lungo per movimento lentissimo
            _currentOscillation = Mathf.SmoothDamp(
                _currentOscillation,
                _targetOscillation,
                ref _smoothVelocity,
                smoothTime
            );
            
            // Applica oscillazione al sistema pH
            _phSystem.SetIdleOscillation(_currentOscillation);
            
            // Debug log ogni 3 secondi per verificare che funzioni
            if (showDebugLogs && Time.time % 3f < 0.1f)
            {
                SporiumLogger.LogDebug(LogCategory.Ph, $"🔄 Base: {_basePhValue:F2}, Oscillation: {_currentOscillation:F2}, Total pH: {_phSystem.CurrentPh:F2}");
            }
        }
        
        private void TryGetPhSystem()
        {
            try
            {
                var serviceContainer = ServiceContainer.Instance;
                if (serviceContainer != null && serviceContainer.Contains(typeof(PhSystem)))
                {
                    _phSystem = serviceContainer.Get<PhSystem>();
                    
                    if (_phSystem != null && showDebugLogs)
                    {
                        SporiumLogger.LogInfo(LogCategory.Ph, "PhSystem trovato nel ServiceContainer");
                    }
                }
                else
                {
                    // Prova a cercare PhSystemDebugConsole che potrebbe avere il sistema
                    var debugConsole = FindObjectOfType<PhSystemDebugConsole>();
                    if (debugConsole != null)
                    {
                        // Il debug console potrebbe avere accesso al sistema
                        // Per ora aspettiamo che venga registrato
                        if (showDebugLogs)
                        {
                            SporiumLogger.LogWarning(LogCategory.Ph, "PhSystem non ancora registrato. Riproverà in Start().");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                if (showDebugLogs)
                {
                    SporiumLogger.LogWarning(LogCategory.Ph, $"Errore nel recupero di PhSystem: {ex.Message}");
                }
            }
        }
        
        private void OnEnable()
        {
            // Riprova a ottenere PhSystem quando viene abilitato
            if (_phSystem == null)
            {
                TryGetPhSystem();
            }
            
            // Sottoscrivi all'evento OnServiceRegistered se non già fatto
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered += OnServiceRegistered;
            }
        }
        
        private void OnDisable()
        {
            // Rimuovi sottoscrizione
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered -= OnServiceRegistered;
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered -= OnServiceRegistered;
            }
        }
        
        /// <summary>
        /// Resetta l'oscillazione a 0.0 (sempre parte da 0 quando si preme PLAY)
        /// </summary>
        public void ResetOscillation()
        {
            if (_phSystem != null)
            {
                _basePhValue = GetBasePhValue();
                // Oscillazione sempre parte da 0.0 quando si preme PLAY
                _currentOscillation = 0f;
                _targetOscillation = Random.Range(minOscillation, maxOscillation);
                _lastRandomChangeTime = Time.time;
                _phSystem.SetIdleOscillation(0f);
                
                if (showDebugLogs)
                {
                    SporiumLogger.LogInfo(LogCategory.Ph, $"Oscillazione resettata a 0.0. Base pH: {_basePhValue:F2}");
                }
            }
        }
    }
}

