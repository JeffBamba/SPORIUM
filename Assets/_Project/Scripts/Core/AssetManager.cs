using System.Collections.Generic;
using UnityEngine;
using _Project;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using Sporae.DevTools;

namespace _Project.Sporae.Core
{
    /// <summary>
    /// Asset Manager centralizzato per caricamento e caching di ScriptableObjects.
    /// Sostituisce Resources.Load sparsi nel codice con sistema centralizzato e cache.
    /// </summary>
    public class AssetManager : MonoBehaviour
    {
        private static AssetManager _instance;
        private Dictionary<string, ScriptableObject> _assetCache = new Dictionary<string, ScriptableObject>();
        
        /// <summary>
        /// Istanza singleton dell'AssetManager.
        /// Auto-crea se non esiste e si registra nel ServiceContainer.
        /// </summary>
        public static AssetManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Prova a ottenere da ServiceContainer (suppress warning durante inizializzazione)
                    if (ServiceContainer.Instance != null)
                    {
                        _instance = ServiceContainer.Instance.Get<AssetManager>(suppressWarning: true);
                    }
                    
                    // Se non trovato, crea nuova istanza
                    if (_instance == null)
                    {
                        var go = new GameObject("AssetManager");
                        _instance = go.AddComponent<AssetManager>();
                        
                        // Registra nel ServiceContainer se disponibile
                        if (ServiceContainer.Instance != null)
                        {
                            ServiceContainer.Instance.Register(_instance);
                        }
                        else
                        {
                            DontDestroyOnLoad(go);
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
        /// Carica un asset ScriptableObject con caching automatico.
        /// </summary>
        /// <typeparam name="T">Tipo dell'asset (ScriptableObject)</typeparam>
        /// <param name="path">Percorso relativo a Resources/ (es. "Configs/PlantGrowthConfig")</param>
        /// <returns>Asset caricato o null se non trovato</returns>
        public T LoadAsset<T>(string path) where T : ScriptableObject
        {
            string key = $"{typeof(T).Name}_{path}";
            
            // Controlla cache
            if (_assetCache.ContainsKey(key))
            {
                return _assetCache[key] as T;
            }
            
            // Carica da Resources
            T asset = Resources.Load<T>(path);
            if (asset != null)
            {
                _assetCache[key] = asset;
#if UNITY_EDITOR
                SporiumLogger.LogDebug(LogCategory.Core, $"Asset caricato e cachato: {path} (tipo: {typeof(T).Name})");
#endif
            }
            else
            {
                SporiumLogger.LogError(LogCategory.Core, $"Asset non trovato: {path} (tipo: {typeof(T).Name})");
            }
            
            return asset;
        }
        
        /// <summary>
        /// Carica tutti gli asset di un tipo da una cartella Resources con caching.
        /// </summary>
        /// <typeparam name="T">Tipo dell'asset (ScriptableObject)</typeparam>
        /// <param name="path">Percorso relativo a Resources/ (es. "Plants")</param>
        /// <returns>Array di asset caricati</returns>
        public T[] LoadAllAssets<T>(string path) where T : ScriptableObject
        {
            T[] assets = Resources.LoadAll<T>(path);
            
            // Aggiungi tutti alla cache
            foreach (var asset in assets)
            {
                if (asset != null)
                {
                    string key = $"{typeof(T).Name}_{asset.name}";
                    _assetCache[key] = asset;
                }
            }
            
            return assets;
        }
        
        /// <summary>
        /// Pulisce la cache degli asset.
        /// Utile per ricaricare asset modificati in Editor.
        /// </summary>
        public void ClearCache()
        {
            _assetCache.Clear();
        }
        
        /// <summary>
        /// Rimuove un asset specifico dalla cache.
        /// </summary>
        public void RemoveFromCache<T>(string path) where T : ScriptableObject
        {
            string key = $"{typeof(T).Name}_{path}";
            _assetCache.Remove(key);
        }
        
        /// <summary>
        /// Precarica asset critici all'avvio.
        /// </summary>
        public void PreloadCriticalAssets()
        {
            // Precarica configurazioni critiche
            LoadAsset<PlantGrowthConfig>("Configs/PlantGrowthConfig");
            LoadAsset<PlantGrowthConfig>("Configs/PlantGrowthConfig_Default");
            LoadAsset<PotSystemConfig>("Configs/PotSystemConfig");
            LoadAsset<CondensationConfig>("Configs/CondensationConfig");
        }
    }
}

