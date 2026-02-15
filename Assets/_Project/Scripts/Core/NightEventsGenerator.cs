using System.Collections.Generic;
using _Project.Sporae.Core;
using UnityEngine;

namespace _Project
{
    /// <summary>
    /// Genera la lista di eventi notturni per la Dawn Summary (Step 6 EoD).
    /// Invocato dopo OnDayChanged; legge da PhSystem, CondensationSystem, stato vasi.
    /// </summary>
    public class NightEventsGenerator
    {
        /// <summary>
        /// Genera le righe di evento da mostrare nella schermata Dawn (una alla volta, 0.6s).
        /// </summary>
        /// <param name="newDay">Giorno appena iniziato (dopo la notte).</param>
        /// <returns>Lista di testi da mostrare; può essere vuota.</returns>
        public IReadOnlyList<string> Generate(int newDay)
        {
            var events = new List<string>();

            var ph = ServiceContainer.Instance?.Get<PhSystem>(suppressWarning: true);
            if (ph != null)
            {
                // Placeholder: quando PhSystem espone trend/drift si può aggiungere "pH stabilized..." ecc.
                events.Add("The Dome breathes through the night.");
            }

            var cond = ServiceContainer.Instance?.Get<CondensationSystem>(suppressWarning: true);
            if (cond != null)
            {
                events.Add("Condensation systems nominal.");
            }

            if (events.Count == 0)
                events.Add("Night passed.");

            return events;
        }
    }
}
