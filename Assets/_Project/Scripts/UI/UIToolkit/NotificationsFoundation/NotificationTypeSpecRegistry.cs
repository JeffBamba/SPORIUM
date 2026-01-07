using System.Collections.Generic;
using UnityEngine;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    /// <summary>
    /// Registry editabile in Editor (opzionale).
    /// Se non presente in Resources, il sistema usa i default in NotificationTypeSpecDefaults.
    /// </summary>
    [CreateAssetMenu(menuName = "Spore/Notifications/TypeSpecRegistry")]
    public sealed class NotificationTypeSpecRegistry : ScriptableObject
    {
        public List<NotificationTypeSpec> Specs = new List<NotificationTypeSpec>();
    }
}


