using UnityEngine.Events;

namespace Group3d.Notifications
{
    internal struct Notification
    {
        internal string Message { get; set; }
        internal NotificationType Type { get; set; }
        internal UnityAction OnClickEvent { get; set; }
    }
}
