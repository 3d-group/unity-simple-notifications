using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private const int CleanUpTimeStampsIntervalInMs = 10000;

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
        [SerializeField] private int sendNotificationIntervalInMs = 10;
        [SerializeField] private int preventDuplicateTimeInMs = 500;
        [SerializeField] private int notificationQueueMaxLength = 100;
        [Header("Optional parameters:")]
        [SerializeField] private Font font;
        [SerializeField] private GameObject notificationPrefab;
#pragma warning restore CS0649

        // Singleton design, assigned in Awake().
        private static Notifications instance;

        // Represents slots for notifications, from top to bottom.
        // Size of the array means (in practice) amount of max notifications on the screen at once.
        private readonly int[] notificationSlots = new int[10];

        private readonly ConcurrentDictionary<int, long> timestamps = new ConcurrentDictionary<int, long>();
        private readonly ConcurrentQueue<Notification> notificationsToSend = new ConcurrentQueue<Notification>();
        private long notificationsRateLimitedCount;

        private Timer cleanUpTimer;
        private float sendNotificationTimer = -1;

        private void Awake()
        {
            instance = this;
            cleanUpTimer = new Timer(CleanUpTimeStampsIntervalInMs);
            cleanUpTimer.Elapsed += CleanUpTimestamps;
            cleanUpTimer.AutoReset = true;
            cleanUpTimer.Enabled = true;

            // Font not given, creating font dynamically.
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 60);
            }
        }

        private void Update()
        {
            if (notificationsToSend.Count > 0 && sendNotificationTimer <= 0)
            {
                if (notificationsToSend.TryDequeue(out var notification))
                {
                    SendNotification(notification);
                }

                if (notificationsRateLimitedCount > 0)
                {
#if DEBUG || UNITY_EDITOR
                    Debug.LogWarning($"Duplicate notifications rate limited: {notificationsRateLimitedCount}");
#endif
                    notificationsRateLimitedCount = 0;
                }

                sendNotificationTimer = sendNotificationIntervalInMs / 1000f;
            }
            
            if (sendNotificationTimer > 0)
            {
                sendNotificationTimer -= Time.deltaTime;
            }
        }

        private void CleanUpTimestamps(object o, ElapsedEventArgs e)
        {
            if (timestamps.Count == 0) return;

            Task.Run(() =>
            {
                var threshold = DateTime.Now.Ticks - CleanUpTimeStampsIntervalInMs * 10;

                var keysToRemove = new List<int>();
                
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var kvp in timestamps)
                {
                    if (kvp.Value < threshold)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    timestamps.TryRemove(key, out _);
                }
            });
        }

        private void OnApplicationQuit()
        {
            cleanUpTimer?.Dispose();
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
        public static void Send(string message, NotificationType type = NotificationType.Success, UnityAction onClickEvent = null)
        {
#pragma warning disable 4014 // no await warning.
            SendAsync(message, type, onClickEvent);
#pragma warning restore 4014
        }

        public static async Task SendAsync(string message, NotificationType type = NotificationType.Success, UnityAction onClickEvent = null)
        {
            var notification = new Notification
            {
                Message = message,
                Type = type,
                OnClickEvent = onClickEvent,
            };

            await instance.HandleNotificationAsync(notification);
        }

        private async Task HandleNotificationAsync(Notification notification)
        {
            // Rate limit if max reached.
            if (notificationsToSend.Count >= notificationQueueMaxLength)
            {
                notificationsRateLimitedCount++;
                return;
            }

            await Task.Run(() =>
            {
                var hash = notification.Message.GetHashCode();
                var timestamp = DateTime.Now.Ticks;

                if (timestamps.ContainsKey(hash))
                {
                    // Check timestamps, if duplicate notification is sent recently, abort.
                    if (timestamps.TryGetValue(hash, out var previousStamp) && timestamp - previousStamp <= preventDuplicateTimeInMs)
                    {
                        notificationsRateLimitedCount++;
                    }
                    else
                    {
                        notificationsToSend.Enqueue(notification);
                        timestamps.AddOrUpdate(hash, i => timestamp, (a, b) => timestamp);
                    }
                }
                else
                {
                    // Add new timestamp.
                    // Rate limit if addition fails - that means notification is probably added during few milliseconds after last check.
                    if (timestamps.TryAdd(hash, timestamp))
                    {
                        notificationsToSend.Enqueue(notification);
                    }
                    else
                    {
                        notificationsRateLimitedCount++;
                    }
                }
            });
        }

        private void SendNotification(Notification notification)
        {
            // If prefab is not given, creates notification GameObject dynamically.
            var notificationGameObject = notificationPrefab == null
                ? CreateNotificationDynamically(transform)
                : Instantiate(notificationPrefab, transform);
            var rect = notificationGameObject.GetComponent<RectTransform>();
            notificationGameObject.GetComponent<NotificationUI>().SetUp(notification.Message, GetColor(notification.Type), notification.OnClickEvent);

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

        private int GetIndexOfFreeSlot()
        {
            int smallestIndex = 0;

            for (int i = 0; i < notificationSlots.Length; i++)
            {
                if (notificationSlots[i] < notificationSlots[smallestIndex]) smallestIndex = i;
            }

            return smallestIndex;
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

        private Color GetColor(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success:
                    return successColor;
                case NotificationType.Warning:
                    return warningColor;
                case NotificationType.Error:
                    return errorColor;
                default:
                    return successColor;
            }
        }

        private GameObject CreateNotificationDynamically(Transform parent)
        {
            var notificationGameObject = new GameObject("Notification");
            var rect = notificationGameObject.AddComponent<RectTransform>();
            rect.SetParent(parent);
            rect.sizeDelta = new Vector2(0, defaultHeight);
            rect.localScale = Vector3.one;
            // Set anchors to top-stretch
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;

            notificationGameObject.AddComponent<CanvasRenderer>();

            var image = notificationGameObject.AddComponent<Image>();
            image.raycastTarget = true; // So button works.
            image.maskable = false;

            var button = notificationGameObject.AddComponent<Button>();
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

            var notificationUI = notificationGameObject.AddComponent<NotificationUI>();
            notificationUI.button = button;
            notificationUI.panelImage = image;
            notificationUI.messageText = text;

            return notificationGameObject;
        }
    }
}
