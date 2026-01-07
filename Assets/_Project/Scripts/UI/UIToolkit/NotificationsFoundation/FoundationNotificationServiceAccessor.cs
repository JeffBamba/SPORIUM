using _Project.Sporae.Core;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    public static class FoundationNotificationServiceAccessor
    {
        public static FoundationNotificationService Get(bool suppressWarning = true)
        {
            return ServiceContainer.Instance?.Get<FoundationNotificationService>(suppressWarning);
        }
    }
}


