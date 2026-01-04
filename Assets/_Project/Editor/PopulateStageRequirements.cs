using UnityEngine;
using UnityEditor;
using Sporae.Dome.PotSystem.Growth;

namespace _Project.Editor
{
    /// <summary>
    /// Script editor per popolare i requisiti di crescita per stadio nelle piante esistenti.
    /// Crea valori di default basati sulla famiglia della pianta.
    /// </summary>
    public class PopulateStageRequirements : EditorWindow
    {
        [MenuItem("Sporae/Populate Stage Requirements")]
        public static void ShowWindow()
        {
            GetWindow<PopulateStageRequirements>("Populate Stage Requirements");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Popola Requisiti Stadi per Piante", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("Questo script popola i requisiti di crescita per stadio", EditorStyles.wordWrappedLabel);
            GUILayout.Label("nelle piante esistenti con valori di default basati sulla famiglia.", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);
            
            if (GUILayout.Button("Popola Requisiti per Tutte le Piante", GUILayout.Height(30)))
            {
                PopulateAllPlants();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Popola Solo PLT-STD-001 (Ferric Fern)", GUILayout.Height(30)))
            {
                PopulatePlant("PLT-STD-001", GetStandardRequirements());
            }
            
            if (GUILayout.Button("Popola Solo PLT-PURE-001 (Arctic Hask)", GUILayout.Height(30)))
            {
                PopulatePlant("PLT-PURE-001", GetPureRequirements());
            }
            
            if (GUILayout.Button("Popola Solo PLT-EVIL-001 (Glasscap Fungus)", GUILayout.Height(30)))
            {
                PopulatePlant("PLT-EVIL-001", GetEvilRequirements());
            }
        }
        
        private void PopulateAllPlants()
        {
            string[] guids = AssetDatabase.FindAssets("t:PlantData", new[] { "Assets/Resources/Plants" });
            
            int populated = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PlantData plantData = AssetDatabase.LoadAssetAtPath<PlantData>(path);
                
                if (plantData == null) continue;
                
                StageRequirements[] requirements = null;
                
                // Determina i requisiti in base alla famiglia
                switch (plantData.Family)
                {
                    case PlantFamily.Standard:
                        requirements = GetStandardRequirements();
                        break;
                    case PlantFamily.Pure:
                        requirements = GetPureRequirements();
                        break;
                    case PlantFamily.Evil:
                        requirements = GetEvilRequirements();
                        break;
                }
                
                if (requirements != null)
                {
                    PopulatePlantData(plantData, requirements);
                    populated++;
                }
            }
            
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Completato", 
                $"Requisiti popolati per {populated} piante!", "OK");
        }
        
        private void PopulatePlant(string plantCode, StageRequirements[] requirements)
        {
            string[] guids = AssetDatabase.FindAssets("t:PlantData", new[] { "Assets/Resources/Plants" });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PlantData plantData = AssetDatabase.LoadAssetAtPath<PlantData>(path);
                
                if (plantData != null && plantData.PlantCode == plantCode)
                {
                    PopulatePlantData(plantData, requirements);
                    AssetDatabase.SaveAssets();
                    EditorUtility.DisplayDialog("Completato", 
                        $"Requisiti popolati per {plantCode}!", "OK");
                    return;
                }
            }
            
            EditorUtility.DisplayDialog("Errore", 
                $"Pianta {plantCode} non trovata!", "OK");
        }
        
        private void PopulatePlantData(PlantData plantData, StageRequirements[] requirements)
        {
            // Crea una copia modificabile degli array per assegnare correttamente
            StageRequirements[] requirementsCopy = new StageRequirements[requirements.Length];
            for (int i = 0; i < requirements.Length; i++)
            {
                requirementsCopy[i] = new StageRequirements
                {
                    stage = requirements[i].stage,
                    hydrationMin = requirements[i].hydrationMin,
                    hydrationMed = requirements[i].hydrationMed,
                    hydrationMax = requirements[i].hydrationMax,
                    durationDays = requirements[i].durationDays,
                    notes = requirements[i].notes,
                    // BLK-03.01-T1: Range fertilizzante fissi (valori identici per tutte le piante)
                    fertilizerMin = requirements[i].fertilizerMin,
                    fertilizerMed = requirements[i].fertilizerMed,
                    fertilizerMax = requirements[i].fertilizerMax,
                    // BLK-03.01-T2: Range luce
                    lightMin = requirements[i].lightMin,
                    lightMed = requirements[i].lightMed,
                    lightMax = requirements[i].lightMax
                };
                // Usa SetRequiredLed per impostare correttamente il SerializableLedType
                LedType? requiredLed = requirements[i].GetRequiredLed();
                requirementsCopy[i].SetRequiredLed(requiredLed);
            }
            
            // Usa SerializedObject per modificare i campi serializzati
            SerializedObject serializedObject = new SerializedObject(plantData);
            SerializedProperty stageRequirementsProp = serializedObject.FindProperty("stageRequirements");
            
            stageRequirementsProp.arraySize = requirementsCopy.Length;
            
            for (int i = 0; i < requirementsCopy.Length; i++)
            {
                SerializedProperty element = stageRequirementsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("stage").enumValueIndex = (int)requirementsCopy[i].stage;
                element.FindPropertyRelative("hydrationMin").intValue = requirementsCopy[i].hydrationMin;
                element.FindPropertyRelative("hydrationMed").intValue = requirementsCopy[i].hydrationMed;
                element.FindPropertyRelative("hydrationMax").intValue = requirementsCopy[i].hydrationMax;
                
                // BLK-03.01-T1: Range fertilizzante fissi
                element.FindPropertyRelative("fertilizerMin").intValue = requirementsCopy[i].fertilizerMin;
                element.FindPropertyRelative("fertilizerMed").intValue = requirementsCopy[i].fertilizerMed;
                element.FindPropertyRelative("fertilizerMax").intValue = requirementsCopy[i].fertilizerMax;
                
                // BLK-03.01-T2: Range luce
                element.FindPropertyRelative("lightMin").intValue = requirementsCopy[i].lightMin;
                element.FindPropertyRelative("lightMed").intValue = requirementsCopy[i].lightMed;
                element.FindPropertyRelative("lightMax").intValue = requirementsCopy[i].lightMax;
                
                // Gestisci SerializableLedType wrapper
                SerializedProperty ledProp = element.FindPropertyRelative("requiredLed");
                ledProp.FindPropertyRelative("hasValue").boolValue = requirementsCopy[i].requiredLed.hasValue;
                if (requirementsCopy[i].requiredLed.hasValue)
                {
                    ledProp.FindPropertyRelative("value").enumValueIndex = (int)requirementsCopy[i].requiredLed.value;
                }
                
                element.FindPropertyRelative("durationDays").intValue = requirementsCopy[i].durationDays;
                element.FindPropertyRelative("notes").stringValue = requirementsCopy[i].notes;
            }
            
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(plantData);
            
            Debug.Log($"[PopulateStageRequirements] ✅ Requisiti popolati per {plantData.PlantCode}");
        }
        
        /// <summary>
        /// Requisiti di default per piante Standard (basati su Ferric Fern)
        /// </summary>
        private StageRequirements[] GetStandardRequirements()
        {
            var reqs = new StageRequirements[]
            {
                new StageRequirements
                {
                    stage = PlantStage.Seed,
                    hydrationMin = 30,
                    hydrationMed = 45,
                    hydrationMax = 60,
                    durationDays = 2,
                    notes = "Ampia tolleranza",
                    // BLK-03.01-T1: Range fertilizzante fissi
                    fertilizerMin = 40,
                    fertilizerMed = 75,
                    fertilizerMax = 100,
                    // BLK-03.01-T2: Range luce (LED non richiesto, range generico)
                    lightMin = 0,
                    lightMed = 50,
                    lightMax = 100
                },
                new StageRequirements
                {
                    stage = PlantStage.Sprout,
                    hydrationMin = 40,
                    hydrationMed = 50,
                    hydrationMax = 60,
                    durationDays = 2,
                    notes = "Germoglio attivo",
                    // BLK-03.01-T1: Range fertilizzante fissi (stesso di Seed)
                    fertilizerMin = 40,
                    fertilizerMed = 75,
                    fertilizerMax = 100,
                    // BLK-03.01-T2: Range luce (LED non richiesto, range generico)
                    lightMin = 0,
                    lightMed = 50,
                    lightMax = 100
                },
                new StageRequirements
                {
                    stage = PlantStage.Growth,
                    hydrationMin = 35,
                    hydrationMed = 55,
                    hydrationMax = 75,
                    durationDays = 3,
                    notes = "Accrescimento vegetativo",
                    // BLK-03.01-T1: Range fertilizzante fissi
                    fertilizerMin = 40,
                    fertilizerMed = 60,
                    fertilizerMax = 80,
                    // BLK-03.01-T2: Range luce (Blue LED richiesto, range ottimale)
                    lightMin = 50,
                    lightMed = 75,
                    lightMax = 100
                },
                new StageRequirements
                {
                    stage = PlantStage.Flowering,
                    hydrationMin = 40,
                    hydrationMed = 50,
                    hydrationMax = 70,
                    durationDays = 3,
                    notes = "Fioritura attiva",
                    // BLK-03.01-T1: Range fertilizzante fissi
                    fertilizerMin = 20,
                    fertilizerMed = 40,
                    fertilizerMax = 60,
                    // BLK-03.01-T2: Range luce (Red LED richiesto, range ottimale)
                    lightMin = 50,
                    lightMed = 75,
                    lightMax = 100
                },
                new StageRequirements
                {
                    stage = PlantStage.HarvestReady,
                    hydrationMin = 0,
                    hydrationMed = 50,
                    hydrationMax = 100,
                    durationDays = 3,
                    notes = "Finestra di raccolta multi-giorno",
                    // BLK-03.01-T1: Range fertilizzante fissi (non richiesto)
                    fertilizerMin = 0,
                    fertilizerMed = 0,
                    fertilizerMax = 0,
                    // BLK-03.01-T2: Range luce (LED non richiesto, range generico)
                    lightMin = 0,
                    lightMed = 50,
                    lightMax = 100
                },
                new StageRequirements
                {
                    stage = PlantStage.Resting,
                    hydrationMin = 0,
                    hydrationMed = 50,
                    hydrationMax = 100,
                    durationDays = 2,
                    notes = "Riposo post-raccolta, riattivabile con fertilizzante",
                    // BLK-03.01-T1: Range fertilizzante fissi
                    fertilizerMin = 30,
                    fertilizerMed = 50,
                    fertilizerMax = 70,
                    // BLK-03.01-T2: Range luce (LED non richiesto, range generico)
                    lightMin = 0,
                    lightMed = 50,
                    lightMax = 100
                }
            };
            
            // BLK-02.08: Imposta i LED richiesti usando SetRequiredLed (Standard: nessun LED richiesto - accetta entrambi)
            reqs[0].SetRequiredLed(null); // Seed: nessun LED
            reqs[1].SetRequiredLed(null); // Sprout: nessun LED
            reqs[2].SetRequiredLed(null); // Growth: nessun LED richiesto (accetta Blue o Red)
            reqs[3].SetRequiredLed(null); // Flowering: nessun LED richiesto (accetta Blue o Red)
            reqs[4].SetRequiredLed(null); // HarvestReady: nessun LED
            reqs[5].SetRequiredLed(null); // Resting: nessun LED
            
            return reqs;
        }
        
        /// <summary>
        /// Requisiti di default per piante Pure (più stringenti, richiedono più cura)
        /// </summary>
        private StageRequirements[] GetPureRequirements()
        {
            var reqs = new StageRequirements[]
            {
                new StageRequirements
                {
                    stage = PlantStage.Seed,
                    hydrationMin = 35,
                    hydrationMed = 50,
                    hydrationMax = 65,
                    durationDays = 2,
                    notes = "Pianta Pure richiede cura attenta",
                    // BLK-03.01-T1: Range fertilizzante fissi
                    fertilizerMin = 60,
                    fertilizerMed = 75,
                    fertilizerMax = 90,
                    // BLK-03.01-T2: Range luce (LED non richiesto, range generico)
                    lightMin = 0,
                    lightMed = 50,
                    lightMax = 100
                },
                new StageRequirements
                {
                    stage = PlantStage.Sprout,
                    hydrationMin = 45,
                    hydrationMed = 55,
                    hydrationMax = 65,
                    durationDays = 3,
                    notes = "Germoglio Pure, Blue LED consigliato",
                    // BLK-03.01-T1: Range fertilizzante fissi (stesso di Seed)
                    fertilizerMin = 60,
                    fertilizerMed = 75,
                    fertilizerMax = 90,
                    // BLK-03.01-T2: Range luce (Blue LED consigliato, range ottimale)
                    lightMin = 50,
                    lightMed = 75,
                    lightMax = 100
                },
                new StageRequirements
                {
                    stage = PlantStage.Growth,
                    hydrationMin = 40,
                    hydrationMed = 55,
                    hydrationMax = 70,
                    durationDays = 3,
                    notes = "Crescita Pure, Blue LED richiesto",
                    // BLK-03.01-T1: Range fertilizzante fissi
                    fertilizerMin = 40,
                    fertilizerMed = 60,
                    fertilizerMax = 80,
                    // BLK-03.01-T2: Range luce (Blue LED richiesto, range ottimale)
                    lightMin = 50,
                    lightMed = 75,
                    lightMax = 100
                },
                new StageRequirements
                {
                    stage = PlantStage.Flowering,
                    hydrationMin = 45,
                    hydrationMed = 55,
                    hydrationMax = 65,
                    durationDays = 3,
                    notes = "Fioritura Pure, Red LED richiesto",
                    // BLK-03.01-T1: Range fertilizzante fissi
                    fertilizerMin = 20,
                    fertilizerMed = 40,
                    fertilizerMax = 60,
                    // BLK-03.01-T2: Range luce (Red LED richiesto, range ottimale)
                    lightMin = 50,
                    lightMed = 75,
                    lightMax = 100
                },
                new StageRequirements
                {
                    stage = PlantStage.HarvestReady,
                    hydrationMin = 40,
                    hydrationMed = 55,
                    hydrationMax = 70,
                    durationDays = 3,
                    notes = "Raccolta Pure, mantenere idratazione ottimale",
                    // BLK-03.01-T1: Range fertilizzante fissi (non richiesto)
                    fertilizerMin = 0,
                    fertilizerMed = 0,
                    fertilizerMax = 0,
                    // BLK-03.01-T2: Range luce (LED non richiesto, range generico)
                    lightMin = 0,
                    lightMed = 50,
                    lightMax = 100
                },
                new StageRequirements
                {
                    stage = PlantStage.Resting,
                    hydrationMin = 0,
                    hydrationMed = 50,
                    hydrationMax = 100,
                    durationDays = 2,
                    notes = "Riposo Pure, riattivabile con fertilizzante",
                    // BLK-03.01-T1: Range fertilizzante fissi
                    fertilizerMin = 30,
                    fertilizerMed = 50,
                    fertilizerMax = 70,
                    // BLK-03.01-T2: Range luce (LED non richiesto, range generico)
                    lightMin = 0,
                    lightMed = 50,
                    lightMax = 100
                }
            };
            
            // BLK-02.08: Imposta i LED richiesti usando SetRequiredLed (Pure: solo Blue LED)
            reqs[0].SetRequiredLed(null); // Seed: nessun LED
            reqs[1].SetRequiredLed(LedType.Blue); // Sprout: Blue LED consigliato
            reqs[2].SetRequiredLed(LedType.Blue); // Growth: Blue LED richiesto
            reqs[3].SetRequiredLed(LedType.Blue); // Flowering: Blue LED richiesto (cambiato da Red a Blue per BLK-02.08)
            reqs[4].SetRequiredLed(null); // HarvestReady: nessun LED
            reqs[5].SetRequiredLed(null); // Resting: nessun LED
            
            return reqs;
        }
        
        /// <summary>
        /// Requisiti di default per piante Evil (più tolleranti ma con requisiti specifici)
        /// </summary>
        private StageRequirements[] GetEvilRequirements()
        {
            var reqs = new StageRequirements[]
            {
                new StageRequirements
                {
                    stage = PlantStage.Seed,
                    hydrationMin = 25,
                    hydrationMed = 40,
                    hydrationMax = 55,
                    durationDays = 2,
                    notes = "Pianta Evil più tollerante",
                    // BLK-03.01-T1: Range fertilizzante fissi
                    fertilizerMin = 60,
                    fertilizerMed = 75,
                    fertilizerMax = 90,
                    // BLK-03.01-T2: Range luce (LED non richiesto, range generico)
                    lightMin = 0,
                    lightMed = 50,
                    lightMax = 100
                },
                new StageRequirements
                {
                    stage = PlantStage.Sprout,
                    hydrationMin = 30,
                    hydrationMed = 45,
                    hydrationMax = 60,
                    durationDays = 2,
                    notes = "Germoglio Evil, tollerante",
                    // BLK-03.01-T1: Range fertilizzante fissi (stesso di Seed)
                    fertilizerMin = 60,
                    fertilizerMed = 75,
                    fertilizerMax = 90,
                    // BLK-03.01-T2: Range luce (LED non richiesto, range generico)
                    lightMin = 0,
                    lightMed = 50,
                    lightMax = 100
                },
                new StageRequirements
                {
                    stage = PlantStage.Growth,
                    hydrationMin = 30,
                    hydrationMed = 50,
                    hydrationMax = 70,
                    durationDays = 3,
                    notes = "Crescita Evil, Blue LED accelera",
                    // BLK-03.01-T1: Range fertilizzante fissi
                    fertilizerMin = 40,
                    fertilizerMed = 60,
                    fertilizerMax = 80,
                    // BLK-03.01-T2: Range luce (Blue LED accelera, range ottimale)
                    lightMin = 50,
                    lightMed = 75,
                    lightMax = 100
                },
                new StageRequirements
                {
                    stage = PlantStage.Flowering,
                    hydrationMin = 35,
                    hydrationMed = 50,
                    hydrationMax = 65,
                    durationDays = 3,
                    notes = "Fioritura Evil, Red LED richiesto",
                    // BLK-03.01-T1: Range fertilizzante fissi
                    fertilizerMin = 20,
                    fertilizerMed = 40,
                    fertilizerMax = 60,
                    // BLK-03.01-T2: Range luce (Red LED richiesto, range ottimale)
                    lightMin = 50,
                    lightMed = 75,
                    lightMax = 100
                },
                new StageRequirements
                {
                    stage = PlantStage.HarvestReady,
                    hydrationMin = 0,
                    hydrationMed = 45,
                    hydrationMax = 100,
                    durationDays = 3,
                    notes = "Raccolta Evil, tollerante a condizioni variabili",
                    // BLK-03.01-T1: Range fertilizzante fissi (non richiesto)
                    fertilizerMin = 0,
                    fertilizerMed = 0,
                    fertilizerMax = 0,
                    // BLK-03.01-T2: Range luce (LED non richiesto, range generico)
                    lightMin = 0,
                    lightMed = 50,
                    lightMax = 100
                },
                new StageRequirements
                {
                    stage = PlantStage.Resting,
                    hydrationMin = 0,
                    hydrationMed = 40,
                    hydrationMax = 100,
                    durationDays = 2,
                    notes = "Riposo Evil, riattivabile con fertilizzante",
                    // BLK-03.01-T1: Range fertilizzante fissi
                    fertilizerMin = 30,
                    fertilizerMed = 50,
                    fertilizerMax = 70,
                    // BLK-03.01-T2: Range luce (LED non richiesto, range generico)
                    lightMin = 0,
                    lightMed = 50,
                    lightMax = 100
                }
            };
            
            // BLK-02.08: Imposta i LED richiesti usando SetRequiredLed (Evil: solo Red LED)
            reqs[0].SetRequiredLed(null); // Seed: nessun LED
            reqs[1].SetRequiredLed(null); // Sprout: nessun LED
            reqs[2].SetRequiredLed(LedType.Red); // Growth: Red LED richiesto (cambiato da Blue a Red per BLK-02.08)
            reqs[3].SetRequiredLed(LedType.Red); // Flowering: Red LED richiesto
            reqs[4].SetRequiredLed(null); // HarvestReady: nessun LED
            reqs[5].SetRequiredLed(null); // Resting: nessun LED
            
            return reqs;
        }
    }
}

