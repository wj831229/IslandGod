using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    public GameObject foodPrefab;
    public float spawnInterval = 8f;
    public float foodLifetime = 15f;

    private float spawnTimer = 0f;

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnFood();
            spawnTimer = 0f;
        }
    }

    void SpawnFood()
    {
        float x = Random.Range(-3f, 3f);
        float y = Random.Range(-2f, 2f);
        Vector3 pos = new Vector3(x, y, 0);

        GameObject food = Instantiate(foodPrefab, pos, Quaternion.identity);
        Destroy(food, foodLifetime);
    }
}