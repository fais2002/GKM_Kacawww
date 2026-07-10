using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class GalleryPreview : MonoBehaviour
{
    [SerializeField] private Button closeButton;

    void OnEnable()
    {
        // Fokus ke close button saat preview terbuka
        StartCoroutine(FocusClose());
    }

    private IEnumerator FocusClose()
    {
        yield return null;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        // Fokus kembali ke gallery otomatis karena panel preview inactive
    }
}
