using System.Collections.Generic;
using UnityEngine;
using _Project.Sporae.Core;

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
        
        private bool _isInitialized = false;
        
        public static PlantDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Prova a ottenere da ServiceContainer
                    if (ServiceContainer.Instance != null)
                    {
                        _instance = ServiceContainer.Instance.Get<PlantDatabase>();
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
                DontDestroyOnLoad(gameObject);
                
                // Registra nel ServiceContainer se disponibile
                if (ServiceContainer.Instance != null)
                {
                    ServiceContainer.Instance.Register(this);
                }
                
                InitializeDatabase();
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
                    Debug.Log($"[PlantDatabase] Caricati {loadedData.Length} PlantData da Resources/Plants/");
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
                            Debug.LogWarning($"[PlantDatabase] Duplicato TypeId '{typeId}' trovato! Sovrascritto con {plantData.name}");
                        }
                        _plantDataBySeedTypeId[typeId] = plantData;
                    }
                }
                
                // Mappa per PlantCode
                if (!string.IsNullOrEmpty(plantData.PlantCode))
                {
                    if (_plantDataByCode.ContainsKey(plantData.PlantCode))
                    {
                        Debug.LogWarning($"[PlantDatabase] Duplicato PlantCode '{plantData.PlantCode}' trovato! Sovrascritto con {plantData.name}");
                    }
                    _plantDataByCode[plantData.PlantCode] = plantData;
                }
            }
            
            _isInitialized = true;
            Debug.Log($"[PlantDatabase] Inizializzato: {_plantDataBySeedTypeId.Count} piante registrate");
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
            
            Debug.Log($"[PlantDatabase] Registrato PlantData: {plantData.PlantCode} ({plantData.name})");
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
            Debug.Log($"[PlantDatabase] Database ricaricato: {_plantDataBySeedTypeId.Count} piante");
        }
        
        [ContextMenu("Log All Plants")]
        private void EditorLogAllPlants()
        {
            if (!_isInitialized)
                InitializeDatabase();
            
            Debug.Log($"[PlantDatabase] === TUTTE LE PIANTE REGISTRATE ({_plantDataBySeedTypeId.Count}) ===");
            foreach (var kvp in _plantDataBySeedTypeId)
            {
                var plantData = kvp.Value;
                Debug.Log($"  [{kvp.Key}] {plantData.PlantCode} - {plantData.Family} - Drift pH: {plantData.DailyPhDrift}/giorno");
            }
        }
        #endif
    }
}

