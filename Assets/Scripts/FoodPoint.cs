using UnityEngine;
using System.Collections.Generic;

public class FoodPoint : MonoBehaviour
{
    public GameObject foodPrefab;
    public int maxAmount = 5;
    public int currentAmount = 5;

    private List<GameObject> spawnedFoods = new();

    void Start()
    {
        SpawnAll();
    }

    void SpawnAll()
    {
        if (foodPrefab == null)
        {
            Debug.LogError($"[FoodPoint] foodPrefab is null on {gameObject.name}", this);
            return;
        }

        for (int i = 0; i < currentAmount; i++)
            SpawnOne(i);
    }

    void SpawnOne(int index)
    {
        if (foodPrefab == null) return;

        // 포인트 주변에 약간씩 퍼뜨려서 배치
        float angle = index * (360f / maxAmount) * Mathf.Deg2Rad;
        float radius = 0.2f;
        Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        Vector3 pos = transform.position + offset;

        GameObject food = Instantiate(foodPrefab, pos, Quaternion.identity);
        FoodItem item = food.GetComponent<FoodItem>();
        if (item == null) item = food.AddComponent<FoodItem>();
        item.foodPoint = this;
        item.hungerRestore = 40f;

        spawnedFoods.Add(food);
    }

    public void OnFoodTaken()
    {
        currentAmount--;

        // 파괴된 오브젝트 목록 정리
        spawnedFoods.RemoveAll(f => f == null);

        Debug.Log($"[FoodPoint] 남은 수량: {currentAmount}/{maxAmount}");
    }

    // DayNightCycle에서 밤 종료 시 호출
    public void Restock()
    {
        if (currentAmount >= maxAmount) return;

        currentAmount++;
        SpawnOne(spawnedFoods.Count);
        Debug.Log($"[FoodPoint] 리스폰 +1 → {currentAmount}/{maxAmount}");
    }
}
