using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sporae.UI.Icons
{
    [CreateAssetMenu(fileName = "GlobalIconCatalog", menuName = "Sporae/UI/Global Icon Catalog")]
    public class GlobalIconCatalog : ScriptableObject
    {
        [Serializable]
        public struct TypeIconEntry
        {
            public string TypeId;
            public Sprite Icon;
        }

        [Serializable]
        public struct CategoryIconEntry
        {
            public string CategoryKey;
            public Sprite Icon;
        }

        [Serializable]
        public struct ActionIconEntry
        {
            public string ActionKey;
            public Sprite Icon;
        }

        [Serializable]
        public struct PlantCodeIconEntry
        {
            public string PlantCode;
            public Sprite Icon;
        }

        [Header("Defaults")]
        [SerializeField] private Sprite _defaultItemIcon;
        [SerializeField] private Sprite _defaultActionIcon;
        [SerializeField] private Sprite _defaultPlantIcon;

        [Header("Overrides by Item TypeId")]
        [SerializeField] private List<TypeIconEntry> _typeIcons = new();

        [Header("Overrides by Category Key")]
        [SerializeField] private List<CategoryIconEntry> _categoryIcons = new();

        [Header("Overrides by Action Key")]
        [SerializeField] private List<ActionIconEntry> _actionIcons = new();

        [Header("Overrides by PlantCode")]
        [SerializeField] private List<PlantCodeIconEntry> _plantCodeIcons = new();

        public Sprite DefaultItemIcon => _defaultItemIcon;
        public Sprite DefaultActionIcon => _defaultActionIcon;
        public Sprite DefaultPlantIcon => _defaultPlantIcon;

        public bool TryGetTypeIcon(string typeId, out Sprite icon)
        {
            icon = null;
            if (string.IsNullOrWhiteSpace(typeId)) return false;
            for (int i = 0; i < _typeIcons.Count; i++)
            {
                var e = _typeIcons[i];
                if (string.Equals(e.TypeId, typeId, StringComparison.OrdinalIgnoreCase))
                {
                    icon = e.Icon;
                    return icon != null;
                }
            }
            return false;
        }

        public bool TryGetCategoryIcon(string categoryKey, out Sprite icon)
        {
            icon = null;
            if (string.IsNullOrWhiteSpace(categoryKey)) return false;
            for (int i = 0; i < _categoryIcons.Count; i++)
            {
                var e = _categoryIcons[i];
                if (string.Equals(e.CategoryKey, categoryKey, StringComparison.OrdinalIgnoreCase))
                {
                    icon = e.Icon;
                    return icon != null;
                }
            }
            return false;
        }

        public bool TryGetActionIcon(string actionKey, out Sprite icon)
        {
            icon = null;
            if (string.IsNullOrWhiteSpace(actionKey)) return false;
            for (int i = 0; i < _actionIcons.Count; i++)
            {
                var e = _actionIcons[i];
                if (string.Equals(e.ActionKey, actionKey, StringComparison.OrdinalIgnoreCase))
                {
                    icon = e.Icon;
                    return icon != null;
                }
            }
            return false;
        }

        public bool TryGetPlantCodeIcon(string plantCode, out Sprite icon)
        {
            icon = null;
            if (string.IsNullOrWhiteSpace(plantCode)) return false;
            for (int i = 0; i < _plantCodeIcons.Count; i++)
            {
                var e = _plantCodeIcons[i];
                if (string.Equals(e.PlantCode, plantCode, StringComparison.OrdinalIgnoreCase))
                {
                    icon = e.Icon;
                    return icon != null;
                }
            }
            return false;
        }
    }
}
