using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class ScrollToSelected : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    private GameObject lastSelected;

    void Update()
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || selected == lastSelected) return;
        if (!selected.transform.IsChildOf(scrollRect.content)) return;

        lastSelected = selected;
        StartCoroutine(ScrollToItem(selected.GetComponent<RectTransform>()));
    }

    private IEnumerator ScrollToItem(RectTransform target)
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

        RectTransform viewport = scrollRect.viewport;
        RectTransform content = scrollRect.content;

        Vector3[] itemCorners = new Vector3[4];
        Vector3[] viewportCorners = new Vector3[4];

        target.GetWorldCorners(itemCorners);
        viewport.GetWorldCorners(viewportCorners);

        float itemTop = itemCorners[1].y;
        float itemBottom = itemCorners[0].y;
        float viewportTop = viewportCorners[1].y;
        float viewportBottom = viewportCorners[0].y;

        bool isAbove = itemTop > viewportTop;
        bool isBelow = itemBottom < viewportBottom;

        if (!isAbove && !isBelow) yield break;

        float currentPos = content.anchoredPosition.y;
        float newPos = currentPos;

        if (isBelow)
            newPos = currentPos - (viewportBottom - itemBottom);
        else if (isAbove)
            newPos = currentPos + (itemTop - viewportTop);

        float scrollableHeight = content.rect.height - viewport.rect.height;
        newPos = Mathf.Clamp(newPos, 0, scrollableHeight);
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, newPos);
    }
}