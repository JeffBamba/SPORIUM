using System.Collections.Generic;
using System.Linq;
using _Project.Sporae.Core;

namespace _Project
{
    /// <summary>
    /// Stato "unlocked" per voci/categorie Wiki. Applicato dopo scelta Night Research.
    /// WikipediaUI può filtrare o evidenziare in base a IsUnlocked(id).
    /// </summary>
    public class WikiUnlockService
    {
        private const string NightResearchPrefix = "night_research:";
        private readonly HashSet<string> _unlockedIds = new();
        private readonly Dictionary<int, string> _nightResearchByDay = new();

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
            _nightResearchByDay.Clear();
        }

        public void RecordNightResearch(int day, string branch)
        {
            if (day < 1 || string.IsNullOrWhiteSpace(branch))
                return;

            _nightResearchByDay[day] = branch.Trim();
        }

        public bool TryGetNightResearchForDay(int day, out string branch)
        {
            if (_nightResearchByDay.TryGetValue(day, out branch) && !string.IsNullOrWhiteSpace(branch))
                return true;

            branch = null;
            return false;
        }

        public List<string> ExportUnlockedIds()
        {
            var exported = _unlockedIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .OrderBy(id => id)
                .ToList();

            foreach (var kv in _nightResearchByDay.OrderBy(x => x.Key))
            {
                if (string.IsNullOrWhiteSpace(kv.Value))
                    continue;
                exported.Add($"{NightResearchPrefix}{kv.Key}:{kv.Value.Trim()}");
            }

            return exported;
        }

        public void ImportUnlockedIds(IEnumerable<string> ids)
        {
            _unlockedIds.Clear();
            _nightResearchByDay.Clear();
            if (ids == null) return;

            foreach (var id in ids)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    var cleaned = id.Trim();
                    if (cleaned.StartsWith(NightResearchPrefix))
                    {
                        var payload = cleaned.Substring(NightResearchPrefix.Length);
                        var separatorIdx = payload.IndexOf(':');
                        if (separatorIdx > 0)
                        {
                            var dayText = payload.Substring(0, separatorIdx);
                            var branch = payload.Substring(separatorIdx + 1);
                            if (int.TryParse(dayText, out var day) && day >= 1 && !string.IsNullOrWhiteSpace(branch))
                                _nightResearchByDay[day] = branch.Trim();
                        }
                        continue;
                    }

                    _unlockedIds.Add(cleaned);
                }
            }
        }
    }
}
