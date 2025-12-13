using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using _Project;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using Sporae.DevTools;

namespace Sporae.Core
{
    /// <summary>
    /// Sistema di salvataggio e caricamento completo per il gioco.
    /// Salva: stato gioco, inventario, vasi, statistiche, missioni.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        private static SaveManager _instance;
        private const string SAVE_FILE_NAME = "sporium_save.json";
        private const string SAVE_KEY_PREFIX = "Sporium_Save_";
        
        public static SaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Prova a ottenere da ServiceContainer (suppress warning durante inizializzazione)
                    if (ServiceContainer.Instance != null)
                    {
                        _instance = ServiceContainer.Instance.Get<SaveManager>(suppressWarning: true);
                    }
                    
                    // Se non trovato, crea nuova istanza
                    if (_instance == null)
                    {
                        var go = new GameObject("SaveManager");
                        _instance = go.AddComponent<SaveManager>();
                        DontDestroyOnLoad(go);
                        
                        // Registra nel ServiceContainer se disponibile
                        if (ServiceContainer.Instance != null)
                        {
                            ServiceContainer.Instance.Register(_instance);
                        }
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
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Salva lo stato completo del gioco.
        /// </summary>
        public bool SaveGame(string slotName = "default")
        {
            try
            {
                var saveData = CollectSaveData();
                string json = JsonUtility.ToJson(saveData, true);
                
                // Salva su file system (più robusto di PlayerPrefs per dati complessi)
                string savePath = GetSaveFilePath(slotName);
                File.WriteAllText(savePath, json);
                
                // Salva anche in PlayerPrefs come backup/metadata
                PlayerPrefs.SetString($"{SAVE_KEY_PREFIX}{slotName}", json);
                PlayerPrefs.SetString($"{SAVE_KEY_PREFIX}{slotName}_timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                PlayerPrefs.Save();
                
#if UNITY_EDITOR
                SporiumLogger.LogInfo(LogCategory.Save, $"Gioco salvato con successo: {slotName}");
#endif
                return true;
            }
            catch (Exception ex)
            {
                SporiumLogger.LogError(LogCategory.Save, $"Errore durante il salvataggio: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Carica lo stato completo del gioco.
        /// </summary>
        public bool LoadGame(string slotName = "default")
        {
            try
            {
                string json = null;
                
                // Prova prima a caricare da file system
                string savePath = GetSaveFilePath(slotName);
                if (File.Exists(savePath))
                {
                    json = File.ReadAllText(savePath);
                }
                // Fallback a PlayerPrefs
                else if (PlayerPrefs.HasKey($"{SAVE_KEY_PREFIX}{slotName}"))
                {
                    json = PlayerPrefs.GetString($"{SAVE_KEY_PREFIX}{slotName}");
                }
                
                if (string.IsNullOrEmpty(json))
                {
                    SporiumLogger.LogWarning(LogCategory.Save, $"Nessun salvataggio trovato: {slotName}");
                    return false;
                }
                
                var saveData = JsonUtility.FromJson<GameSaveData>(json);
                ApplySaveData(saveData);
                
#if UNITY_EDITOR
                SporiumLogger.LogInfo(LogCategory.Save, $"Gioco caricato con successo: {slotName}");
#endif
                return true;
            }
            catch (Exception ex)
            {
                SporiumLogger.LogError(LogCategory.Save, $"Errore durante il caricamento: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Elimina un salvataggio specifico.
        /// </summary>
        public bool DeleteSave(string slotName = "default")
        {
            try
            {
                // Elimina file
                string savePath = GetSaveFilePath(slotName);
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                }
                
                // Elimina PlayerPrefs
                PlayerPrefs.DeleteKey($"{SAVE_KEY_PREFIX}{slotName}");
                PlayerPrefs.DeleteKey($"{SAVE_KEY_PREFIX}{slotName}_timestamp");
                PlayerPrefs.Save();
                
#if UNITY_EDITOR
                SporiumLogger.LogInfo(LogCategory.Save, $"Salvataggio eliminato: {slotName}");
#endif
                return true;
            }
            catch (Exception ex)
            {
                SporiumLogger.LogError(LogCategory.Save, $"Errore durante l'eliminazione: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Verifica se esiste un salvataggio per lo slot specificato.
        /// </summary>
        public bool SaveExists(string slotName = "default")
        {
            string savePath = GetSaveFilePath(slotName);
            return File.Exists(savePath) || PlayerPrefs.HasKey($"{SAVE_KEY_PREFIX}{slotName}");
        }
        
        /// <summary>
        /// Ottiene la data/ora dell'ultimo salvataggio.
        /// </summary>
        public string GetSaveTimestamp(string slotName = "default")
        {
            if (PlayerPrefs.HasKey($"{SAVE_KEY_PREFIX}{slotName}_timestamp"))
            {
                return PlayerPrefs.GetString($"{SAVE_KEY_PREFIX}{slotName}_timestamp");
            }
            return "N/A";
        }
        
        /// <summary>
        /// Raccoglie tutti i dati da salvare.
        /// </summary>
        private GameSaveData CollectSaveData()
        {
            var saveData = new GameSaveData();
            
            // Stato del gioco
            // BUG FIX: Controllo più robusto per ServiceContainer
            var gameManager = ServiceContainer.Instance?.Get<GameManager>();
            if (gameManager != null)
            {
                var dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>();
                saveData.gameState = new GameStateData
                {
                    currentDay = dayCycleSystem?.CurrentDay ?? 1,
                    currentCRY = gameManager.CurrentCRY,
                    actionsLeft = gameManager.ActionsLeft,
                    condensationAmount = gameManager.CondensationSystem?.CondensationAmount ?? 0f
                };
            }
            
            // Inventario del giocatore
            if (gameManager != null && gameManager.PlayerInventory != null)
            {
                saveData.inventory = SerializeInventory(gameManager.PlayerInventory);
            }
            
            // Stato dei vasi
            saveData.pots = CollectPotStates();
            
            // Statistiche del diario (se disponibile)
            var diaryStats = ServiceContainer.Instance?.Get<DiaryStatistics>();
            if (diaryStats != null)
            {
                saveData.diaryStatistics = SerializeDiaryStatistics(diaryStats);
            }
            
            // Missioni completate (se disponibile)
            var missionManager = ServiceContainer.Instance?.Get<MissionManager>();
            if (missionManager != null)
            {
                saveData.missions = SerializeMissions(missionManager);
            }
            
            saveData.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            saveData.gameVersion = Application.version;
            
            return saveData;
        }
        
        /// <summary>
        /// Applica i dati caricati al gioco.
        /// </summary>
        private void ApplySaveData(GameSaveData saveData)
        {
            // Stato del gioco
            // DEBUG_SAFE_FIX: Suppress warning durante inizializzazione (GameManager potrebbe non essere ancora registrato)
            var gameManager = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
            var dayCycleSystem = ServiceContainer.Instance?.Get<DayCycleSystem>(suppressWarning: true);
            
            if (dayCycleSystem != null && saveData.gameState.currentDay > 0)
            {
                // Nota: CurrentDay è privato, potrebbe essere necessario aggiungere un setter
                // Per ora, il sistema di giorni si aggiornerà naturalmente
            }
            
            if (gameManager != null && saveData.gameState != null)
            {
                // Ripristina CRY usando EconomySystem
                if (gameManager.EconomySystem != null)
                {
                    gameManager.EconomySystem.RestoreState(saveData.gameState.currentCRY);
                }
                
                // Ripristina azioni usando ActionSystem
                if (gameManager.ActionSystem != null)
                {
                    // Usa maxActions dal sistema attuale se non salvato
                    int maxActions = gameManager.ActionSystem.MaxActions;
                    gameManager.ActionSystem.RestoreState(saveData.gameState.actionsLeft, maxActions);
                }
            }
            
            // Inventario
            if (gameManager != null && gameManager.PlayerInventory != null && saveData.inventory != null)
            {
                DeserializeInventory(gameManager.PlayerInventory, saveData.inventory);
            }
            
            // Vasi
            if (saveData.pots != null && saveData.pots.Count > 0)
            {
                ApplyPotStates(saveData.pots);
            }
            
            // Statistiche e missioni vengono ripristinate quando i sistemi vengono caricati
        }
        
        /// <summary>
        /// Raccoglie lo stato di tutti i vasi nella scena.
        /// </summary>
        private List<PotStateData> CollectPotStates()
        {
            var potStates = new List<PotStateData>();
            
            // Trova tutti i PotActions nella scena
            var potActions = FindObjectsOfType<PotActions>();
            foreach (var potAction in potActions)
            {
                var potState = potAction.GetCurrentState();
                if (potState != null)
                {
                    potStates.Add(new PotStateData
                    {
                        potId = potState.PotId,
                        hasPlant = potState.HasPlant,
                        stage = potState.Stage,
                        amountFruits = potState.AmountFruits,
                        plantCode = potState.PlantCode,
                        hydration = potState.Hydration,
                        lightExposure = potState.LightExposure,
                        growthPoints = potState.GrowthPoints,
                        daysSincePlant = potState.DaysSincePlant,
                        daysNeglectedStreak = potState.DaysNeglectedStreak,
                        daysInCurrentStage = potState.DaysInCurrentStage,
                        daysInHarvestReady = potState.DaysInHarvestReady,
                        daysFruitsUnharvested = potState.DaysFruitsUnharvested,
                        plantedDay = potState.PlantedDay,
                        lastWateredDay = potState.LastWateredDay,
                        lastLitDay = potState.LastLitDay,
                        lastLedType = potState.LastLedType?.ToString(),
                        // BLK-02.07: Nuovi campi per sistema LED persistente
                        ledSystemState = potState.LedSystemState.ToString(),
                        daysLedBlueConsecutive = potState.DaysLedBlueConsecutive,
                        daysLedRedConsecutive = potState.DaysLedRedConsecutive,
                        // GDD AZ-11: Nuovi campi per sistema irrigazione toggle
                        wateringSystemOn = potState.WateringSystemOn,
                        daysWateringSystemOn = potState.DaysWateringSystemOn,
                        wateringRawWaterAccumulator = potState.WateringRawWaterAccumulator
                    });
                }
            }
            
            return potStates;
        }
        
        /// <summary>
        /// Applica lo stato salvato ai vasi.
        /// </summary>
        private void ApplyPotStates(List<PotStateData> potStates)
        {
            var potActions = FindObjectsOfType<PotActions>();
            
            foreach (var potStateData in potStates)
            {
                // Trova il vaso corrispondente
                foreach (var potAction in potActions)
                {
                    var potState = potAction.GetCurrentState();
                    if (potState != null && potState.PotId == potStateData.potId)
                    {
                        // Applica lo stato
                        potState.HasPlant = potStateData.hasPlant;
                        potState.Stage = potStateData.stage;
                        potState.AmountFruits = potStateData.amountFruits;
                        potState.PlantCode = potStateData.plantCode;
                        potState.Hydration = potStateData.hydration;
                        potState.LightExposure = potStateData.lightExposure;
                        potState.GrowthPoints = potStateData.growthPoints;
                        potState.DaysSincePlant = potStateData.daysSincePlant;
                        potState.DaysNeglectedStreak = potStateData.daysNeglectedStreak;
                        potState.DaysInCurrentStage = potStateData.daysInCurrentStage;
                        potState.DaysInHarvestReady = potStateData.daysInHarvestReady;
                        potState.DaysFruitsUnharvested = potStateData.daysFruitsUnharvested;
                        potState.PlantedDay = potStateData.plantedDay;
                        potState.LastWateredDay = potStateData.lastWateredDay;
                        potState.LastLitDay = potStateData.lastLitDay;
                        
                        // MIGRAZIONE: Converti LastLedType a LedSystemState se presente
                        if (!string.IsNullOrEmpty(potStateData.lastLedType))
                        {
                            if (Enum.TryParse<LedType>(potStateData.lastLedType, out var ledType))
                            {
                                potState.LastLedType = ledType;  // Mantieni per compatibilità
                                
                                // Converti a nuovo sistema
                                if (ledType == LedType.Blue)
                                    potState.LedSystemState = LedSystemState.Blue;
                                else if (ledType == LedType.Red)
                                    potState.LedSystemState = LedSystemState.Red;
                                // Se null, rimane Off (default)
                            }
                        }
                        
                        // BLK-02.07: Applica nuovi campi sistema LED (con default se mancanti - migrazione automatica)
                        if (Enum.TryParse<LedSystemState>(potStateData.ledSystemState, out var ledState))
                            potState.LedSystemState = ledState;
                        else
                            potState.LedSystemState = LedSystemState.Off;  // Default se parsing fallisce
                        
                        potState.DaysLedBlueConsecutive = potStateData.daysLedBlueConsecutive;
                        potState.DaysLedRedConsecutive = potStateData.daysLedRedConsecutive;
                        
                        // GDD AZ-11: Applica nuovi campi sistema irrigazione (con default per migration)
                        // JSON serialization gestisce automaticamente i default se i campi mancano
                        potState.WateringSystemOn = potStateData.wateringSystemOn;
                        potState.DaysWateringSystemOn = potStateData.daysWateringSystemOn;
                        potState.WateringRawWaterAccumulator = potStateData.wateringRawWaterAccumulator;
                        
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Serializza l'inventario in formato salvabile.
        /// </summary>
        private InventoryData SerializeInventory(Inventory inventory)
        {
            var inventoryData = new InventoryData();
            inventoryData.items = new List<InventoryItemData>();
            
            foreach (var slot in inventory.Items)
            {
                inventoryData.items.Add(new InventoryItemData
                {
                    typeId = slot.TypeId,
                    quantity = slot.Quantity
                });
            }
            
            return inventoryData;
        }
        
        /// <summary>
        /// Deserializza l'inventario dal formato salvato.
        /// </summary>
        private void DeserializeInventory(Inventory inventory, InventoryData inventoryData)
        {
            inventory.Clear();
            
            foreach (var itemData in inventoryData.items)
            {
                inventory.Add(itemData.typeId, itemData.quantity);
            }
        }
        
        /// <summary>
        /// Serializza le statistiche del diario.
        /// </summary>
        private DiaryStatisticsData SerializeDiaryStatistics(DiaryStatistics diaryStats)
        {
            // Implementa serializzazione quando DiaryStatistics ha dati serializzabili
            return new DiaryStatisticsData();
        }
        
        /// <summary>
        /// Serializza le missioni completate.
        /// </summary>
        private MissionsData SerializeMissions(MissionManager missionManager)
        {
            // Implementa serializzazione quando MissionManager ha dati serializzabili
            return new MissionsData();
        }
        
        /// <summary>
        /// Ottiene il percorso completo del file di salvataggio.
        /// </summary>
        private string GetSaveFilePath(string slotName)
        {
            string saveDir = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }
            
            return Path.Combine(saveDir, $"{SAVE_FILE_NAME}_{slotName}");
        }
        
        // Strutture dati per serializzazione
        
        [Serializable]
        private class GameSaveData
        {
            public GameStateData gameState;
            public InventoryData inventory;
            public List<PotStateData> pots;
            public DiaryStatisticsData diaryStatistics;
            public MissionsData missions;
            public string saveTimestamp;
            public string gameVersion;
        }
        
        [Serializable]
        private class GameStateData
        {
            public int currentDay;
            public int currentCRY;
            public int actionsLeft;
            public float condensationAmount;
        }
        
        [Serializable]
        private class InventoryData
        {
            public List<InventoryItemData> items;
        }
        
        [Serializable]
        private class InventoryItemData
        {
            public string typeId;
            public int quantity;
        }
        
        [Serializable]
        private class PotStateData
        {
            public string potId;
            public bool hasPlant;
            public int stage;
            public float amountFruits;
            public string plantCode;
            public int hydration;
            public int lightExposure;
            public int growthPoints;
            public int daysSincePlant;
            public int daysNeglectedStreak;
            public int daysInCurrentStage;
            public int daysInHarvestReady;
            public int daysFruitsUnharvested;
            public int plantedDay;
            public int lastWateredDay;
            public int lastLitDay;
            public string lastLedType;  // Legacy
            
            // BLK-02.07: Nuovi campi per sistema LED persistente
            public string ledSystemState = "Off";  // Default per salvataggi vecchi
            public int daysLedBlueConsecutive = 0;
            public int daysLedRedConsecutive = 0;
            
            // GDD AZ-11: Nuovi campi per sistema irrigazione toggle persistente
            public bool wateringSystemOn = false;  // Default per migration salvataggi vecchi
            public int daysWateringSystemOn = 0;
            public float wateringRawWaterAccumulator = 0f;
        }
        
        [Serializable]
        private class DiaryStatisticsData
        {
            // Aggiungi campi quando DiaryStatistics è implementato
        }
        
        [Serializable]
        private class MissionsData
        {
            // Aggiungi campi quando MissionManager ha dati serializzabili
        }
    }
}
