using System.Collections;
using System.Timers;
using UnityEngine;
using UnityEngine.Events;
using Group3d.Notifications.Utilities;

namespace Group3d.Notifications
{
    [RequireComponent(typeof(RectTransform))]
    public class Notifications : MonoBehaviour
    {
        private static readonly Color SuccessColor = new Color(.198f, .720f, .353f);
        private static readonly Color WarningColor = new Color(1f, .661f, .429f);
        private static readonly Color ErrorColor = new Color(1f, .471f, .471f);

#pragma warning disable CS0649
        [SerializeField] private GameObject notificationPrefab;
#pragma warning restore CS0649

        private const float ShowAnimationDuration = .5f;
        private const float ShowDuration = 2f;
        private const float DestroyAnimationDuration = .5f;
        private const float SpaceBetweenNotifications = 10f;
        private const float NotificationSpawnOffset = 150f;
        private const int PreventDuplicatesIntervalInMs = 1000;

        private static Notifications instance;

        // Represents slots for notifications, from top to bottom.
        // Size of the array means (in practice) amount of max notifications on the screen at once.
        private readonly int[] notificationSlots = new int[10];

        // Used to prevent sending multiple duplicate notifications.
        private int lastNotificationHash;
        private Timer timer;

        private void Awake()
        {
            if (instance == null) instance = this;

            timer = new Timer(PreventDuplicatesIntervalInMs);
            timer.Elapsed += ResetLastNotificationHash;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void ResetLastNotificationHash(object o, ElapsedEventArgs e) => lastNotificationHash = 0;

        private void OnApplicationQuit()
        {
            timer?.Dispose();
        }

        private int GetIndexOfFreeSlot()
        {
            int smallestIndex = 0;

            for (int i = 0; i < notificationSlots.Length; i++)
            {
                if (notificationSlots[i] < notificationSlots[smallestIndex]) smallestIndex = i;
            }

            return smallestIndex;
        }

        /// <summary>
        /// Sends a notification.
        /// <para><b>Note:</b> You must have a <see cref="GameObject"/> in the scene with <see cref="Notifications"/> attached for this to work!</para>
        /// </summary>
        /// <param name="message">Message to display.</param>
        /// <param name="type">Type of notification, eg. Warning or Error.</param>
        /// <param name="onClickEvent">Optional click event/action.</param>
        /// <remarks>
        /// <example>
        /// Sending simple notification:
        /// <code>
        /// Notifications.Send("Hello world", NotificationTypes.Warning);
        /// </code>
        /// You can also specify on click event.
        /// If you have method:
        /// <code>
        /// void DoSomething()
        /// </code>
        /// You can:
        /// <code>
        /// Notifications.Send("Hello world", NotificationTypes.Success, DoSomething);
        /// </code>
        /// </example>
        /// </remarks>
        public static void Send(string message, NotificationTypes type = NotificationTypes.Success, UnityAction onClickEvent = null)
        {
            instance.SendNotification(message, type, onClickEvent);
        }

        private void SendNotification(string message, NotificationTypes type, UnityAction onClickEvent)
        {
            var hash = message.GetHashCode();
            if (lastNotificationHash == hash)
            {
                Debug.Log("Duplicate notification after too short delay silenced");
                return;
            }
            lastNotificationHash = hash;

            var notification = Instantiate(notificationPrefab, transform);
            notification.GetComponent<NotificationUI>().SetUp(message, GetColor(type), onClickEvent);

            var rect = notification.GetComponent<RectTransform>();
            var pos = rect.anchoredPosition;
            var sizeDelta = rect.sizeDelta;

            var slotIndex = GetIndexOfFreeSlot();
            var spaceToSkip = slotIndex * -(sizeDelta.y + SpaceBetweenNotifications) - NotificationSpawnOffset;

            notificationSlots[slotIndex]++;

            var sizeY = sizeDelta.y / 2;
            // Move to screen.
            StartCoroutine(ObjectMovingUtilities.MoveYCoroutine(rect,
                new BezierCurve
                {
                    P0 = spaceToSkip - sizeY,
                    P1 = spaceToSkip - sizeY * 3f,
                    P2 = spaceToSkip - sizeY * 0.5f,
                    P3 = spaceToSkip - sizeY,
                }, ShowAnimationDuration));

            StartCoroutine(DestroyNotification(rect, ShowDuration, slotIndex));
        }

        private IEnumerator DestroyNotification(RectTransform rect, float delay, int slotIndex)
        {
            yield return new WaitForSeconds(delay);

            var pos = rect.anchoredPosition;
            var sizeY = rect.sizeDelta.y / 2;

            // Move away from screen.
            StartCoroutine(ObjectMovingUtilities.MoveYCoroutine(rect,
                new BezierCurve
                {
                    P0 = pos.y,
                    P1 = pos.y * 1.1f,
                    P2 = sizeY,
                    P3 = sizeY,
                }, DestroyAnimationDuration - 0.1f));

            notificationSlots[slotIndex]--;
            Destroy(rect.gameObject, DestroyAnimationDuration);
        }

        private static Color GetColor(NotificationTypes type)
        {
            switch (type)
            {
                case NotificationTypes.Success:
                    return SuccessColor;
                case NotificationTypes.Warning:
                    return WarningColor;
                case NotificationTypes.Error:
                    return ErrorColor;
                default:
                    return SuccessColor;
            }
        }
    }
}
