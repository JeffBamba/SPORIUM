using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
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
        private static readonly Regex TokenRegex = new Regex(@"\{([a-zA-Z0-9_]+)\}", RegexOptions.Compiled);

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
            var lang = GetLanguage();
            if (lang == NotificationLanguage.It)
                return string.IsNullOrEmpty(spec.TemplateIt) ? spec.TemplateEn : spec.TemplateIt;
            return string.IsNullOrEmpty(spec.TemplateEn) ? spec.TemplateIt : spec.TemplateEn;
        }

        public static string Format(string template, IReadOnlyDictionary<string, string> args)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            if (args == null || args.Count == 0)
                return template;

            return TokenRegex.Replace(template, m =>
            {
                var key = m.Groups[1].Value;
                return args.TryGetValue(key, out var value) ? value : m.Value;
            });
        }

        /// <summary>Titolo per il toast "Added To Inventory" (layout item). Maiuscolo e grassetto in UI.</summary>
        public static string GetAddedToInventoryTitle()
        {
            return GetLanguage() == NotificationLanguage.It ? "AGGIUNTO ALL'INVENTARIO" : "ADDED TO INVENTORY";
        }
    }
}


