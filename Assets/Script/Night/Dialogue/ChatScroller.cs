using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ChatScroller : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private float scrollSpeed = 500f;

    private InputAction scrollAction;
    private bool autoScroll = true; // aktif saat bubble baru muncul

    void Awake()
    {
        var uiMap = inputActions.FindActionMap("UI");
        scrollAction = uiMap.FindAction("UIFeed"); // tambah action baru
    }

    void OnEnable()
    {
        scrollAction.Enable();
    }

    void OnDisable()
    {
        scrollAction.Disable();
    }

    void Update()
    {
        float input = scrollAction.ReadValue<Vector2>().y;
        if (input == 0) return;

        autoScroll = false;

        float contentHeight = scrollRect.content.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;
        float scrollableHeight = contentHeight - viewportHeight;

        if (scrollableHeight <= 0) return;

        // Pakai normalizedPosition milik ScrollRect
        float currentNorm = scrollRect.verticalNormalizedPosition;
        float delta = (input * scrollSpeed * Time.deltaTime) / scrollableHeight;
        float newNorm = Mathf.Clamp01(currentNorm + delta);

        scrollRect.verticalNormalizedPosition = newNorm;

        if (scrollRect.verticalNormalizedPosition <= 0.01f)
            autoScroll = true;
    }

    // Dipanggil dari ChatSystem setiap bubble baru spawn
    public void ScrollToBottom()
    {
        if (!autoScroll) return;
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
