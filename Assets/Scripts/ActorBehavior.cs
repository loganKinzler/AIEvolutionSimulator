using System;//
using System.Collections;//
using System.Collections.Generic;//
using UnityEngine;//

public class ActorBehavior : MonoBehaviour
{
    private GameObject terrainObject;
    private float boundMargin = 0.0f;

    private enum Action {Wander}
    private Dictionary<Action, float> actionDelays = new Dictionary<Action, float>{[Action.Wander] = 0.5f};
    
    private bool canStartNewAction = true;
    private bool finishedPerformingAction = false;
    private Action currentAction;

    // WANDER VARS
    private Vector2 targetPosition;
    private Vector2 currentPosition;
    
    private (float, float) wanderRadius = (1f,2f);
    private float moveSpeed = 0.8f;

    private delegate float GetHeightFromPlanePos(Vector2 planePos);
    private GetHeightFromPlanePos GetTerrainHeight;


    void Start()
    {
        currentAction = DecideOnNewAction();

        terrainObject = FindObjectOfType<PerlinFloor>().gameObject;
        GetTerrainHeight = new GetHeightFromPlanePos( terrainObject.GetComponent<PerlinFloor>().GetHeightFromPlanePos );

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
                Vector2 targetDelta = targetPosition - currentPosition;

                if (targetDelta.magnitude < 0.01f) return true;
                TransformCurrentPosition(targetDelta.normalized * moveSpeed * Time.deltaTime);
            break;
        }

        return false;
    }

    private IEnumerator NextActionDelay(Action currentAction) {
        yield return new WaitForSeconds(actionDelays[currentAction]);
        canStartNewAction = true;
    }


    // WANDER ACTION METHODS
    private Vector2 GetCurrentPosition() {
        Vector3 terrainDelta = transform.position - terrainObject.transform.position;
        return new Vector2(terrainDelta.x, terrainDelta.z);
    }

    private void TransformCurrentPosition(Vector2 transformPosition) {
        currentPosition += transformPosition;

        Vector2 localPosition = new Vector2(
            Mathf.Clamp01(0.5f + currentPosition.x / terrainObject.transform.localScale.x),
            Mathf.Clamp01(0.5f + currentPosition.y / terrainObject.transform.localScale.z)
        );

        transform.localPosition = new Vector3(
            localPosition.x - 0.5f, GetTerrainHeight(localPosition), localPosition.y - 0.5f
        );
    }

    private bool IsInBounds(Vector2 pos) {
        return Mathf.Abs(pos.x + boundMargin) < 0.5f*terrainObject.transform.localScale.x &&
            Mathf.Abs(pos.y + boundMargin) < 0.5f*terrainObject.transform.localScale.z;
    }

    private Vector2 GetNewTargetPos() {
        System.Random sysRand = new System.Random();
        float theta = (float)sysRand.NextDouble() * 2f*(float)Math.PI;
        float radius = (float) sysRand.NextDouble();
        
        Vector2 newPos = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) *
            Mathf.Lerp(wanderRadius.Item1, wanderRadius.Item2, radius);
        
        // rotate by 90 degs unitl it's in bounds
        for (int i=0; i<4; i++) {
            if (IsInBounds(targetPosition + newPos)) return targetPosition + newPos;
            newPos = new Vector2(-newPos.y, newPos.x);
        }

        return Vector2.zero;
    }
}
