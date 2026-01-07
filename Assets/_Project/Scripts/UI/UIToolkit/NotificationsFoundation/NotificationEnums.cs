namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    public enum NotificationSeverity
    {
        // Stato UI "idle" (nessuna notifica attiva). Manteniamo i valori esistenti (Info=0...)
        Idle = -1,
        Info = 0,
        Success = 1,
        Warning = 2,
        Danger = 3
    }

    public enum NotificationChannel
    {
        Gameplay = 0,
        Lore = 1
    }

    public enum NotificationCategory
    {
        Ecosystem = 0,
        System = 1,
        TopBar = 2,
        Room = 3,
        Lab = 4,
        Pot = 5,
        Inventory = 6,
        Diary = 7,
        Research = 8,
        Wiki = 9,
        Reputation = 10,
        Economy = 11,
        Player = 12,
        Lore = 13,
        Ph = 14,
        Water = 15,
        Light = 16,
        Fertilizer = 17,
        Mold = 18,

        // NOTE: aggiunto in coda per non alterare i valori numerici pre-esistenti.
        Actions = 19
    }
}


