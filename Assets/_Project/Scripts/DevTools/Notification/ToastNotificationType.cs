namespace Sporae.DevTools
{
    /// <summary>
    /// Tipi di toast notifications con sistema di severità
    /// Severità: 0=Success, 1=Info, 2=Warning, 3=Error, 4=Critical
    /// </summary>
    public enum ToastNotificationType
    {
        // Success (Severity: 0)
        Success,
        ActionSuccess,
        ItemCollected,
        ResourceGained,
        
        // Info (Severity: 1)
        Info,
        StageUp,
        ConditionImproved,
        SystemEnabled,
        Mission,       // Nuova missione (recap) — colore cyan #00FFC6; completamento usa Success (verde)
        
        // Warning (Severity: 2)
        Warning,
        ConditionDegraded,
        SystemDisabled,
        CountdownAlert,
        
        // Error (Severity: 3)
        Error,
        ActionFailed,
        ResourceInsufficient,
        InvalidOperation,
        
        // Critical (Severity: 4)
        Critical,
        PlantDied,
        ExtremePhDeath,
        SystemFailure
    }
    
    /// <summary>
    /// Extension methods per ToastNotificationType
    /// </summary>
    public static class ToastNotificationTypeExtensions
    {
        /// <summary>
        /// Ottiene la severità del tipo toast (0-4)
        /// </summary>
        public static int GetSeverity(this ToastNotificationType type)
        {
            return type switch
            {
                ToastNotificationType.Success or ToastNotificationType.ActionSuccess 
                    or ToastNotificationType.ItemCollected or ToastNotificationType.ResourceGained => 0,
                ToastNotificationType.Info or ToastNotificationType.StageUp
                    or ToastNotificationType.ConditionImproved or ToastNotificationType.SystemEnabled
                    or ToastNotificationType.Mission => 1,
                ToastNotificationType.Warning or ToastNotificationType.ConditionDegraded 
                    or ToastNotificationType.SystemDisabled or ToastNotificationType.CountdownAlert => 2,
                ToastNotificationType.Error or ToastNotificationType.ActionFailed 
                    or ToastNotificationType.ResourceInsufficient or ToastNotificationType.InvalidOperation => 3,
                ToastNotificationType.Critical or ToastNotificationType.PlantDied 
                    or ToastNotificationType.ExtremePhDeath or ToastNotificationType.SystemFailure => 4,
                _ => 1
            };
        }
    }
}

