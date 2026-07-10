using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class PindahScene : MonoBehaviour
{
    public CinemachineVirtualCameraBase j;

    public void OnPindah(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void changepriority(int priority)
    {
        if (j != null)
            j.Priority = priority;
    }
}
