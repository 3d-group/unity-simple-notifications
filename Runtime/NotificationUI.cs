using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Group3d.Notifications
{
    public class NotificationUI : MonoBehaviour
    {
#pragma warning disable CS0649
        [SerializeField] private Text messageText;
        [SerializeField] private Image panelImage;
        [SerializeField] private Button button;
#pragma warning restore CS0649

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
