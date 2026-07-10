using UnityEngine;
using UnityEngine.SceneManagement;

public class DummyScene : MonoBehaviour
{
    public void AnalyzeScene(string Nama)
    {
        SceneManager.LoadScene(Nama);
    }
}
