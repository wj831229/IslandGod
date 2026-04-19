using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightVisual : MonoBehaviour
{
    [Header("Global Light 2D")]
    public Light2D globalLight;

    [Header("낮 설정")]
    public Color dayColor     = new Color(1.0f, 0.95f, 0.85f);
    public float dayIntensity = 1.0f;

    [Header("노을/새벽 설정")]
    public Color duskColor     = new Color(1.0f, 0.5f, 0.2f);
    public float duskIntensity = 0.75f;

    [Header("밤 설정")]
    public Color nightColor     = new Color(0.1f, 0.15f, 0.35f);
    public float nightIntensity = 0.35f;

    void Update()
    {
        if (DayNightCycle.Instance == null || globalLight == null) return;

        float r = DayNightCycle.Instance.GetTimeRatio(); // 0 ~ 1

        // 시간대 구간 (ratio 기준)
        // 0.00 ~ 0.08 : 새벽 (밤 → 노을)
        // 0.08 ~ 0.18 : 아침 (노을 → 낮)
        // 0.18 ~ 0.42 : 낮
        // 0.42 ~ 0.52 : 저녁 노을 (낮 → 노을)
        // 0.52 ~ 0.62 : 황혼 (노을 → 밤)
        // 0.62 ~ 1.00 : 밤

        Color  col;
        float  intensity;

        if (r < 0.08f)
        {
            float t = r / 0.08f;
            col = Color.Lerp(nightColor, duskColor, t);
            intensity = Mathf.Lerp(nightIntensity, duskIntensity, t);
        }
        else if (r < 0.18f)
        {
            float t = (r - 0.08f) / 0.10f;
            col = Color.Lerp(duskColor, dayColor, t);
            intensity = Mathf.Lerp(duskIntensity, dayIntensity, t);
        }
        else if (r < 0.42f)
        {
            col = dayColor;
            intensity = dayIntensity;
        }
        else if (r < 0.52f)
        {
            float t = (r - 0.42f) / 0.10f;
            col = Color.Lerp(dayColor, duskColor, t);
            intensity = Mathf.Lerp(dayIntensity, duskIntensity, t);
        }
        else if (r < 0.62f)
        {
            float t = (r - 0.52f) / 0.10f;
            col = Color.Lerp(duskColor, nightColor, t);
            intensity = Mathf.Lerp(duskIntensity, nightIntensity, t);
        }
        else
        {
            col = nightColor;
            intensity = nightIntensity;
        }

        globalLight.color     = col;
        globalLight.intensity = intensity;
    }
}
