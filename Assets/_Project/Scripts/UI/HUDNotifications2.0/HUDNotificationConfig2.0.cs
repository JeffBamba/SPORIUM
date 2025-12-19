using UnityEngine;
using System.Linq;
using Sporae.DevTools;

namespace _Project.UI.HUDNotifications2_0
{
    /// <summary>
    /// Configurazione centralizzata per sistema HUD Notifications 2.0
    /// Include tutte le dimensioni configurabili, colori, timing e riferimenti a font/sprite
    /// </summary>
    [CreateAssetMenu(menuName = "Spore/HUDNotificationConfig2.0")]
    public class HUDNotificationConfig2_0 : ScriptableObject
    {
        [Header("Container Settings")]
        [Tooltip("Larghezza fissa del container (px)")]
        public float ContainerWidth = 306f;
        
        [Tooltip("Offset dal top dello schermo (px)")]
        public float ContainerTopOffset = 96f;
        
        [Tooltip("Offset dal right dello schermo (px)")]
        public float ContainerRightOffset = 24f;
        
        [Header("Header Settings")]
        [Tooltip("Padding interno header (px)")]
        public float HeaderPadding = 8f;
        
        [Tooltip("Larghezza bordo header (px)")]
        public float HeaderBorderWidth = 2f;
        
        [Tooltip("Margin bottom tra header e lista notifiche (px)")]
        public float HeaderMarginBottom = 6f;
        
        [Tooltip("Font size testo header (px)")]
        public float HeaderFontSize = 10f;
        
        [Tooltip("Dimensione icona info header (px)")]
        public float HeaderIconSize = 14f;
        
        [Tooltip("Dimensione chevron header (px)")]
        public float HeaderChevronSize = 16f;
        
        [Tooltip("Padding badge contatore (x, y)")]
        public Vector2 HeaderBadgePadding = new Vector2(6f, 2f);
        
        [Tooltip("Font size badge contatore (px)")]
        public float HeaderBadgeFontSize = 10f;
        
        [Header("Toast Settings")]
        [Tooltip("Padding interno toast (px)")]
        public float ToastPadding = 8f;
        
        [Tooltip("Larghezza bordo toast (px)")]
        public float ToastBorderWidth = 2f;
        
        [Tooltip("Gap tra toast (px)")]
        public float ToastGap = 6f;
        
        [Tooltip("Dimensione icona toast (px)")]
        public float ToastIconSize = 14f;
        
        [Tooltip("Font size codice toast (px)")]
        public float ToastCodeFontSize = 10f;
        
        [Tooltip("Font size messaggio toast (px)")]
        public float ToastMessageFontSize = 11f;
        
        [Header("Item Notification Settings")]
        [Tooltip("Dimensione icona item grande (px)")]
        public float ItemIconSize = 40f;
        
        [Tooltip("Gap tra icona item e info (px)")]
        public float ItemIconGap = 8f;
        
        [Tooltip("Font size header item (px)")]
        public float ItemHeaderFontSize = 10f;
        
        [Tooltip("Font size nome item (px)")]
        public float ItemNameFontSize = 11f;
        
        [Tooltip("Font size location item (px)")]
        public float ItemLocationFontSize = 9f;
        
        [Tooltip("Dimensione icona package item (px)")]
        public float ItemPackageIconSize = 20f;
        
        [Header("Background & Effects")]
        [Tooltip("Colore background (#1E282A con 90% opacità)")]
        public Color BackgroundColor = new Color(0.11f, 0.16f, 0.16f, 0.9f); // #1E282A /90
        
        [Tooltip("Colore background hover (#1E282A con 95% opacità)")]
        public Color BackgroundHoverColor = new Color(0.11f, 0.16f, 0.16f, 0.95f); // #1E282A /95
        
        [Tooltip("Abilita backdrop blur (approccio semplice con CanvasGroup)")]
        public bool EnableBackdropBlur = true;
        
        [Header("Header Colors")]
        [Tooltip("Colore header idle (no notifiche)")]
        public Color32 ColorIdle = new Color32(93, 182, 227, 255); // #5DB6E3
        
        [Tooltip("Colore header con notifiche DANGER")]
        public Color32 ColorDanger = new Color32(211, 95, 95, 255); // #D35F5F
        
        [Tooltip("Colore header con notifiche WARNING")]
        public Color32 ColorWarning = new Color32(230, 201, 111, 255); // #E6C96F
        
        [Tooltip("Colore header con notifiche INFO")]
        public Color32 ColorInfo = new Color32(127, 255, 122, 255); // #7FFF7A
        
        [Header("Timing Settings")]
        [Tooltip("Durata auto-dismiss standard (secondi)")]
        public float AutoDismissDuration = 8f;
        
        [Tooltip("Durata auto-dismiss quando overflow >3 notifiche (secondi)")]
        public float OverflowDismissDuration = 5f;
        
        [Tooltip("Numero massimo di notifiche visibili")]
        public int MaxVisibleNotifications = 3;
        
        [Header("Fonts & Sprites")]
        [Tooltip("Font monospaced per testo")]
        public TMPro.TMP_FontAsset MonospacedFont;
        
        [Tooltip("Icona 'i' in cerchio per header")]
        public Sprite InfoIcon;
        
        [Tooltip("Icona chevron per toggle")]
        public Sprite ChevronIcon;
        
        [Tooltip("Icona warning")]
        public Sprite WarningIcon;
        
        [Tooltip("Icona danger")]
        public Sprite DangerIcon;
        
        [Tooltip("Icona success/info")]
        public Sprite SuccessIcon;
        
        [Tooltip("Sprite bordo (2px solid)")]
        public Sprite BorderSprite;
        
        [Tooltip("Sprite corner decorativo")]
        public Sprite CornerSprite;
        
        [Header("Animation Settings")]
        [Tooltip("Durata animazione entrata toast (secondi)")]
        public float EnterAnimationDuration = 0.3f;
        
        [Tooltip("Durata animazione uscita toast (secondi)")]
        public float ExitAnimationDuration = 0.3f;
        
        [Tooltip("Durata rotazione chevron (secondi)")]
        public float ChevronRotationDuration = 0.2f;
        
        /// <summary>
        /// Ottiene il colore header in base alla severità più alta delle notifiche attive
        /// </summary>
        public Color32 GetHeaderColor(int maxSeverity)
        {
            return maxSeverity switch
            {
                3 or 4 => ColorDanger,    // DANGER → Rosso
                2 => ColorWarning,        // WARNING → Giallo
                0 or 1 => ColorInfo,      // INFO → Verde
                _ => ColorIdle            // Nessuna notifica → Blu idle
            };
        }
        
        /// <summary>
        /// Ottiene il colore per un tipo toast basato su severità
        /// </summary>
        public Color32 GetToastColor(ToastNotificationType type)
        {
            int severity = type.GetSeverity();
            return severity switch
            {
                0 or 1 => ColorInfo,      // Success/Info → Verde
                2 => ColorWarning,        // Warning → Giallo
                3 or 4 => ColorDanger,    // Error/Critical → Rosso
                _ => ColorIdle
            };
        }
        
        /// <summary>
        /// Ottiene il prefisso codice sci-fi post-apocalittico per un tipo toast
        /// </summary>
        public string GetCodePrefix(ToastNotificationType type)
        {
            return type switch
            {
                // Success (Severity: 0)
                ToastNotificationType.Success => "OPR",
                ToastNotificationType.ActionSuccess => "OPR",
                ToastNotificationType.ItemCollected => "INV",
                ToastNotificationType.ResourceGained => "RES",
                
                // Info (Severity: 1)
                ToastNotificationType.Info => "SYS",
                ToastNotificationType.StageUp => "STG",
                ToastNotificationType.ConditionImproved => "CND",
                ToastNotificationType.SystemEnabled => "SYS-EN",
                
                // Warning (Severity: 2)
                ToastNotificationType.Warning => "WRN",
                ToastNotificationType.ConditionDegraded => "CND",
                ToastNotificationType.SystemDisabled => "SYS-DS",
                ToastNotificationType.CountdownAlert => "CNT",
                
                // Error (Severity: 3)
                ToastNotificationType.Error => "ERR",
                ToastNotificationType.ActionFailed => "OPR-FAIL",
                ToastNotificationType.ResourceInsufficient => "RES-INS",
                ToastNotificationType.InvalidOperation => "INV-OP",
                
                // Critical (Severity: 4)
                ToastNotificationType.Critical => "CRI",
                ToastNotificationType.PlantDied => "PLT-DTH",
                ToastNotificationType.ExtremePhDeath => "PH-DTH",
                ToastNotificationType.SystemFailure => "SYS-FAIL",
                
                _ => "SYS"
            };
        }
        
        /// <summary>
        /// Ottiene l'icona per un tipo toast basato su severità e tipo specifico
        /// Assicura che ogni notifica abbia l'icona corretta
        /// </summary>
        public Sprite GetToastIcon(ToastNotificationType type)
        {
            // Logica specifica per tipo per icone più accurate
            return type switch
            {
                // Warning types → WarningIcon (triangolo esclamazione)
                ToastNotificationType.Warning => WarningIcon ?? DangerIcon ?? InfoIcon,
                ToastNotificationType.ConditionDegraded => WarningIcon ?? DangerIcon ?? InfoIcon,
                ToastNotificationType.SystemDisabled => WarningIcon ?? DangerIcon ?? InfoIcon,
                ToastNotificationType.CountdownAlert => WarningIcon ?? DangerIcon ?? InfoIcon,
                
                // Info types → InfoIcon (cerchio "i")
                ToastNotificationType.Info => InfoIcon ?? SuccessIcon,
                ToastNotificationType.StageUp => InfoIcon ?? SuccessIcon,
                ToastNotificationType.ConditionImproved => InfoIcon ?? SuccessIcon,
                ToastNotificationType.SystemEnabled => InfoIcon ?? SuccessIcon,
                
                // Success types → SuccessIcon o InfoIcon (cerchio "i" o check)
                ToastNotificationType.Success => SuccessIcon ?? InfoIcon,
                ToastNotificationType.ActionSuccess => SuccessIcon ?? InfoIcon,
                ToastNotificationType.ItemCollected => SuccessIcon ?? InfoIcon,
                ToastNotificationType.ResourceGained => SuccessIcon ?? InfoIcon,
                
                // Error types → DangerIcon (cerchio esclamazione)
                ToastNotificationType.Error => DangerIcon ?? WarningIcon ?? InfoIcon,
                ToastNotificationType.ActionFailed => DangerIcon ?? WarningIcon ?? InfoIcon,
                ToastNotificationType.ResourceInsufficient => DangerIcon ?? WarningIcon ?? InfoIcon,
                ToastNotificationType.InvalidOperation => DangerIcon ?? WarningIcon ?? InfoIcon,
                
                // Critical types → DangerIcon (cerchio esclamazione)
                ToastNotificationType.Critical => DangerIcon ?? WarningIcon ?? InfoIcon,
                ToastNotificationType.PlantDied => DangerIcon ?? WarningIcon ?? InfoIcon,
                ToastNotificationType.ExtremePhDeath => DangerIcon ?? WarningIcon ?? InfoIcon,
                ToastNotificationType.SystemFailure => DangerIcon ?? WarningIcon ?? InfoIcon,
                
                // Fallback basato su severità
                _ => GetToastIconBySeverity(type)
            };
        }
        
        /// <summary>
        /// Fallback: ottiene icona basata solo su severità
        /// </summary>
        private Sprite GetToastIconBySeverity(ToastNotificationType type)
        {
            int severity = type.GetSeverity();
            return severity switch
            {
                0 or 1 => SuccessIcon ?? InfoIcon,  // Success/Info
                2 => WarningIcon ?? DangerIcon ?? InfoIcon,  // Warning
                3 or 4 => DangerIcon ?? WarningIcon ?? InfoIcon,  // Error/Critical
                _ => InfoIcon
            };
        }
    }
}

