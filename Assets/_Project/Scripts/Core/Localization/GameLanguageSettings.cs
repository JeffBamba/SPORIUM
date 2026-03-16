using System;
using UnityEngine;

namespace Sporae.Core.Localization
{
    /// <summary>
    /// Impostazioni lingua di gioco persistenti (PlayerPrefs).
    /// Usato dal menu Opzioni (ESC) e dal sistema di localizzazione.
    /// </summary>
    public static class GameLanguageSettings
    {
        private const string PrefsKey = "Sporium_Language";

        private static GameLanguage? _cached;

        /// <summary>Lingua attualmente selezionata. Legge/scrive da PlayerPrefs.</summary>
        public static GameLanguage CurrentLanguage
        {
            get
            {
                if (_cached.HasValue)
                    return _cached.Value;

                string raw = PlayerPrefs.GetString(PrefsKey, "Auto");
                _cached = ParseLanguage(raw);
                return _cached.Value;
            }
            set
            {
                if (_cached == value)
                    return;

                _cached = value;
                PlayerPrefs.SetString(PrefsKey, value.ToString());
                PlayerPrefs.Save();
                OnLanguageChanged?.Invoke(value);
            }
        }

        /// <summary>Notifica quando la lingua viene cambiata (es. da Opzioni).</summary>
        public static event Action<GameLanguage> OnLanguageChanged;

        /// <summary>Risolve la lingua effettiva: se Auto, usa lingua di sistema.</summary>
        public static GameLanguage GetEffectiveLanguage()
        {
            var current = CurrentLanguage;
            if (current != GameLanguage.Auto)
                return current;
            return Application.systemLanguage == SystemLanguage.Italian
                ? GameLanguage.Italian
                : GameLanguage.English;
        }

        private static GameLanguage ParseLanguage(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return GameLanguage.Auto;
            if (Enum.TryParse<GameLanguage>(raw, true, out var lang))
                return lang;
            return GameLanguage.Auto;
        }

        /// <summary>Invalida la cache (es. dopo caricamento impostazioni).</summary>
        public static void InvalidateCache()
        {
            _cached = null;
        }
    }
}
