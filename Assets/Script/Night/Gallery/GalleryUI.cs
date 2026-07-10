using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;


public class GalleryUI : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private Transform gridContent;
    [SerializeField] private GameObject photoItemPrefab;
    [SerializeField] private GameObject emptyLabel; // "No pictures taken"

    [Header("Preview")]
    [SerializeField] private UIPanel previewPanel;
    [SerializeField] private Image previewImage;
    [SerializeField] private TextMeshProUGUI previewCaption;

    private GameObject firstItem;

    void Awake()
    {
        GalleryManager.OnPhotoUnlocked += OnPhotoUnlocked;
    }

    void OnDestroy()
    {
        GalleryManager.OnPhotoUnlocked -= OnPhotoUnlocked;
    }

    void OnEnable()
    {
        // Load semua foto yang sudah unlock saat panel dibuka
        foreach (Transform child in gridContent)
            Destroy(child.gameObject);

        firstItem = null;

        var photos = GalleryManager.Instance.GetUnlockedPhotos();
        emptyLabel.SetActive(photos.Count == 0);

        foreach (var photo in photos)
            SpawnPhotoItem(photo);

        StartCoroutine(FocusFirst());
    }

    private void OnPhotoUnlocked(PhotoList photo)
    {
        emptyLabel.SetActive(false);
        SpawnPhotoItem(photo);
    }

    private void SpawnPhotoItem(PhotoList photo)
    {
        GameObject item = Instantiate(photoItemPrefab, gridContent);
        item.GetComponent<Image>().sprite = photo.photo;

        if (firstItem == null) firstItem = item;

        item.GetComponent<Button>().onClick.AddListener(() => OpenPreview(photo));
    }

    private void OpenPreview(PhotoList photo)
    {
        PanelManager.Instance.OpenPanelSilent(previewPanel);
        previewImage.sprite = photo.photo;
        previewCaption.text = photo.caption;
    }

    //public void ClosePreview()
    //{
        //previewPanel.SetActive(false);
    //}

    private IEnumerator FocusFirst()
    {
        yield return null;
        if (firstItem == null) yield break;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstItem);
    }
}
