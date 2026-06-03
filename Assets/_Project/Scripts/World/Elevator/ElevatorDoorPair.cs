using System.Collections;
using UnityEngine;

/// <summary>
/// Coppia di ante (sinistra/destra) per le porte dell'ascensore di UN piano.
/// Le ante scorrono lateralmente per aprirsi (sx verso -X, dx verso +X) e tornano
/// alla posizione iniziale (chiuso) per chiudersi.
/// La posizione "chiuso" è quella autorata in scena, catturata in Awake.
/// Se un'anta non è assegnata, i metodi sono no-op (fallback non bloccante).
/// </summary>
[DisallowMultipleComponent]
public class ElevatorDoorPair : MonoBehaviour
{
    [Header("Ante (assegnare i due Transform/SpriteRenderer)")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;

    [Header("Apertura")]
    [Tooltip("Distanza laterale di cui scorre OGNI anta per aprirsi (unità mondo). Sx va verso -X, Dx verso +X.")]
    [SerializeField] private float slideDistance = 1.0f;

    [Tooltip("Durata dell'animazione di apertura/chiusura (secondi).")]
    [SerializeField] private float animDuration = 0.5f;

    [Tooltip("Se true, all'avvio le porte sono forzate chiuse.")]
    [SerializeField] private bool startClosed = true;

    private Vector3 _leftClosedLocalPos;
    private Vector3 _rightClosedLocalPos;
    private bool _captured;
    private Coroutine _anim;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        CaptureClosedPositions();
        if (startClosed)
            ApplyInstant(false);
    }

    private void CaptureClosedPositions()
    {
        if (_captured) return;
        if (leftDoor != null) _leftClosedLocalPos = leftDoor.localPosition;
        if (rightDoor != null) _rightClosedLocalPos = rightDoor.localPosition;
        _captured = true;
    }

    /// <summary>Apre le ante (animazione se attivo, istantaneo se l'oggetto è disattivo).</summary>
    public void Open()
    {
        if (!isActiveAndEnabled) { ApplyInstant(true); return; }
        StartAnim(true);
    }

    /// <summary>Chiude le ante (animazione se attivo, istantaneo se l'oggetto è disattivo).</summary>
    public void Close()
    {
        if (!isActiveAndEnabled) { ApplyInstant(false); return; }
        StartAnim(false);
    }

    /// <summary>Imposta lo stato aperto/chiuso senza animazione.</summary>
    public void SetInstant(bool open) => ApplyInstant(open);

    private void StartAnim(bool open)
    {
        CaptureClosedPositions();
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(AnimRoutine(open));
    }

    private IEnumerator AnimRoutine(bool open)
    {
        IsOpen = open;

        Vector3 leftFrom = leftDoor != null ? leftDoor.localPosition : Vector3.zero;
        Vector3 rightFrom = rightDoor != null ? rightDoor.localPosition : Vector3.zero;
        Vector3 leftTo = _leftClosedLocalPos + (open ? new Vector3(-slideDistance, 0f, 0f) : Vector3.zero);
        Vector3 rightTo = _rightClosedLocalPos + (open ? new Vector3(slideDistance, 0f, 0f) : Vector3.zero);

        float dur = Mathf.Max(0.0001f, animDuration);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            if (leftDoor != null) leftDoor.localPosition = Vector3.Lerp(leftFrom, leftTo, k);
            if (rightDoor != null) rightDoor.localPosition = Vector3.Lerp(rightFrom, rightTo, k);
            yield return null;
        }

        if (leftDoor != null) leftDoor.localPosition = leftTo;
        if (rightDoor != null) rightDoor.localPosition = rightTo;
        _anim = null;
    }

    private void ApplyInstant(bool open)
    {
        CaptureClosedPositions();
        IsOpen = open;
        if (leftDoor != null)
            leftDoor.localPosition = _leftClosedLocalPos + (open ? new Vector3(-slideDistance, 0f, 0f) : Vector3.zero);
        if (rightDoor != null)
            rightDoor.localPosition = _rightClosedLocalPos + (open ? new Vector3(slideDistance, 0f, 0f) : Vector3.zero);
    }

    [ContextMenu("Test - Open")]
    private void TestOpen() => Open();

    [ContextMenu("Test - Close")]
    private void TestClose() => Close();
}
