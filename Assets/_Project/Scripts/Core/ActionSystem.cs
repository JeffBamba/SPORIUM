using System;
using _Project;
using _Project.Sporae.Core;

namespace Sporae.Core
{
    public class ActionSystem
    {
        public int ActionsLeft { get; private set; }
        public int MaxActions { get; private set; }
        
        public event Action<int> OnActionsChanged;
        
        private readonly DiaryStatistics _diaryStatistics;

        public ActionSystem(int maxActions)
        {
            _diaryStatistics = ServiceContainer.Instance.Get<DiaryStatistics>();
            
            MaxActions = maxActions;
            ActionsLeft = maxActions;
        }

        public bool CanSpendAction(int amount = 1)
        {
            return ActionsLeft >= amount;
        }

        public bool SpendAction(int amount = 1)
        {
            if (!CanSpendAction()) 
                return false;

            _diaryStatistics.ActionsSpent += amount;
            
            ActionsLeft -= amount;
            OnActionsChanged?.Invoke(ActionsLeft);
            
            return true;
        }
        
        public void ResetActions(int specificAmount)
        {
            ActionsLeft += specificAmount;
            MaxActions += specificAmount;
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
        
        /// <summary>
        /// Ripristina lo stato del sistema di azioni da dati salvati.
        /// Usato durante il caricamento del gioco.
        /// </summary>
        /// <param name="actionsLeft">Azioni rimanenti da ripristinare</param>
        /// <param name="maxActions">Azioni massime da ripristinare</param>
        public void RestoreState(int actionsLeft, int maxActions)
        {
            MaxActions = Math.Max(1, maxActions);
            ActionsLeft = Math.Max(0, Math.Min(actionsLeft, MaxActions));
            OnActionsChanged?.Invoke(ActionsLeft);
        }
    }
}
