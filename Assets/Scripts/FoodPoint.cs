using UnityEngine;
using TMPro;

public class FoodPoint : MonoBehaviour
{
    public GameObject foodPrefab;
    public int maxAmount = 5;
    public int currentAmount = 5;

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
            // 수량 0이면 반투명하게
            var c = iconRenderer.color;
            c.a = currentAmount > 0 ? 1f : 0.35f;
            iconRenderer.color = c;
        }

        // 수량 0이면 Food 태그 제거 (채집 불가)
        if (foodIcon != null)
            foodIcon.tag = currentAmount > 0 ? "Food" : "Untagged";
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
