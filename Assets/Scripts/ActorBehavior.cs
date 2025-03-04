using System;//
using System.Collections;//
using System.Collections.Generic;//
using UnityEngine;//

public class ActorBehavior : MonoBehaviour
{
    private System.Random sysRand = new System.Random();

    private GameObject terrainObject;
    public GameObject actorBody;


    [Header("Materials")]
    [SerializeField] private Material idleMaterial;
    [SerializeField] private Material wanderMaterial;
    [SerializeField] private Material deathMaterial;
    [SerializeField] private Material forageMaterial;
    [SerializeField] private Material eatingMaterial;


    private enum Action {Wander, Death, Forage, Eat}
    private Dictionary<Action, float> actionDelays = new Dictionary<Action, float>{
        [Action.Wander] = 0.5f, [Action.Death] = 0f, [Action.Forage] = 0f, [Action.Eat] = 1f};
    

    // ACTION VARS
    [Header("Action")]
    [SerializeField] private bool canStartNewAction = true;
    [SerializeField] private bool finishedPerformingAction = false;
    [SerializeField] private Action currentAction;
    private float finishedTime = 0f;

    // FOOD VARS
    private FoodBehavior currentFood;

    // ENERGY VARS

    [Header("Energy")]
    [SerializeField] private float maxEnergy;
    [SerializeField] private float energy;// the actor can move X units 
    // private float gravity = 10f;// the actor gets slowed by hills


    // WANDER VARS
    private Vector2 targetPosition;
    private Vector2 currentPosition;
    

    private float boundMargin = 0.1f;
    private (float, float) wanderRadius = (1f,2f);
    private float moveSpeed = 0.8f;


    // DELEGATES
    private delegate float GetHeightFromPlanePos(Vector2 planePos);
    private GetHeightFromPlanePos GetTerrainHeight;

    private delegate Vector3 GetNormalFromPlanePos(Vector2 planePos);
    private GetNormalFromPlanePos GetTerrainNormal;

    private delegate Quaternion GetRotationFromPlanePos(Vector2 planePos, Vector2 forwardVect);
    private GetRotationFromPlanePos GetTerrainRotation;

    private delegate void PlaceInHashFolder(GameObject hashObject);
    private PlaceInHashFolder HashObjectByPosition;

    private delegate FoodBehavior GetClosestFood(ActorBehavior actor);
    private GetClosestFood FindNearbyFood;


    void Start()
    {
        
        PerlinFloor terrainScript = FindObjectOfType<PerlinFloor>();
        SceneSpawner spawningScript = FindAnyObjectByType<SceneSpawner>();
        terrainObject = terrainScript.gameObject;

        // delegates
        GetTerrainHeight = new GetHeightFromPlanePos( terrainScript.GetHeightFromPlanePos );
        GetTerrainNormal = new GetNormalFromPlanePos( terrainScript.GetNormalAt );
        // GetTerrainRotation = new GetRotationFromPlanePos( terrainScript.GetRotationAt );

        HashObjectByPosition = new PlaceInHashFolder( spawningScript.PlaceInHashFolder );
        FindNearbyFood = new GetClosestFood( spawningScript.GetClosestFood );


        // action setup
        currentAction = DecideOnNewAction();

        // wander setup
        currentPosition = GetCurrentPosition(); 
        targetPosition = currentPosition + GetNewTargetPos();
    
        // energy setup
        maxEnergy = Mathf.Lerp(12f, 15f, (float)sysRand.NextDouble());
        energy = Mathf.Lerp(0.9f*maxEnergy, maxEnergy, (float)sysRand.NextDouble());
    }

    void Update()
    {
        if (finishedPerformingAction && Time.time < finishedTime + actionDelays[currentAction]) return;

        if (canStartNewAction) StartNewAction();
        finishedPerformingAction = PerformCurrentAction();

        if (finishedPerformingAction) {
            StopCurrentAction();

            finishedTime = Time.time;
            canStartNewAction = true;
        }
    }


    // EVENUAL DELEGATES
    private Action DecideOnNewAction() {
        if (energy <= 0f) return Action.Death;

        if (currentAction == Action.Eat) return Action.Wander;
        if (currentAction == Action.Forage) return Action.Eat;
        if (energy <= 7.5f) return Action.Forage;

        return Action.Wander;
    }

    private void StartNewAction() {
        canStartNewAction = false;
        currentAction = DecideOnNewAction();

        switch (currentAction) {
            case Action.Wander:
                targetPosition = GetNewTargetPos();
                actorBody.GetComponent<MeshRenderer>().material = wanderMaterial;
            break;

            case Action.Death:
                print(String.Format("Oh no. {0} is dead.", gameObject.name));
                actorBody.GetComponent<MeshRenderer>().material = deathMaterial;
            break;

            case Action.Forage:
                currentFood = FindNearbyFood(this);
                if (currentFood == null) return;

                targetPosition = new Vector2(
                    terrainObject.transform.localScale.x * currentFood.gameObject.transform.localPosition.x,
                    terrainObject.transform.localScale.z * currentFood.gameObject.transform.localPosition.z);
                actorBody.GetComponent<MeshRenderer>().material = forageMaterial;
            break;

            case Action.Eat:
                if (currentFood == null) return;// food wasn't found / already eaten / reached by other actor

                currentFood.StartEating();
                actorBody.GetComponent<MeshRenderer>().material = eatingMaterial;
            break;
        }
    }

    private bool PerformCurrentAction() {
        if (energy <= 0f && currentAction != Action.Death) return true;

        switch (currentAction) {
            case Action.Wander:
                Vector2 targetDelta = targetPosition - currentPosition;

                if (targetDelta.magnitude < 0.01f) return true;
                TransformCurrentPosition(targetDelta.normalized * moveSpeed * Time.deltaTime);
            break;

            case Action.Death:
                // do nothing on death
            break;

            case Action.Forage:
                if (currentFood == null) return true;// food wasn't found
                if (currentFood.IsBeingEaten()) {currentFood = null; return true;}// food was reached by another actor

                targetDelta = currentFood.GetCurrentPosition() - currentPosition;

                if (targetDelta.magnitude < 0.05) return true;// wait for actor to reach food
                TransformCurrentPosition(targetDelta.normalized * moveSpeed * Time.deltaTime);
            break;

            case Action.Eat:
                if (currentFood == null) return true;// food wasn't reached first
                return currentFood.IsFinishedEating();// food was reached first
        }

        return false;
    }

    private void StopCurrentAction() {
        // for cleaning up non-isolated actions

        switch (currentAction) {
            case Action.Eat:
                if (currentFood == null) return;

                currentFood.FinishEating(this);
                currentFood = null;
            break;
        }

        actorBody.GetComponent<MeshRenderer>().material = idleMaterial;
    }


    // ENERGY METHODS
    public void AddEnergy(float energyDelta) {
        energy = Mathf.Min(maxEnergy, energy + energyDelta);
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
        HashObjectByPosition(gameObject);
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
