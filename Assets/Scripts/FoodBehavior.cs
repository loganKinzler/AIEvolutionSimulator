using System;//
using UnityEngine;//

public class FoodBehavior : MonoBehaviour
{
    // EATING VARS
    public float calories = 15f;

    private float eatingStartTime = 0f;
    public float eatingDelay = 3f;

    public Boolean isBeingEaten = false;
    public Boolean finishedEating = false;


    void Update()
    {
        if (isBeingEaten && Time.time < eatingStartTime + eatingDelay)
            finishedEating = true;
    }


    // EATING METHODS
    public void StartEating() {
        eatingStartTime = Time.time;
        isBeingEaten = true;
    }

    public void FinishEating(ActorBehavior actor) {
        actor.AddEnergy(calories);
        GameObject.Destroy(gameObject);
    }

    public Vector2 GetCurrentPosition() {
        Vector3 parentDelta = transform.position - gameObject.transform.parent.position;
        return new Vector2(parentDelta.x, parentDelta.z);
    }
}
