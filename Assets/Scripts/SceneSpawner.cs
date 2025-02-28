using System;//
using System.Collections;//
using UnityEngine;//

public class SceneSpawner : MonoBehaviour
{
    private System.Random sysRand;

    [Header("Hashes")]
    [SerializeField] private float hashResolution = 1.0f;
    [SerializeField] private GameObject emptyFolder;
    private GameObject[,] hashFolders;
    private int hashX;
    private int hashY;

    [Header("Actors")]
    [SerializeField] private int numActors;
    [SerializeField] private GameObject actorPrefab;
    [SerializeField] private GameObject actorFolder;

    [Header("Food")]
    [SerializeField] private float foodDelay = 2.5f;
    [SerializeField] private int foodPerDelay = 5;
    // [SerializeField] private int maxFood = 50;
    // [SerializeField] private float maxDensity = 1f;
    [SerializeField] private GameObject foodPrefab;
    private GameObject[] foodObjects;


    // DELEGATES
    private delegate float GetHeightFromPlanePos(Vector2 u);
    private GetHeightFromPlanePos GetTerrainHeight;
    
    private delegate Vector3 GetNormalAt(Vector2 planePos);
    private GetNormalAt GetTerrainNormal;

    private delegate Vector3 GetOrthographicPlane(Vector3 staticVect, Vector3 orthoVect);
    private GetOrthographicPlane GetTerrainForward;


    void Start()
    {
        sysRand = new System.Random();

        hashX = (int) Math.Max(1, transform.localScale.x / hashResolution);
        hashY = (int) Math.Max(1, transform.localScale.z / hashResolution);
        
        hashFolders = new GameObject[hashX,hashY];

        // delegates
        GetTerrainHeight = new GetHeightFromPlanePos( GetComponent<PerlinFloor>().GetHeightFromPlanePos );
        GetTerrainNormal = new GetNormalAt( GetComponent<PerlinFloor>().GetNormalAt );
        GetTerrainForward = new GetOrthographicPlane( GetComponent<PerlinFloor>().GetOrthographicPlane );

        // make sure the terrain exists before spawning actors
        StartCoroutine(WaitForTerrain());
    }

    IEnumerator WaitForTerrain() {
        yield return new WaitUntil(() => GetComponent<MeshCollider>().sharedMesh != null);
        
        CreateHashFolders();

        for (int i=0; i<50; i++) SpawnFood();
        StartCoroutine(FoodRoutine());

        SpawnActors();
    }


    // RANDOM SAMPLE METHODS
    private Vector2 RandomPos() {
        return new Vector2((float)sysRand.NextDouble(), (float)sysRand.NextDouble());
    }

    private Vector2 RandomSquare(float radius) {
        return 0.5f*radius*Vector2.one + radius*RandomPos();
    }

    private Vector2 RandomCircle(float radius) {
        float theta = (float)(2.0*Mathf.PI * sysRand.NextDouble());
        return 0.5f*Vector2.one + radius*(float)sysRand.NextDouble() * new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
    }

    private Vector2 RandomDonut(float innerRadius, float outterRadius) {
        float theta = (float)(2.0*Mathf.PI * sysRand.NextDouble());

        return 0.5f*Vector2.one + new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) *
            Mathf.Lerp(innerRadius, outterRadius, (float)sysRand.NextDouble());
    }


    // SPAWNING METHODS
    private IEnumerator FoodRoutine() {
        while (true) {
            SpawnFood();
            yield return new WaitForSeconds(foodDelay); 
        }
    }

    private void SpawnFood() {
        for (int f=0; f<foodPerDelay; f++) {
            GameObject newFood = Instantiate<GameObject>(foodPrefab);
            newFood.name = String.Format("Food_{0}", f);

            Vector2 flatPos = RandomPos();
            newFood.transform.parent = actorFolder.transform;// adjust to scaled transform before positioning
            newFood.transform.localPosition = new Vector3(// set food local position
                flatPos.x - 0.5f, GetTerrainHeight(flatPos), flatPos.y - 0.5f );
            PlaceInHashFolder(newFood);// hash the position
        }
    }

    void SpawnActors() {
        for (int a=0; a<numActors; a++) {
            GameObject newActor = Instantiate<GameObject>(actorPrefab);
            newActor.name = String.Format("Actor_{0}", a);

            Vector2 flatPos = RandomPos();
            newActor.transform.parent = actorFolder.transform;// adjust to scaled transform before positioning
            newActor.transform.localPosition = new Vector3(// set actor local position
                flatPos.x - 0.5f, GetTerrainHeight(flatPos), flatPos.y - 0.5f );
            PlaceInHashFolder(newActor);// hash the position
        }
    }


    // HASHING METHODS
    private Vector2Int GetHashPosition(Vector2 planePos) {
        Vector2 relativePos = Vector2.Scale(planePos, new Vector2(hashX, hashY));
        return Vector2Int.FloorToInt(relativePos);
    }

    public void PlaceInHashFolder(GameObject planeObject) {
        Vector2Int hashPos = GetHashPosition(new Vector2(
            planeObject.transform.localPosition.x,
            planeObject.transform.localPosition.z) + 0.5f*Vector2.one);

        planeObject.transform.parent = hashFolders[hashPos.x, hashPos.y].transform;
    }

    private void CreateHashFolders() {
        for (int y=0; y<hashY; y++) {
            for (int x=0; x<hashX; x++) {
                GameObject newHashFolder = Instantiate<GameObject>(emptyFolder, actorFolder.transform);

                newHashFolder.name = String.Format("HashFolder_{0},{1}", x,y);
                hashFolders[x,y] = newHashFolder;
            }
        }
    }

    public FoodBehavior GetClosestFood(ActorBehavior actor) {
        GameObject actorObject = actor.gameObject;
        Vector2 actorLocalPos = 0.5f*Vector2.one + new Vector2(actorObject.transform.localPosition.x, actorObject.transform.localPosition.z);
        
        Vector2Int actorHashPos = GetHashPosition(actorLocalPos);
        GameObject actorFolder = hashFolders[actorHashPos.x, actorHashPos.y];
        FoodBehavior[] hashFolderFood = actorFolder.GetComponentsInChildren<FoodBehavior>();

        if (hashFolderFood.Length == 0 || // no nearby food or only food nearby is being eaten
            (hashFolderFood.Length == 1 && hashFolderFood[0].isBeingEaten)) return null;

        if (hashFolderFood.Length == 1) return hashFolderFood[0];// only one nearby food

        Vector2 foodLocalPos =  0.5f*Vector2.one + new Vector2(
            hashFolderFood[0].transform.localPosition.x, hashFolderFood[0].transform.localPosition.z);

        int closestIndex = 0;
        float closestDistance = (foodLocalPos - actorLocalPos).magnitude;

        for (int i=1; i<hashFolderFood.Length; i++) {
            if (hashFolderFood[i].isBeingEaten) continue;
            
            foodLocalPos =  0.5f*Vector2.one + new Vector2(
                hashFolderFood[i].transform.localPosition.x, hashFolderFood[i].transform.localPosition.z);
            
            if ((foodLocalPos - actorLocalPos).magnitude < closestDistance) {
                closestDistance = (foodLocalPos - actorLocalPos).magnitude;
                closestIndex = i;
            }
        }

        // currently the closest within the current hash region
        return hashFolderFood[closestIndex];
    }
}
