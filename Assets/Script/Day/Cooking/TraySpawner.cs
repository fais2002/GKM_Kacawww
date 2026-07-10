using UnityEngine;

/// <summary>Click this object to receive a fresh tray.</summary>
public class TraySpawner : MonoBehaviour
{
    public GameObject trayPrefab;
    public Transform  spawnPoint;

    void OnMouseDown() => TrySpawnTray();

    public void TrySpawnTray()
    {
        if (GameManager.Instance.ActiveTray != null)
        {
            Debug.Log("[TraySpawner] Already holding a tray.");
            return;
        }
        if (!PhaseManager.Instance.PhaseIsRunning)
        {
            Debug.Log("[TraySpawner] No active phase.");
            return;
        }

        Vector3    pos  = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject go   = Instantiate(trayPrefab, pos, Quaternion.identity);
        FoodTray   tray = go.GetComponent<FoodTray>();
        if (tray == null) { Destroy(go); return; }

        tray.PickUp();
        GameManager.Instance.SetActiveTray(tray);
    }
}
