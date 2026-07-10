using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class ChatSystem : MonoBehaviour
{
    public static ChatSystem Instance { get; private set; }

    public static event Action OnChatStarted;
    public static event Action OnChatFinished;
    public static event Action<string> OnChoiceMade;

    [Header("UI")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject choiceContainer;
    [SerializeField] private GameObject buttonNext;
    [SerializeField] private ChatScroller chatScroller;

    [Header("Prefabs")]
    [SerializeField] private GameObject bubblePlayerPrefab;
    [SerializeField] private GameObject bubbleNPCPrefab;
    [SerializeField] private GameObject choiceButtonPrefab;

    private List<ChatMessage> messages;
    private bool isWaiting = false;
    private bool isInChatZone = false;

    private DialogueData currentData;
    private int currentIndex = 0;
    private GameObject lastBubble;

    // Simpan history teks per contact
    private Dictionary<DialogueData, List<(Sender sender, string message)>> historyMap
        = new Dictionary<DialogueData, List<(Sender, string)>>();
    private Dictionary<DialogueData, int> progressMap
        = new Dictionary<DialogueData, int>();

    private List<(Sender sender, string message)> currentHistory;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        buttonNext.GetComponent<Button>().onClick.AddListener(ShowNext);
    }

    // ═══════════════════════════
    // PUBLIC API
    // ═══════════════════════════

    public void StartChat(DialogueData data)
    {
        // Simpan progress contact sebelumnya
        if (currentData != null)
            progressMap[currentData] = currentIndex;

        currentData = data;
        messages = data.message;

        // Bersihkan bubble yang tampil sekarang
        foreach (Transform child in content)
            Destroy(child.gameObject);

        lastBubble = null;
        choiceContainer.SetActive(false);
        buttonNext.SetActive(false);
        isWaiting = false;

        if (historyMap.ContainsKey(data))
        {
            // Re-spawn bubble dari history
            currentHistory = historyMap[data];
            foreach (var (sender, message) in currentHistory)
                SpawnBubble(sender, message, false);

            currentIndex = progressMap.ContainsKey(data) ? progressMap[data] : 0;

            if (currentIndex < messages.Count)
            {
                isWaiting = true;
                buttonNext.SetActive(true);
                StartCoroutine(FocusObject(buttonNext));
            }
            else
            {
                buttonNext.SetActive(false);
                if (lastBubble != null)
                    StartCoroutine(FocusObject(lastBubble));
            }
        }
        else
        {
            // Pertama kali buka contact ini
            currentHistory = new List<(Sender, string)>();
            historyMap[data] = currentHistory;
            currentIndex = 0;
            OnChatStarted?.Invoke();
            ShowMessage();
        }

        StartCoroutine(ScrollToBottom());
    }

    public void SaveAndExit()
    {
        if (currentData != null)
            progressMap[currentData] = currentIndex;
    }

    public void ShowNext()
    {
        if (!isWaiting) return;
        isWaiting = false;
        buttonNext.SetActive(false);
        currentIndex++;
        ShowMessage();
    }

    public void FocusChat()
    {
        StartCoroutine(FocusChatCoroutine());
    }

    public GameObject GetFocusTarget()
    {
        if (messages != null && currentIndex < messages.Count)
            return buttonNext;
        return lastBubble;
    }

    public void SaveAndExitToContactList()
    {
        SaveAndExit();
        isInChatZone = false; // keluar dari chat zone

        // Kembalikan fokus ke contact list
        if (ThreadListUI.Instance != null)
            ThreadListUI.Instance.FocusContactList();
    }

    //public bool IsFocusedOnChat()
    //{
        //GameObject selected = EventSystem.current.currentSelectedGameObject;
        //if (selected == null) return false;

        // Cek apakah selected object ada di dalam chat content (kanan)
        //return selected.transform.IsChildOf(content) ||
               //selected == buttonNext ||
               //selected.transform.IsChildOf(choiceContainer.transform);
    //}

    public bool IsFocusedOnChat() => isInChatZone;

    public void EnterChatZone()
    {
        isInChatZone = true;
    }

    public void ExitChatZone()
    {
        isInChatZone = false;
    }

    // ═══════════════════════════
    // PRIVATE
    // ═══════════════════════════

    private void ShowMessage()
    {
        if (messages == null) return;

        if (currentIndex >= messages.Count)
        {
            buttonNext.SetActive(false);
            isWaiting = false;
            if (lastBubble != null)
                StartCoroutine(FocusObject(lastBubble));
            OnChatFinished?.Invoke();
            return;
        }

        ChatMessage msg = messages[currentIndex];

        if (msg.type == Message.Normal)
        {
            isWaiting = true;
            SpawnBubble(msg.sender, msg.message);
            buttonNext.SetActive(true);
            StartCoroutine(FocusObject(buttonNext));
        }
        else if (msg.type == Message.Choice)
        {
            isWaiting = false;
            buttonNext.SetActive(false);
            SpawnChoices(msg);
        }

        StartCoroutine(ScrollToBottom());
    }

    private void SpawnBubble(Sender sender, string message, bool saveToHistory = true)
    {
        GameObject prefab = sender == Sender.Player
            ? bubblePlayerPrefab
            : bubbleNPCPrefab;

        GameObject bubble = Instantiate(prefab, content);
        bubble.GetComponent<ChatBubble>().Setup(message);
        lastBubble = bubble;

        if (saveToHistory)
            currentHistory.Add((sender, message));
    }

    private void SpawnChoices(ChatMessage msg)
    {
        foreach (Transform child in choiceContainer.transform)
            Destroy(child.gameObject);

        choiceContainer.SetActive(true);
        GameObject firstButton = null;

        foreach (Choice choice in msg.choices)
        {
            Choice captured = choice;
            GameObject btn = Instantiate(choiceButtonPrefab, choiceContainer.transform);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = captured.buttonText;
            btn.GetComponent<Button>().onClick.AddListener(() => OnChoiceSelected(captured));
            if (firstButton == null) firstButton = btn;
        }

        StartCoroutine(FocusObject(firstButton));
    }

    private void OnChoiceSelected(Choice choice)
    {
        choiceContainer.SetActive(false);

        SpawnBubble(Sender.Player, choice.playerText);

        if (!string.IsNullOrEmpty(choice.npcReply))
            SpawnBubble(Sender.NPC, choice.npcReply);

        OnChoiceMade?.Invoke(choice.playerText);

        currentIndex++;
        StartCoroutine(FocusObject(buttonNext));
        ShowMessage();
        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator FocusObject(GameObject target)
    {
        yield return null;
        if (target == null) yield break;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target);
    }

    private IEnumerator FocusChatCoroutine()
    {
        yield return null;
        GameObject target = GetFocusTarget();
        if (target == null) yield break;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target);
    }

    private IEnumerator ScrollToBottom()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        chatScroller.ScrollToBottom();
    }
}