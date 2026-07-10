using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class UIPanel : MonoBehaviour
{
    [SerializeField] protected GameObject firstSelect;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public virtual void Open()
    {
        gameObject.SetActive(true);
        //StartCoroutine(FocusNextFrame());
        FocusView();
    }

    // Update is called once per frame
    public virtual void Close()
    {
        gameObject.SetActive(false);
    }

    public void SetFirstSelect(GameObject obj)
    {
        //this.firstSelect = firstSelect;
        firstSelect = obj;
    }

    //private IEnumerator FocusNextFrame()
    //{
        //yield return null;
        //if (firstSelect == null)
            //yield break;

        //if (firstSelect == null) return;
        //EventSystem.current.SetSelectedGameObject(null);
        //EventSystem.current.SetSelectedGameObject(firstSelect);
    //}

    private void FocusView()
    {
        if (firstSelect == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelect);
    }
}
