using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    /// <summary>
    /// Risolve TypeSpec da code.
    /// - Prima prova un Registry in Resources/Configs/NotificationsTypeSpecRegistry
    /// - Altrimenti usa i default in NotificationTypeSpecDefaults
    /// </summary>
    public static class NotificationTypeSpecResolver
    {
        private static Dictionary<string, NotificationTypeSpec> _cache;

        public static void Warmup()
        {
            if (_cache != null) return;

            IEnumerable<NotificationTypeSpec> specs = null;
            var registry = Resources.Load<NotificationTypeSpecRegistry>("Configs/NotificationsTypeSpecRegistry");
            if (registry != null && registry.Specs != null && registry.Specs.Count > 0)
            {
                specs = registry.Specs;
            }
            else
            {
                specs = NotificationTypeSpecDefaults.BuildDefaults();
            }

            _cache = specs
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Code))
                .GroupBy(s => s.Code)
                .ToDictionary(g => g.Key, g => g.First());
        }

        public static bool TryGet(string code, out NotificationTypeSpec spec)
        {
            Warmup();
            return _cache.TryGetValue(code, out spec);
        }

        public static IReadOnlyList<NotificationTypeSpec> GetAll()
        {
            Warmup();
            return _cache.Values.ToList();
        }
    }
}


