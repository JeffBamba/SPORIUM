using System.Collections.Generic;
using _Project.Sporae.Core;
using UnityEngine;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    public sealed class NotificationPayload
    {
        public readonly Dictionary<string, string> Args = new Dictionary<string, string>();

        // Item layout extras (opzionali)
        public string ItemTypeId;
        /// <summary>Solo <see cref="Items.SporeGeneric"/>: usato con <see cref="NotificationItemIconResolver.GetIcon"/> per <c>spore-raw</c> / <c>spore-matured</c> nel catalog.</summary>
        public SporeStage? ItemSporeStage;
        public string ItemName;
        public int ItemQuantity;
        public string ItemLocation;
        public Sprite ItemIcon;

        public NotificationPayload With(string key, string value)
        {
            if (!string.IsNullOrEmpty(key))
                Args[key] = value ?? string.Empty;
            return this;
        }
    }
}


