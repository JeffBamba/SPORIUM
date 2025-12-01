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
                    notes = requirements[i].notes
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
                    notes = "Ampia tolleranza"
                },
                new StageRequirements
                {
                    stage = PlantStage.Sprout,
                    hydrationMin = 40,
                    hydrationMed = 50,
                    hydrationMax = 60,
                    durationDays = 2,
                    notes = "Germoglio attivo"
                },
                new StageRequirements
                {
                    stage = PlantStage.Growth,
                    hydrationMin = 35,
                    hydrationMed = 55,
                    hydrationMax = 75,
                    durationDays = 3,
                    notes = "Accrescimento vegetativo"
                },
                new StageRequirements
                {
                    stage = PlantStage.Flowering,
                    hydrationMin = 40,
                    hydrationMed = 50,
                    hydrationMax = 70,
                    durationDays = 3,
                    notes = "Fioritura attiva"
                },
                new StageRequirements
                {
                    stage = PlantStage.HarvestReady,
                    hydrationMin = 0,
                    hydrationMed = 50,
                    hydrationMax = 100,
                    durationDays = 3,
                    notes = "Finestra di raccolta multi-giorno"
                },
                new StageRequirements
                {
                    stage = PlantStage.Resting,
                    hydrationMin = 0,
                    hydrationMed = 50,
                    hydrationMax = 100,
                    durationDays = 2,
                    notes = "Riposo post-raccolta, riattivabile con fertilizzante"
                }
            };
            
            // Imposta i LED richiesti usando SetRequiredLed
            reqs[0].SetRequiredLed(null); // Seed: nessun LED
            reqs[1].SetRequiredLed(null); // Sprout: nessun LED
            reqs[2].SetRequiredLed(LedType.Blue); // Growth: Blue LED
            reqs[3].SetRequiredLed(LedType.Red); // Flowering: Red LED
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
                    notes = "Pianta Pure richiede cura attenta"
                },
                new StageRequirements
                {
                    stage = PlantStage.Sprout,
                    hydrationMin = 45,
                    hydrationMed = 55,
                    hydrationMax = 65,
                    durationDays = 3,
                    notes = "Germoglio Pure, Blue LED consigliato"
                },
                new StageRequirements
                {
                    stage = PlantStage.Growth,
                    hydrationMin = 40,
                    hydrationMed = 55,
                    hydrationMax = 70,
                    durationDays = 3,
                    notes = "Crescita Pure, Blue LED richiesto"
                },
                new StageRequirements
                {
                    stage = PlantStage.Flowering,
                    hydrationMin = 45,
                    hydrationMed = 55,
                    hydrationMax = 65,
                    durationDays = 3,
                    notes = "Fioritura Pure, Red LED richiesto"
                },
                new StageRequirements
                {
                    stage = PlantStage.HarvestReady,
                    hydrationMin = 40,
                    hydrationMed = 55,
                    hydrationMax = 70,
                    durationDays = 3,
                    notes = "Raccolta Pure, mantenere idratazione ottimale"
                },
                new StageRequirements
                {
                    stage = PlantStage.Resting,
                    hydrationMin = 0,
                    hydrationMed = 50,
                    hydrationMax = 100,
                    durationDays = 2,
                    notes = "Riposo Pure, riattivabile con fertilizzante"
                }
            };
            
            // Imposta i LED richiesti usando SetRequiredLed
            reqs[0].SetRequiredLed(null); // Seed: nessun LED
            reqs[1].SetRequiredLed(LedType.Blue); // Sprout: Blue LED consigliato
            reqs[2].SetRequiredLed(LedType.Blue); // Growth: Blue LED richiesto
            reqs[3].SetRequiredLed(LedType.Red); // Flowering: Red LED richiesto
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
                    notes = "Pianta Evil più tollerante"
                },
                new StageRequirements
                {
                    stage = PlantStage.Sprout,
                    hydrationMin = 30,
                    hydrationMed = 45,
                    hydrationMax = 60,
                    durationDays = 2,
                    notes = "Germoglio Evil, tollerante"
                },
                new StageRequirements
                {
                    stage = PlantStage.Growth,
                    hydrationMin = 30,
                    hydrationMed = 50,
                    hydrationMax = 70,
                    durationDays = 3,
                    notes = "Crescita Evil, Blue LED accelera"
                },
                new StageRequirements
                {
                    stage = PlantStage.Flowering,
                    hydrationMin = 35,
                    hydrationMed = 50,
                    hydrationMax = 65,
                    durationDays = 3,
                    notes = "Fioritura Evil, Red LED richiesto"
                },
                new StageRequirements
                {
                    stage = PlantStage.HarvestReady,
                    hydrationMin = 0,
                    hydrationMed = 45,
                    hydrationMax = 100,
                    durationDays = 3,
                    notes = "Raccolta Evil, tollerante a condizioni variabili"
                },
                new StageRequirements
                {
                    stage = PlantStage.Resting,
                    hydrationMin = 0,
                    hydrationMed = 40,
                    hydrationMax = 100,
                    durationDays = 2,
                    notes = "Riposo Evil, riattivabile con fertilizzante"
                }
            };
            
            // Imposta i LED richiesti usando SetRequiredLed
            reqs[0].SetRequiredLed(null); // Seed: nessun LED
            reqs[1].SetRequiredLed(null); // Sprout: nessun LED
            reqs[2].SetRequiredLed(LedType.Blue); // Growth: Blue LED accelera
            reqs[3].SetRequiredLed(LedType.Red); // Flowering: Red LED richiesto
            reqs[4].SetRequiredLed(null); // HarvestReady: nessun LED
            reqs[5].SetRequiredLed(null); // Resting: nessun LED
            
            return reqs;
        }
    }
}

