using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class ThreadListUI : MonoBehaviour
{
    [SerializeField] private Transform threadListContent;
    [SerializeField] private GameObject threadButtonPrefab;
    [SerializeField] private UIPanel chatPanel;
    [SerializeField] private ScrollRect scrollRect; // untuk ScrollToSelected

    private GameObject firstButton;
    public static ThreadListUI Instance { get; private set; }
    private Dictionary<string, GameObject> npcButtonMap = new Dictionary<string, GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ContactManager.OnThreadUnlocked += OnNewThreadUnlocked;
    }

    void OnEnable()
    {
        //ContactManager.OnThreadUnlocked += OnNewThreadUnlocked;
        StartCoroutine(FocusFirst());
    }

    void OnDisable()
    {
        //ContactManager.OnThreadUnlocked -= OnNewThreadUnlocked;
    }

    void OnDestroy()
    {
        ContactManager.OnThreadUnlocked -= OnNewThreadUnlocked;
    }

    void Start()
    {
        foreach (var thread in ContactManager.Instance.GetDefaultThreads())
            SpawnOrUpdateButton(thread);
    }

    public void FocusContactList()
    {
        ChatSystem.Instance.ExitChatZone();
        //Debug.Log($"FocusContactList - firstButton: {firstButton?.name}");
        StartCoroutine(FocusFirst());
    }

    private void OnNewThreadUnlocked(ChatEntry thread)
    {
        // Cek apakah NPC ini sudah punya button
        NPCChatDatabase db = ContactManager.Instance.GetDatabaseByThread(thread.id);
        if (db == null) return;

        if (npcButtonMap.ContainsKey(db.npcName))
        {
            // NPC sudah ada — update data button yang ada (tidak buat baru)
            UpdateButton(npcButtonMap[db.npcName], thread);
        }
        else
        {
            Debug.Log($"OnNewThreadUnlocked dipanggil: {thread.threadName}");
            // NPC baru — buat button baru
            SpawnOrUpdateButton(thread);
        }
    }

    private void SpawnOrUpdateButton(ChatEntry thread)
    {
        NPCChatDatabase db = ContactManager.Instance.GetDatabaseByThread(thread.id);
        if (db == null) return;

        // Kalau NPC ini sudah punya button, skip
        if (npcButtonMap.ContainsKey(db.npcName)) return;

        GameObject btn = Instantiate(threadButtonPrefab, threadListContent);
        npcButtonMap[db.npcName] = btn;

        UpdateButton(btn, thread);

        if (firstButton == null)
            firstButton = btn;
    }

    private void UpdateButton(GameObject btn, ChatEntry thread)
    {
        TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = thread.threadName;

        Button button = btn.GetComponent<Button>();
        if (button != null)
        {
            // Clear listener lama supaya tidak dobel
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                ChatSystem.Instance.StartChat(thread.dialogue);
                //PanelManager.Instance.OpenPanelSilent(chatPanel);
                ChatSystem.Instance.EnterChatZone(); // masuk chat zone
                ChatSystem.Instance.FocusChat(); // langsung panggil dari ChatSystem
            });
        }
    }


    private IEnumerator FocusFirst()
    {
        yield return null;
        Debug.Log($"FocusFirst - firstButton: {firstButton?.name}");
        if (firstButton == null) yield break;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstButton);
        Debug.Log($"Selected: {EventSystem.current.currentSelectedGameObject?.name}");
    }
}