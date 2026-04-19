using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Sporae.Core
{
    /// <summary>
    /// Sorgenti di contribuzione al budget giornaliero di Azioni.
    /// Estensibile: domani arriveranno Moduli (es. MOD-201), bonus Ambiente, Item, ecc.
    /// </summary>
    public enum ActionBudgetSource
    {
        Breakfast,
        /// <summary>Riduzione azioni per giorni consecutivi senza pasto (fame).</summary>
        Malnutrition,
        Module,
        Environment,
        Item,
        Override,
        Other
    }

    /// <summary>
    /// Singola voce del breakdown del cap giornaliero. Usata dal tooltip Azioni e da eventuali
    /// consumer futuri (debug, report fine giornata).
    /// </summary>
    public struct ActionBudgetEntry
    {
        public ActionBudgetSource Source;
        public string Label;
        public int Amount;
        public string Detail;
    }

    /// <summary>
    /// Ledger del breakdown del cap giornaliero Azioni.
    /// Unica responsabilità: tenere l’elenco delle contribuzioni (colazione, moduli, ambiente, …)
    /// e notificare chi ascolta (tooltip TopBar in primis).
    ///
    /// Regole:
    /// - Una voce per (Source, Label). Riassegnarla la aggiorna in-place.
    /// - <see cref="TotalCap"/> è la somma delle <see cref="ActionBudgetEntry.Amount"/>
    ///   clampata tra 0 e 5 (cap di progetto).
    /// - L’aggiornamento del cap effettivo di ActionSystem resta responsabilità di chi lo consuma
    ///   (es. GameManager a ogni alba): questa classe è solo il “cruscotto” dei contributi.
    /// </summary>
    public class DailyActionBudgetLedger
    {
        private readonly List<ActionBudgetEntry> _entries = new();

        public IReadOnlyList<ActionBudgetEntry> Entries => _entries;

        public event Action OnChanged;

        public int TotalCap
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < _entries.Count; i++)
                    sum += _entries[i].Amount;
                return Mathf.Clamp(sum, 0, 5);
            }
        }

        public void Clear()
        {
            if (_entries.Count == 0) return;
            _entries.Clear();
            OnChanged?.Invoke();
        }

        public void AddOrReplace(ActionBudgetSource source, string label, int amount, string detail = null)
        {
            if (string.IsNullOrEmpty(label)) label = source.ToString();
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.Source == source && e.Label == label)
                {
                    if (e.Amount == amount && e.Detail == detail) return;
                    _entries[i] = new ActionBudgetEntry { Source = source, Label = label, Amount = amount, Detail = detail };
                    OnChanged?.Invoke();
                    return;
                }
            }
            _entries.Add(new ActionBudgetEntry { Source = source, Label = label, Amount = amount, Detail = detail });
            OnChanged?.Invoke();
        }

        public bool Remove(ActionBudgetSource source, string label = null)
        {
            bool removed = false;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var e = _entries[i];
                if (e.Source != source) continue;
                if (label != null && e.Label != label) continue;
                _entries.RemoveAt(i);
                removed = true;
            }
            if (removed) OnChanged?.Invoke();
            return removed;
        }

        /// <summary>Restituisce la prima voce con la Source indicata, o default se assente.</summary>
        public bool TryGet(ActionBudgetSource source, out ActionBudgetEntry entry)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Source == source)
                {
                    entry = _entries[i];
                    return true;
                }
            }
            entry = default;
            return false;
        }
    }
}
