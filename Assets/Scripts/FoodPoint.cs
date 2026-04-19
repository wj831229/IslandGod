using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class FoodPoint : MonoBehaviour
{
    public GameObject foodPrefab;
    public int maxAmount = 5;
    public int currentAmount = 5;

    [Header("발견 설정")]
    [Range(0f, 1f)]
    public float discoveryChance = 1f;  // 1 = 무조건 발견, 0.5 = 50% 확률

    private GameObject foodIcon;
    private TextMeshPro amountText;
    private SpriteRenderer iconRenderer;

    void Start()
    {
        SpawnIcon();
    }

    void SpawnIcon()
    {
        if (foodPrefab == null)
        {
            Debug.LogError($"[FoodPoint] foodPrefab is null on {gameObject.name}", this);
            return;
        }

        foodIcon = Instantiate(foodPrefab, transform.position, Quaternion.identity);

        FoodItem item = foodIcon.GetComponent<FoodItem>();
        if (item == null) item = foodIcon.AddComponent<FoodItem>();
        item.foodPoint = this;
        item.hungerRestore = 40f;

        iconRenderer = foodIcon.GetComponent<SpriteRenderer>();

        // 수량 텍스트 생성
        GameObject textObj = new GameObject("AmountText");
        textObj.transform.SetParent(foodIcon.transform);
        textObj.transform.localPosition = new Vector3(0, 0.3f, 0);

        amountText = textObj.AddComponent<TextMeshPro>();
        amountText.fontSize = 1.2f;
        amountText.alignment = TextAlignmentOptions.Center;
        amountText.sortingOrder = 5;

        UpdateDisplay();

        SetVisible(true);
    }

    void Update()
    {
        UpdateVisibility();
    }

    void UpdateVisibility()
    {
        if (ViewManager.Instance == null || ViewManager.Instance.IsGodView)
        {
            // 신의 시점: 모두 표시
            SetVisible(true);
            SetAlpha(currentAmount > 0 ? 1f : 0.35f);
        }
        else
        {
            // 표류자 시점: 발견한 건 선명, 미발견은 숨김
            var survivor = ViewManager.Instance.Selected;
            bool discovered = survivor != null && survivor.discoveredFoods.Contains(this);
            if (discovered)
            {
                SetVisible(true);
                SetAlpha(currentAmount > 0 ? 1f : 0.35f);
            }
            else
                SetVisible(false);
        }
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

    void SetVisible(bool visible)
    {
        if (iconRenderer != null) iconRenderer.enabled = visible;
        if (amountText != null) amountText.enabled = visible;
    }

    void UpdateDisplay()
    {
        if (amountText != null)
        {
            amountText.text = $"{currentAmount}/{maxAmount}";
            amountText.color = currentAmount > 0 ? Color.white : Color.gray;
        }

        if (iconRenderer != null)
        {
            var c = iconRenderer.color;
            c.a = currentAmount > 0 ? 1f : 0.35f;
            iconRenderer.color = c;
        }
    }

    public void OnFoodTaken()
    {
        currentAmount--;
        UpdateDisplay();
        Debug.Log($"[FoodPoint] {currentAmount}/{maxAmount}");
    }

    // DayNightCycle에서 밤→낮 전환 시 호출
    public void Restock()
    {
        if (currentAmount >= maxAmount) return;
        currentAmount++;
        UpdateDisplay();
        Debug.Log($"[FoodPoint] 리스폰 +1 → {currentAmount}/{maxAmount}");
    }
}
