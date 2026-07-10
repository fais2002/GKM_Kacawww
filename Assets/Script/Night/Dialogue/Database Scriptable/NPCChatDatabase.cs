using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ChatEntry
{
    public string id;              // identifier unik, misal "budi_awal", "budi_quest1"
    public string threadName;      // nama yang tampil di UI
    public DialogueData dialogue;  // data chat nya
    public bool unlockedByDefault; // true = langsung terbuka dari awal
}

[CreateAssetMenu(fileName = "NPCChatDatabase", menuName = "Chat/NPCChatDatabase")]
public class NPCChatDatabase : ScriptableObject
{
    public string npcName;
    public List<ChatEntry> chats; // semua chat NPC ini berurutan
}
