using UnityEngine;

public class FoodPoint : MonoBehaviour
{
    public GameObject foodPrefab;
    public int maxAmount = 5;
    public int currentAmount = 5;

    private GameObject spawnedFood;

    void Start()
    {
        SpawnFood();
    }

    public void SpawnFood()
    {
        if (currentAmount <= 0) return;
        if (spawnedFood != null) return;
        if (foodPrefab == null)
        {
            Debug.LogError($"[FoodPoint] foodPrefab is null on {gameObject.name}. Assign it in Inspector.", this);
            return;
        }

        spawnedFood = Instantiate(foodPrefab, transform.position, Quaternion.identity);
        FoodItem item = spawnedFood.GetComponent<FoodItem>();
        if (item == null)
            item = spawnedFood.AddComponent<FoodItem>();
        item.foodPoint = this;
        item.hungerRestore = 40f;
    }

    public void OnFoodTaken()
    {
        currentAmount--;
        spawnedFood = null;

        if (currentAmount > 0)
            Invoke("SpawnFood", 2f);
    }

    public void Restock()
    {
        if (currentAmount < maxAmount)
        {
            currentAmount++;
            if (spawnedFood == null)
                SpawnFood();
        }
    }
}