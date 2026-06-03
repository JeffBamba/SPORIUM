using System.Collections;
using System.Collections.Generic;
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

    [Header("Occlusione player")]
    [Tooltip("Durante apertura/chiusura porta le ante sopra al player, poi ripristina il sorting originale.")]
    [SerializeField] private bool raiseSortingDuringAnimation = true;

    [Tooltip("Sorting order temporaneo usato mentre le ante si muovono.")]
    [SerializeField] private int animationSortingOrder = 200;

    [Header("Blocco cammino 2.5D")]
    [Tooltip("Collider solidi (layer WalkBlocker) sul piano di cammino: attivi a porte chiuse, disattivi a porte aperte. Se vuoto, cerca figli BLK_DoorThreshold.")]
    [SerializeField] private Collider2D[] walkBlockers;

    private Vector3 _leftClosedLocalPos;
    private Vector3 _rightClosedLocalPos;
    private SpriteRenderer[] _doorRenderers;
    private int[] _originalSortingOrders;
    private bool _captured;
    private bool _sortingRaised;
    private Coroutine _anim;

    public bool IsOpen { get; private set; }
    public bool IsAnimating => _anim != null;
    public float AnimationDuration => Mathf.Max(0.0001f, animDuration);

    private void Awake()
    {
        CacheWalkBlockers();
        CaptureClosedPositions();
        if (startClosed)
            ApplyInstant(false);
    }

    private void OnDisable()
    {
        RestoreSortingAfterAnimation();
    }

    private void CacheWalkBlockers()
    {
        if (walkBlockers != null && walkBlockers.Length > 0)
            return;

        var found = new List<Collider2D>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == leftDoor || child == rightDoor)
                continue;

            if (!child.name.StartsWith("BLK_Door", System.StringComparison.OrdinalIgnoreCase))
                continue;

            Collider2D col = child.GetComponent<Collider2D>();
            if (col != null)
                found.Add(col);
        }

        walkBlockers = found.ToArray();
    }

    private void ApplyWalkBlockers(bool doorClosed)
    {
        if (walkBlockers == null)
            return;

        for (int i = 0; i < walkBlockers.Length; i++)
        {
            if (walkBlockers[i] != null)
                walkBlockers[i].enabled = doorClosed;
        }
    }

    private void CaptureClosedPositions()
    {
        if (_captured) return;
        if (leftDoor != null) _leftClosedLocalPos = leftDoor.localPosition;
        if (rightDoor != null) _rightClosedLocalPos = rightDoor.localPosition;
        CacheDoorRenderers();
        _captured = true;
    }

    private void CacheDoorRenderers()
    {
        var renderers = new List<SpriteRenderer>();
        if (leftDoor != null)
            renderers.AddRange(leftDoor.GetComponentsInChildren<SpriteRenderer>(true));
        if (rightDoor != null)
            renderers.AddRange(rightDoor.GetComponentsInChildren<SpriteRenderer>(true));

        _doorRenderers = renderers.ToArray();
        _originalSortingOrders = new int[_doorRenderers.Length];
        for (int i = 0; i < _doorRenderers.Length; i++)
            _originalSortingOrders[i] = _doorRenderers[i] != null ? _doorRenderers[i].sortingOrder : 0;
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
        RaiseSortingForAnimation();

        if (open)
            ApplyWalkBlockers(false);

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

        if (!open)
            ApplyWalkBlockers(true);

        RestoreSortingAfterAnimation();
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

        ApplyWalkBlockers(!open);
    }

    private void RaiseSortingForAnimation()
    {
        if (!raiseSortingDuringAnimation || _doorRenderers == null)
            return;

        for (int i = 0; i < _doorRenderers.Length; i++)
        {
            if (_doorRenderers[i] != null)
                _doorRenderers[i].sortingOrder = animationSortingOrder;
        }

        _sortingRaised = true;
    }

    private void RestoreSortingAfterAnimation()
    {
        if (!_sortingRaised || _doorRenderers == null || _originalSortingOrders == null)
            return;

        for (int i = 0; i < _doorRenderers.Length && i < _originalSortingOrders.Length; i++)
        {
            if (_doorRenderers[i] != null)
                _doorRenderers[i].sortingOrder = _originalSortingOrders[i];
        }

        _sortingRaised = false;
    }

    [ContextMenu("Test - Open")]
    private void TestOpen() => Open();

    [ContextMenu("Test - Close")]
    private void TestClose() => Close();
}
