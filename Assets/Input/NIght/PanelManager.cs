using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PanelManager : MonoBehaviour
{
    public static PanelManager Instance { get; private set; }

    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Animator animator;

    private Stack<UIPanel> panelStack = new Stack<UIPanel>();
    private InputAction cancelInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        var uiMap = inputActions.FindActionMap("UI");
        cancelInput = uiMap.FindAction("UIBack");
    }

    // Update is called once per frame
    void OnEnable()
    {
        cancelInput.Enable();
        cancelInput.performed += OnCancel;
    }

    void OnDisable()
    {
        cancelInput.performed -= OnCancel;
        cancelInput.Disable();
    }

    public void OpenPanelSilent(UIPanel panel)
    {
        if (panelStack.Count > 0)
        {
            panelStack.Peek().gameObject.SetActive(false);
        }

        panelStack.Push(panel);
        panel.Open();
    }

    public void OpenPanelShow(UIPanel panel)
    {
        panelStack.Push(panel);
        panel.Open();
    }

    // Panggil ini untuk membuka panel baru
    public void OpenPanel(UIPanel panel)
    {
        // Pause panel yang sedang aktif (tapi tidak di-close)
        if (panelStack.Count > 0)
        {
            panelStack.Peek().gameObject.SetActive(false);
        }

        if (animator != null)
            animator.SetTrigger("Trigger");

        panelStack.Push(panel);
        panel.Open();
    }

    // Panggil ini untuk kembali ke panel sebelumnya
    public void GoBack()
    {
        if (panelStack.Count <= 1)
        {
            Debug.Log("Sudah di root panel");
            return;
        }

        if (ChatSystem.Instance != null && ChatSystem.Instance.IsFocusedOnChat())
        {
            // Kembali ke contact list (kiri), tidak pop stack
            ChatSystem.Instance.SaveAndExitToContactList();
            return; // jangan pop stack!
        }

        // Focus di contact list → pop stack → kembali ke Home
        if (ChatSystem.Instance != null)
            ChatSystem.Instance.SaveAndExit();

        UIPanel current = panelStack.Pop();
        current.Close();

        if (panelStack.Count > 0)
        {
            if (panelStack.Count == 1 && animator != null)
                animator.SetTrigger("TriggerInvert");
            panelStack.Peek().Open();
        }
    }

    // Kembali langsung ke panel tertentu (skip semua di antaranya)
    public void GoBackTo(UIPanel targetPanel)
    {
        while (panelStack.Count > 0 && panelStack.Peek() != targetPanel)
        {
            panelStack.Pop().Close();
        }

        if (panelStack.Count > 0)
        {
            panelStack.Peek().Open();
        }
    }

    // Reset semua dan buka panel dari awal
    public void ClearAndOpen(UIPanel panel)
    {
        while (panelStack.Count > 0)
        {
            panelStack.Pop().Close();
        }

        OpenPanel(panel);
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        GoBack();
    }
}
