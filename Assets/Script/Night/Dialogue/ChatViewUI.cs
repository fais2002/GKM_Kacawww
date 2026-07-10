using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class ChatViewUI : MonoBehaviour
{
    public static ChatViewUI Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void FocusChat()
    {
        // Fokus ke buttonNext kalau chat belum selesai
        // atau ke lastBubble kalau sudah selesai
        StartCoroutine(FocusChatCoroutine());
    }

    private IEnumerator FocusChatCoroutine()
    {
        yield return null;
        GameObject target = ChatSystem.Instance.GetFocusTarget();
        if (target == null) yield break;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target);
    }
}
