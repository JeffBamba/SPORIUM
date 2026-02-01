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

        private ExtractorProcessState _state = ExtractorProcessState.Idle;
        private float _extractionProgress;
        private int _pendingSporeCount;
        private int _pendingCell001;
        private int _pendingCell002;
        private int _pendingCell003;
        private Coroutine _extractionCoroutine;

        public ExtractorProcessState State => _state;
        public float ExtractionProgress => _extractionProgress;
        public int PendingSporeCount => _pendingSporeCount;
        public int PendingCell001 => _pendingCell001;
        public int PendingCell002 => _pendingCell002;
        public int PendingCell003 => _pendingCell003;

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
            UpdateWorldStatusLabel();
        }

        private void OnDestroy()
        {
            _interactable.OnInteract -= HandleInteract;
            if (_extractionCoroutine != null)
                StopCoroutine(_extractionCoroutine);
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
            {
                _labMiniGame.Show();
            }
#if UNITY_EDITOR
            else
                Debug.LogWarning("[Extractor] Nessun pannello assegnato: su Extractor (Inspector) assegna 'Lab Extractor Panel' al GameObject che ha LabExtractorPanelController (es. UI_LabExtractorPanel).");
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
            if (_state != ExtractorProcessState.Idle) return false;

            var gm = Sporae.Core.ServiceContainer.Instance?.Get<GameManager>();
            if (gm == null) gm = FindObjectOfType<GameManager>();
            if (gm == null || gm.ActionSystem == null || gm.ActionSystem.ActionsLeft < 1) return false;
            if (!gm.TrySpendAction(1)) return false;

            bool hasStem = HasStemCellModule;
            if (_inventory.Has(Items.Fruits))
            {
                _inventory.Consume(Items.Fruits, 1);
                _extractionCoroutine = StartCoroutine(RunExtraction(1, 0, 1, 0));
            }
            else if (hasStem && _inventory.Has(Items.WholePlant))
            {
                _inventory.Consume(Items.WholePlant, 1);
                _extractionCoroutine = StartCoroutine(RunExtraction(0, 1, 0, 0));
            }
            else if (hasStem && _inventory.Has(Items.OrganicScrap001))
            {
                _inventory.Consume(Items.OrganicScrap001, 1);
                _extractionCoroutine = StartCoroutine(RunExtraction(0, 1, 0, 0));
            }
            else if (hasStem && _inventory.Has(Items.ProteinResidue))
            {
                _inventory.Consume(Items.ProteinResidue, 1);
                _extractionCoroutine = StartCoroutine(RunExtraction(0, 0, 0, 1));
            }
            else
                return false;

            _state = ExtractorProcessState.InProgress;
            UpdateWorldStatusLabel();
            var foundationStart = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            if (foundationStart != null && foundationStart.Enabled)
                foundationStart.UpsertToast("extractor-progress", "LAB-EXT-START", new NotificationPayload().With("percent", "0"));
            return true;
        }

        private const string ExtractorProgressToastKey = "extractor-progress";

        private IEnumerator RunExtraction(int sporeOut, int cell001Out, int cell002Out, int cell003Out)
        {
            var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
            float elapsed = 0f;
            while (elapsed < _extractionDurationSeconds)
            {
                elapsed += Time.deltaTime;
                _extractionProgress = Mathf.Clamp01(elapsed / _extractionDurationSeconds);
                UpdateWorldStatusLabel();
                if (foundation != null && foundation.Enabled)
                {
                    int pct = Mathf.RoundToInt(_extractionProgress * 100f);
                    foundation.UpsertToast(ExtractorProgressToastKey, "LAB-EXT-START", new NotificationPayload().With("percent", pct.ToString()));
                }
                yield return null;
            }
            _pendingSporeCount = sporeOut;
            _pendingCell001 = cell001Out;
            _pendingCell002 = cell002Out;
            _pendingCell003 = cell003Out;
            _state = ExtractorProcessState.Completed;
            _extractionProgress = 1f;
            _extractionCoroutine = null;
            UpdateWorldStatusLabel();
            if (foundation != null && foundation.Enabled)
            {
                foundation.RemoveToast(ExtractorProgressToastKey);
                foundation.PostToastImmediate("LAB-EXT-DONE");
            }
        }

        public void CollectOutput(Inventory playerInventory)
        {
            if (playerInventory == null) return;
            if (_pendingSporeCount > 0) { playerInventory.AddSporeRaw(_pendingSporeCount); _pendingSporeCount = 0; }
            if (_pendingCell001 > 0) { playerInventory.Add(Items.StemCellVegetable, _pendingCell001); _pendingCell001 = 0; }
            if (_pendingCell002 > 0) { playerInventory.Add(Items.StemCellFungus, _pendingCell002); _pendingCell002 = 0; }
            if (_pendingCell003 > 0) { playerInventory.Add(Items.StemCellAnimal, _pendingCell003); _pendingCell003 = 0; }
            _state = ExtractorProcessState.Idle;
            _extractionProgress = 0f;
            UpdateWorldStatusLabel();
        }

        private void UpdateWorldStatusLabel()
        {
            if (_worldStatusLabel == null) return;
            if (_state == ExtractorProcessState.InProgress)
            {
                int pct = Mathf.RoundToInt(_extractionProgress * 100f);
                _worldStatusLabel.text = $"Estrazione in Corso.. {pct}%";
            }
            else if (_state == ExtractorProcessState.Completed)
                _worldStatusLabel.text = "Estrazione completata";
            else
                _worldStatusLabel.text = "";
        }
    }
}