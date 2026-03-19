using System;
using System.Collections.Generic;
using UnityEngine;
using _Project.Sporae.Core;
using Sporae.DevTools;

/// <summary>
/// Gestisce i 3 CryoSlot della Cryo Machine. Registrato nel ServiceContainer come
/// punto di accesso unico ai slot passivi della Dome.
/// I CryoSlot non partecipano mai al loop produttivo del DayCycleController.
/// </summary>
public class CryoMachineController : MonoBehaviour
{
    [Header("Cryo Slots")]
    [SerializeField] private CryoSlot[] _slots;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = true;

    private void Awake()
    {
        if (ServiceContainer.Instance != null)
            ServiceContainer.Instance.Register(this);

        if (_showDebugLogs)
            SporiumLogger.LogInfo(LogCategory.Dome, $"CryoMachineController registrato con {(_slots != null ? _slots.Length : 0)} slot.");
    }

    private void OnDestroy()
    {
        // Non deregistrare dal ServiceContainer: il container non supporta de-register
        // e la scena viene distrutta prima che il riferimento venga usato di nuovo.
    }

    // ─────────────────────────────────────────────────────────────────
    //  Accesso agli slot
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Restituisce una snapshot read-only di tutti e 3 i CryoSlot.
    /// </summary>
    public IReadOnlyList<CryoSlot> GetPassiveSlotsSnapshot()
    {
        return _slots ?? Array.Empty<CryoSlot>();
    }

    /// <summary>
    /// Cerca lo slot per ID. Ritorna null se non trovato.
    /// </summary>
    public CryoSlot GetSlotById(string slotId)
    {
        if (_slots == null || string.IsNullOrEmpty(slotId)) return null;
        foreach (var s in _slots)
        {
            if (s != null && string.Equals(s.SlotId, slotId, StringComparison.OrdinalIgnoreCase))
                return s;
        }
        return null;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Trasferimento
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tenta di occupare il primo slot libero con il payload fornito.
    /// </summary>
    /// <param name="payload">Payload della pianta da conservare.</param>
    /// <param name="occupied">Lo slot che è stato occupato, se il metodo ritorna true.</param>
    /// <returns>True se è stato trovato e occupato uno slot libero.</returns>
    public bool TryOccupySlot(CryoPlantPayload payload, out CryoSlot occupied)
    {
        occupied = null;
        if (_slots == null) return false;

        foreach (var slot in _slots)
        {
            if (slot != null && !slot.IsOccupied)
            {
                if (slot.Occupy(payload))
                {
                    occupied = slot;
                    if (_showDebugLogs)
                        SporiumLogger.LogInfo(LogCategory.Dome, $"CryoMachineController: pianta {payload?.PlantCode} trasferita in {slot.SlotId}.");
                    return true;
                }
            }
        }

        if (_showDebugLogs)
            SporiumLogger.LogWarning(LogCategory.Dome, "CryoMachineController: nessuno slot libero disponibile.");
        return false;
    }

    /// <summary>
    /// Libera lo slot indicato e restituisce il payload salvato.
    /// </summary>
    public CryoPlantPayload FreeSlot(CryoSlot slot)
    {
        if (slot == null) return null;
        return slot.Free();
    }

    /// <summary>
    /// Restituisce il numero di slot attualmente occupati.
    /// </summary>
    public int OccupiedCount()
    {
        if (_slots == null) return 0;
        int count = 0;
        foreach (var s in _slots)
            if (s != null && s.IsOccupied) count++;
        return count;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Save / Load
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Serializza lo stato corrente di tutti gli slot per il salvataggio.
    /// </summary>
    public List<CryoSlotSaveEntry> CollectSaveData()
    {
        var entries = new List<CryoSlotSaveEntry>();
        if (_slots == null) return entries;

        foreach (var slot in _slots)
        {
            if (slot == null) continue;
            entries.Add(new CryoSlotSaveEntry
            {
                slotId      = slot.SlotId,
                isOccupied  = slot.IsOccupied,
                payloadJson = slot.IsOccupied ? JsonUtility.ToJson(slot.Payload) : null
            });
        }
        return entries;
    }

    /// <summary>
    /// Ripristina lo stato degli slot da una lista di entry salvate.
    /// </summary>
    public void RestoreFromSave(List<CryoSlotSaveEntry> entries)
    {
        if (entries == null || _slots == null) return;

        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry.slotId) || !entry.isOccupied) continue;

            var slot = GetSlotById(entry.slotId);
            if (slot == null)
            {
                SporiumLogger.LogWarning(LogCategory.Dome, $"CryoMachineController: slot {entry.slotId} non trovato durante il ripristino.");
                continue;
            }

            if (string.IsNullOrEmpty(entry.payloadJson)) continue;

            var payload = JsonUtility.FromJson<CryoPlantPayload>(entry.payloadJson);
            if (payload != null)
            {
                slot.Occupy(payload);
                if (_showDebugLogs)
                    SporiumLogger.LogInfo(LogCategory.Dome, $"CryoMachineController: ripristinato {payload.PlantCode} in {entry.slotId}.");
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Log Cryo Slot Status")]
    private void EditorLogStatus()
    {
        if (_slots == null) { Debug.Log("Nessuno slot configurato."); return; }
        foreach (var s in _slots)
        {
            if (s == null) continue;
            Debug.Log(s.IsOccupied
                ? $"[{s.SlotId}] OCCUPATO — {s.Payload?.PlantCode} Lvl {s.Payload?.PlantLevel}"
                : $"[{s.SlotId}] VUOTO");
        }
    }
#endif
}

/// <summary>
/// Dati di un singolo CryoSlot da serializzare nel file di salvataggio.
/// Dichiarata in namespace globale per accessibilità da SaveManager.
/// </summary>
[Serializable]
public class CryoSlotSaveEntry
{
    public string slotId;
    public bool isOccupied;
    public string payloadJson;
}
