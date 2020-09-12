using System.Collections;
using System.Timers;
using UnityEngine;
using UnityEngine.Events;
using Group3d.Notifications.Utilities;
using UnityEngine.UI;

namespace Group3d.Notifications
{
    [RequireComponent(typeof(RectTransform))]
    public class Notifications : MonoBehaviour
    {
#pragma warning disable CS0649
        [SerializeField] private Color successColor = new Color(.198f, .720f, .353f);
        [SerializeField] private Color warningColor = new Color(1f, .661f, .429f);
        [SerializeField] private Color errorColor = new Color(1f, .471f, .471f);
        [SerializeField] private float defaultHeight = 150f;
        [SerializeField] private int defaultMinFontSize = 8;
        [SerializeField] private int defaultMaxFontSize = 80;
        [SerializeField] private float showAnimationDuration = .5f;
        [SerializeField] private float showDuration = 2f;
        [SerializeField] private float hideAnimationDuration = .5f;
        [SerializeField] private float spaceBetweenNotifications = 10f;
        [SerializeField] private float notificationSpawnOffset = 150f;
        [SerializeField] private int preventDuplicatesTimeInMs = 1000;
        [Header("Optional parameters:")]
        [SerializeField] private Font font;
        [SerializeField] private GameObject notificationPrefab;
#pragma warning restore CS0649

        // Singleton design, assigned in Awake().
        private static Notifications instance;

        // Represents slots for notifications, from top to bottom.
        // Size of the array means (in practice) amount of max notifications on the screen at once.
        private readonly int[] notificationSlots = new int[10];

        // Used to prevent sending multiple duplicate notifications.
        private int lastNotificationHash;
        private Timer timer;

        private void Awake()
        {
            instance = this;

            timer = new Timer(preventDuplicatesTimeInMs);
            timer.Elapsed += ResetLastNotificationHash;
            timer.AutoReset = true;
            timer.Enabled = true;

            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 60);
            }
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

            if (lastNotificationHash == 0)
            {
                // Reset timer 
                timer.Stop();
                timer.Start();
            }
            lastNotificationHash = hash;

            // Parent is this GameObject by default.
            var parent = transform;

            GameObject notification;
            RectTransform rect;

            if (notificationPrefab == null)
            {
                // Prefab not given, creating notification dynamically.
                notification = new GameObject("Notification");
                rect = notification.AddComponent<RectTransform>();
                rect.SetParent(parent);
                rect.sizeDelta = new Vector2(0, defaultHeight);
                rect.localScale = Vector3.one;
                // Set anchors to top-stretch
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = Vector2.zero;

                notification.AddComponent<CanvasRenderer>();

                var image = notification.AddComponent<Image>();
                image.raycastTarget = true; // So button works.
                image.maskable = false;

                var button = notification.AddComponent<Button>();
                button.interactable = true;
                button.transition = Selectable.Transition.None;

                // Create text label as separate child GameObject.
                var textGameObject = new GameObject("Label");
                var textRect = textGameObject.AddComponent<RectTransform>();
                textRect.SetParent(rect);
                textRect.localScale = Vector3.one;
                // Set anchors to stretch-stretch
                textRect.anchorMin = new Vector2(0, 0);
                textRect.anchorMax = new Vector2(1, 1);
                textRect.pivot = new Vector2(.5f, .5f);
                textRect.anchoredPosition = Vector2.zero;
                textRect.sizeDelta = Vector2.zero;

                textGameObject.AddComponent<CanvasRenderer>();

                var text = textGameObject.AddComponent<Text>();
                text.raycastTarget = false; // Don't interfere with button we just created.
                text.maskable = false;
                text.resizeTextMinSize = defaultMinFontSize;
                text.resizeTextMaxSize = defaultMaxFontSize;
                text.resizeTextForBestFit = true;
                text.font = font;
                text.alignment = TextAnchor.MiddleCenter;

                var notificationUI = notification.AddComponent<NotificationUI>();
                notificationUI.button = button;
                notificationUI.panelImage = image;
                notificationUI.messageText = text;
            }
            else
            {
                notification = Instantiate(notificationPrefab, parent);
                rect = notification.GetComponent<RectTransform>();
            }

            notification.GetComponent<NotificationUI>().SetUp(message, GetColor(type), onClickEvent);

            var slotIndex = GetIndexOfFreeSlot();

            notificationSlots[slotIndex]++;

            var sizeDelta = rect.sizeDelta;
            var spaceToSkip = slotIndex * -(sizeDelta.y + spaceBetweenNotifications) - notificationSpawnOffset;
            var sizeY = sizeDelta.y / 2;
            // Move to screen.
            StartCoroutine(ObjectMovingUtilities.MoveYCoroutine(rect,
                new BezierCurve
                {
                    P0 = spaceToSkip - sizeY,
                    P1 = spaceToSkip - sizeY * 3f,
                    P2 = spaceToSkip - sizeY * 0.5f,
                    P3 = spaceToSkip - sizeY,
                }, showAnimationDuration));

            StartCoroutine(DestroyNotification(rect, showDuration, slotIndex));
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
                }, hideAnimationDuration - 0.1f));

            notificationSlots[slotIndex]--;
            Destroy(rect.gameObject, hideAnimationDuration);
        }

        private Color GetColor(NotificationTypes type)
        {
            switch (type)
            {
                case NotificationTypes.Success:
                    return successColor;
                case NotificationTypes.Warning:
                    return warningColor;
                case NotificationTypes.Error:
                    return errorColor;
                default:
                    return successColor;
            }
        }
    }
}
