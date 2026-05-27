using System;
using System.Collections.Generic;
using _Project.Sporae.Core.Knowledge;
using _Project.Sporae.Core;

namespace _Project.Sporae.Core.LabBlueprint
{
    public sealed class LabBlueprintService
    {
        public event Action<LabBlueprintState> OnStateChanged;

        private LabBlueprintState _state = LabBlueprintState.Empty();

        public LabBlueprintState Current => _state;
        public bool HasDraftOrActiveProject => _state != null && _state.HasDraftOrActiveProject;

        public LabBlueprintState StartDraft(
            LabBlueprintInputKind inputKind,
            Item inputItem,
            KnowledgeProgressionService knowledge,
            int currentDay,
            string projectId = null)
        {
            if (inputKind == LabBlueprintInputKind.None)
                throw new ArgumentException("LAB 4.0 richiede un input frutto o spora.", nameof(inputKind));
            if (inputItem == null)
                throw new ArgumentNullException(nameof(inputItem));

            int baseBudget = knowledge != null ? knowledge.GetProjectBudgetBase() : 8;
            var tier = knowledge != null ? knowledge.CurrentTier : new KnowledgeTierInfo(0, 0, "knowledge.tier.neofita", 8);

            _state = new LabBlueprintState
            {
                projectId = string.IsNullOrWhiteSpace(projectId) ? BuildProjectId(currentDay) : projectId.Trim(),
                status = LabBlueprintStatus.Draft,
                inputKind = inputKind,
                inputItem = LabBlueprintItemSnapshot.FromItem(inputItem),
                knowledgeTierLabel = knowledge != null ? knowledge.GetTierLabelLocalized() : "NEOFITA",
                knowledgeTierRank = tier.Rank,
                baseBudget = baseBudget,
                totalBudget = baseBudget,
                freePoints = baseBudget,
                draftCreatedDay = currentDay,
                currentStep = LabBlueprintStep.None,
                outcome = LabBlueprintOutcome.None,
                allocations = CreateDefaultAllocations()
            };
            _state.RecalculateBudget();
            NotifyChanged();
            return _state;
        }

        public bool TrySetAllocation(LabBlueprintField field, string targetId, string targetLabel, int points, int maxTicks = 5)
        {
            if (_state == null || _state.status != LabBlueprintStatus.Draft)
                return false;
            if (points < 0)
                return false;

            EnsureAllocations();
            var allocation = _state.allocations.Find(a => a != null && a.field == field);
            if (allocation == null)
            {
                allocation = new LabBlueprintAllocation { field = field };
                _state.allocations.Add(allocation);
            }

            int currentPoints = allocation.points;
            int currentAllocatedWithoutField = Math.Max(0, _state.allocatedPoints - currentPoints);
            if (currentAllocatedWithoutField + points > _state.totalBudget)
                return false;

            allocation.targetId = targetId;
            allocation.targetLabel = targetLabel;
            allocation.points = points;
            allocation.maxTicks = Math.Max(1, maxTicks);
            _state.RecalculateBudget();
            NotifyChanged();
            return true;
        }

        public void SetReagent(string reagentTypeId, int reagentIncrement)
        {
            if (_state == null || _state.status != LabBlueprintStatus.Draft)
                return;

            _state.reagentTypeId = reagentTypeId;
            _state.reagentIncrement = Math.Max(0, reagentIncrement);
            _state.RecalculateBudget();
            NotifyChanged();
        }

        public bool Seal(int currentDay)
        {
            if (_state == null || _state.status != LabBlueprintStatus.Draft)
                return false;

            _state.status = LabBlueprintStatus.Sealed;
            _state.sealedDay = currentDay;
            _state.extractorStepSkipped = _state.inputKind == LabBlueprintInputKind.Spore;
            _state.currentStep = _state.extractorStepSkipped
                ? LabBlueprintStep.Catalizzatore
                : LabBlueprintStep.Extractor;
            NotifyChanged();
            return true;
        }

        public void MarkInProgress(LabBlueprintStep step)
        {
            if (_state == null || _state.status == LabBlueprintStatus.Empty)
                return;

            _state.status = LabBlueprintStatus.InProgress;
            _state.currentStep = step;
            NotifyChanged();
        }

        public void MarkOutcomePending(LabBlueprintOutcome outcome)
        {
            if (_state == null || _state.status == LabBlueprintStatus.Empty)
                return;

            _state.status = LabBlueprintStatus.OutcomePending;
            _state.outcome = outcome;
            _state.currentStep = LabBlueprintStep.Completed;
            NotifyChanged();
        }

        public void MarkRetired()
        {
            if (_state == null || _state.status == LabBlueprintStatus.Empty)
                return;

            _state.status = LabBlueprintStatus.Retired;
            NotifyChanged();
        }

        public void Abandon()
        {
            if (_state == null || _state.status == LabBlueprintStatus.Empty)
                return;

            _state.status = LabBlueprintStatus.Abandoned;
            NotifyChanged();
        }

        public void Clear()
        {
            _state = LabBlueprintState.Empty();
            NotifyChanged();
        }

        public LabBlueprintState ExportState()
        {
            return _state?.Clone() ?? LabBlueprintState.Empty();
        }

        public void LoadState(LabBlueprintState state)
        {
            _state = state != null ? state.Clone() : LabBlueprintState.Empty();
            _state.schemaVersion = LabBlueprintState.CurrentSchemaVersion;
            _state.RecalculateBudget();
            NotifyChanged();
        }

        private static string BuildProjectId(int currentDay)
        {
            return $"LAB4-D{Math.Max(1, currentDay)}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        private void EnsureAllocations()
        {
            if (_state.allocations == null)
                _state.allocations = CreateDefaultAllocations();
        }

        private static List<LabBlueprintAllocation> CreateDefaultAllocations()
        {
            return new List<LabBlueprintAllocation>
            {
                new LabBlueprintAllocation { field = LabBlueprintField.Line },
                new LabBlueprintAllocation { field = LabBlueprintField.Family },
                new LabBlueprintAllocation { field = LabBlueprintField.GeneticMutation },
                new LabBlueprintAllocation { field = LabBlueprintField.PhDrift },
                new LabBlueprintAllocation { field = LabBlueprintField.ActivePower },
                new LabBlueprintAllocation { field = LabBlueprintField.PassivePower }
            };
        }

        private void NotifyChanged()
        {
            OnStateChanged?.Invoke(ExportState());
        }
    }
}
