using UnityEngine;

[CreateAssetMenu(fileName = "PhotoList", menuName = "Gallery/PhotoList")]
public class PhotoList : ScriptableObject
{
    public string photoId;
    public Sprite photo;
    public string caption;
}
