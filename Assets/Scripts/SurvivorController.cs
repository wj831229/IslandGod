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

    private Vector2 targetPosition;
    private float wanderTimer = 0f;
    public float wanderInterval = 5f;
    private GameObject targetFood;
    private DetectionRange detectionRange;

    private SurvivorState currentState = SurvivorState.이동중;
    public SurvivorState CurrentState => currentState;
    private TextMeshPro statusText;

    // 채집 관련
    private float gatherTimer = 0f;
    private const float gatherDuration = 3f;
    private bool isGathering = false;

    // 인벤토리
    public List<string> inventory = new List<string>();

    public static readonly Dictionary<SurvivorState, string> stateLabels = new()
    {
        { SurvivorState.이동중,  "Moving"    },
        { SurvivorState.채집중,  "Gathering" },
        { SurvivorState.먹는중,  "Eating"    },
        { SurvivorState.마시는중, "Drinking"  },
        { SurvivorState.수면중,  "Sleeping"  },
        { SurvivorState.건설중,  "Building"  },
        { SurvivorState.전투중,  "Fighting"  },
        { SurvivorState.대화중,  "Talking"   },
        { SurvivorState.탈출중,  "Escaping"  },
        { SurvivorState.사망,    "Dead"      },
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
            Debug.Log(gameObject.name + " 사망!");
            Destroy(gameObject);
            return;
        }

        if (targetFood != null && !targetFood.activeInHierarchy)
        {
            targetFood = null;
            isGathering = false;
            gatherTimer = 0f;
        }

        // 채집 중이면 이동 없이 타이머만 진행
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
    }

    void CompleteGather()
    {
        if (targetFood == null) return;

        FoodItem item = targetFood.GetComponent<FoodItem>();
        if (item != null)
        {
            // 인벤토리에 추가
            inventory.Add("Coconut");
            Debug.Log($"{gameObject.name} 인벤토리: [{string.Join(", ", inventory)}]");

            SetState(SurvivorState.먹는중);

            // 배고픔 회복
            if (item.foodPoint != null)
            {
                hunger = Mathf.Min(hunger + item.hungerRestore, 100f);
                item.foodPoint.OnFoodTaken();
            }
        }

        if (targetFood != null)
            Destroy(targetFood);

        targetFood = null;

        // 잠깐 먹는 상태 유지 후 이동으로 전환
        Invoke(nameof(BackToMoving), 1f);
    }

    void BackToMoving()
    {
        SetState(SurvivorState.이동중);
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
