using UnityEngine;
using TMPro;

/// <summary>
/// Displays the current day, phase name, and countdown timer.
/// Phase 3 (Special) shows "Special Guest" instead of a timer.
/// </summary>
public class PhaseUI : MonoBehaviour
{
    [Header("HUD labels")]
    public TextMeshProUGUI dayLabel;      // "Day 3 / 7"
    public TextMeshProUGUI phaseLabel;    // "Morning" / "Afternoon" / "Special Guest"
    public TextMeshProUGUI timeLabel;     // "4:23" or "Special Phase"

    [Header("Announcement")]
    public GameObject      announcementPanel;
    public TextMeshProUGUI announcementText;
    public float           announceDuration = 3f;

    void Awake()
    {
        PhaseManager.Instance.OnPhaseStart.AddListener(OnPhaseStart);
        PhaseManager.Instance.OnPhaseEnd.AddListener(OnPhaseEnd);
        PhaseManager.Instance.OnPhaseTimeTick.AddListener(OnTimeTick);
        PhaseManager.Instance.OnBreakStart.AddListener(OnBreakStart);
        DayManager.Instance.OnNewDay.AddListener(OnNewDay);
        DayManager.Instance.OnGameEnd.AddListener(OnGameEnd);

        announcementPanel?.SetActive(false);
    }

    void OnNewDay(int day)
    {
        if (dayLabel != null)
            dayLabel.text = $"Day {day} / {DayManager.MAX_DAYS}";
        string suffix = day <= 2 ? " (Tutorial)" : "";
        ShowAnnouncement($"Day {day}{suffix}");
    }

    void OnPhaseStart(int phase, string name)
    {
        if (phaseLabel != null) phaseLabel.text = name;

        if (phase == 1 && timeLabel != null) timeLabel.text = "7:00 AM";
        if (phase == 2 && timeLabel != null) timeLabel.text = "1:00 PM";
        if (phase == 3 && timeLabel != null) timeLabel.text = "Special Phase";
        if (phase == 4 && timeLabel != null) timeLabel.text = "Night Phase";

        ShowAnnouncement(phase == 3 ? "A special guest is arriving..." : $"{name} phase begins!");
    }

    void OnPhaseEnd(int phase)
    {
        if (timeLabel != null) timeLabel.text = "--";
        if (phase < 3) ShowAnnouncement(phase == 1 ? "Morning over — lunch break!" : "Afternoon over!");
    }

    void OnBreakStart()
    {
        ShowAnnouncement("Break time — get ready!");
    }

    void OnTimeTick(float remaining)
    {
        if (timeLabel == null) return;
        int mins = Mathf.FloorToInt(remaining / 60f);
        int secs = Mathf.FloorToInt(remaining % 60f);
        timeLabel.text = $"{mins}:{secs:D2}";
    }

    void OnGameEnd()
    {
        if (phaseLabel != null) phaseLabel.text = "Day Over";
        if (timeLabel  != null) timeLabel.text  = "";
    }

    void ShowAnnouncement(string text)
    {
        if (announcementPanel == null) return;
        if (announcementText  != null) announcementText.text = text;
        announcementPanel.SetActive(true);
        CancelInvoke(nameof(HideAnnouncement));
        Invoke(nameof(HideAnnouncement), announceDuration);
    }

    void HideAnnouncement() => announcementPanel?.SetActive(false);
}
