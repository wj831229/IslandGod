using UnityEngine;

public class FoodItem : MonoBehaviour
{
    public FoodPoint foodPoint;
    public float hungerRestore = 40f;

    void Start()
    {
        gameObject.tag = "Food";
    }
}