using UnityEngine;

public class StartingPanel : MonoBehaviour
{
    [SerializeField] private UIPanel mainMenu;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PanelManager.Instance.OpenPanelSilent(mainMenu);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
