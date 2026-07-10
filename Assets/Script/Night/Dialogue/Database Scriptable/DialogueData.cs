using System.Collections.Generic;
using UnityEngine;

public enum Sender { Player, NPC }
public enum Message { Normal, Choice }

[System.Serializable]
public class Choice
{
    public string buttonText;
    public string playerText;    // teks pilihan player
    [TextArea] public string npcReply; // balasan NPC otomatis setelah pilihan ini
}


[System.Serializable]
public class ChatMessage
{
    public Sender sender;
    public Message type;
    [TextArea] public string message;
    public List<Choice> choices;
}

[CreateAssetMenu(fileName = "DialogueData", menuName = "Chat/DialogueData")]
public class DialogueData : ScriptableObject
{
    public List<ChatMessage> message;
}
