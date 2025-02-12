using System;
using UnityEngine;

public class ActorSpawner : MonoBehaviour
{

    [SerializeField] private int numActors;
    [SerializeField] private GameObject actor;
    [SerializeField] private GameObject actorFolder;

    void Start()
    {
        SpawnActors();
    }

    void SpawnActors() {
        System.Random sysRand = new System.Random();

        for (int a=0; a<numActors; a++) {
            GameObject newActor = Instantiate<GameObject>(actor);
            newActor.transform.parent = actorFolder.transform;
            newActor.name = String.Format("Actor_{0}", a);

            newActor.transform.localPosition = new Vector3(
                (float)sysRand.NextDouble() - 0.5f,
                1,
                (float)sysRand.NextDouble() - 0.5f
            );
        }
    }
}
