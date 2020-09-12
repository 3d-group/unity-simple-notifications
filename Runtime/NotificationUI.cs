using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Group3d.Notifications
{
    public class NotificationUI : MonoBehaviour
    {
        public Text messageText;
        public Image panelImage;
        public Button button;

        public void SetUp(string message, Color messageColor, UnityAction onClickEvent)
        {
            messageText.text = message;
            panelImage.color = messageColor;

            if (onClickEvent == null)
            {
                panelImage.raycastTarget = false;
                button.interactable = false;
            }
            else
            {
                button.onClick.AddListener(onClickEvent);
            }
        }
    }
}
