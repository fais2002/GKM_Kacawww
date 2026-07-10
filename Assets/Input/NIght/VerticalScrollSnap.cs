using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class VerticalScrollSnap : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private float snapDuration = 0.3f;

    private InputAction navigateAction;
    private int currentIndex = 0;
    private int itemCount = 0;
    private bool isScrolling = false;

    private void Awake()
    {
        if (!InitializeInputActions())
        {
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (navigateAction == null) return;
        navigateAction.Enable();
        navigateAction.performed += OnNavigate;
    }

    private void OnDisable()
    {
        if (navigateAction == null) return;
        navigateAction.performed -= OnNavigate;
        navigateAction.Disable();
    }

    private void Start()
    {
        if (scrollRect == null || scrollRect.content == null)
        {
            Debug.LogError("ScrollRect or Content is not assigned!");
            enabled = false;
            return;
        }

        itemCount = scrollRect.content.childCount;

        if (itemCount == 0)
        {
            Debug.LogWarning("ScrollRect content has no items!");
            enabled = false;
        }
    }

    private bool InitializeInputActions()
    {
        if (inputActions == null)
        {
            Debug.LogError("InputActionAsset is not assigned!");
            return false;
        }

        var uiMap = inputActions.FindActionMap("UI");
        if (uiMap == null)
        {
            Debug.LogError("Action Map 'UI' not found!");
            return false;
        }

        navigateAction = uiMap.FindAction("UIFeed");
        if (navigateAction == null)
        {
            Debug.LogError("Action 'UIFeed' not found!");
            return false;
        }

        return true;
    }

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (isScrolling) return;

        float input = ctx.ReadValue<Vector2>().y;

        if (input < 0) // Down
            ScrollToIndex(currentIndex + 1);
        else if (input > 0) // Up
            ScrollToIndex(currentIndex - 1);
    }

    public void ScrollToIndex(int index)
    {
        index = Mathf.Clamp(index, 0, itemCount - 1);
        if (index == currentIndex) return;

        currentIndex = index;
        StartCoroutine(SmoothScrollCoroutine());
    }

    public void ScrollToNext() => ScrollToIndex(currentIndex + 1);
    public void ScrollToPrev() => ScrollToIndex(currentIndex - 1);

    public int CurrentIndex => currentIndex;
    public int ItemCount => itemCount;

    private IEnumerator SmoothScrollCoroutine()
    {
        isScrolling = true;

        float targetPosition = itemCount > 1 ? 1f - ((float)currentIndex / (itemCount - 1)) : 1f;
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
    }
}