using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class SceneSpawner : MonoBehaviour
{
    private System.Random sysRand;

    [SerializeField] private int numActors;
    [SerializeField] private GameObject actor;
    [SerializeField] private GameObject actorFolder;

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

        GetTerrainHeight = new GetHeightFromPlanePos( GetComponent<PerlinFloor>().GetHeightFromPlanePos );
        GetTerrainNormal = new GetNormalAt( GetComponent<PerlinFloor>().GetNormalAt );
        GetTerrainForward = new GetOrthographicPlane( GetComponent<PerlinFloor>().GetOrthographicPlane );

        // make sure the terrain exists before spawning actors
        StartCoroutine(WaitForTerrain());
    }

    IEnumerator WaitForTerrain() {
        yield return new WaitUntil(() => GetComponent<MeshCollider>().sharedMesh != null);
        SpawnFood();
        SpawnActors();
    }

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

    void SpawnFood() {

    }

    void SpawnActors() {
        for (int a=0; a<numActors; a++) {
            GameObject newActor = Instantiate<GameObject>(actor);
            newActor.transform.parent = actorFolder.transform;
            newActor.name = String.Format("Actor_{0}", a);

            Vector2 flatPos = RandomPos();
            newActor.transform.localPosition = new Vector3( flatPos.x - 0.5f, GetTerrainHeight(flatPos), flatPos.y - 0.5f );
        }
    }
}
