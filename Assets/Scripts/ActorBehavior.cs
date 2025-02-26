using System;//
using System.Collections;//
using System.Collections.Generic;//
using UnityEngine;//

public class ActorBehavior : MonoBehaviour
{
    private System.Random sysRand = new System.Random();

    private GameObject terrainObject;
    public GameObject actorBody;
    public Material deathMaterial;


    private enum Action {Wander, Death}
    private Dictionary<Action, float> actionDelays = new Dictionary<Action, float>{
        [Action.Wander] = 0.5f, [Action.Death] = 0f};
    

    // ACTION VARS
    private bool canStartNewAction = true;
    private bool finishedPerformingAction = false;
    private float finishedTime = 0f;
    private Action currentAction;


    // ENERGY VARS
    private float maxEnergy;
    private float energy;// the actor can move X units 
    private float gravity = 10f;// the actor gets slowed by hills


    // WANDER VARS
    private Vector2 targetPosition;
    private Vector2 currentPosition;
    

    private float boundMargin = 0.0f;
    private (float, float) wanderRadius = (1f,2f);
    private float moveSpeed = 0.8f;


    // DELEGATES
    private delegate float GetHeightFromPlanePos(Vector2 planePos);
    private GetHeightFromPlanePos GetTerrainHeight;

    private delegate Vector3 GetNormalFromPlanePos(Vector2 planePos);
    private GetNormalFromPlanePos GetTerrainNormal;

    private delegate Quaternion GetRotationFromPlanePos(Vector2 planePos, Vector2 forwardVect);
    private GetRotationFromPlanePos GetTerrainRotation;


    void Start()
    {
        PerlinFloor terrainScript = FindObjectOfType<PerlinFloor>();
        terrainObject = terrainScript.gameObject;
        currentAction = DecideOnNewAction();

        // delegates
        GetTerrainHeight = new GetHeightFromPlanePos( terrainScript.GetHeightFromPlanePos );
        GetTerrainNormal = new GetNormalFromPlanePos( terrainScript.GetNormalAt );
        GetTerrainRotation = new GetRotationFromPlanePos( terrainScript.GetRotationAt );

        // wander setup
        currentPosition = GetCurrentPosition(); 
        targetPosition = currentPosition + GetNewTargetPos();
    
        // energy setup
        maxEnergy = Mathf.Lerp(12f, 15f, (float)sysRand.NextDouble());
        energy = Mathf.Lerp(0.8f*maxEnergy, maxEnergy, (float)sysRand.NextDouble());
    }

    void Update()
    {
        if (finishedPerformingAction && Time.time < finishedTime + actionDelays[currentAction]) return;

        if (canStartNewAction) StartNewAction();
        finishedPerformingAction = PerformCurrentAction();

        if (finishedPerformingAction) {
            finishedTime = Time.time;

            if (energy <= 0f) {canStartNewAction = true; return;} // actions that happen immediatly
            StartCoroutine( NextActionDelay(currentAction) );
        }
    }

    // EVENUAL DELEGATES
    private Action DecideOnNewAction() {
        if (energy <= 0f) return Action.Death;
        return Action.Wander;
    }

    private void StartNewAction() {
        canStartNewAction = false;
        currentAction = DecideOnNewAction();

        switch (currentAction) {
            case Action.Wander:
                targetPosition = GetNewTargetPos();
            break;

            case Action.Death:
                actorBody.GetComponent<MeshRenderer>().material = deathMaterial;
            break;
        }
    }

    private bool PerformCurrentAction() {
        switch (currentAction) {
            case Action.Wander:
                Vector2 targetDelta = targetPosition - currentPosition;

                if (targetDelta.magnitude < 0.01f || energy <= 0f) return true;
                TransformCurrentPosition(targetDelta.normalized * moveSpeed * Time.deltaTime);
            break;

            case Action.Death:
                // do nothing during death
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

    private void TransformCurrentPosition(Vector2 transformDelta) {
        currentPosition += transformDelta;
        energy -= transformDelta.magnitude;

        Vector2 localPosition = new Vector2(
            Mathf.Clamp01(0.5f + currentPosition.x / terrainObject.transform.localScale.x),
            Mathf.Clamp01(0.5f + currentPosition.y / terrainObject.transform.localScale.z)
        );

        Vector3 newPos = new Vector3( localPosition.x - 0.5f, GetTerrainHeight(localPosition), localPosition.y - 0.5f );
        transform.localPosition = newPos;
    }

    private bool IsInBounds(Vector2 pos) {
        return Mathf.Abs(pos.x + boundMargin) < 0.5f*terrainObject.transform.localScale.x &&
            Mathf.Abs(pos.y + boundMargin) < 0.5f*terrainObject.transform.localScale.z;
    }

    private Vector2 GetNewTargetPos() {
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
