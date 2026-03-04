using UnityEngine;
using Sporae.UI.Icons;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    /// <summary>Risolve l'icona per un item typeId (Resources/Icons/Items/{typeId}). Fallback su icona generica se mancante.</summary>
    public static class NotificationItemIconResolver
    {
        /// <summary>Restituisce lo sprite per il typeId tramite catalogo globale; fallback su icona default.</summary>
        public static Sprite GetIcon(string itemTypeId)
        {
            return GlobalIconResolver.GetItemIcon(itemTypeId);
        }
    }
}
