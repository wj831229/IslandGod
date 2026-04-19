using UnityEngine;

public class FoodItem : MonoBehaviour
{
    public FoodPoint foodPoint;
    public float hungerRestore = 40f;
    public int gatherAmount = 5;
    public string itemName = "코코넛";

    void Start()
    {
        gameObject.tag = "Food";
    }
}
