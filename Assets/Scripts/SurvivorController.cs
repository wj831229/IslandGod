using UnityEngine;
using TMPro;
using System.Collections.Generic;

public enum SurvivorState
{
    이동중, 채집중, 먹는중, 마시는중,
    수면중, 건설중, 전투중, 대화중, 탈출중, 사망
}

public class SurvivorController : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float hunger = 100f;
    public float hungerDecreaseRate = 2f;
    public float hungerEatThreshold = 25f;

    private Vector2 targetPosition;
    private float wanderTimer;
    public float wanderInterval = 5f;
    private FoodPoint targetFoodPoint;
    private DetectionRange detectionRange;

    private SurvivorState currentState = SurvivorState.이동중;
    public SurvivorState CurrentState => currentState;
    private TextMeshPro statusText;

    private float gatherTimer;
    private const float gatherDuration = 3f;
    private bool isGathering;

    private float eatTimer;
    private const float eatDuration = 2f;
    private bool isEating;
    private float pendingHungerRestore;

    public List<InventorySlot> inventory = new();

    [Header("수면")]
    private bool isSleeping = false;
    private bool wasDay = true;
    public float sleepHungerMultiplier = 0.4f; // 수면 중 배고픔 감소 배율

    [Header("특성")]
    public float discoveryBonus = 0f;  // 발견 확률 보너스 (0.2 = +20%)

    // 이 표류자가 발견한 FoodPoint 목록 (개인 기억)
    public HashSet<FoodPoint> discoveredFoods = new();

    // FoodPoint별 "이미 범위 안에 있었는가" 추적 (진입 순간 판단용)
    private HashSet<FoodPoint> foodsInRange = new();

    public float GetDetectionRadius() => detectionRange != null ? detectionRange.GetRadius() : 0f;

    public static readonly Dictionary<SurvivorState, string> stateLabels = new()
    {
        { SurvivorState.이동중,  "이동중"  },
        { SurvivorState.채집중,  "채집중"  },
        { SurvivorState.먹는중,  "먹는중"  },
        { SurvivorState.마시는중, "마시는중" },
        { SurvivorState.수면중,  "수면중"  },
        { SurvivorState.건설중,  "건설중"  },
        { SurvivorState.전투중,  "전투중"  },
        { SurvivorState.대화중,  "대화중"  },
        { SurvivorState.탈출중,  "탈출중"  },
        { SurvivorState.사망,    "사망"    },
    };

    // 씬에 존재하는 모든 표류자 목록
    public static readonly List<SurvivorController> All = new();

    void OnEnable()  => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start()
    {
        detectionRange = GetComponent<DetectionRange>();
        if (detectionRange == null)
            detectionRange = gameObject.AddComponent<DetectionRange>();
        CreateStatusText();
        SetNewTarget();
    }

    void CreateStatusText()
    {
        var textObj = new GameObject("StatusText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = new Vector3(0, 0.4f, 0);
        statusText = textObj.AddComponent<TextMeshPro>();
        statusText.fontSize = 1.5f;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.sortingOrder = 10;
        UpdateStatusText();
    }

    void SetState(SurvivorState s) { if (currentState == s) return; currentState = s; UpdateStatusText(); }

    void UpdateStatusText()
    {
        if (statusText == null) return;
        statusText.text = stateLabels[currentState];
        statusText.color = currentState switch
        {
            SurvivorState.채집중 => Color.yellow,
            SurvivorState.먹는중 => Color.green,
            SurvivorState.전투중 => Color.red,
            SurvivorState.수면중 => new Color(0.5f, 0.7f, 1f),
            SurvivorState.사망   => Color.gray,
            _ => Color.white
        };
    }

    void Update()
    {
        // 낮/밤 전환 감지
        bool isDay = DayNightCycle.Instance == null || DayNightCycle.Instance.IsDay();
        if (wasDay && !isDay)  StartSleep();
        else if (!wasDay && isDay) WakeUp();
        wasDay = isDay;

        // 수면 중 배고픔 감소 속도 줄임
        float currentHungerRate = isSleeping ? hungerDecreaseRate * sleepHungerMultiplier : hungerDecreaseRate;
        hunger -= currentHungerRate * Time.deltaTime;
        hunger = Mathf.Clamp(hunger, 0, 100);

        if (hunger <= 0) { SetState(SurvivorState.사망); Destroy(gameObject); return; }

        // 수면 중에는 아무것도 안 함
        if (isSleeping) return;

        if (isEating)
        {
            eatTimer += Time.deltaTime;
            if (eatTimer >= eatDuration)
            {
                isEating = false;
                hunger = Mathf.Min(hunger + pendingHungerRestore, 100f);
                Debug.Log($"[식사 완료] 배고픔: {hunger:F0}");
                SetState(SurvivorState.이동중);
            }
            return;
        }

        if (hunger <= hungerEatThreshold && HasFood())
        {
            StartEating();
            return;
        }

        if (isGathering)
        {
            gatherTimer += Time.deltaTime;
            if (gatherTimer >= gatherDuration)
            {
                isGathering = false;
                gatherTimer = 0f;
                CompleteGather();
            }
            return;
        }

        if (targetFoodPoint != null && !targetFoodPoint.gameObject.activeInHierarchy)
            targetFoodPoint = null;

        // 발견 확률 체크 (진입 순간)
        ScanForFood();

        // 알고 있는 음식 중 수량 있는 것으로 이동
        if (targetFoodPoint == null)
            FindTargetFromMemory();

        if (targetFoodPoint != null)
        {
            SetState(SurvivorState.이동중);
            Vector2 foodPos = targetFoodPoint.transform.position;
            transform.position = Vector2.MoveTowards(transform.position, foodPos, moveSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, foodPos) < 0.3f)
                StartGather();
        }
        else
        {
            wanderTimer += Time.deltaTime;
            if (wanderTimer >= wanderInterval) { SetNewTarget(); wanderTimer = 0f; }
            SetState(SurvivorState.이동중);
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
    }

    // 인식범위 진입 순간에만 발견 확률 체크
    void ScanForFood()
    {
        float range = GetDetectionRadius();

        // 파괴된 FoodPoint 정리
        foodsInRange.RemoveWhere(fp => fp == null);
        discoveredFoods.RemoveWhere(fp => fp == null);

        foreach (var fp in FindObjectsByType<FoodPoint>())
        {
            if (fp == null) continue;
            float dist = Vector2.Distance(transform.position, fp.transform.position);
            bool inRange = dist <= range;
            bool wasInRange = foodsInRange.Contains(fp);

            if (inRange && !wasInRange)
            {
                // 진입 순간 → 발견 확률 체크
                foodsInRange.Add(fp);

                if (!discoveredFoods.Contains(fp))
                {
                    float chance = Mathf.Clamp01(fp.discoveryChance + discoveryBonus);
                    float roll = Random.value;
                    if (roll <= chance)
                    {
                        discoveredFoods.Add(fp);
                        Debug.Log($"[{gameObject.name}] {fp.gameObject.name} 발견! ({chance*100:F0}%)");
                    }
                }
            }
            else if (!inRange && wasInRange)
            {
                // 이탈 → 다음 진입 시 재체크 허용
                foodsInRange.Remove(fp);
            }
        }
    }

    // 자신이 아는 FoodPoint 중 수량 있는 가장 가까운 곳을 목표로
    void FindTargetFromMemory()
    {
        FoodPoint closest = null;
        float minDist = float.MaxValue;

        foreach (var fp in discoveredFoods)
        {
            if (fp == null || fp.currentAmount <= 0) continue;
            float dist = Vector2.Distance(transform.position, fp.transform.position);
            if (dist < minDist) { minDist = dist; closest = fp; }
        }

        targetFoodPoint = closest;
    }

    void StartGather()
    {
        isGathering = true;
        gatherTimer = 0f;
        SetState(SurvivorState.채집중);
    }

    void CompleteGather()
    {
        if (targetFoodPoint == null) return;

        // 채집 완료 시점에 수량 재확인 (다른 표류자가 먼저 가져갔을 수 있음)
        if (targetFoodPoint.currentAmount <= 0)
        {
            Debug.Log($"[채집 실패] {gameObject.name} → {targetFoodPoint.gameObject.name} 수량 없음");
            targetFoodPoint = null;
            SetState(SurvivorState.이동중);
            return;
        }

        FoodItem item = targetFoodPoint.GetComponent<FoodItem>();
        if (item == null) item = targetFoodPoint.gameObject.GetComponentInChildren<FoodItem>();

        string name = item != null ? item.itemName : "코코넛";
        int maxStack = name == "코코넛" ? 3 : 20;

        AddToInventory(name, maxStack);
        targetFoodPoint.OnFoodTaken();

        Debug.Log($"[채집 완료] {gameObject.name} {name} 획득 → {GetInventorySummary()}");
        targetFoodPoint = null;
        SetState(SurvivorState.이동중);
    }

    void AddToInventory(string itemName, int maxStack = 20)
    {
        var slot = inventory.Find(s => s.itemName == itemName && !s.IsFull);
        if (slot != null)
            slot.count++;
        else
            inventory.Add(new InventorySlot(itemName, maxStack));
    }

    bool HasFood()
    {
        return inventory.Exists(s => s.itemName == "코코넛" || s.itemName == "베리" || s.itemName == "생선");
    }

    void StartEating()
    {
        var slot = inventory.Find(s => s.count > 0);
        if (slot == null) return;

        pendingHungerRestore = 40f;
        slot.count--;
        if (slot.count <= 0) inventory.Remove(slot);

        isEating = true;
        eatTimer = 0f;
        SetState(SurvivorState.먹는중);
        Debug.Log($"[먹는중] {slot.itemName} 섭취 시작, 배고픔: {hunger:F0}");
    }

    public void SetRingVisible(bool visible)
    {
        detectionRange?.SetSelected(visible);
    }

    void OnMouseDown()
    {
        ViewManager.Instance?.Select(this);
    }

    void SetNewTarget()
    {
        targetPosition = new Vector2(Random.Range(-3f, 3f), Random.Range(-2f, 2f));
    }

    string GetInventorySummary()
    {
        var result = new System.Text.StringBuilder();
        foreach (var s in inventory) result.Append($"{s.itemName}x{s.count} ");
        return result.ToString().TrimEnd();
    }
}
