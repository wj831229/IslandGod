using UnityEngine;

public class DetectionRange : MonoBehaviour
{
    public float radius = 1f;
    public Color rangeColor = new Color(1f, 1f, 1f, 0.3f);

    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.loop = true;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = rangeColor;
        lineRenderer.endColor = rangeColor;
        lineRenderer.positionCount = 60;
        lineRenderer.enabled = false;
        lineRenderer.sortingOrder = 10;

        DrawCircle();
    }

    void DrawCircle()
    {
        for (int i = 0; i < 60; i++)
        {
            float angle = i * Mathf.PI * 2f / 60;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }

    public void SetSelected(bool selected)
    {
        if (lineRenderer != null)
            lineRenderer.enabled = selected;
    }

    public float GetRadius()
    {
        return radius;
    }
}