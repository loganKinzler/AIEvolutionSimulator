using System;
using System.Collections;
using UnityEngine;

public class ActorController : MonoBehaviour
{

    [SerializeField] private int numActors;
    [SerializeField] private GameObject actor;
    [SerializeField] private GameObject actorFolder;

    void Start()
    {
        // make sure the terrain exists before spawning actors
        StartCoroutine(WaitForTerrain());
    }

    IEnumerator WaitForTerrain() {
        yield return new WaitUntil(() => GetComponent<MeshCollider>().sharedMesh != null);
        SpawnActors();
    }

    void SpawnActors() {
        System.Random sysRand = new System.Random();

        for (int a=0; a<numActors; a++) {
            GameObject newActor = Instantiate<GameObject>(actor);
            newActor.transform.parent = actorFolder.transform;
            newActor.name = String.Format("Actor_{0}", a);

            Vector2 flatPos = new Vector2((float)sysRand.NextDouble(), (float)sysRand.NextDouble());
            
            newActor.transform.localPosition = new Vector3(
                flatPos.x,
                GetComponent<PerlinFloor>().GetHeightFromPlanePos(flatPos),
                flatPos.y
            ) - 0.5f*Vector3.one;
        }
    }
}
