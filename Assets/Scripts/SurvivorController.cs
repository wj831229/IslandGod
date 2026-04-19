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

    [Header("체력")]
    public float health = 100f;
    public float maxHealth = 100f;
    public float healthTickInterval = 5f;
    private float healthTimer = 0f;

    private Vector2 targetPosition;
    private float wanderTimer;
    public float wanderInterval = 5f;
    private DetectionRange detectionRange;

    private SurvivorState currentState = SurvivorState.이동중;
    public SurvivorState CurrentState => currentState;
    private TextMeshPro statusText;

    private float gatherTimer;
    private const float gatherDuration = 3f;
    private bool isGathering;

    // 현재 채집 대상 (음식 or 자원 둘 중 하나만 세팅)
    private FoodPoint targetFoodPoint;
    private ResourcePoint targetResourcePoint;

    private float eatTimer;
    private const float eatDuration = 2f;
    private bool isEating;
    private float pendingHungerRestore;

    public List<InventorySlot> inventory = new();

    [Header("수면")]
    private bool isSleeping = false;
    private bool wasDay = true;
    public float sleepHungerMultiplier = 0.4f;

    [Header("특성")]
    public float discoveryBonus = 0f;

    // 발견 목록 (음식 / 자원 각각)
    public HashSet<FoodPoint>     discoveredFoods     = new();
    public HashSet<ResourcePoint> discoveredResources = new();

    // 진입 추적 (진입 순간 판단용)
    private HashSet<FoodPoint>     foodsInRange     = new();
    private HashSet<ResourcePoint> resourcesInRange = new();

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

        // 배고픔 감소
        float currentHungerRate = isSleeping ? hungerDecreaseRate * sleepHungerMultiplier : hungerDecreaseRate;
        hunger -= currentHungerRate * Time.deltaTime;
        hunger = Mathf.Clamp(hunger, 0, 100);

        // 체력 틱
        healthTimer += Time.deltaTime;
        if (healthTimer >= healthTickInterval)
        {
            healthTimer = 0f;
            if (hunger <= 0)
            {
                health = Mathf.Max(0, health - 1f);
                Debug.Log($"[{gameObject.name}] 굶주림으로 체력 감소 → {health:F0}");
            }
            else if (health < maxHealth)
                health = Mathf.Min(maxHealth, health + 1f);
        }

        if (health <= 0) { SetState(SurvivorState.사망); Destroy(gameObject); return; }
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

        // 비활성 타겟 초기화
        if (targetFoodPoint != null && !targetFoodPoint.gameObject.activeInHierarchy)
            targetFoodPoint = null;
        if (targetResourcePoint != null && !targetResourcePoint.gameObject.activeInHierarchy)
            targetResourcePoint = null;

        // 아이템 스캔 (음식 + 자원 동시)
        ScanForItems();

        // 우선순위: 배고프면 음식, 배부르면 자원
        if (targetFoodPoint == null && hunger < 80f)
            FindFoodTarget();

        if (targetFoodPoint == null && targetResourcePoint == null)
            FindResourceTarget();

        // 이동 및 채집
        if (targetFoodPoint != null)
        {
            SetState(SurvivorState.이동중);
            Vector2 pos = targetFoodPoint.transform.position;
            transform.position = Vector2.MoveTowards(transform.position, pos, moveSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, pos) < 0.3f)
                StartGather();
        }
        else if (targetResourcePoint != null)
        {
            SetState(SurvivorState.이동중);
            Vector2 pos = targetResourcePoint.transform.position;
            transform.position = Vector2.MoveTowards(transform.position, pos, moveSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, pos) < 0.3f)
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

    // 음식 + 자원 동시 스캔 (진입 순간만 발견 체크)
    void ScanForItems()
    {
        float range = GetDetectionRadius();

        foodsInRange.RemoveWhere(fp => fp == null);
        discoveredFoods.RemoveWhere(fp => fp == null);
        resourcesInRange.RemoveWhere(rp => rp == null);
        discoveredResources.RemoveWhere(rp => rp == null);

        foreach (var fp in FindObjectsByType<FoodPoint>())
        {
            if (fp == null) continue;
            bool inRange   = Vector2.Distance(transform.position, fp.transform.position) <= range;
            bool wasInRange = foodsInRange.Contains(fp);

            if (inRange && !wasInRange)
            {
                foodsInRange.Add(fp);
                if (!discoveredFoods.Contains(fp))
                {
                    float chance = Mathf.Clamp01(fp.discoveryChance + discoveryBonus);
                    if (Random.value <= chance)
                    {
                        discoveredFoods.Add(fp);
                        Debug.Log($"[{gameObject.name}] 음식 발견: {fp.gameObject.name}");
                    }
                }
            }
            else if (!inRange && wasInRange)
                foodsInRange.Remove(fp);
        }

        foreach (var rp in FindObjectsByType<ResourcePoint>())
        {
            if (rp == null) continue;
            bool inRange    = Vector2.Distance(transform.position, rp.transform.position) <= range;
            bool wasInRange = resourcesInRange.Contains(rp);

            if (inRange && !wasInRange)
            {
                resourcesInRange.Add(rp);
                if (!discoveredResources.Contains(rp))
                {
                    float chance = Mathf.Clamp01(rp.discoveryChance + discoveryBonus);
                    if (Random.value <= chance)
                    {
                        discoveredResources.Add(rp);
                        Debug.Log($"[{gameObject.name}] 자원 발견: {rp.itemName}");
                    }
                }
            }
            else if (!inRange && wasInRange)
                resourcesInRange.Remove(rp);
        }
    }

    void FindFoodTarget()
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

    void FindResourceTarget()
    {
        ResourcePoint closest = null;
        float minDist = float.MaxValue;
        foreach (var rp in discoveredResources)
        {
            if (rp == null || rp.currentAmount <= 0) continue;
            float dist = Vector2.Distance(transform.position, rp.transform.position);
            if (dist < minDist) { minDist = dist; closest = rp; }
        }
        targetResourcePoint = closest;
    }

    void StartSleep()
    {
        isSleeping = true;
        isGathering = false;
        isEating = false;
        targetFoodPoint = null;
        targetResourcePoint = null;
        SetState(SurvivorState.수면중);
    }

    void WakeUp()
    {
        isSleeping = false;
        SetNewTarget();
        SetState(SurvivorState.이동중);
    }

    void StartGather()
    {
        isGathering = true;
        gatherTimer = 0f;
        SetState(SurvivorState.채집중);
    }

    void CompleteGather()
    {
        // 음식 채집
        if (targetFoodPoint != null)
        {
            if (targetFoodPoint.currentAmount <= 0)
            {
                Debug.Log($"[채집 실패] {gameObject.name} → 음식 수량 없음");
                targetFoodPoint = null;
                SetState(SurvivorState.이동중);
                return;
            }
            FoodItem item = targetFoodPoint.GetComponentInChildren<FoodItem>();
            string fname = item != null ? item.itemName : "코코넛";
            int fstack = fname == "코코넛" ? 3 : 20;
            AddToInventory(fname, fstack);
            targetFoodPoint.OnFoodTaken();
            Debug.Log($"[채집] {gameObject.name} {fname} 획득");
            targetFoodPoint = null;
            SetState(SurvivorState.이동중);
            return;
        }

        // 자원 채집
        if (targetResourcePoint != null)
        {
            if (targetResourcePoint.currentAmount <= 0)
            {
                Debug.Log($"[채집 실패] {gameObject.name} → 자원 수량 없음");
                targetResourcePoint = null;
                SetState(SurvivorState.이동중);
                return;
            }
            AddToInventory(targetResourcePoint.itemName, 10);
            targetResourcePoint.OnResourceTaken();
            Debug.Log($"[채집] {gameObject.name} {targetResourcePoint.itemName} 획득");
            targetResourcePoint = null;
            SetState(SurvivorState.이동중);
        }
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
        var slot = inventory.Find(s => s.itemName == "코코넛" || s.itemName == "베리" || s.itemName == "생선");
        if (slot == null) return;

        pendingHungerRestore = 40f;
        slot.count--;
        if (slot.count <= 0) inventory.Remove(slot);

        isEating = true;
        eatTimer = 0f;
        SetState(SurvivorState.먹는중);
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
