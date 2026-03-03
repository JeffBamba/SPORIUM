using UnityEngine;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    /// <summary>Risolve l'icona per un item typeId (Resources/Icons/Items/{typeId}). Fallback su icona generica se mancante.</summary>
    public static class NotificationItemIconResolver
    {
        private const string ItemsIconPath = "Icons/Items/";
        private const string DefaultIconName = "default";

        private static Sprite _defaultIcon;

        /// <summary>Restituisce lo sprite per il typeId; se non trovato usa l'icona default (Resources/Icons/Items/default).</summary>
        public static Sprite GetIcon(string itemTypeId)
        {
            if (string.IsNullOrEmpty(itemTypeId))
                return GetDefaultIcon();

            var sprite = Resources.Load<Sprite>(ItemsIconPath + itemTypeId);
            if (sprite != null)
                return sprite;

            return GetDefaultIcon();
        }

        private static Sprite GetDefaultIcon()
        {
            if (_defaultIcon != null)
                return _defaultIcon;
            _defaultIcon = Resources.Load<Sprite>(ItemsIconPath + DefaultIconName);
            return _defaultIcon;
        }
    }
}
