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
        /// <summary>Vecchio e nuovo cap giornaliero quando <see cref="MaxActions"/> cambia (alba, fame, load).</summary>
        public event Action<int, int> OnDailyCapChanged;
        
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
            // BUG FIX: Passa amount a CanSpendAction invece di ignorarlo
            if (!CanSpendAction(amount)) 
                return false;

            _diaryStatistics.ActionsSpent += amount;
            
            ActionsLeft -= amount;
            OnActionsChanged?.Invoke(ActionsLeft);
            
            return true;
        }
        
        public void ResetActions(int specificAmount)
        {
            // BUG FIX: ResetActions dovrebbe RESETTARE, non aggiungere
            // Se ActionsLeft = 0 e specificAmount = 4, dovrebbe diventare 4, non 0+4=4 (ok)
            // Ma MaxActions dovrebbe essere resettato a specificAmount, non aggiunto
            // Se MaxActions = 4 e specificAmount = 4, dovrebbe rimanere 4, non diventare 8!
            int oldMax = MaxActions;
            ActionsLeft = specificAmount;
            MaxActions = specificAmount;
            if (oldMax != MaxActions)
                OnDailyCapChanged?.Invoke(oldMax, MaxActions);

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
            int oldMax = MaxActions;
            MaxActions = Math.Max(1, maxActions);
            ActionsLeft = Math.Max(0, Math.Min(actionsLeft, MaxActions));
            if (oldMax != MaxActions)
                OnDailyCapChanged?.Invoke(oldMax, MaxActions);

            OnActionsChanged?.Invoke(ActionsLeft);
        }
    }
}
