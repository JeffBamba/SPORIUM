using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sporae.Core
{
    /// <summary>
    /// Sistema di eventi centralizzato per la comunicazione tra sistemi
    /// </summary>
    public class EventSystem : MonoBehaviour
    {
        private static EventSystem _instance;
        private Dictionary<string, List<Action<object[]>>> _eventListeners = new Dictionary<string, List<Action<object[]>>>();
        private Dictionary<string, List<Action>> _simpleEventListeners = new Dictionary<string, List<Action>>();
        
        [Header("Event System Configuration")]
        [SerializeField] private bool showDebugLogs = false;
        [SerializeField] private int maxEventQueueSize = 100;

        private Queue<EventData> _eventQueue = new Queue<EventData>();
        private bool _isProcessingEvents = false;

        public static EventSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<EventSystem>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("EventSystem");
                        _instance = go.AddComponent<EventSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (showDebugLogs)
            {
                Debug.Log("[EventSystem] EventSystem inizializzato.");
            }
        }

        /// <summary>
        /// Registra un listener per un evento con parametri
        /// </summary>
        public void Subscribe(string eventName, Action<object[]> listener)
        {
            if (string.IsNullOrEmpty(eventName) || listener == null) return;

            if (!_eventListeners.ContainsKey(eventName))
            {
                _eventListeners[eventName] = new List<Action<object[]>>();
            }

            if (!_eventListeners[eventName].Contains(listener))
            {
                _eventListeners[eventName].Add(listener);
                
                if (showDebugLogs)
                {
                    Debug.Log($"[EventSystem] Listener registrato per evento: {eventName}");
                }
            }
        }

        /// <summary>
        /// Registra un listener per un evento senza parametri
        /// </summary>
        public void Subscribe(string eventName, Action listener)
        {
            if (string.IsNullOrEmpty(eventName) || listener == null) return;

            if (!_simpleEventListeners.ContainsKey(eventName))
            {
                _simpleEventListeners[eventName] = new List<Action>();
            }

            if (!_simpleEventListeners[eventName].Contains(listener))
            {
                _simpleEventListeners[eventName].Add(listener);
                
                if (showDebugLogs)
                {
                    Debug.Log($"[EventSystem] Listener semplice registrato per evento: {eventName}");
                }
            }
        }

        /// <summary>
        /// Rimuove un listener per un evento con parametri
        /// </summary>
        public void Unsubscribe(string eventName, Action<object[]> listener)
        {
            if (string.IsNullOrEmpty(eventName) || listener == null) return;

            if (_eventListeners.ContainsKey(eventName))
            {
                _eventListeners[eventName].Remove(listener);
                
                if (showDebugLogs)
                {
                    Debug.Log($"[EventSystem] Listener rimosso per evento: {eventName}");
                }
            }
        }

        /// <summary>
        /// Rimuove un listener per un evento senza parametri
        /// </summary>
        public void Unsubscribe(string eventName, Action listener)
        {
            if (string.IsNullOrEmpty(eventName) || listener == null) return;

            if (_simpleEventListeners.ContainsKey(eventName))
            {
                _simpleEventListeners[eventName].Remove(listener);
                
                if (showDebugLogs)
                {
                    Debug.Log($"[EventSystem] Listener semplice rimosso per evento: {eventName}");
                }
            }
        }

        /// <summary>
        /// Emette un evento con parametri
        /// </summary>
        public void Emit(string eventName, params object[] parameters)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            // Aggiungi alla coda se stiamo già processando eventi
            if (_isProcessingEvents)
            {
                if (_eventQueue.Count < maxEventQueueSize)
                {
                    _eventQueue.Enqueue(new EventData(eventName, parameters, false));
                }
                return;
            }

            ProcessEvent(eventName, parameters, false);
        }

        /// <summary>
        /// Emette un evento senza parametri
        /// </summary>
        public void Emit(string eventName)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            // Aggiungi alla coda se stiamo già processando eventi
            if (_isProcessingEvents)
            {
                if (_eventQueue.Count < maxEventQueueSize)
                {
                    _eventQueue.Enqueue(new EventData(eventName, null, true));
                }
                return;
            }

            ProcessEvent(eventName, null, true);
        }

        /// <summary>
        /// Emette un evento immediatamente (bypass coda)
        /// </summary>
        public void EmitImmediate(string eventName, params object[] parameters)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            ProcessEvent(eventName, parameters, false);
        }

        private void ProcessEvent(string eventName, object[] parameters, bool isSimple)
        {
            _isProcessingEvents = true;

            try
            {
                if (isSimple)
                {
                    // Processa eventi semplici
                    if (_simpleEventListeners.ContainsKey(eventName))
                    {
                        var listeners = new List<Action>(_simpleEventListeners[eventName]);
                        foreach (var listener in listeners)
                        {
                            try
                            {
                                listener?.Invoke();
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"[EventSystem] Errore nel listener per evento {eventName}: {e.Message}");
                            }
                        }
                    }
                }
                else
                {
                    // Processa eventi con parametri
                    if (_eventListeners.ContainsKey(eventName))
                    {
                        var listeners = new List<Action<object[]>>(_eventListeners[eventName]);
                        foreach (var listener in listeners)
                        {
                            try
                            {
                                listener?.Invoke(parameters);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"[EventSystem] Errore nel listener per evento {eventName}: {e.Message}");
                            }
                        }
                    }
                }

                if (showDebugLogs)
                {
                    Debug.Log($"[EventSystem] Evento emesso: {eventName}");
                }
            }
            finally
            {
                _isProcessingEvents = false;
                
                // Processa eventi in coda
                ProcessEventQueue();
            }
        }

        private void ProcessEventQueue()
        {
            while (_eventQueue.Count > 0)
            {
                var eventData = _eventQueue.Dequeue();
                ProcessEvent(eventData.EventName, eventData.Parameters, eventData.IsSimple);
            }
        }

        /// <summary>
        /// Rimuove tutti i listener per un evento specifico
        /// </summary>
        public void ClearEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            if (_eventListeners.ContainsKey(eventName))
            {
                _eventListeners[eventName].Clear();
            }

            if (_simpleEventListeners.ContainsKey(eventName))
            {
                _simpleEventListeners[eventName].Clear();
            }

            if (showDebugLogs)
            {
                Debug.Log($"[EventSystem] Evento cancellato: {eventName}");
            }
        }

        /// <summary>
        /// Rimuove tutti i listener
        /// </summary>
        public void ClearAllEvents()
        {
            _eventListeners.Clear();
            _simpleEventListeners.Clear();
            _eventQueue.Clear();

            if (showDebugLogs)
            {
                Debug.Log("[EventSystem] Tutti gli eventi cancellati.");
            }
        }

        /// <summary>
        /// Ottiene il numero di listener per un evento
        /// </summary>
        public int GetListenerCount(string eventName)
        {
            if (string.IsNullOrEmpty(eventName)) return 0;

            int count = 0;
            
            if (_eventListeners.ContainsKey(eventName))
            {
                count += _eventListeners[eventName].Count;
            }

            if (_simpleEventListeners.ContainsKey(eventName))
            {
                count += _simpleEventListeners[eventName].Count;
            }

            return count;
        }

        /// <summary>
        /// Ottiene informazioni di debug sul sistema eventi
        /// </summary>
        public string GetDebugInfo()
        {
            string info = "EventSystem Debug Info:\n";
            info += $"Eventi con parametri: {_eventListeners.Count}\n";
            info += $"Eventi semplici: {_simpleEventListeners.Count}\n";
            info += $"Eventi in coda: {_eventQueue.Count}\n";
            info += $"Processando eventi: {_isProcessingEvents}\n";
            
            foreach (var kvp in _eventListeners)
            {
                info += $"  {kvp.Key}: {kvp.Value.Count} listeners\n";
            }
            
            foreach (var kvp in _simpleEventListeners)
            {
                info += $"  {kvp.Key}: {kvp.Value.Count} listeners\n";
            }
            
            return info;
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                ClearAllEvents();
                _instance = null;
            }
        }

        private class EventData
        {
            public string EventName { get; }
            public object[] Parameters { get; }
            public bool IsSimple { get; }

            public EventData(string eventName, object[] parameters, bool isSimple)
            {
                EventName = eventName;
                Parameters = parameters;
                IsSimple = isSimple;
            }
        }
    }
}
