using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using _Project;
using _Project.Sporae.Core;
using _Project.Systems.FoodRoom;
using Sporae.Dome.PotSystem.Growth;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.PlantCard.Components;

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
                string timestampStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                PlayerPrefs.SetString($"{SAVE_KEY_PREFIX}{slotName}_timestamp", timestampStr);

                // Riepilogo per UI (Giorno, CRY, Piante in Dome)
                int plantsInDome = saveData.pots != null ? saveData.pots.Count(p => p.hasPlant) : 0;
                var summary = new SaveSlotSummary
                {
                    day = saveData.gameState?.currentDay ?? 1,
                    cry = saveData.gameState?.currentCRY ?? 0,
                    plantsInDome = plantsInDome,
                    timestamp = timestampStr
                };
                PlayerPrefs.SetString($"{SAVE_KEY_PREFIX}{slotName}_summary", JsonUtility.ToJson(summary));

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
                if (saveData == null)
                {
                    SporiumLogger.LogError(LogCategory.Save, "Salvataggio corrotto: deserializzazione restituita null");
                    return false;
                }
                if (saveData.gameState == null)
                {
                    SporiumLogger.LogWarning(LogCategory.Save, "Salvataggio senza gameState: formato vecchio o corrotto. Ripristino parziale.");
                    saveData.gameState = new GameStateData
                    {
                        currentDay = 1,
                        currentCRY = 250,
                        actionsLeft = 4,
                        condensationAmount = 0f,
                        dehydrationZeroDayStreak = 0,
                        consecutiveDaysWithoutMeal = 0,
                        starvationDaysAtMinCapWithoutFood = 0,
                        ateMealSincePreviousDawn = false
                    };
                }
                if (saveData.inventoryVersion <= 0)
                    saveData.inventoryVersion = 1;
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
                PlayerPrefs.DeleteKey($"{SAVE_KEY_PREFIX}{slotName}_summary");
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
        /// Se impostato, alla prossima entrata nella scena di gioco verrà caricato questo slot invece di "default".
        /// Usato quando l'utente sceglie "Carica" dal menu principale: si carica la scena e poi si applica il save.
        /// </summary>
        public static string SlotToLoadOnNextScene { get; set; }

        /// <summary>
        /// Nomi slot disponibili per salvare/caricare.
        /// </summary>
        public static readonly string[] SlotNames = { "default", "slot2", "slot3" };

        /// <summary>
        /// Nome visualizzato per uno slot (es. "Slot 1", "Slot 2").
        /// </summary>
        public static string GetSlotDisplayName(string slotName)
        {
            for (int i = 0; i < SlotNames.Length; i++)
                if (SlotNames[i] == slotName)
                    return $"Slot {i + 1}";
            return slotName;
        }

        /// <summary>
        /// Riepilogo partita salvata (per UI Load/Save).
        /// </summary>
        [Serializable]
        public struct SaveSlotSummary
        {
            public int day;
            public int cry;
            public int plantsInDome;
            public string timestamp;
        }

        /// <summary>
        /// Ottiene il riepilogo di una partita salvata senza caricarla (Giorno, CRY, Piante in Dome).
        /// </summary>
        public SaveSlotSummary? GetSaveSummary(string slotName)
        {
            if (!SaveExists(slotName)) return null;

            string summaryKey = $"{SAVE_KEY_PREFIX}{slotName}_summary";
            if (PlayerPrefs.HasKey(summaryKey))
            {
                try
                {
                    var s = JsonUtility.FromJson<SaveSlotSummary>(PlayerPrefs.GetString(summaryKey));
                    return s;
                }
                catch { /* ignore */ }
            }

            // Salvataggio vecchio senza summary: leggi da file
            try
            {
                string json = null;
                string savePath = GetSaveFilePath(slotName);
                if (File.Exists(savePath)) json = File.ReadAllText(savePath);
                else if (PlayerPrefs.HasKey($"{SAVE_KEY_PREFIX}{slotName}")) json = PlayerPrefs.GetString($"{SAVE_KEY_PREFIX}{slotName}");
                if (string.IsNullOrEmpty(json)) return null;

                var data = JsonUtility.FromJson<GameSaveData>(json);
                if (data?.gameState == null) return null;
                int plants = data.pots != null ? data.pots.Count(p => p.hasPlant) : 0;
                var summary = new SaveSlotSummary
                {
                    day = data.gameState.currentDay,
                    cry = data.gameState.currentCRY,
                    plantsInDome = plants,
                    timestamp = GetSaveTimestamp(slotName)
                };
                return summary;
            }
            catch { return null; }
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
                    condensationAmount = gameManager.CondensationSystem?.CondensationAmount ?? 0f,
                    hydrationPercent = gameManager.PlayerHydrationSystem?.HydrationPercent ?? 100f,
                    dehydrationZeroDayStreak = gameManager.DehydrationZeroDayStreak,
                    consecutiveDaysWithoutMeal = gameManager.ConsecutiveDaysWithoutMeal,
                    starvationDaysAtMinCapWithoutFood = gameManager.StarvationDaysAtMinCapWithoutFood,
                    ateMealSincePreviousDawn = gameManager.AteMealSincePreviousDawn
                };
            }

            if (gameManager?.FoodRoomSystem != null)
                saveData.foodRoomData = SerializeFoodRoom(gameManager.FoodRoomSystem);
            
            // Inventario del giocatore
            if (gameManager != null && gameManager.PlayerInventory != null)
            {
                saveData.inventory = SerializeInventory(gameManager.PlayerInventory);
            }
            
            // Stato dei vasi
            saveData.pots = CollectPotStates();

            // Stato slot cryo (piante passive conservate nella Cryo Machine)
            saveData.cryoSlots = CollectCryoSlotStates();
            
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

            // Note diario piante
            saveData.diaryNotes = new List<DiaryNoteEntry>();
            var diaryManager = PlantDiaryManager.Instance;
            if (diaryManager != null)
            {
                diaryManager.CollectNotesForSave((potId, day, text, timestamp) =>
                {
                    saveData.diaryNotes.Add(new DiaryNoteEntry { potId = potId, day = day, text = text, timestamp = timestamp });
                });
            }

            if (gameManager != null)
                saveData.stemCellModuleUnlocked = gameManager.IsStemCellModuleUnlocked;

            var plantDatabase = ServiceContainer.Instance?.Get<PlantDatabase>(suppressWarning: true) ?? PlantDatabase.Instance;
            if (plantDatabase != null)
            {
                saveData.discoveredPlantCodes = plantDatabase.ExportDiscoveredPlantCodes();
            }

            var wikiUnlockService = ServiceContainer.Instance?.Get<WikiUnlockService>(suppressWarning: true);
            if (wikiUnlockService != null)
            {
                saveData.wikiUnlockedIds = wikiUnlockService.ExportUnlockedIds();
            }
            
            saveData.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            saveData.gameVersion = Application.version;
            saveData.inventoryVersion = INVENTORY_VERSION_WITH_METADATA;

            saveData.playerOutfitIndex = CollectPlayerOutfitIndex();

            return saveData;
        }

        private int CollectPlayerOutfitIndex()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return -1;
            var outfit = player.GetComponent<_Project.Player.PlayerOutfitController>();
            return outfit != null ? outfit.CurrentIndex : -1;
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
            
            if (dayCycleSystem != null && saveData.gameState != null && saveData.gameState.currentDay > 0)
            {
                dayCycleSystem.SetCurrentDay(saveData.gameState.currentDay);
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

                if (gameManager.CondensationSystem != null)
                {
                    gameManager.CondensationSystem.SetCurrentAccumulation(saveData.gameState.condensationAmount);
                }
                if (gameManager.PlayerHydrationSystem != null && saveData.gameState.hydrationPercent >= 0)
                {
                    gameManager.PlayerHydrationSystem.SetHydrationPercent(saveData.gameState.hydrationPercent);
                }

                gameManager.SetDehydrationZeroDayStreakForLoad(saveData.gameState.dehydrationZeroDayStreak);
                gameManager.SetMealSurvivalStateForLoad(
                    saveData.gameState.consecutiveDaysWithoutMeal,
                    saveData.gameState.starvationDaysAtMinCapWithoutFood,
                    saveData.gameState.ateMealSincePreviousDawn);
            }

            if (gameManager?.FoodRoomSystem != null && saveData.foodRoomData != null)
                DeserializeFoodRoom(gameManager.FoodRoomSystem, saveData.foodRoomData);
            
            // Inventario
            if (gameManager != null && gameManager.PlayerInventory != null && saveData.inventory != null)
            {
                int invVersion = saveData.inventoryVersion;
                DeserializeInventory(gameManager.PlayerInventory, saveData.inventory, invVersion);
            }
            
            // Vasi
            if (saveData.pots != null && saveData.pots.Count > 0)
            {
                ApplyPotStates(saveData.pots);
            }

            // Slot cryo
            if (saveData.cryoSlots != null && saveData.cryoSlots.Count > 0)
            {
                ApplyCryoSlotStates(saveData.cryoSlots);
            }

            if (gameManager != null && saveData.gameState != null)
                gameManager.SetStemCellModuleUnlocked(saveData.stemCellModuleUnlocked);

            var plantDatabase = ServiceContainer.Instance?.Get<PlantDatabase>(suppressWarning: true) ?? PlantDatabase.Instance;
            if (plantDatabase != null && saveData.discoveredPlantCodes != null)
            {
                plantDatabase.ImportDiscoveredPlantCodes(saveData.discoveredPlantCodes, persistToPrefs: true);
            }

            var wikiUnlockService = ServiceContainer.Instance?.Get<WikiUnlockService>(suppressWarning: true);
            if (wikiUnlockService != null && saveData.wikiUnlockedIds != null)
            {
                wikiUnlockService.ImportUnlockedIds(saveData.wikiUnlockedIds);
            }

            // Note diario piante
            if (saveData.diaryNotes != null && saveData.diaryNotes.Count > 0)
            {
                var diaryManager = PlantDiaryManager.Instance;
                if (diaryManager != null)
                {
                    var notes = new List<(string potId, int day, string text, string timestampIso)>();
                    foreach (var e in saveData.diaryNotes)
                    {
                        if (string.IsNullOrEmpty(e.potId)) continue;
                        notes.Add((e.potId, e.day, e.text ?? "", e.timestamp ?? ""));
                    }
                    diaryManager.ApplyNotesFromSave(notes);
                }
            }

            ApplyPlayerOutfitIndex(saveData.playerOutfitIndex);

            // Missioni
            if (saveData.missions?.entries != null && saveData.missions.entries.Count > 0)
            {
                var missionManager = ServiceContainer.Instance?.Get<MissionManager>(suppressWarning: true);
                if (missionManager != null)
                {
                    var entries = new List<(string configName, bool isCompleted)>();
                    foreach (var e in saveData.missions.entries)
                    {
                        if (string.IsNullOrEmpty(e.configName)) continue;
                        entries.Add((e.configName, e.isCompleted));
                    }
                    missionManager.RestoreFromSave(entries);
                }
            }
        }
        
        private void ApplyPlayerOutfitIndex(int savedIndex)
        {
            if (savedIndex < 0) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            var outfit = player.GetComponent<_Project.Player.PlayerOutfitController>();
            if (outfit == null)
                outfit = player.AddComponent<_Project.Player.PlayerOutfitController>();
            outfit.Apply(savedIndex);
        }

        private FoodRoomSaveData SerializeFoodRoom(FoodRoomSystem foodRoom)
        {
            if (foodRoom == null) return null;
            var data = new FoodRoomSaveData
            {
                slots = new List<FoodRoomSlotSaveData>(),
                waterRawInput = foodRoom.WaterSlot.RawWaterInput,
                waterPotableOutput = foodRoom.WaterSlot.PotableWaterOutput,
                waterCurrentProgress = foodRoom.WaterSlot.CurrentUnitProgress,
                waterIsActive = foodRoom.WaterSlot.IsActive
            };
            foreach (var slot in foodRoom.ProductionSlots)
            {
                data.slots.Add(new FoodRoomSlotSaveData
                {
                    type = (int)slot.Type,
                    daysRemaining = slot.DaysRemaining,
                    startDay = slot.StartDay,
                    hasStemCell = slot.HasStemCell,
                    stemCellTypeId = slot.StemCellTypeId ?? "",
                    state = (int)slot.State
                });
            }
            return data;
        }

        private void DeserializeFoodRoom(FoodRoomSystem foodRoom, FoodRoomSaveData data)
        {
            if (foodRoom == null || data == null) return;
            var slots = new List<(int typeInt, int daysRemaining, int startDay, bool hasStemCell, string stemCellTypeId, int stateInt)>();
            if (data.slots != null)
            {
                foreach (var s in data.slots)
                {
                    slots.Add((s.type, s.daysRemaining, s.startDay, s.hasStemCell, s.stemCellTypeId ?? "", s.state));
                }
            }
            foodRoom.RestoreState(slots, data.waterRawInput, data.waterPotableOutput, data.waterCurrentProgress, data.waterIsActive);
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
                        payloadJson = JsonUtility.ToJson(potState),
                        hasPlant = potState.HasPlant,
                        stage = potState.Stage,
                        amountFruits = potState.AmountFruits,
                        plantCode = potState.PlantCode,
                        hydration = potState.Hydration,
                        lightExposure = potState.LightExposure,
                        growthPoints = potState.GrowthPoints,
                        daysSincePlant = potState.DaysSincePlant,
                        daysNeglectedStreak = potState.DaysNeglectedStreak,
                        daysCritical = potState.DaysCritical,
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
                        wateringRawWaterAccumulator = potState.WateringRawWaterAccumulator,
                        plantGeneticType = (int)potState.PlantGeneticType
                    });
                }
            }
            
            return potStates;
        }
        
        /// <summary>
        /// Raccoglie lo stato corrente di tutti i CryoSlot dalla Cryo Machine.
        /// </summary>
        private List<CryoSlotSaveEntry> CollectCryoSlotStates()
        {
            var cryo = ServiceContainer.Instance?.Get<CryoMachineController>(suppressWarning: true);
            if (cryo == null)
                return new List<CryoSlotSaveEntry>();

            return cryo.CollectSaveData();
        }

        /// <summary>
        /// Ripristina lo stato dei CryoSlot della Cryo Machine dai dati salvati.
        /// </summary>
        private void ApplyCryoSlotStates(List<CryoSlotSaveEntry> entries)
        {
            var cryo = ServiceContainer.Instance?.Get<CryoMachineController>(suppressWarning: true);
            if (cryo == null)
            {
                SporiumLogger.LogWarning(LogCategory.Save, "ApplyCryoSlotStates: CryoMachineController non disponibile. Salto ripristino slot cryo.");
                return;
            }

            cryo.RestoreFromSave(entries);
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
                        if (!string.IsNullOrEmpty(potStateData.payloadJson))
                        {
                            JsonUtility.FromJsonOverwrite(potStateData.payloadJson, potState);
                            if (string.IsNullOrEmpty(potState.PotId))
                                potState.PotId = potStateData.potId;
                            break;
                        }

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
                        potState.DaysCritical = potStateData.daysCritical;
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
                        if (potStateData.plantGeneticType >= 0 && potStateData.plantGeneticType <= 2)
                            potState.PlantGeneticType = (_Project.Sporae.Core.GeneticType)potStateData.plantGeneticType;
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
                foreach (var item in slot.Items)
                {
                    inventoryData.items.Add(new InventoryItemData
                    {
                        typeId = slot.TypeId,
                        quantity = 1,
                        quality = item.Quality,
                        hasGeneticType = item.GeneticTypeValue.HasValue,
                        geneticType = item.GeneticTypeValue.HasValue ? (int)item.GeneticTypeValue.Value : 0,
                        hasSporeStage = item.SporeStageValue.HasValue,
                        sporeStage = item.SporeStageValue.HasValue ? (int)item.SporeStageValue.Value : 0,
                        familyMetadata = item.FamilyMetadata,
                        sourcePlantCodeMetadata = item.SourcePlantCodeMetadata,
                        sourcePlantDisplayName = item.SourcePlantDisplayName,
                        activePowerLabel = item.ActivePowerLabel,
                        passivePowerLabel = item.PassivePowerLabel,
                        parentFamilyA = item.ParentFamilyA,
                        parentFamilyB = item.ParentFamilyB,
                        plantLevelMetadata = item.PlantLevelMetadata,
                        candidateTraitsCsv = item.CandidateTraitsCsv,
                        selectedTraitsCsv = item.SelectedTraitsCsv,
                        traitPowerPercent = item.TraitPowerPercent,
                        reagentUsedMetadata = item.ReagentUsedMetadata,
                        labCareProfileMetadata = item.LabCareProfileMetadata,
                        customPlantName = item.CustomPlantName,
                        resolvedPlantCodeMetadata = item.ResolvedPlantCodeMetadata
                    });
                }
            }
            
            return inventoryData;
        }
        
        /// <summary>
        /// Deserializza l'inventario dal formato salvato.
        /// Save vecchi (inventoryVersion &lt; 1): spore senza metadata vengono caricate come Raw + STABLE.
        /// </summary>
        private void DeserializeInventory(Inventory inventory, InventoryData inventoryData, int inventoryVersion)
        {
            inventory.Clear();
            if (inventoryData?.items == null) return;

            foreach (var itemData in inventoryData.items)
            {
                if (string.IsNullOrEmpty(itemData.typeId) || itemData.quantity <= 0) continue;
                for (int q = 0; q < itemData.quantity; q++)
                {
                    var item = _Project.Sporae.Core.ItemFabric.CreateItemByType(itemData.typeId);
                    if (item == null)
                        continue;

                    if (inventoryVersion < INVENTORY_VERSION_WITH_METADATA)
                    {
                        // Legacy fallback: spore vecchie restano Raw + Stable.
                        if (item.TypeId == _Project.Sporae.Core.Items.SporeGeneric)
                        {
                            item.SporeStageValue = _Project.Sporae.Core.SporeStage.Raw;
                            item.GeneticTypeValue = _Project.Sporae.Core.GeneticType.Stable;
                        }
                    }
                    else
                    {
                        item.Quality = itemData.quality > 0f ? itemData.quality : item.Quality;
                        if (itemData.hasGeneticType)
                            item.GeneticTypeValue = (_Project.Sporae.Core.GeneticType)itemData.geneticType;
                        if (itemData.hasSporeStage)
                            item.SporeStageValue = (_Project.Sporae.Core.SporeStage)itemData.sporeStage;
                        item.FamilyMetadata = itemData.familyMetadata;
                        item.SourcePlantCodeMetadata = itemData.sourcePlantCodeMetadata;
                        item.SourcePlantDisplayName = itemData.sourcePlantDisplayName;
                        item.ActivePowerLabel = itemData.activePowerLabel;
                        item.PassivePowerLabel = itemData.passivePowerLabel;
                        item.ParentFamilyA = itemData.parentFamilyA;
                        item.ParentFamilyB = itemData.parentFamilyB;
                        item.PlantLevelMetadata = itemData.plantLevelMetadata;
                        item.CandidateTraitsCsv = itemData.candidateTraitsCsv;
                        item.SelectedTraitsCsv = itemData.selectedTraitsCsv;
                        item.TraitPowerPercent = itemData.traitPowerPercent > 0 ? itemData.traitPowerPercent : 100;
                        item.ReagentUsedMetadata = itemData.reagentUsedMetadata;
                        item.LabCareProfileMetadata = itemData.labCareProfileMetadata;
                        item.CustomPlantName = itemData.customPlantName;
                        item.ResolvedPlantCodeMetadata = itemData.resolvedPlantCodeMetadata;
                        if (string.IsNullOrWhiteSpace(item.ResolvedPlantCodeMetadata))
                        {
                            var pdSeed = PlantDatabase.Instance?.GetPlantDataBySeedTypeId(item.TypeId);
                            item.ResolvedPlantCodeMetadata = pdSeed?.PlantCode;
                        }
                    }

                    if (_Project.Sporae.Core.Items.IsLegacyFruitType(item.TypeId)
                        && (!string.IsNullOrWhiteSpace(item.SourcePlantCodeMetadata) || !string.IsNullOrWhiteSpace(item.FamilyMetadata)))
                    {
                        string migratedTypeId = _Project.Sporae.Core.ItemFabric.ResolveFruitTypeIdForPlant(item.SourcePlantCodeMetadata, item.FamilyMetadata);
                        var migratedItem = _Project.Sporae.Core.ItemFabric.CreateItemWithMetadata(
                            migratedTypeId,
                            item.Quality,
                            item.GeneticTypeValue,
                            item.FamilyMetadata,
                            item.SourcePlantCodeMetadata,
                            item.PlantLevelMetadata,
                            item.SourcePlantDisplayName,
                            item.ActivePowerLabel,
                            item.PassivePowerLabel);
                        if (migratedItem != null)
                        {
                            migratedItem.ParentFamilyA = item.ParentFamilyA;
                            migratedItem.ParentFamilyB = item.ParentFamilyB;
                            migratedItem.CandidateTraitsCsv = item.CandidateTraitsCsv;
                            migratedItem.SelectedTraitsCsv = item.SelectedTraitsCsv;
                            migratedItem.TraitPowerPercent = item.TraitPowerPercent;
                            migratedItem.ReagentUsedMetadata = item.ReagentUsedMetadata;
                            migratedItem.CustomPlantName = item.CustomPlantName;
                            migratedItem.LabCareProfileMetadata = item.LabCareProfileMetadata;
                            migratedItem.ResolvedPlantCodeMetadata = item.ResolvedPlantCodeMetadata;
                            item = migratedItem;
                        }
                    }

                    inventory.Add(item);
                }
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
            var data = new MissionsData();
            data.entries = new List<MissionEntryData>();
            if (missionManager == null) return data;

            var seen = new HashSet<string>();

            if (missionManager.CurrentMissions != null)
            {
                foreach (var m in missionManager.CurrentMissions)
                {
                    if (m?.Config == null) continue;
                    if (!seen.Add(m.Config.name)) continue;
                    data.entries.Add(new MissionEntryData { configName = m.Config.name, isCompleted = false });
                }
            }

            if (missionManager.CompletedMissions != null)
            {
                foreach (var m in missionManager.CompletedMissions)
                {
                    if (m?.Config == null) continue;
                    if (!seen.Add(m.Config.name)) continue;
                    data.entries.Add(new MissionEntryData { configName = m.Config.name, isCompleted = true });
                }
            }

            return data;
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
        
        /// <summary>Versione formato inventario con metadata per item (genetica/famiglia/tratti).</summary>
        private const int INVENTORY_VERSION_WITH_METADATA = 3;

        [Serializable]
        private class GameSaveData
        {
            public GameStateData gameState;
            public InventoryData inventory;
            public List<CryoSlotSaveEntry> cryoSlots;
            public List<PotStateData> pots;
            public DiaryStatisticsData diaryStatistics;
            public MissionsData missions;
            public List<DiaryNoteEntry> diaryNotes;
            public bool stemCellModuleUnlocked;
            public string saveTimestamp;
            public string gameVersion;
            public int inventoryVersion;
            public FoodRoomSaveData foodRoomData;
            public List<string> discoveredPlantCodes;
            public List<string> wikiUnlockedIds;
            /// <summary>Indice outfit selezionato dall'armadio (-1 = non impostato, usa default).</summary>
            public int playerOutfitIndex = -1;
        }
        
        [Serializable]
        private class GameStateData
        {
            public int currentDay;
            public int currentCRY;
            public int actionsLeft;
            public float condensationAmount;
            public float hydrationPercent = 100f;
            public int dehydrationZeroDayStreak;
            public int consecutiveDaysWithoutMeal;
            public int starvationDaysAtMinCapWithoutFood;
            public bool ateMealSincePreviousDawn;
        }

        [Serializable]
        private class FoodRoomSaveData
        {
            public List<FoodRoomSlotSaveData> slots;
            public int waterRawInput;
            public int waterPotableOutput;
            public float waterCurrentProgress;
            public bool waterIsActive;
        }

        [Serializable]
        private class FoodRoomSlotSaveData
        {
            public int type;
            public int daysRemaining;
            public int startDay;
            public bool hasStemCell;
            public string stemCellTypeId;
            public int state;
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
            public float quality;
            public bool hasGeneticType;
            public int geneticType;
            public bool hasSporeStage;
            public int sporeStage;
            public string familyMetadata;
            public string sourcePlantCodeMetadata;
            public string sourcePlantDisplayName;
            public string activePowerLabel;
            public string passivePowerLabel;
            public string parentFamilyA;
            public string parentFamilyB;
            public int plantLevelMetadata;
            public string candidateTraitsCsv;
            public string selectedTraitsCsv;
            public int traitPowerPercent;
            public string reagentUsedMetadata;
            public string labCareProfileMetadata;
            public string customPlantName;
            public string resolvedPlantCodeMetadata;
        }
        
        [Serializable]
        private class PotStateData
        {
            public string potId;
            public string payloadJson;
            public bool hasPlant;
            public int stage;
            public float amountFruits;
            public string plantCode;
            public int hydration;
            public int lightExposure;
            public int growthPoints;
            public int daysSincePlant;
            public int daysNeglectedStreak;
            public int daysCritical = 0;
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

            // GDD 42: tipo genetico pianta (0=Fixed, 1=Stable, 2=Unstable)
            public int plantGeneticType = 1;
        }
        
        [Serializable]
        private class DiaryNoteEntry
        {
            public string potId;
            public int day;
            public string text;
            public string timestamp;
        }

        [Serializable]
        private class DiaryStatisticsData
        {
            // Aggiungi campi quando DiaryStatistics è implementato
        }
        
        [Serializable]
        private class MissionEntryData
        {
            public string configName;
            public bool isCompleted;
        }

        [Serializable]
        private class MissionsData
        {
            public List<MissionEntryData> entries;
        }
    }
}
