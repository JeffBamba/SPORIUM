using System.Collections.Generic;
using UnityEngine;

namespace Sporae.Core.Localization
{
    /// <summary>
    /// Punto centrale per la localizzazione del gioco.
    /// Predisposizione al multilanguage: usare GetString(key) per testi futuri;
    /// attualmente i testi sono ancora in gran parte hardcoded in italiano.
    /// Estendibile con tabelle da JSON/ScriptableObject.
    /// </summary>
    public static class LocalizationManager
    {
        private static readonly Dictionary<string, (string It, string En)> Table = new Dictionary<string, (string, string)>
        {
            { "menu_options", ("Opzioni", "Options") },
            { "menu_load", ("Carica", "Load") },
            { "menu_save", ("Salva", "Save") },
            { "menu_continue", ("Continua", "Continue") },
            { "menu_new_game", ("Nuova partita", "New game") },
            { "menu_quit", ("Esci", "Quit") },
            { "options_sound_volume", ("Volume suoni", "Sound Volume") },
            { "options_music_volume", ("Volume musica", "Music Volume") },
            { "options_language", ("Lingua", "Language") },
            { "notifications_title", ("NOTIFICHE", "NOTIFICATIONS") },
            { "added_to_inventory", ("AGGIUNTO ALL'INVENTARIO", "ADDED TO INVENTORY") }
        };

        /// <summary>Restituisce la stringa localizzata per la chiave. Se assente, restituisce la chiave.</summary>
        public static string GetString(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            if (Table.TryGetValue(key, out var pair))
            {
                bool useItalian = GameLanguageSettings.GetEffectiveLanguage() == GameLanguage.Italian;
                return useItalian ? pair.It : pair.En;
            }

            return key;
        }

        /// <summary>Registra una coppia IT/EN per una chiave (utile per estensioni).</summary>
        public static void Register(string key, string textIt, string textEn)
        {
            Table[key] = (textIt, textEn);
        }
    }
}
