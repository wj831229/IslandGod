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
    public bool isDiscovered = false;   // true로 설정 시 처음부터 보임

    private GameObject foodIcon;
    private TextMeshPro amountText;
    private SpriteRenderer iconRenderer;

    // 현재 범위 안에 있는 표류자 추적 (진입 순간 판단용)
    private HashSet<SurvivorController> survivorsInRange = new();

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

        // 처음 발견 상태에 따라 가시성 결정
        SetVisible(isDiscovered);

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

    void Update()
    {
        // 이미 발견된 아이템은 항상 표시, 더 이상 체크 불필요
        if (isDiscovered)
        {
            SetVisible(true);
            return;
        }

        // 파괴된 표류자 정리
        survivorsInRange.RemoveWhere(s => s == null);

        foreach (var survivor in SurvivorController.All)
        {
            if (survivor == null) continue;

            float dist = Vector2.Distance(transform.position, survivor.transform.position);
            bool inRange = dist <= survivor.GetDetectionRadius();
            bool wasInRange = survivorsInRange.Contains(survivor);

            if (inRange && !wasInRange)
            {
                // 진입 순간 → 발견 확률 체크
                survivorsInRange.Add(survivor);

                float chance = Mathf.Clamp01(discoveryChance + survivor.discoveryBonus);
                float roll = Random.value;

                if (roll <= chance)
                {
                    isDiscovered = true;
                    SetVisible(true);
                    Debug.Log($"[발견] {gameObject.name} | 확률 {chance*100:F0}% | 굴림 {roll*100:F0}% → 성공");
                    return;
                }
                else
                {
                    Debug.Log($"[미발견] {gameObject.name} | 확률 {chance*100:F0}% | 굴림 {roll*100:F0}% → 실패");
                }
            }
            else if (!inRange && wasInRange)
            {
                // 이탈 → 다음 진입 시 재체크 허용
                survivorsInRange.Remove(survivor);
            }
        }
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
