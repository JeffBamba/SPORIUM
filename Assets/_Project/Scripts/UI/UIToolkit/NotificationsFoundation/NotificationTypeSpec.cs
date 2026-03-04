using System;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    [Serializable]
    public sealed class NotificationTypeSpec
    {
        public string Code;
        public NotificationCategory Category;
        public NotificationSeverity DefaultSeverity;
        public NotificationChannel Channel;
        public bool IsDangerPersistent;
        public bool IsItemLayout;

        /// <summary>
        /// Per dedup state-driven: se true, il sistema userà una key deterministica (es. per PotId) per Upsert/Resolve.
        /// </summary>
        public bool IsStateDriven;

        /// <summary>
        /// Cooldown minimo tra due emissioni dello stesso code/key (secondi). 0 = nessun cooldown.
        /// </summary>
        public float CooldownSeconds;

        /// <summary>
        /// LocKey logico (per integrazione futura con un vero sistema di localizzazione).
        /// Nel frattempo usiamo TemplateIt/TemplateEn come fallback.
        /// </summary>
        public string LocKey;

        public string TemplateIt;
        public string TemplateEn;

        /// <summary>
        /// Testo di contesto per il mini-tooltip al passaggio del mouse (max ~3 righe). Usare \n per andare a capo.
        /// </summary>
        public string TooltipIt;
    }
}


