using _Project.Sporae.Core;
using UnityEngine;
using Sporae.UI.Icons;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    /// <summary>Risolve l'icona per un item typeId solo via <see cref="GlobalIconResolver"/> (catalogo, senza Resources).</summary>
    public static class NotificationItemIconResolver
    {
        /// <summary>Sprite dal catalogo globale; può essere null se non configurato.</summary>
        /// <param name="sporeStage">Per <see cref="Items.SporeGeneric"/>: distingue varianti catalogo <c>raw</c> / <c>matured</c>.</param>
        public static Sprite GetIcon(string itemTypeId, SporeStage? sporeStage = null)
        {
            return GlobalIconResolver.GetItemIcon(itemTypeId, sporeStage);
        }
    }
}
