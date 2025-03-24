using System;//
using System.Collections;//
using System.Collections.Generic;//
using Unity.VisualScripting;
using UnityEngine;//

public class ActorBehavior : MonoBehaviour
{
    private System.Random sysRand = new System.Random();

    private GameObject terrainObject;
    public GameObject actorBody;

    private SceneManager managerScript;


    [Header("Materials")]
    [SerializeField] private Material idleMaterial;
    [SerializeField] private Material wanderMaterial;
    [SerializeField] private Material deathMaterial;
    [SerializeField] private Material forageMaterial;
    [SerializeField] private Material eatingMaterial;
    [SerializeField] private Material courtingMaterial;
    [SerializeField] private Material meetingMaterial;
    [SerializeField] private Material fuckingMaterial;


    public enum Status {Neutral, Dead, Hungry, Horny}// unused for now
    public enum Action {Wander, Death, Forage, Eat, Court, Meet, Fuck}
    private Dictionary<Action, float> actionDelays = new Dictionary<Action, float>{
        [Action.Wander] = 0.5f, [Action.Death] = 0f, [Action.Forage] = 0f,
        [Action.Eat] = 1f, [Action.Court] = 0f, [Action.Meet] = 0f,
        [Action.Fuck] = 2f};
    

    // STATUS VARS
    [Header("Status")]
    [SerializeField] private Status currentStatus;

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
    [SerializeField] private float energy;// the actor can move 1 unit per energy
    // private float gravity = 10f;// the actor gets slowed by hills

    // MATING VARS
    private ActorBehavior currentPartner;
    private bool isDesired = false;

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

    private delegate ActorBehavior GetClosestActor(ActorBehavior actor);
    private GetClosestActor FindNearbyPartner;


    void Start()
    {
        PerlinFloor terrainScript = FindObjectOfType<PerlinFloor>();
        managerScript = FindAnyObjectByType<SceneManager>();
        terrainObject = terrainScript.gameObject;

        // delegates
        GetTerrainHeight = new GetHeightFromPlanePos( terrainScript.GetHeightFromPlanePos );
        GetTerrainNormal = new GetNormalFromPlanePos( terrainScript.GetNormalAt );
        // GetTerrainRotation = new GetRotationFromPlanePos( terrainScript.GetRotationAt );

        HashObjectByPosition = new PlaceInHashFolder( managerScript.PlaceInHashFolder );
        FindNearbyFood = new GetClosestFood( managerScript.GetClosestFood );
        FindNearbyPartner = new GetClosestActor( managerScript.GetClosestMate );

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
    private Status GetNewStatus() {
        if (energy <= 0f) return Status.Dead;
        if (energy <= 7.5f) return Status.Hungry;
        if (energy >= 8f) return Status.Horny;
        return Status.Neutral;
    }

    private Action DecideOnNewAction() {
        switch (currentStatus) {
            case Status.Neutral:
                return Action.Wander;

            case Status.Dead:
                return Action.Death;

            case Status.Hungry:
                if (currentAction == Action.Eat) return Action.Forage;
                if (currentAction == Action.Forage) return Action.Eat;
                return Action.Forage;

            case Status.Horny:
                if (currentAction == Action.Fuck) return Action.Wander;
                if (currentAction == Action.Meet) return Action.Fuck;
                if (currentAction == Action.Court) return Action.Meet;
                if (isDesired && currentPartner != null) return Action.Meet;
                return Action.Court;
        }

        return Action.Wander;
    }

    private void StartNewAction() {
        canStartNewAction = false;
        currentStatus = GetNewStatus();
        currentAction = DecideOnNewAction();

        switch (currentAction) {
            case Action.Wander:
                targetPosition = GetNewTargetPos();
                actorBody.GetComponent<MeshRenderer>().material = wanderMaterial;
            break;

            case Action.Death:
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

            case Action.Court:
                currentPartner = FindNearbyPartner(this);
                if (currentPartner == null) return;// no mates found
                currentPartner.SetPartner(this);// create comms

                SetDesired(true);// adjust currect actor
                currentPartner.SetDesired(true);// skip parter's search
                actorBody.GetComponent<MeshRenderer>().material = courtingMaterial;
            break;

            case Action.Meet:
                if (currentPartner == null) {// no mates found
                    SetDesired(false);
                    return;
                }

                targetPosition = 0.5f*(currentPosition + currentPartner.GetCurrentPosition());// meet halfway
                actorBody.GetComponent<MeshRenderer>().material = meetingMaterial;
            break;

            case Action.Fuck:
                if (currentPartner == null) {// no mates found
                    SetDesired(false);
                    return;
                }

                if (currentPartner.GetAction() == Action.Fuck) return;// wait for other to reach
                
                // the last one to get into position will perform action
                // shit balls to spawn a child
                energy -= 5f;
                currentPartner.AddEnergy(-5);

                actorBody.GetComponent<MeshRenderer>().material = fuckingMaterial;
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

            case Action.Court:
                if (currentPartner == null) {// mate wasn't found or died
                    currentPartner = null;
                    SetDesired(false);

                    return true;
                }

                // if the other partner is no longer horny
                if (currentPartner.GetStatus() != Status.Horny) {
                    currentPartner.SetDesired(false);
                    currentPartner.SetPartner(null);
                    
                    currentPartner = null;
                    SetDesired(false);
                    return true;
                }

                return currentPartner.finishedPerformingAction || currentPartner.GetAction() == Action.Court;

            case Action.Meet:
                if (currentPartner == null) {// partner may have died
                    SetDesired(false);
                    return true;
                }

                if (currentPartner.GetStatus() != Status.Horny) {
                    currentPartner.SetDesired(false);
                    currentPartner.SetPartner(null);
                    currentPartner = null;
                    return true;
                }

                targetDelta = currentPartner.GetCurrentPosition() - currentPosition;
                if (targetDelta.magnitude < 0.05) return true;// wait for actor to reach mate
                TransformCurrentPosition(targetDelta.normalized * moveSpeed * Time.deltaTime);
            break;

            case Action.Fuck:
                if (currentPartner == null) return true;// other partner fucked current actor
                return currentPartner.GetAction() == Action.Fuck;// wait for both to each eachother
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

            case Action.Fuck:
                if (currentPartner == null) return;
                currentPartner.SetDesired(false);
                currentPartner.SetPartner(null);

                SetDesired(false);
                currentPartner = null;
            break;
        }

        actorBody.GetComponent<MeshRenderer>().material = idleMaterial;
    }

    // GETTERS / SETTERS
    public void SetPartner(ActorBehavior partner) {this.currentPartner = partner;}
    public void SetDesired(bool desired) {this.isDesired = desired;}
    public bool IsDesired() {return isDesired;}

    public GameObject GetBody() {return actorBody;}

    public Status GetStatus() {return currentStatus;}
    public Action GetAction() {return currentAction;}


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
