using System.Collections.Generic;
using _Project.Sporae.Core;

namespace _Project
{
    /// <summary>
    /// Stato "unlocked" per voci/categorie Wiki. Applicato dopo scelta Night Research.
    /// WikipediaUI può filtrare o evidenziare in base a IsUnlocked(id).
    /// </summary>
    public class WikiUnlockService
    {
        private readonly HashSet<string> _unlockedIds = new();

        public void Unlock(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            _unlockedIds.Add(id);
        }

        public void UnlockCategory(string categoryId)
        {
            if (string.IsNullOrEmpty(categoryId)) return;
            _unlockedIds.Add("cat:" + categoryId);
        }

        public bool IsUnlocked(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return _unlockedIds.Contains(id) || _unlockedIds.Contains("cat:" + id);
        }

        public void Clear()
        {
            _unlockedIds.Clear();
        }
    }
}
