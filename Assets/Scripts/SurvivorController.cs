using UnityEngine;
using TMPro;
using System.Collections.Generic;

public enum SurvivorState
{
    이동중,
    채집중,
    먹는중,
    마시는중,
    수면중,
    건설중,
    전투중,
    대화중,
    탈출중,
    사망
}

public class SurvivorController : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float hunger = 100f;
    public float hungerDecreaseRate = 2f;
    public float hungerEatThreshold = 25f;  // 이 수치 이하면 인벤토리에서 꺼내 먹음

    private Vector2 targetPosition;
    private float wanderTimer = 0f;
    public float wanderInterval = 5f;
    private GameObject targetFood;
    private DetectionRange detectionRange;

    private SurvivorState currentState = SurvivorState.이동중;
    public SurvivorState CurrentState => currentState;
    private TextMeshPro statusText;

    // 채집
    private float gatherTimer = 0f;
    private const float gatherDuration = 3f;
    private bool isGathering = false;

    // 식사
    private float eatTimer = 0f;
    private const float eatDuration = 2f;
    private bool isEating = false;
    private float pendingHungerRestore = 0f;

    // 인벤토리
    public List<string> inventory = new List<string>();

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
        GameObject textObj = new GameObject("StatusText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = new Vector3(0, 0.4f, 0);

        statusText = textObj.AddComponent<TextMeshPro>();
        statusText.fontSize = 1.5f;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.sortingOrder = 10;
        UpdateStatusText();
    }

    void SetState(SurvivorState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        UpdateStatusText();
    }

    void UpdateStatusText()
    {
        if (statusText == null) return;
        statusText.text = stateLabels[currentState];

        statusText.color = currentState switch
        {
            SurvivorState.채집중  => Color.yellow,
            SurvivorState.먹는중  => Color.green,
            SurvivorState.전투중  => Color.red,
            SurvivorState.수면중  => new Color(0.5f, 0.7f, 1f),
            SurvivorState.사망    => Color.gray,
            _ => Color.white
        };
    }

    void Update()
    {
        hunger -= hungerDecreaseRate * Time.deltaTime;
        hunger = Mathf.Clamp(hunger, 0, 100);

        if (hunger <= 0)
        {
            SetState(SurvivorState.사망);
            Destroy(gameObject);
            return;
        }

        // 식사 중
        if (isEating)
        {
            eatTimer += Time.deltaTime;
            if (eatTimer >= eatDuration)
            {
                isEating = false;
                eatTimer = 0f;
                hunger = Mathf.Min(hunger + pendingHungerRestore, 100f);
                pendingHungerRestore = 0f;
                Debug.Log($"{gameObject.name} 식사 완료! 배고픔: {hunger:F0}");
                SetState(SurvivorState.이동중);
            }
            return;
        }

        // 배고프고 인벤토리에 음식 있으면 꺼내 먹기
        if (hunger <= hungerEatThreshold && inventory.Count > 0)
        {
            StartEating();
            return;
        }

        // 채집 중
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
        {
            targetFood = null;
            isGathering = false;
            gatherTimer = 0f;
        }

        DetectFood();

        if (targetFood != null)
        {
            SetState(SurvivorState.이동중);
            transform.position = Vector2.MoveTowards(
                transform.position,
                targetFood.transform.position,
                moveSpeed * Time.deltaTime
            );

            float dist = Vector2.Distance(transform.position, targetFood.transform.position);
            if (dist < 0.3f)
                StartGather();
        }
        else
        {
            wanderTimer += Time.deltaTime;
            if (wanderTimer >= wanderInterval)
            {
                SetNewTarget();
                wanderTimer = 0f;
            }

            SetState(SurvivorState.이동중);
            transform.position = Vector2.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
        }
    }

    void StartGather()
    {
        isGathering = true;
        gatherTimer = 0f;
        SetState(SurvivorState.채집중);
        Debug.Log($"[채집 시작] targetFood={targetFood?.name}");
    }

    void CompleteGather()
    {
        Debug.Log($"[채집 완료 시도] targetFood={(targetFood == null ? "NULL" : targetFood.name)}");
        if (targetFood == null) return;

        FoodItem item = targetFood.GetComponent<FoodItem>();
        Debug.Log($"[FoodItem] item={(item == null ? "NULL" : "있음")}, foodPoint={(item?.foodPoint == null ? "NULL" : "있음")}");

        if (item != null)
        {
            inventory.Add("코코넛");
            Debug.Log($"[인벤토리 추가] 현재: [{string.Join(", ", inventory)}]");

            if (item.foodPoint != null)
                item.foodPoint.OnFoodTaken();
        }

        Destroy(targetFood);
        targetFood = null;
        SetState(SurvivorState.이동중);
    }

    void StartEating()
    {
        string food = inventory[0];
        inventory.RemoveAt(0);

        pendingHungerRestore = 40f;
        isEating = true;
        eatTimer = 0f;
        SetState(SurvivorState.먹는중);
        Debug.Log($"[먹는중 시작] {food} 섭취, 배고픔: {hunger:F0}");
    }

    void DetectFood()
    {
        if (targetFood != null) return;

        float range = detectionRange.GetRadius();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            if (hit.gameObject == null) continue;
            if (!hit.gameObject.activeInHierarchy) continue;

            if (hit.CompareTag("Food"))
            {
                targetFood = hit.gameObject;
                break;
            }
        }
    }

    void OnMouseDown()
    {
        bool current = detectionRange.gameObject.GetComponent<LineRenderer>().enabled;
        detectionRange.SetSelected(!current);
        SurvivorInfoPanel.Instance?.Show(this);
    }

    void SetNewTarget()
    {
        float x = Random.Range(-3f, 3f);
        float y = Random.Range(-2f, 2f);
        targetPosition = new Vector2(x, y);
    }
}
