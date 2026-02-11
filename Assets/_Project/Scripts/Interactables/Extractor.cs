using System.Collections;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.UI.UIToolkit.Lab;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using UnityEngine;

namespace _Project
{
    public enum ExtractorProcessState { Idle, InProgress, Completed }

    [RequireComponent(typeof(Interactable))]
    public class Extractor : Storage
    {
        [Header("Lab UI — prefer Foundation UIToolkit")]
        [SerializeField] private LabExtractorPanelController _labExtractorPanel;
        [SerializeField] private LabMinigameExtractor _labMiniGame;

        [Header("Stato sopra l'Extractor (opzionale)")]
        [SerializeField] private TMPro.TMP_Text _worldStatusLabel;

        [Header("Processo estrazione")]
        [SerializeField] private float _extractionDurationSeconds = 60f;

        private readonly Inventory _inventory = new();
        private Interactable _interactable;
        private LabUpgradesConfig _labUpgradesConfig;
        private DayCycleSystem _dayCycleSystem;

        /// <summary>Per ogni slot: 0=vuoto, 1=in corso, 2=completato. Fino a 3 processi in parallelo.</summary>
        private readonly int[] _slotStates = new int[3];
        private readonly float[] _slotProgress = new float[3];
        private readonly int[] _slotSpore = new int[3];
        private readonly int[] _slotCell001 = new int[3];
        private readonly int[] _slotCell002 = new int[3];
        private readonly int[] _slotCell003 = new int[3];
        private readonly int[] _slotPlannedSpore = new int[3];
        private readonly int[] _slotPlannedCell001 = new int[3];
        private readonly int[] _slotPlannedCell002 = new int[3];
        private readonly int[] _slotPlannedCell003 = new int[3];
        private readonly Coroutine[] _slotCoroutines = new Coroutine[3];
        /// <summary>Snapshot del frutto consumato per ogni slot (per tooltip output).</summary>
        private readonly ExtractionResultSnapshot[] _slotResultSnapshot = new ExtractionResultSnapshot[3];
        /// <summary>Frutto realmente consumato per ogni slot (per propagare metadata genetici all'output spora).</summary>
        private readonly Item[] _slotInputFruit = new Item[3];

        private static string ExtractorProgressToastKey(int slot) => $"extractor-progress-{slot}";

        public ExtractorProcessState State =>
            AnySlotInProgress() ? ExtractorProcessState.InProgress :
            CompletedCount() > 0 ? ExtractorProcessState.Completed : ExtractorProcessState.Idle;

        public float ExtractionProgress
        {
            get
            {
                for (int i = 0; i < 3; i++)
                    if (_slotStates[i] == 1) return _slotProgress[i];
                return CompletedCount() > 0 ? 1f : 0f;
            }
        }

        public int PendingSporeCount => _slotSpore[0] + _slotSpore[1] + _slotSpore[2];
        public int PendingCell001 => _slotCell001[0] + _slotCell001[1] + _slotCell001[2];
        public int PendingCell002 => _slotCell002[0] + _slotCell002[1] + _slotCell002[2];
        public int PendingCell003 => _slotCell003[0] + _slotCell003[1] + _slotCell003[2];

        public int CompletedCount()
        {
            int n = 0;
            for (int i = 0; i < 3; i++)
                if (_slotStates[i] == 2) n++;
            return n;
        }

        public bool AnySlotInProgress()
        {
            for (int i = 0; i < 3; i++)
                if (_slotStates[i] == 1) return true;
            return false;
        }

        public int FreeSlotIndex()
        {
            for (int i = 0; i < 3; i++)
                if (_slotStates[i] == 0) return i;
            return -1;
        }

        /// <summary>Restituisce lo snapshot del primo slot completato (per tooltip output). Null se nessuno completato.</summary>
        public ExtractionResultSnapshot GetFirstCompletedResultSnapshot()
        {
            for (int i = 0; i < 3; i++)
                if (_slotStates[i] == 2 && _slotResultSnapshot[i] != null)
                    return _slotResultSnapshot[i];
            return null;
        }

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            _interactable.OnInteract += HandleInteract;
            _labUpgradesConfig = Resources.Load<LabUpgradesConfig>("LabUpgradesConfig");
        }

        private void Start()
        {
            if (_labUpgradesConfig == null)
                _labUpgradesConfig = Resources.Load<LabUpgradesConfig>("LabUpgradesConfig");
            _dayCycleSystem = Sporae.Core.ServiceContainer.Instance?.Get<DayCycleSystem>();
            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged += HandleDayChanged;
            UpdateWorldStatusLabel();
        }

        private void OnDestroy()
        {
            _interactable.OnInteract -= HandleInteract;
            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged -= HandleDayChanged;
            for (int i = 0; i < 3; i++)
            {
                if (_slotCoroutines[i] != null)
                    StopCoroutine(_slotCoroutines[i]);
            }
        }

        private void HandleDayChanged(int day)
        {
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            for (int i = 0; i < 3; i++)
            {
                if (_slotStates[i] != 1) continue;
                if (_slotCoroutines[i] != null)
                {
                    StopCoroutine(_slotCoroutines[i]);
                    _slotCoroutines[i] = null;
                }
                CompleteSlot(i);
                if (foundation != null && foundation.Enabled)
                    foundation.RemoveToast(ExtractorProgressToastKey(i));
            }
            UpdateWorldStatusLabel();
            if (foundation != null && foundation.Enabled)
            {
                int ready = CompletedCount();
                if (ready > 0)
                    foundation.UpsertToast("extractor-done", "LAB-EXT-DONE", new NotificationPayload().With("count", ready.ToString()));
            }
        }

        private void Update()
        {
            if (State != ExtractorProcessState.Completed) return;
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
                foundation.UpsertToast("extractor-done", "LAB-EXT-DONE", new NotificationPayload().With("count", CompletedCount().ToString()));
        }

        private void HandleInteract()
        {
            if (_labExtractorPanel != null)
            {
                _labExtractorPanel.Show();
#if UNITY_EDITOR
                Debug.Log("[Extractor] Pannello Lab Extractor aperto.");
#endif
            }
            else if (_labMiniGame != null)
                _labMiniGame.Show();
#if UNITY_EDITOR
            else
                Debug.LogWarning("[Extractor] Nessun pannello assegnato.");
#endif
        }

        public override Inventory GetInventory()
        {
            return _inventory;
        }

        private bool HasStemCellModule =>
            (_labUpgradesConfig != null && _labUpgradesConfig.HasStemCellModule) ||
            (Sporae.Core.ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true)?.IsStemCellModuleUnlocked ?? false);

        public bool TryStartExtraction()
        {
            int idx = FreeSlotIndex();
            if (idx < 0) return false;

            var gm = Sporae.Core.ServiceContainer.Instance?.Get<GameManager>();
            if (gm == null) gm = FindObjectOfType<GameManager>();
            if (gm == null || gm.ActionSystem == null || gm.ActionSystem.ActionsLeft < 1)
            {
                var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                if (foundation != null && foundation.Enabled)
                    foundation.PostToastImmediate("ACT-050");
                return false;
            }
            if (!gm.TrySpendAction(1)) return false;

            bool hasStem = HasStemCellModule;
            if (_inventory.Has(Items.Fruits) && _inventory.TryRemoveFirst(Items.Fruits, out var fruit))
            {
                _slotInputFruit[idx] = fruit;
                _slotResultSnapshot[idx] = ExtractionResultSnapshot.FromFruit(fruit);
                SetSlotPlannedOutputs(idx, 1, 0, 1, 0);
                _slotCoroutines[idx] = StartCoroutine(RunExtraction(idx, 1, 0, 1, 0));
            }
            else if (_inventory.Has(Items.FruitsKnown) && _inventory.TryRemoveFirst(Items.FruitsKnown, out fruit))
            {
                _slotInputFruit[idx] = fruit;
                _slotResultSnapshot[idx] = ExtractionResultSnapshot.FromFruit(fruit);
                SetSlotPlannedOutputs(idx, 1, 0, 1, 0);
                _slotCoroutines[idx] = StartCoroutine(RunExtraction(idx, 1, 0, 1, 0));
            }
            else if (hasStem && _inventory.Has(Items.WholePlant))
            {
                _slotResultSnapshot[idx] = null;
                _slotInputFruit[idx] = null;
                _inventory.Consume(Items.WholePlant, 1);
                SetSlotPlannedOutputs(idx, 0, 1, 0, 0);
                _slotCoroutines[idx] = StartCoroutine(RunExtraction(idx, 0, 1, 0, 0));
            }
            else if (hasStem && _inventory.Has(Items.OrganicScrap001))
            {
                _slotResultSnapshot[idx] = null;
                _slotInputFruit[idx] = null;
                _inventory.Consume(Items.OrganicScrap001, 1);
                SetSlotPlannedOutputs(idx, 0, 1, 0, 0);
                _slotCoroutines[idx] = StartCoroutine(RunExtraction(idx, 0, 1, 0, 0));
            }
            else if (hasStem && _inventory.Has(Items.ProteinResidue))
            {
                _slotResultSnapshot[idx] = null;
                _slotInputFruit[idx] = null;
                _inventory.Consume(Items.ProteinResidue, 1);
                SetSlotPlannedOutputs(idx, 0, 0, 0, 1);
                _slotCoroutines[idx] = StartCoroutine(RunExtraction(idx, 0, 0, 0, 1));
            }
            else
                return false;

            _slotStates[idx] = 1;
            _slotProgress[idx] = 0f;
            UpdateWorldStatusLabel();
            var foundationStart = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundationStart != null && foundationStart.Enabled)
                foundationStart.UpsertToast(ExtractorProgressToastKey(idx), "LAB-EXT-START", new NotificationPayload().With("percent", "0"));
            return true;
        }

        private void CompleteSlot(int slotIndex)
        {
            _slotSpore[slotIndex] = _slotPlannedSpore[slotIndex];
            _slotCell001[slotIndex] = _slotPlannedCell001[slotIndex];
            _slotCell002[slotIndex] = _slotPlannedCell002[slotIndex];
            _slotCell003[slotIndex] = _slotPlannedCell003[slotIndex];
            _slotStates[slotIndex] = 2;
            _slotProgress[slotIndex] = 1f;
        }

        private void SetSlotPlannedOutputs(int slotIndex, int sporeOut, int cell001Out, int cell002Out, int cell003Out)
        {
            _slotPlannedSpore[slotIndex] = sporeOut;
            _slotPlannedCell001[slotIndex] = cell001Out;
            _slotPlannedCell002[slotIndex] = cell002Out;
            _slotPlannedCell003[slotIndex] = cell003Out;
        }

        private IEnumerator RunExtraction(int slotIndex, int sporeOut, int cell001Out, int cell002Out, int cell003Out)
        {
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            float elapsed = 0f;
            while (elapsed < _extractionDurationSeconds)
            {
                elapsed += Time.deltaTime;
                _slotProgress[slotIndex] = Mathf.Clamp01(elapsed / _extractionDurationSeconds);
                UpdateWorldStatusLabel();
                if (foundation != null && foundation.Enabled)
                {
                    int pct = Mathf.RoundToInt(_slotProgress[slotIndex] * 100f);
                    foundation.UpsertToast(ExtractorProgressToastKey(slotIndex), "LAB-EXT-START", new NotificationPayload().With("percent", pct.ToString()));
                }
                yield return null;
            }
            _slotSpore[slotIndex] = sporeOut;
            _slotCell001[slotIndex] = cell001Out;
            _slotCell002[slotIndex] = cell002Out;
            _slotCell003[slotIndex] = cell003Out;
            _slotStates[slotIndex] = 2;
            _slotProgress[slotIndex] = 1f;
            _slotCoroutines[slotIndex] = null;
            UpdateWorldStatusLabel();
            if (foundation != null && foundation.Enabled)
            {
                foundation.RemoveToast(ExtractorProgressToastKey(slotIndex));
                int ready = CompletedCount();
                foundation.UpsertToast("extractor-done", "LAB-EXT-DONE", new NotificationPayload().With("count", ready.ToString()));
            }
        }

        public const string ExtractorDoneToastKey = "extractor-done";

        public void CollectOutput(Inventory playerInventory)
        {
            if (playerInventory == null) return;
            int totalC1 = PendingCell001;
            int totalC2 = PendingCell002;
            int totalC3 = PendingCell003;

            // Spore: crea item per-slot preservando metadata del frutto madre.
            for (int i = 0; i < 3; i++)
            {
                if (_slotStates[i] != 2 || _slotSpore[i] <= 0) continue;
                for (int n = 0; n < _slotSpore[i]; n++)
                {
                    var spore = ItemFabric.CreateSporeRawFromFruit(_slotInputFruit[i]);
                    if (spore != null)
                        playerInventory.Add(spore);
                    else
                        playerInventory.AddSporeRaw(1);
                }
            }

            if (totalC1 > 0) playerInventory.Add(Items.StemCellVegetable, totalC1);
            if (totalC2 > 0) playerInventory.Add(Items.StemCellFungus, totalC2);
            if (totalC3 > 0) playerInventory.Add(Items.StemCellAnimal, totalC3);
            for (int i = 0; i < 3; i++)
            {
                if (_slotStates[i] == 2)
                {
                    _slotStates[i] = 0;
                    _slotSpore[i] = _slotCell001[i] = _slotCell002[i] = _slotCell003[i] = 0;
                    _slotPlannedSpore[i] = _slotPlannedCell001[i] = _slotPlannedCell002[i] = _slotPlannedCell003[i] = 0;
                    _slotResultSnapshot[i] = null;
                    _slotInputFruit[i] = null;
                }
            }
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundation != null && foundation.Enabled)
                foundation.RemoveToast("extractor-done");
            UpdateWorldStatusLabel();
        }

        private void UpdateWorldStatusLabel()
        {
            if (_worldStatusLabel == null) return;
            if (AnySlotInProgress())
            {
                int pct = 0;
                for (int i = 0; i < 3; i++)
                    if (_slotStates[i] == 1) { pct = Mathf.RoundToInt(_slotProgress[i] * 100f); break; }
                _worldStatusLabel.text = $"Estrazione in Corso.. {pct}%";
            }
            else if (CompletedCount() > 0)
                _worldStatusLabel.text = "Estrazione completata";
            else
                _worldStatusLabel.text = "";
        }
    }
}
