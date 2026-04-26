using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        private static readonly Regex TokenRegex = new Regex(@"\{([a-zA-Z0-9_]+)\}", RegexOptions.Compiled);

        private static readonly Dictionary<string, (string It, string En)> Table = new Dictionary<string, (string, string)>
        {
            { "menu.options", ("Opzioni", "Options") },
            { "menu.load", ("Carica", "Load") },
            { "menu.load_game", ("Carica partita", "Load Game") },
            { "menu.save", ("Salva", "Save") },
            { "menu.continue", ("Continua", "Continue") },
            { "menu.new_game", ("Nuova partita", "New Game") },
            { "menu.play_demo", ("Gioca demo", "Play Demo") },
            { "menu.credits", ("Credits", "Credits") },
            { "menu.exit_sporium", ("Esci da Sporium", "Exit Sporium") },
            { "menu.quit", ("Esci", "Quit") },
            { "menu.loading_progress", ("Caricamento {percent}%", "Loading {percent}%") },

            { "options.language", ("Lingua", "Language") },
            { "options.language.auto", ("Sistema (Auto)", "System (Auto)") },
            { "options.language.it", ("Italiano", "Italian") },
            { "options.language.en", ("English", "English") },
            { "options.title", ("OPZIONI", "OPTIONS") },
            { "options.subtitle", ("Configura lingua e preferenze di sessione", "Configure language and session preferences") },
            { "options.language.description", ("Scegli la lingua dei testi di gioco.", "Choose the language for game text.") },
            { "options.sound_volume", ("Volume suoni", "Sound Volume") },
            { "options.music_volume", ("Volume musica", "Music Volume") },

            { "notifications.title", ("NOTIFICHE", "NOTIFICATIONS") },
            { "notifications.added_to_inventory", ("AGGIUNTO ALL'INVENTARIO", "ADDED TO INVENTORY") },

            { "inventory.title", ("INVENTARIO", "INVENTORY") },
            { "inventory.subtitle", ("Oggetti nel tuo inventario", "Items in your inventory") },
            { "inventory.empty", ("Nessun oggetto in inventario.", "No items in inventory.") },
            { "inventory.select", ("Seleziona", "Select") },
            { "inventory.drink", ("Bevi", "Drink") },
            { "inventory.eat", ("Mangia", "Eat") },
            { "inventory.use", ("Usa", "Use") },
            { "inventory.cancel", ("ANNULLA", "CANCEL") },

            { "player_status.hydration", ("IDRATAZIONE", "HYDRATION") },
            { "player_status.diary", ("Diario SPORAE", "SPORAE Diary") },
            { "player_status.low_action_warning", ("▲ -1 Azione se <40%", "▲ -1 Action if <40%") },

            { "save.title.save", ("SALVA PARTITA", "SAVE GAME") },
            { "save.title.load", ("CARICA PARTITA", "LOAD GAME") },
            { "save.title.default", ("SALVATAGGI VAULT", "VAULT SAVES") },
            { "save.subtitle.default", ("Seleziona uno slot — stile terminale VLT-01", "Select a slot — VLT-01 terminal style") },
            { "save.subtitle.save", ("Scegli uno slot — i dati esistenti verranno sovrascritti.", "Choose a slot — existing data will be overwritten.") },
            { "save.subtitle.load", ("Seleziona uno slot con salvataggio valido.", "Select a slot with a valid save.") },
            { "save.summary.filled", ("{slot} — Giorno {day}, Piante in Dome {plants}, CRY {cry} — {timestamp}", "{slot} — Day {day}, Plants in Dome {plants}, CRY {cry} — {timestamp}") },
            { "save.summary.empty_for_save", ("{slot} — Vuoto (salva qui)", "{slot} — Empty (save here)") },
            { "save.summary.empty_for_load", ("{slot} — Nessun salvataggio", "{slot} — No save data") },
            { "save.action.save", ("Salva", "Save") },
            { "save.action.load", ("Carica", "Load") },
            { "save.action.delete", ("Elimina", "Delete") },
            { "save.action.close", ("CHIUDI", "CLOSE") },

            // Legacy keys kept while older controllers migrate to dotted keys.
            { "menu_options", ("Opzioni", "Options") },
            { "menu_load", ("Carica", "Load") },
            { "menu_save", ("Salva", "Save") },
            { "menu_continue", ("Continua", "Continue") },
            { "menu_new_game", ("Nuova partita", "New Game") },
            { "menu_quit", ("Esci", "Quit") },
            { "options_sound_volume", ("Volume suoni", "Sound Volume") },
            { "options_music_volume", ("Volume musica", "Music Volume") },
            { "options_language", ("Lingua", "Language") },
            { "notifications_title", ("NOTIFICHE", "NOTIFICATIONS") },
            { "added_to_inventory", ("AGGIUNTO ALL'INVENTARIO", "ADDED TO INVENTORY") }
        };

        public static bool IsItalian =>
            GameLanguageSettings.GetEffectiveLanguage() == GameLanguage.Italian;

        /// <summary>Restituisce la stringa localizzata per la chiave. Se assente, restituisce la chiave.</summary>
        public static string GetString(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            if (Table.TryGetValue(key, out var pair))
                return Pick(pair.It, pair.En);

            return key;
        }

        public static string GetString(string key, IReadOnlyDictionary<string, string> args)
        {
            return Format(GetString(key), args);
        }

        public static string Pick(string italian, string english)
        {
            if (IsItalian)
                return string.IsNullOrEmpty(italian) ? english : italian;
            return string.IsNullOrEmpty(english) ? italian : english;
        }

        public static string Format(string template, IReadOnlyDictionary<string, string> args)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            if (args == null || args.Count == 0)
                return template;

            return TokenRegex.Replace(template, match =>
            {
                var key = match.Groups[1].Value;
                return args.TryGetValue(key, out var value) ? value : match.Value;
            });
        }

        /// <summary>Registra una coppia IT/EN per una chiave (utile per estensioni).</summary>
        public static void Register(string key, string textIt, string textEn)
        {
            Table[key] = (textIt, textEn);
        }
    }
}
