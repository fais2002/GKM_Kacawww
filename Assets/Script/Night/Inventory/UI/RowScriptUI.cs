using UnityEngine;
using TMPro;

public class RowScriptUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI NamaItem;

    public void Setup(string nama)
    {
        NamaItem.text = nama;
    }
}
