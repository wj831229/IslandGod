using UnityEngine;

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

    void Start()
    {
        detectionRange = GetComponent<DetectionRange>();
        if (detectionRange == null)
            detectionRange = gameObject.AddComponent<DetectionRange>();

        SetNewTarget();
    }

    void Update()
    {
        hunger -= hungerDecreaseRate * Time.deltaTime;
        hunger = Mathf.Clamp(hunger, 0, 100);

        if (hunger <= 0)
        {
            Debug.Log(gameObject.name + " 사망!");
            Destroy(gameObject);
            return;
        }

        // 타겟 음식이 파괴됐으면 null로
        if (targetFood != null && !targetFood.activeInHierarchy)
            targetFood = null;

        DetectFood();

        if (targetFood != null)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                targetFood.transform.position,
                moveSpeed * Time.deltaTime
            );

            float dist = Vector2.Distance(
                transform.position,
                targetFood.transform.position
            );

            if (dist < 0.3f)
                EatFood();
        }
        else
        {
            wanderTimer += Time.deltaTime;
            if (wanderTimer >= wanderInterval)
            {
                SetNewTarget();
                wanderTimer = 0f;
            }

            transform.position = Vector2.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
        }
    }

    void DetectFood()
    {
        if (targetFood != null) return;

        float range = detectionRange.GetRadius();
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position, range
        );

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

    void EatFood()
    {
        if (targetFood == null) return;

        FoodItem item = targetFood.GetComponent<FoodItem>();
        if (item != null && item.foodPoint != null)
        {
            hunger = Mathf.Min(hunger + item.hungerRestore, 100f);
            Debug.Log(gameObject.name + " 음식 획득! 배고픔: " + hunger);
            item.foodPoint.OnFoodTaken();
        }

        if (targetFood != null)
            Destroy(targetFood);

        targetFood = null;
    }

    void OnMouseDown()
    {
        bool current = detectionRange.gameObject.GetComponent<LineRenderer>().enabled;
        detectionRange.SetSelected(!current);
    }

    void SetNewTarget()
    {
        float x = Random.Range(-3f, 3f);
        float y = Random.Range(-2f, 2f);
        targetPosition = new Vector2(x, y);
    }
}