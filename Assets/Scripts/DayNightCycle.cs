using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance;

    public float dayLength = 300f;
    public float currentTime = 0f;
    public int currentDay = 1;
    public bool isDay = true;

    void Awake()
    {
        Instance = this;
    }

    private bool wasNight = false;

    void Update()
    {
        currentTime += Time.deltaTime;

        if (currentTime >= dayLength)
        {
            currentTime = 0f;
            currentDay++;
            Debug.Log("Day " + currentDay + " 시작!");
        }

        bool nightNow = currentTime >= dayLength / 2f;

        // 밤 → 낮 전환 시점 = 밤 종료 → FoodPoint 리스폰
        if (wasNight && !nightNow)
        {
            foreach (var fp in FindObjectsByType<FoodPoint>())
                fp.Restock();
        }

        wasNight = nightNow;
        isDay = !nightNow;
    }

    public float GetTimeRatio()
    {
        return currentTime / dayLength;
    }

    public bool IsDay()
    {
        return isDay;
    }
}