using System.Collections.Generic;
using Sporae.Core.Localization;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    public enum NotificationLanguage
    {
        Auto = 0,
        It = 1,
        En = 2
    }

    public static class NotificationLocalization
    {
        public static NotificationLanguage OverrideLanguage = NotificationLanguage.Auto;

        public static NotificationLanguage GetLanguage()
        {
            if (OverrideLanguage != NotificationLanguage.Auto)
                return OverrideLanguage;

            // Usa impostazione lingua da Opzioni (menu ESC) se disponibile
            var effective = GameLanguageSettings.GetEffectiveLanguage();
            return effective == GameLanguage.Italian ? NotificationLanguage.It : NotificationLanguage.En;
        }

        public static string ResolveTemplate(NotificationTypeSpec spec)
        {
            if (spec == null)
                return string.Empty;

            var lang = GetLanguage();
            if (lang == NotificationLanguage.It)
                return string.IsNullOrEmpty(spec.TemplateIt) ? spec.TemplateEn : spec.TemplateIt;
            return string.IsNullOrEmpty(spec.TemplateEn) ? spec.TemplateIt : spec.TemplateEn;
        }

        public static string ResolveTooltip(NotificationTypeSpec spec)
        {
            if (spec == null)
                return string.Empty;

            var lang = GetLanguage();
            if (lang == NotificationLanguage.It)
                return string.IsNullOrEmpty(spec.TooltipIt) ? spec.TooltipEn : spec.TooltipIt;
            return string.IsNullOrEmpty(spec.TooltipEn) ? spec.TooltipIt : spec.TooltipEn;
        }

        public static string Format(string template, IReadOnlyDictionary<string, string> args)
        {
            return LocalizationManager.Format(template, args);
        }

        /// <summary>Titolo per il toast "Added To Inventory" (layout item). Maiuscolo e grassetto in UI.</summary>
        public static string GetAddedToInventoryTitle()
        {
            return Pick("AGGIUNTO ALL'INVENTARIO", "ADDED TO INVENTORY");
        }

        /// <summary>Stringa per toast/messaggi costruiti a codice: italiano se lingua IT, altrimenti inglese.</summary>
        public static string Pick(string italian, string english)
        {
            return GetLanguage() == NotificationLanguage.It ? italian : english;
        }
    }
}


