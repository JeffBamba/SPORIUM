using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using _Project.Sporae.Core;
using Sporae.DevTools;

namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// Database centrale per tutte le piante del gioco.
    /// Mappa ItemConfig.TypeId -> PlantData per permettere di trovare i dati pianta
    /// quando si pianta un seme dall'inventario.
    /// </summary>
    public class PlantDatabase : MonoBehaviour
    {
        private static PlantDatabase _instance;
        
        [Header("Plant Data Assets")]
        [Tooltip("Lista di tutti i PlantData ScriptableObject del gioco")]
        [SerializeField] private List<PlantData> allPlantData = new List<PlantData>();
        
        // Mappa per lookup veloce: ItemConfig.TypeId -> PlantData
        private readonly Dictionary<string, PlantData> _plantDataBySeedTypeId = new Dictionary<string, PlantData>();
        
        // Mappa per lookup veloce: PlantCode -> PlantData
        private readonly Dictionary<string, PlantData> _plantDataByCode = new Dictionary<string, PlantData>();
        private readonly HashSet<string> _discoveredPlantCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private const string DiscoveryPrefsKey = "Sporae_DiscoveredPlantCodes";
        
        private bool _isInitialized = false;
        public event Action<string> OnPlantDiscovered;
        
        public static PlantDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Prova a ottenere da ServiceContainer (suppress warning durante inizializzazione)
                    if (ServiceContainer.Instance != null)
                    {
                        _instance = ServiceContainer.Instance.Get<PlantDatabase>(suppressWarning: true);
                    }
                    
                    // Fallback a FindObjectOfType se ServiceContainer non disponibile
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<PlantDatabase>();
                    }
                    
                    // Se non trovato, crea nuova istanza
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("PlantDatabase");
                        _instance = go.AddComponent<PlantDatabase>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                
                // DEBUG_SAFE_FIX: DontDestroyOnLoad funziona solo su root GameObjects
                // Se questo GameObject non è root, spostalo alla root prima di chiamare DontDestroyOnLoad
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                DontDestroyOnLoad(gameObject);
                
                // Registra nel ServiceContainer se disponibile
                if (ServiceContainer.Instance != null)
                {
                    ServiceContainer.Instance.Register(this);
                }
                
                InitializeDatabase();
                LoadDiscoveryState();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Inizializza il database caricando tutti i PlantData
        /// </summary>
        private void InitializeDatabase()
        {
            if (_isInitialized)
                return;
            
            // Se la lista è vuota, prova a caricare da Resources tramite AssetManager
            if (allPlantData.Count == 0)
            {
                PlantData[] loadedData = AssetManager.Instance.LoadAllAssets<PlantData>("Plants");
                allPlantData.AddRange(loadedData);
                
                if (loadedData.Length > 0)
                {
                    SporiumLogger.LogInfo(LogCategory.Pot, $"Caricati {loadedData.Length} PlantData da Resources/Plants/");
                }
            }
            
            // Costruisci le mappe di lookup
            foreach (var plantData in allPlantData)
            {
                if (plantData == null)
                    continue;
                
                // Mappa per TypeId del seme
                if (plantData.SeedItemConfig != null)
                {
                    string typeId = plantData.SeedItemConfig.TypeId;
                    if (!string.IsNullOrEmpty(typeId))
                    {
                        if (_plantDataBySeedTypeId.ContainsKey(typeId))
                        {
                            SporiumLogger.LogWarning(LogCategory.Pot, $"Duplicato TypeId '{typeId}' trovato! Sovrascritto con {plantData.name}");
                        }
                        _plantDataBySeedTypeId[typeId] = plantData;
                    }
                }
                
                // Mappa per PlantCode
                if (!string.IsNullOrEmpty(plantData.PlantCode))
                {
                    if (_plantDataByCode.ContainsKey(plantData.PlantCode))
                    {
                        SporiumLogger.LogWarning(LogCategory.Pot, $"Duplicato PlantCode '{plantData.PlantCode}' trovato! Sovrascritto con {plantData.name}");
                    }
                    _plantDataByCode[plantData.PlantCode] = plantData;
                }
            }
            
            _isInitialized = true;
            SporiumLogger.LogInfo(LogCategory.Pot, $"Inizializzato: {_plantDataBySeedTypeId.Count} piante registrate");
        }

        private void LoadDiscoveryState()
        {
            _discoveredPlantCodes.Clear();
            var raw = PlayerPrefs.GetString(DiscoveryPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
                return;
            var parts = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var code = p.Trim();
                if (!string.IsNullOrEmpty(code))
                    _discoveredPlantCodes.Add(code);
            }
        }

        private void SaveDiscoveryState()
        {
            var raw = string.Join(";", _discoveredPlantCodes.OrderBy(x => x));
            PlayerPrefs.SetString(DiscoveryPrefsKey, raw);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Trova PlantData in base al TypeId del seme (ItemConfig.TypeId)
        /// </summary>
        public PlantData GetPlantDataBySeedTypeId(string seedTypeId)
        {
            if (!_isInitialized)
                InitializeDatabase();
            
            if (string.IsNullOrEmpty(seedTypeId))
                return null;
            
            _plantDataBySeedTypeId.TryGetValue(seedTypeId, out PlantData plantData);
            return plantData;
        }
        
        /// <summary>
        /// Trova PlantData in base al PlantCode (es. PLT-STD-001)
        /// </summary>
        public PlantData GetPlantDataByCode(string plantCode)
        {
            if (!_isInitialized)
                InitializeDatabase();
            
            if (string.IsNullOrEmpty(plantCode))
                return null;
            
            _plantDataByCode.TryGetValue(plantCode, out PlantData plantData);
            return plantData;
        }
        
        /// <summary>
        /// Trova PlantData in base all'ItemConfig del seme
        /// </summary>
        public PlantData GetPlantDataBySeedItemConfig(ItemConfig seedItemConfig)
        {
            if (seedItemConfig == null)
                return null;
            
            return GetPlantDataBySeedTypeId(seedItemConfig.TypeId);
        }
        
        /// <summary>
        /// Registra manualmente un PlantData (utile per setup runtime)
        /// </summary>
        public void RegisterPlantData(PlantData plantData)
        {
            if (plantData == null)
                return;
            
            if (!allPlantData.Contains(plantData))
            {
                allPlantData.Add(plantData);
            }
            
            // Aggiorna le mappe
            if (plantData.SeedItemConfig != null)
            {
                string typeId = plantData.SeedItemConfig.TypeId;
                if (!string.IsNullOrEmpty(typeId))
                {
                    _plantDataBySeedTypeId[typeId] = plantData;
                }
            }
            
            if (!string.IsNullOrEmpty(plantData.PlantCode))
            {
                _plantDataByCode[plantData.PlantCode] = plantData;
            }
            
            SporiumLogger.LogInfo(LogCategory.Pot, $"Registrato PlantData: {plantData.PlantCode} ({plantData.name})");
        }
        
        /// <summary>
        /// Ottiene tutte le piante di una famiglia specifica
        /// </summary>
        public List<PlantData> GetPlantsByFamily(PlantFamily family)
        {
            if (!_isInitialized)
                InitializeDatabase();
            
            List<PlantData> result = new List<PlantData>();
            foreach (var plantData in allPlantData)
            {
                if (plantData != null && plantData.Family == family)
                {
                    result.Add(plantData);
                }
            }
            return result;
        }

        public bool IsPlantCodeDiscovered(string plantCode)
        {
            if (string.IsNullOrWhiteSpace(plantCode))
                return false;
            return _discoveredPlantCodes.Contains(plantCode.Trim());
        }

        public bool MarkPlantCodeDiscovered(string plantCode)
        {
            if (string.IsNullOrWhiteSpace(plantCode))
                return false;
            string code = plantCode.Trim();
            if (!_plantDataByCode.ContainsKey(code))
                return false;
            if (_discoveredPlantCodes.Add(code))
            {
                SaveDiscoveryState();
                OnPlantDiscovered?.Invoke(code);
                return true;
            }
            return false;
        }

        public int MarkPlantCodesDiscoveredFromMetadata(string sourcePlantCodesMetadata)
        {
            if (string.IsNullOrWhiteSpace(sourcePlantCodesMetadata))
                return 0;

            int unlocked = 0;
            var tokens = sourcePlantCodesMetadata.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (MarkPlantCodeDiscovered(token))
                    unlocked++;
            }
            return unlocked;
        }
        
        /// <summary>
        /// Ottiene il numero totale di piante registrate
        /// </summary>
        public int GetTotalPlantCount()
        {
            if (!_isInitialized)
                InitializeDatabase();
            
            return _plantDataBySeedTypeId.Count;
        }
        
        #if UNITY_EDITOR
        [ContextMenu("Reload Database")]
        private void EditorReloadDatabase()
        {
            _plantDataBySeedTypeId.Clear();
            _plantDataByCode.Clear();
            _isInitialized = false;
            InitializeDatabase();
            SporiumLogger.LogInfo(LogCategory.Pot, $"Database ricaricato: {_plantDataBySeedTypeId.Count} piante");
        }
        
        [ContextMenu("Log All Plants")]
        private void EditorLogAllPlants()
        {
            if (!_isInitialized)
                InitializeDatabase();
            
            SporiumLogger.LogInfo(LogCategory.Pot, $"=== TUTTE LE PIANTE REGISTRATE ({_plantDataBySeedTypeId.Count}) ===");
            foreach (var kvp in _plantDataBySeedTypeId)
            {
                var plantData = kvp.Value;
                SporiumLogger.LogDebug(LogCategory.Pot, $"  [{kvp.Key}] {plantData.PlantCode} - {plantData.Family} - Drift pH: {plantData.DailyPhDrift}/giorno");
            }
        }
        #endif
    }
}

