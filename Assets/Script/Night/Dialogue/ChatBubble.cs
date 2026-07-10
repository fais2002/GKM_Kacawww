using UnityEngine;
using TMPro;

public class ChatBubble : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;

    public void Setup(string message)
    {
        messageText.text = message;
    }
}