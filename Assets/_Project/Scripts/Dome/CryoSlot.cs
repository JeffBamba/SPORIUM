using UnityEngine;
using Sporae.DevTools;

/// <summary>
/// Rappresenta uno slot della Cryo Machine. Conserva una pianta Lvl 5 in stato di ibernazione:
/// nessun loop di watering/luce/mold, solo conservazione del payload e attivazione del PassivePower.
/// Non deriva da PotSlot — ha responsabilità radicalmente diverse.
/// </summary>
public class CryoSlot : MonoBehaviour
{
    [Header("Slot Configuration")]
    [SerializeField] private string _slotId = "CRYO-01";

    private CryoPlantPayload _payload;
    private bool _isOccupied;

    public string SlotId => _slotId;
    public bool IsOccupied => _isOccupied;

    /// <summary>
    /// Payload corrente. Null se lo slot è vuoto.
    /// </summary>
    public CryoPlantPayload Payload => _payload;

    private void Awake()
    {
        var controller = GetComponentInParent<CryoMachineController>();
        if (controller == null)
            SporiumLogger.LogWarning(LogCategory.Dome, $"[CryoSlot {_slotId}] CryoMachineController non trovato nel parent.");
    }

    /// <summary>
    /// Occupa lo slot con il payload fornito. Ritorna false se già occupato.
    /// </summary>
    public bool Occupy(CryoPlantPayload payload)
    {
        if (_isOccupied)
        {
            SporiumLogger.LogWarning(LogCategory.Dome, $"[CryoSlot {_slotId}] Tentativo di occupare uno slot già occupato.");
            return false;
        }

        _payload = payload;
        _isOccupied = true;
        SporiumLogger.LogInfo(LogCategory.Dome, $"[CryoSlot {_slotId}] Occupato con {payload?.PlantCode} Lvl {payload?.PlantLevel}.");
        return true;
    }

    /// <summary>
    /// Libera lo slot e restituisce il payload salvato. Ritorna null se già vuoto.
    /// </summary>
    public CryoPlantPayload Free()
    {
        if (!_isOccupied)
        {
            SporiumLogger.LogWarning(LogCategory.Dome, $"[CryoSlot {_slotId}] Tentativo di liberare uno slot già vuoto.");
            return null;
        }

        var result = _payload;
        _payload = null;
        _isOccupied = false;
        SporiumLogger.LogInfo(LogCategory.Dome, $"[CryoSlot {_slotId}] Liberato (era: {result?.PlantCode}).");
        return result;
    }

    /// <summary>
    /// Imposta l'ID dello slot (utile in editor o setup runtime).
    /// </summary>
    public void SetSlotId(string id) => _slotId = id;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _isOccupied ? Color.cyan : Color.grey;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
            _isOccupied ? $"{_slotId}\n{_payload?.PlantCode} Lvl{_payload?.PlantLevel}" : $"{_slotId}\n[EMPTY]");
    }
#endif
}
