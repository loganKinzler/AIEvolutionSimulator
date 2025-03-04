using System;//
using Unity.VisualScripting;
using UnityEngine;//

public class FoodBehavior : MonoBehaviour
{
    // EATING VARS
    [Header("Food")]
    [SerializeField] private float calories = 15f;
    [SerializeField] private float eatingDelay = 3f;
    private float eatingStartTime = 0f;

    private bool beingEaten = false;
    private bool finishedEating = false;


    void Update()
    {
        if (beingEaten && Time.time < eatingStartTime + eatingDelay) return;
        if (beingEaten) finishedEating = true;
    }


    // EATING METHODS
    public bool IsBeingEaten() {return beingEaten;}
    public bool IsFinishedEating() {return finishedEating;}
    
    public void StartEating() {
        eatingStartTime = Time.time;
        beingEaten = true;
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
