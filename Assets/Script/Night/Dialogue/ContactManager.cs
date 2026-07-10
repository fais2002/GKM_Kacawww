using UnityEngine;
using System.Collections.Generic;

public class ContactManager : MonoBehaviour
{
    public static ContactManager Instance { get; private set; }

    [SerializeField] private List<NPCChatDatabase> databases;

    private HashSet<string> unlockedThreads = new HashSet<string>();

    public static event System.Action<ChatEntry> OnThreadUnlocked;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        foreach (var database in databases)
        {
            foreach (var chat in database.chats)
            {
                if (chat.unlockedByDefault)
                    unlockedThreads.Add(chat.id);
            }
        }
    }

    public void UnlockThread(string threadId)
    {
        Debug.Log($"UnlockThread dipanggil: {threadId}");

        if (unlockedThreads.Contains(threadId))
        {
            Debug.Log($"Thread {threadId} sudah unlock sebelumnya");
            return;
        }

        ChatEntry entry = FindThread(threadId);

        if (entry == null)
        {
            Debug.Log($"Thread {threadId} tidak ditemukan di database");
            return;
        }

        unlockedThreads.Add(threadId);
        Debug.Log($"Thread {threadId} berhasil unlock, fire event");
        OnThreadUnlocked?.Invoke(entry);
    }

    private ChatEntry FindThread(string threadId)
    {
        foreach (var database in databases)
        {
            ChatEntry entry =
                database.chats.Find(c => c.id == threadId);

            if (entry != null)
                return entry;
        }

        return null;
    }

    public bool IsUnlocked(string threadId) => unlockedThreads.Contains(threadId);

    public List<ChatEntry> GetDefaultThreads()
    {
        List<ChatEntry> result = new();

        foreach (var database in databases)
        {
            result.AddRange(
                database.chats.FindAll(t => t.unlockedByDefault)
            );
        }

        return result;
    }

    public List<ChatEntry> GetUnlockedThreads()
    {
        List<ChatEntry> result = new();

        foreach (var database in databases)
        {
            result.AddRange(
                database.chats.FindAll(
                    t => unlockedThreads.Contains(t.id))
            );
        }

        return result;
    }

    public NPCChatDatabase GetDatabaseByThread(string threadId)
    {
        foreach (var database in databases)
        {
            if (database.chats.Exists(c => c.id == threadId))
                return database;
        }

        return null;
    }
}