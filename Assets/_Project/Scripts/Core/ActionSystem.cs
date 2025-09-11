using System;

namespace Sporae.Core
{
    public class ActionSystem
    {
        public int ActionsLeft { get; private set; }
        public int MaxActions { get; private set; }
        
        public event Action<int> OnActionsChanged;

        public ActionSystem(int maxActions)
        {
            MaxActions = maxActions;
            ActionsLeft = maxActions;
        }

        public bool CanSpendAction()
        {
            return ActionsLeft > 0;
        }

        public bool SpendAction(int amount = 1)
        {
            if (!CanSpendAction()) return false;
            
            ActionsLeft -= amount;
            OnActionsChanged?.Invoke(ActionsLeft);
            return true;
        }

        public void ResetActions()
        {
            ActionsLeft = MaxActions;
            OnActionsChanged?.Invoke(ActionsLeft);
        }

        public void ResetActions(int specificAmount)
        {
            ActionsLeft = specificAmount;
            MaxActions = specificAmount;
            OnActionsChanged?.Invoke(ActionsLeft);
        }

        public void AddActions(int amount)
        {
            if (amount <= 0) return;
            
            ActionsLeft = Math.Min(ActionsLeft + amount, MaxActions);
            OnActionsChanged?.Invoke(ActionsLeft);
        }

        public float GetActionPercentage()
        {
            return (float)ActionsLeft / MaxActions;
        }
    }
}
