using CSGameServer;
using TMPro;
using UnityEngine;

public class ChatUI : MonoBehaviour
{
    [SerializeField]
    private TMP_Text messageText = null;

    [SerializeField] 
    private TMP_InputField inputField = null;

    public void OnTouchSendButton()
    {
        if (NetworkManager.Instance.IsConnected == false)
            return;

        if (inputField.text.Length <= 0)
            return;

        string message = inputField.text;
        inputField.text = string.Empty;

        MessagePacket messagePacket = new MessagePacket() { Message = message };
        NetworkManager.Instance.Send(messagePacket);
    }

    public void AddMessage(string message)
    {
        messageText.text += $"\n{message}";
    }
}