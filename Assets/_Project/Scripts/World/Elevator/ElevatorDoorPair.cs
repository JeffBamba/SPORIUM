using System.Collections;
using System.Collections.Generic;
using Sporae.DevTools;
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

    [Tooltip("Durante l'animazione alza le ante sopra al player. Con elevator_mask alza le ante solo in chiusura se occludePlayer=true (player in cabina).")]

    [SerializeField] private bool raiseSortingDuringAnimation = true;



    [Tooltip("Sorting order temporaneo usato mentre le ante si muovono (solo senza elevator_mask).")]

    [SerializeField] private int animationSortingOrder = 200;



    [Header("Blocco cammino 2.5D")]

    [Tooltip("Collider solidi (layer WalkBlocker) sul piano di cammino: attivi a porte chiuse, disattivi a porte aperte. Se vuoto, cerca figli BLK_DoorThreshold.")]

    [SerializeField] private Collider2D[] walkBlockers;

    [Tooltip("Collider laterali cabina (BLK_CabinSide_*): attivi solo a porte aperte; disattivi a porte chiuse (come complemento di BLK_DoorThreshold).")]
    [SerializeField] private Collider2D[] cabinSideWalkBlockers;



    [Header("Maschera muro (elevator_mask)")]

    [Tooltip("Sprite unico che copre sx e dx mentre scorrono nel muro. Attivo solo durante l'animazione; a porte aperte entrambi i portelloni restano nascosti.")]

    [SerializeField] private SpriteRenderer elevatorMask;



    [Tooltip("Sorting order applicato a elevator_mask solo durante l'animazione.")]

    [SerializeField] private int wallMaskActiveSortingOrder = 12;



    [Tooltip("Sorting order portelloni a porte chiuse quando il player è fuori cabina (deve restare sotto il player).")]

    [SerializeField] private int closedSortingOrderWhenPlayerOutside = 5;



    private Vector3 _leftClosedLocalPos;

    private Vector3 _rightClosedLocalPos;

    private SpriteRenderer[] _leftDoorRenderers;

    private SpriteRenderer[] _rightDoorRenderers;

    private int[] _leftOriginalSortingOrders;

    private int[] _rightOriginalSortingOrders;

    private bool _captured;

    private bool _sortingRaised;

    private bool _occludePlayerDuringAnimation;

    private bool _pendingCloseAfterOpen;

    private bool _idleSortingLoweredForPlayerOutside;

    private int _floorIndex = -1;

    private ElevatorSystem _elevatorSystem;

    private Coroutine _anim;



    public bool IsOpen { get; private set; }

    public bool IsAnimating => _anim != null;

    public bool IsOpening => IsAnimating && IsOpen;

    public bool IsClosing => IsAnimating && !IsOpen;

    public float AnimationDuration => Mathf.Max(0.0001f, animDuration);



    /// <summary>Registrato da <see cref="ElevatorSystem"/> (indice allineato a levels[] / floorDoors[]).</summary>

    public void BindFloor(int floorIndex, ElevatorSystem elevatorSystem)

    {

        _floorIndex = floorIndex;

        _elevatorSystem = elevatorSystem;

        RefreshIdleClosedSorting();

    }



    private bool HasElevatorMask => elevatorMask != null;

    private bool IsIgnoredRootChild(Transform child)
    {
        if (child == null)
            return true;

        if (child == leftDoor || child == rightDoor)
            return true;

        // Es. ELEV_Doors_LVL_-1_portelloni: le ante sono nipoti, non figli diretti della root.
        if (leftDoor != null && child == leftDoor.parent)
            return true;

        if (rightDoor != null && child == rightDoor.parent)
            return true;

        return false;
    }

    private void Awake()

    {

        CacheWalkBlockers();

        CacheCabinSideWalkBlockers();

        CacheElevatorMask();

        CaptureClosedPositions();

        if (startClosed)

            ApplyInstant(false);

        ApplyElevatorMaskIdle();

        ApplyCabinSideWalkBlockers(false);

    }



    private void OnDisable()

    {

        _pendingCloseAfterOpen = false;

        RestoreSortingAfterAnimation();

        RestoreIdleSortingToSceneOriginal();

        SetDoorRenderersVisible(true);

        ApplyElevatorMaskIdle();

        ApplyCabinSideWalkBlockers(false);

    }



    private void LateUpdate()

    {

        RefreshIdleClosedSorting();

    }



    private void CacheWalkBlockers()

    {

        if (walkBlockers != null && walkBlockers.Length > 0)

            return;



        var found = new List<Collider2D>();

        for (int i = 0; i < transform.childCount; i++)

        {

            Transform child = transform.GetChild(i);

            if (IsIgnoredRootChild(child))
                continue;

            if (!child.name.StartsWith("BLK_Door", System.StringComparison.OrdinalIgnoreCase))

                continue;



            Collider2D col = child.GetComponent<Collider2D>();

            if (col != null)

                found.Add(col);

        }



        walkBlockers = found.ToArray();

    }



    private void CacheCabinSideWalkBlockers()

    {

        if (cabinSideWalkBlockers != null && cabinSideWalkBlockers.Length > 0)

            return;



        var found = new List<Collider2D>();

        for (int i = 0; i < transform.childCount; i++)

        {

            Transform child = transform.GetChild(i);

            if (IsIgnoredRootChild(child))

                continue;



            if (!child.name.StartsWith("BLK_CabinSide", System.StringComparison.OrdinalIgnoreCase))

                continue;



            Collider2D col = child.GetComponent<Collider2D>();

            if (col != null)

                found.Add(col);

        }



        cabinSideWalkBlockers = found.ToArray();

    }



    private void ApplyCabinSideWalkBlockers(bool doorsOpen)

    {

        if (cabinSideWalkBlockers == null)

            return;



        for (int i = 0; i < cabinSideWalkBlockers.Length; i++)

        {

            if (cabinSideWalkBlockers[i] != null)

                cabinSideWalkBlockers[i].enabled = doorsOpen;

        }

    }



    private void SyncDoorWalkBlockers(bool doorsOpen)

    {

        ApplyWalkBlockers(!doorsOpen);

        ApplyCabinSideWalkBlockers(doorsOpen);

    }



    private void CacheElevatorMask()

    {

        if (elevatorMask != null)

            return;



        for (int i = 0; i < transform.childCount; i++)

        {

            Transform child = transform.GetChild(i);

            if (IsIgnoredRootChild(child))
                continue;

            if (!child.name.Contains("elevator_mask", System.StringComparison.OrdinalIgnoreCase))

                continue;



            elevatorMask = child.GetComponent<SpriteRenderer>();

            if (elevatorMask != null)

                return;

        }

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

        _leftDoorRenderers = CollectDoorRenderers(leftDoor);

        _rightDoorRenderers = CollectDoorRenderers(rightDoor);

        _leftOriginalSortingOrders = CaptureSortingOrders(_leftDoorRenderers);

        _rightOriginalSortingOrders = CaptureSortingOrders(_rightDoorRenderers);

    }



    private static SpriteRenderer[] CollectDoorRenderers(Transform doorRoot)

    {

        if (doorRoot == null)

            return System.Array.Empty<SpriteRenderer>();



        return doorRoot.GetComponentsInChildren<SpriteRenderer>(true);

    }



    private static int[] CaptureSortingOrders(SpriteRenderer[] renderers)

    {

        var orders = new int[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)

            orders[i] = renderers[i] != null ? renderers[i].sortingOrder : 0;

        return orders;

    }



    private int GetMaxDoorSortingOrder()

    {

        int max = int.MinValue;

        max = MaxSortingOrder(max, _leftOriginalSortingOrders, _leftDoorRenderers);

        max = MaxSortingOrder(max, _rightOriginalSortingOrders, _rightDoorRenderers);

        return max == int.MinValue ? 0 : max;

    }



    private static int MaxSortingOrder(int current, int[] originalOrders, SpriteRenderer[] renderers)

    {

        if (originalOrders != null)

        {

            for (int i = 0; i < originalOrders.Length; i++)

                current = Mathf.Max(current, originalOrders[i]);

        }



        if (renderers != null)

        {

            for (int i = 0; i < renderers.Length; i++)

            {

                if (renderers[i] != null)

                    current = Mathf.Max(current, renderers[i].sortingOrder);

            }

        }



        return current;

    }



    /// <summary>Apre le ante (animazione se attivo, istantaneo se l'oggetto è disattivo).</summary>

    public void Open()
    {
        if (!isActiveAndEnabled) { ApplyInstant(true); return; }
        if (_anim != null && IsOpen)
            return;

        _pendingCloseAfterOpen = false;
        StartAnim(true);
    }

    /// <summary>
    /// Chiude le ante (animazione se attivo, istantaneo se l'oggetto è disattivo).
    /// Con <paramref name="occludePlayer"/> true alza sorting portelloni/maschera sopra il player
    /// anche se elevator_mask è assegnata (es. chiusura con player in cabina).
    /// </summary>
    public void Close(bool occludePlayer = false)
    {
        if (!isActiveAndEnabled) { _pendingCloseAfterOpen = false; ApplyInstant(false); return; }
        if (_anim != null)
        {
            if (IsOpen)
            {
                _pendingCloseAfterOpen = true;
                _occludePlayerDuringAnimation = occludePlayer;
                return;
            }

            return;
        }

        _pendingCloseAfterOpen = false;
        _occludePlayerDuringAnimation = occludePlayer;
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

        if (open)
            _occludePlayerDuringAnimation = false;

        if (!open)
        {
            SetDoorRenderersVisible(true);
            SyncDoorWalkBlockers(false);
        }



        RaiseSortingForAnimation();

        ApplyElevatorMaskActive();



        // BLK_DoorThreshold attivo per tutta l'animazione; BLK_CabinSide solo a porte completamente aperte.
        SyncDoorWalkBlockers(false);



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



        // Solo a fine animazione: soglia frontale ↔ laterali cabina in base allo stato finale porte.
        SyncDoorWalkBlockers(open);



        RestoreSortingAfterAnimation();

        ApplyElevatorMaskIdle();

        SetDoorRenderersVisible(!open || !HasElevatorMask);

        if (open && _pendingCloseAfterOpen)
        {
            _pendingCloseAfterOpen = false;
            _anim = StartCoroutine(AnimRoutine(false));
            yield break;
        }

        _anim = null;

        if (!open)
            RefreshIdleClosedSorting();
    }



    private void ApplyInstant(bool open)
    {
        _pendingCloseAfterOpen = false;
        CaptureClosedPositions();

        IsOpen = open;

        if (leftDoor != null)

            leftDoor.localPosition = _leftClosedLocalPos + (open ? new Vector3(-slideDistance, 0f, 0f) : Vector3.zero);

        if (rightDoor != null)

            rightDoor.localPosition = _rightClosedLocalPos + (open ? new Vector3(slideDistance, 0f, 0f) : Vector3.zero);



        SyncDoorWalkBlockers(open);

        ApplyElevatorMaskIdle();

        SetDoorRenderersVisible(!open || !HasElevatorMask);

        if (!open)
            RefreshIdleClosedSorting();

    }



    private void RaiseSortingForAnimation()

    {

        if (!raiseSortingDuringAnimation)

            return;



        // Con elevator_mask le ante restano al sorting di scena salvo chiusura con player in cabina.
        if (HasElevatorMask && !_occludePlayerDuringAnimation)

            return;



        RaiseDoorRenderers(_leftDoorRenderers, animationSortingOrder);

        RaiseDoorRenderers(_rightDoorRenderers, animationSortingOrder);

        _sortingRaised = true;

    }



    private void RestoreSortingAfterAnimation()

    {

        if (!_sortingRaised)

            return;



        RestoreDoorRenderers(_leftDoorRenderers, _leftOriginalSortingOrders);

        RestoreDoorRenderers(_rightDoorRenderers, _rightOriginalSortingOrders);

        _sortingRaised = false;

    }



    /// <summary>
    /// A porte chiuse e ferme: sorting di scena se il player è in cabina su questo piano,
    /// altrimenti order basso così il player resta davanti ai portelloni nel pianerottolo.
    /// </summary>
    private void RefreshIdleClosedSorting()

    {

        if (IsAnimating || _sortingRaised || IsOpen || _elevatorSystem == null || _floorIndex < 0)

            return;



        CaptureClosedPositions();



        bool playerInsideOnFloor = _elevatorSystem.IsPlayerInsideCabinOnFloor(_floorIndex);



        if (playerInsideOnFloor)

        {

            if (_idleSortingLoweredForPlayerOutside)

                RestoreIdleSortingToSceneOriginal();

            return;

        }



        RaiseDoorRenderers(_leftDoorRenderers, closedSortingOrderWhenPlayerOutside);

        RaiseDoorRenderers(_rightDoorRenderers, closedSortingOrderWhenPlayerOutside);

        _idleSortingLoweredForPlayerOutside = true;

    }



    private void RestoreIdleSortingToSceneOriginal()

    {

        if (!_idleSortingLoweredForPlayerOutside)

            return;



        CaptureClosedPositions();

        RestoreDoorRenderers(_leftDoorRenderers, _leftOriginalSortingOrders);

        RestoreDoorRenderers(_rightDoorRenderers, _rightOriginalSortingOrders);

        _idleSortingLoweredForPlayerOutside = false;

    }



    private static void RaiseDoorRenderers(SpriteRenderer[] renderers, int sortingOrder)

    {

        if (renderers == null)

            return;



        for (int i = 0; i < renderers.Length; i++)

        {

            if (renderers[i] != null)

                renderers[i].sortingOrder = sortingOrder;

        }

    }



    private static void RestoreDoorRenderers(SpriteRenderer[] renderers, int[] originalSortingOrders)

    {

        if (renderers == null || originalSortingOrders == null)

            return;



        for (int i = 0; i < renderers.Length && i < originalSortingOrders.Length; i++)

        {

            if (renderers[i] != null)

                renderers[i].sortingOrder = originalSortingOrders[i];

        }

    }



    private void ApplyElevatorMaskActive()

    {

        if (!HasElevatorMask)

            return;



        elevatorMask.enabled = true;

        if (_sortingRaised)
            elevatorMask.sortingOrder = animationSortingOrder + 1;
        else
            elevatorMask.sortingOrder = Mathf.Max(wallMaskActiveSortingOrder, GetMaxDoorSortingOrder() + 1);

    }



    private void ApplyElevatorMaskIdle()

    {

        if (!HasElevatorMask)

            return;



        elevatorMask.enabled = false;

    }



    private void SetDoorRenderersVisible(bool visible)

    {

        SetRenderersVisible(_leftDoorRenderers, visible);

        SetRenderersVisible(_rightDoorRenderers, visible);

    }



    private static void SetRenderersVisible(SpriteRenderer[] renderers, bool visible)

    {

        if (renderers == null)

            return;



        for (int i = 0; i < renderers.Length; i++)

        {

            if (renderers[i] != null)

                renderers[i].enabled = visible;

        }

    }



    [ContextMenu("Test - Open")]

    private void TestOpen() => Open();



    [ContextMenu("Test - Close")]

    private void TestClose() => Close();

}


