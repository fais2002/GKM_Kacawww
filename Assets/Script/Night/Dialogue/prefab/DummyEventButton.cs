using UnityEngine;

public class DummyEventButton : MonoBehaviour
{
    [SerializeField] private string threadIdToUnlock;
    [SerializeField] private string photoIdToUnlock;
    [SerializeField] private string photoIdToUnlock1;

    public void TriggerEvent()
    {
        Debug.Log($"TriggerEvent dipanggil, threadId: {threadIdToUnlock}");
        ContactManager.Instance.UnlockThread(threadIdToUnlock);

        
    }

    public void TriggerPhoto()
    {
        if (!string.IsNullOrEmpty(photoIdToUnlock))
            GalleryManager.Instance.UnlockPhoto(photoIdToUnlock);

        if (!string.IsNullOrEmpty(photoIdToUnlock))
            GalleryManager.Instance.UnlockPhoto(photoIdToUnlock1);
    }
}
