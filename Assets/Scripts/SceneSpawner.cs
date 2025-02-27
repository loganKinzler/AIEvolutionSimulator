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
    [SerializeField] private GameObject actor;
    [SerializeField] private GameObject actorFolder;

    [Header("Food")]
    [SerializeField] private GameObject foodObject;


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
        SpawnFood();
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
    void SpawnFood() {

    }

    void SpawnActors() {
        for (int a=0; a<numActors; a++) {
            GameObject newActor = Instantiate<GameObject>(actor);
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

}
