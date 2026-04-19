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

    void Update()
    {
        currentTime += Time.deltaTime;

        if (currentTime >= dayLength)
        {
            currentTime = 0f;
            currentDay++;
            Debug.Log("Day " + currentDay + " 시작!");
        }

        isDay = currentTime < dayLength / 2f;
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