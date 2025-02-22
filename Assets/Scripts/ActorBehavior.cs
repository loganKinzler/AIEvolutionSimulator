using System;//
using System.Collections;//
using System.Collections.Generic;//
using UnityEngine;//

public class ActorBehavior : MonoBehaviour
{
    private Vector3 worldBounds;

    private enum Action {Wander}
    private Dictionary<Action, float> actionDelays = new Dictionary<Action, float>{[Action.Wander] = 1.0f};
    
    private bool canStartNewAction = true;
    private bool finishedPerformingAction = false;
    private Action currentAction;

    // WANDER VARS
    private Vector2 targetPosition;
    private Vector2 currentPosition;
    
    private (float, float) wanderRadius = (0.25f,0.275f);
    private float moveSpeed = 0.05f;

    private delegate float GetHeightFromPlanePos(Vector2 planePos);
    private GetHeightFromPlanePos GetTerrainHeight;


    void Start()
    {
        currentAction = DecideOnNewAction();

        worldBounds = FindObjectOfType<PerlinFloor>().gameObject.transform.localScale;
        GetTerrainHeight = new GetHeightFromPlanePos( FindObjectOfType<PerlinFloor>().GetHeightFromPlanePos );

        currentPosition = GetCurrentPosition(); 
        targetPosition = currentPosition + GetNewTargetPos();
    }

    void Update()
    {
        if (canStartNewAction) StartNewAction();
        else finishedPerformingAction = PerformCurrentAction();

        if (finishedPerformingAction) {
            finishedPerformingAction = false;
            StartCoroutine( NextActionDelay(currentAction) );
        }
    }

    // EVENUAL DELEGATES
    private Action DecideOnNewAction() {
        return Action.Wander;
    }

    private void StartNewAction() {
        print("Starting New Action.");
        print("Performing Current Action...");

        currentAction = DecideOnNewAction();
        canStartNewAction = false;

        switch (currentAction) {
            case Action.Wander:
                targetPosition = GetNewTargetPos();
            break;
        }
    }

    private bool PerformCurrentAction() {
        switch (currentAction) {
            case Action.Wander:
                Vector2 targetDelta = currentPosition - targetPosition;

                if (targetDelta.magnitude < 0.001f) return true;
                TransformCurrentPosition(-targetDelta.normalized * moveSpeed * Time.deltaTime);
            break;
        }

        return false;
    }

    private IEnumerator NextActionDelay(Action currentAction) {
        print("Finished Performing Action.");

        yield return new WaitForSeconds(actionDelays[currentAction]);
        canStartNewAction = true;
    }


    // WANDER ACTION METHODS
    private Vector2 GetCurrentPosition() {
        return new Vector2(transform.localPosition.x, transform.localPosition.z);
    }

    private void TransformCurrentPosition(Vector2 transformPosition) {
        currentPosition += transformPosition;
        
        transform.localPosition = new Vector3(
            currentPosition.x,
            worldBounds.y * GetTerrainHeight(0.5f*Vector2.one +  new Vector2(currentPosition.x/worldBounds.x, currentPosition.y/worldBounds.y)),
            currentPosition.y
        );
    }

    private bool IsInBounds(Vector2 pos) {
        return Mathf.Abs(GetCurrentPosition().x) < worldBounds.x/2.0f && Mathf.Abs(GetCurrentPosition().y) < worldBounds.z/2.0f;
    }

    private Vector2 GetNewTargetPos() {
        System.Random sysRand = new System.Random();
        float theta = (float)sysRand.NextDouble() * 2*(float)Math.PI;
        float radius = (float) sysRand.NextDouble();
        
        Vector2 newPos = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) *
            Mathf.Lerp(wanderRadius.Item1, wanderRadius.Item2, radius);
        
        // rotate by 90 degs unitl it's in bounds
        for (int i=0; i<4; i++) {
            if (IsInBounds(newPos)) break;
            newPos = new Vector2(-newPos.y, newPos.x);
        } 

        if (!IsInBounds(newPos)) {
            newPos = Vector2.zero;
            print(String.Format("{0} is a fucking idiot", gameObject.name));
        }
         

        return newPos + GetCurrentPosition();
    }
}
