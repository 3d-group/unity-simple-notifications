using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Group3d.Notifications
{
    public class NotificationsEditorExtensions
    {
        [MenuItem("GameObject/UI/Notifications", false, 10)]
        public static void CreateNotificationsGameObject(MenuCommand menuCommand)
        {
            var gameObject = new GameObject("Notifications");

            var rectTransform = gameObject.AddComponent<RectTransform>();
            // Set align to top-stretch
            rectTransform.anchorMin = new Vector2(0,1);
            rectTransform.anchorMax = new Vector2(1,1);
            rectTransform.pivot = new Vector2(0.5f,0.5f);
            rectTransform.sizeDelta = Vector2.zero;

            gameObject.AddComponent<Notifications>();

            var parent = menuCommand.context as GameObject;

            // Create canvas if not present yet
            if (parent == null || parent.GetComponentInParent<Canvas>() == null)
            {
                var canvasGameObject = new GameObject("Canvas");
                canvasGameObject.AddComponent<RectTransform>();

                var canvas = canvasGameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                canvasGameObject.AddComponent<GraphicRaycaster>();

                if (parent != null)
                {
                    GameObjectUtility.SetParentAndAlign(canvasGameObject, parent);
                }

                parent = canvasGameObject;
            }

            GameObjectUtility.SetParentAndAlign(gameObject, parent);
            
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
            
            Selection.activeObject = gameObject;
        }
    }
}
