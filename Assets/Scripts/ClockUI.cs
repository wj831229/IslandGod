using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClockUI : MonoBehaviour
{
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI timeText;
    public RectTransform clockHand;

    void Update()
    {
        if (DayNightCycle.Instance == null) return;

        float ratio = DayNightCycle.Instance.GetTimeRatio();
        int day = DayNightCycle.Instance.currentDay;
        int currentTime = (int)DayNightCycle.Instance.currentTime;

        // DAY 텍스트
        dayText.text = "DAY " + day;

        // 시간 텍스트
        timeText.text = currentTime + " / " + (int)DayNightCycle.Instance.dayLength;

        // 시계 바늘 회전 (0초 = 0도, dayLength초 = 360도)
        float angle = ratio * 360f;
        clockHand.localRotation = Quaternion.Euler(0, 0, -angle);

        // 낮/밤 색상 변경
        if (DayNightCycle.Instance.IsDay())
            dayText.color = new Color(1f, 0.8f, 0f); // 노란색 (낮)
        else
            dayText.color = new Color(0.5f, 0.5f, 1f); // 파란색 (밤)
    }
}