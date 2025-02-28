using System;
using UnityEngine;

public class FoodBehavior : MonoBehaviour
{
    // EATING VARS
    public float calories = 15f;
    public float eatingDelay = 3f;
    // public Boolean isBeingEaten = false;


    // EATING METHODS
    // public Boolean StartEating(ActorBehavior actor) {
    //     if (isBeingEaten) return false;
        
    //     isBeingEaten = true;
    //     return true;
    // }

    public void FinishedEating(ActorBehavior actor) {
        actor.AddEnergy(calories);
        GameObject.Destroy(gameObject);
    }
}
