using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Place one of these in every minigame scene.
///
/// When the minigame is done (win or lose), call MinigameComplete().
/// This signals SpecialNPCManager to continue the NPC sequence.
///
/// Usage:
///   - Drop this script on any GameObject in the minigame scene.
///   - Call MinigameComplete() from your minigame logic when finished.
///   - Or wire the completeButton to call it directly.
///
/// Each minigame scene is loaded additively over the cooking scene.
/// It is unloaded automatically by SpecialNPCManager after this fires.
/// </summary>
public class MinigameBridge : MonoBehaviour
{
    [Header("Optional — wire a button for testing")]
    public Button completeButton;

    void Start()
    {
        completeButton?.onClick.AddListener(MinigameComplete);
    }

    /// <summary>
    /// Call this when the minigame ends.
    /// </summary>
    public void MinigameComplete()
    {
        Debug.Log("[MinigameBridge] Minigame complete — signalling NPC manager.");
        SpecialNPCManager.Instance?.OnMinigameComplete();
    }
}
