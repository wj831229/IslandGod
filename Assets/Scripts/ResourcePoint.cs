using UnityEngine;
using TMPro;

public class ResourcePoint : MonoBehaviour
{
    [Header("자원 설정")]
    public string itemName = "목재";
    public int maxAmount = 3;
    public int currentAmount = 3;

    [Header("발견 설정")]
    [Range(0f, 1f)]
    public float discoveryChance = 0.8f;

    private SpriteRenderer iconRenderer;
    private TextMeshPro amountText;

    void Start()
    {
        SetupVisuals();
    }

    void SetupVisuals()
    {
        iconRenderer = GetComponent<SpriteRenderer>();
        if (iconRenderer == null)
            iconRenderer = gameObject.AddComponent<SpriteRenderer>();
        iconRenderer.sortingOrder = 1;

        // 수량 텍스트
        var textObj = new GameObject("AmountText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = new Vector3(0, 0.35f, 0);
        amountText = textObj.AddComponent<TextMeshPro>();
        amountText.fontSize = 1.2f;
        amountText.alignment = TextAlignmentOptions.Center;
        amountText.sortingOrder = 5;

        UpdateDisplay();
        SetVisible(false);
    }

    void Update()
    {
        UpdateVisibility();
    }

    void UpdateVisibility()
    {
        if (ViewManager.Instance == null || ViewManager.Instance.IsGodView)
        {
            SetVisible(true);
            SetAlpha(currentAmount > 0 ? 1f : 0.35f);
        }
        else
        {
            var survivor = ViewManager.Instance.Selected;
            bool discovered = survivor != null && survivor.discoveredResources.Contains(this);
            if (discovered)
            {
                SetVisible(true);
                SetAlpha(currentAmount > 0 ? 1f : 0.35f);
            }
            else
                SetVisible(false);
        }
    }

    void SetVisible(bool visible)
    {
        if (iconRenderer != null) iconRenderer.enabled = visible;
        if (amountText != null)   amountText.enabled   = visible;
    }

    void SetAlpha(float alpha)
    {
        if (iconRenderer != null)
        {
            var c = iconRenderer.color;
            c.a = alpha;
            iconRenderer.color = c;
        }
        if (amountText != null)
            amountText.alpha = alpha;
    }

    void UpdateDisplay()
    {
        if (amountText == null) return;
        amountText.text  = $"{currentAmount}/{maxAmount}";
        amountText.color = currentAmount > 0 ? Color.white : Color.gray;
    }

    public void OnResourceTaken()
    {
        currentAmount = Mathf.Max(0, currentAmount - 1);
        UpdateDisplay();
        Debug.Log($"[ResourcePoint] {itemName} {currentAmount}/{maxAmount}");
    }

    // DayNightCycle에서 밤→낮 전환 시 호출
    public void Restock()
    {
        if (currentAmount >= maxAmount) return;
        currentAmount++;
        UpdateDisplay();
    }
}
