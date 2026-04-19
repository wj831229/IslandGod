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
    private GameObject targetFood;
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

    public float GetDetectionRadius() => detectionRange != null ? detectionRange.GetRadius() : 0f;

    [Header("특성")]
    public float discoveryBonus = 0f;  // 발견 확률 보너스 (0.2 = +20%)

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

    // 씬에 존재하는 모든 표류자 목록 (FoodPoint가 참조)
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
        hunger -= hungerDecreaseRate * Time.deltaTime;
        hunger = Mathf.Clamp(hunger, 0, 100);

        if (hunger <= 0) { SetState(SurvivorState.사망); Destroy(gameObject); return; }

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

        if (targetFood != null && !targetFood.activeInHierarchy)
            targetFood = null;

        DetectFood();

        if (targetFood != null)
        {
            SetState(SurvivorState.이동중);
            transform.position = Vector2.MoveTowards(transform.position, targetFood.transform.position, moveSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, targetFood.transform.position) < 0.3f)
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

    void StartGather()
    {
        isGathering = true;
        gatherTimer = 0f;
        SetState(SurvivorState.채집중);
    }

    void CompleteGather()
    {
        if (targetFood == null) return;

        FoodItem item = targetFood.GetComponent<FoodItem>();
        string name = item != null ? item.itemName : "코코넛";
        int maxStack = name == "코코넛" ? 3 : 20;

        AddToInventory(name, maxStack);

        if (item != null && item.foodPoint != null)
            item.foodPoint.OnFoodTaken();

        Debug.Log($"[채집 완료] {name} 추가 → {GetInventorySummary()}");
        // 아이콘은 유지 (FoodPoint가 관리), targetFood만 해제
        targetFood = null;
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

    void DetectFood()
    {
        float range = detectionRange.GetRadius();

        foreach (var food in FindObjectsByType<FoodItem>())
        {
            if (food == null || food.gameObject == null) continue;
            float dist = Vector2.Distance(transform.position, food.transform.position);
            if (dist > range) continue;

            // 수량 있고 아직 타겟 없으면 채집 대상으로
            if (targetFood == null && (food.foodPoint == null || food.foodPoint.currentAmount > 0))
                targetFood = food.gameObject;
        }
    }

    void OnMouseDown()
    {
        bool current = detectionRange.GetComponent<LineRenderer>().enabled;
        detectionRange.SetSelected(!current);
        SurvivorInfoPanel.Instance?.Show(this);
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
