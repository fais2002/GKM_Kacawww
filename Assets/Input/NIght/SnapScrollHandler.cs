using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class SnapScrollHandler : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private float snapDuration = 0.3f;

    private InputAction navigateAction;
    private int currentIndex = 0;
    private int itemCount;
    private bool isScrolling = false;
    private RectTransform[] items;
    private Coroutine scrollCoroutine;

    void Awake()
    {
        if (inputActions == null) { Debug.LogError("InputActions belum di-assign!"); return; }

        var uiMap = inputActions.FindActionMap("UI");
        navigateAction = uiMap?.FindAction("UIFeed");
    }

    void OnEnable()
    {
        if (navigateAction == null) return;
        navigateAction.Enable();
        navigateAction.performed += OnNavigate;
    }

    void OnDisable()
    {
        if (navigateAction == null) return;
        navigateAction.performed -= OnNavigate;
        navigateAction.Disable();
    }

    void Start()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

        itemCount = scrollRect.content.childCount;
        items = new RectTransform[itemCount];
        for (int i = 0; i < itemCount; i++)
        {
            items[i] = scrollRect.content.GetChild(i) as RectTransform;
            Debug.Log($"[Item {i}] anchoredPosition.y={items[i].anchoredPosition.y} | height={items[i].rect.height}");
        }
    }

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        // isScrolling dicek di sini, bukan di dalam coroutine
        if (isScrolling) return;

        float input = ctx.ReadValue<Vector2>().y;

        if (input < 0)
            ScrollTo(currentIndex + 1);
        else if (input > 0)
            ScrollTo(currentIndex - 1);
    }

    private void ScrollTo(int index)
    {
        index = Mathf.Clamp(index, 0, itemCount - 1);
        if (index == currentIndex) return;

        currentIndex = index;

        // Set true SEBELUM StartCoroutine agar input berikutnya langsung terblock
        isScrolling = true;

        if (scrollCoroutine != null) StopCoroutine(scrollCoroutine);
        scrollCoroutine = StartCoroutine(SmoothScroll());
    }

    private float GetNormalizedPositionForIndex(int index)
    {
        RectTransform content = scrollRect.content;
        RectTransform viewport = scrollRect.viewport;

        float scrollableHeight = content.rect.height - viewport.rect.height;
        if (scrollableHeight <= 0f) return 1f;

        // Offset dari atas content ke top of item
        float itemTopFromContentTop = (-items[index].anchoredPosition.y)
                                      - (items[index].rect.height / 2f);

        // Agar item center di viewport
        float centeredOffset = itemTopFromContentTop
                               - (viewport.rect.height / 2f)
                               + (items[index].rect.height / 2f);

        centeredOffset = Mathf.Clamp(centeredOffset, 0f, scrollableHeight);

        // ScrollRect: 1 = atas, 0 = bawah
        float result = 1f - (centeredOffset / scrollableHeight);

        return result;
    }

    private IEnumerator SmoothScroll()
    {
        float targetPosition = GetNormalizedPositionForIndex(currentIndex);
        float startPosition = scrollRect.verticalNormalizedPosition;
        float elapsed = 0f;

        while (elapsed < snapDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / snapDuration);
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        scrollRect.verticalNormalizedPosition = targetPosition;
        isScrolling = false;
        scrollCoroutine = null;
    }
}