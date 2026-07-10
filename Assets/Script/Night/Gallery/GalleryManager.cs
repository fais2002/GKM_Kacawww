using UnityEngine;
using System.Collections.Generic;

public class GalleryManager : MonoBehaviour
{
    public static GalleryManager Instance { get; private set; }

    public static event System.Action<PhotoList> OnPhotoUnlocked;

    private HashSet<string> unlockedPhotos = new HashSet<string>();
    private Dictionary<string, PhotoList> photoDatabase = new Dictionary<string, PhotoList>();

    [SerializeField] private List<PhotoList> allPhotos;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        foreach (var photo in allPhotos)
            photoDatabase[photo.photoId] = photo;
    }

    public void UnlockPhoto(string photoId)
    {
        if (unlockedPhotos.Contains(photoId)) return;
        if (!photoDatabase.ContainsKey(photoId)) return;

        unlockedPhotos.Add(photoId);
        OnPhotoUnlocked?.Invoke(photoDatabase[photoId]);
    }

    public List<PhotoList> GetUnlockedPhotos()
    {
        List<PhotoList> result = new List<PhotoList>();
        foreach (var id in unlockedPhotos)
            if (photoDatabase.ContainsKey(id))
                result.Add(photoDatabase[id]);
        return result;
    }
}
