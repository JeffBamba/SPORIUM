using System;

namespace Sporae.UI.UIToolkit.NotificationsFoundation
{
    public sealed class NotificationEntry
    {
        public int Id { get; }
        public string Code { get; }
        public string DedupKey { get; }
        public NotificationTypeSpec Spec { get; }
        public NotificationSeverity Severity { get; }
        public string Message { get; }
        public DateTime CreatedAtUtc { get; }
        public float? ExpiresAtRealtime { get; }
        public NotificationPayload Payload { get; }

        public bool IsDangerPersistent => Spec != null && Spec.IsDangerPersistent;

        public NotificationEntry(
            int id,
            string code,
            string dedupKey,
            NotificationTypeSpec spec,
            NotificationSeverity severity,
            string message,
            DateTime createdAtUtc,
            float? expiresAtRealtime,
            NotificationPayload payload = null)
        {
            Id = id;
            Code = code;
            DedupKey = dedupKey;
            Spec = spec;
            Severity = severity;
            Message = message;
            CreatedAtUtc = createdAtUtc;
            ExpiresAtRealtime = expiresAtRealtime;
            Payload = payload;
        }
    }
}


