using System.Collections;
using Group3d.Notifications;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests.Runtime
{
    public class SimpleNotificationTests
    {
        private GameObject canvasGameObject;
        private GameObject notificationsGameObject;

        [SetUp]
        public void Setup()
        {
            notificationsGameObject = new GameObject("Notifications");
            canvasGameObject = new GameObject("Canvas");

            var canvas = canvasGameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasGameObject.AddComponent<GraphicRaycaster>();

            GameObjectUtility.SetParentAndAlign(notificationsGameObject, canvasGameObject);

            var rectTransform = notificationsGameObject.AddComponent<RectTransform>();
            
            // Set align to top-stretch
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = Vector2.zero;
            
            notificationsGameObject.AddComponent<Notifications>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.Destroy(notificationsGameObject);
            Object.Destroy(canvasGameObject);
        }

        [UnityTest]
        public IEnumerator SendNotification_OnlyMessage_NotificationIsSent()
        {
            // Arrange
            const string testMessage = "TestMessage";

            // Act
            yield return null;

            Notifications.Send(testMessage);

            yield return null;

            var notificationUIs = notificationsGameObject.GetComponentsInChildren<NotificationUI>();

            // Assert
            Assert.AreEqual(1, notificationUIs.Length);
            Assert.AreEqual(testMessage, notificationUIs[0].messageText.text);
        }
    }
}
