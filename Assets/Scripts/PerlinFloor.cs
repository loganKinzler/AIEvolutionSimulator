using System;
using UnityEngine;

public class PerlinFloor : MonoBehaviour
{
    [Header("Mesh Data")]
    [SerializeField] [Min(1)] private int resolution = 5;
    [SerializeField] private Material mat;

    [Header("Noise")]
    [SerializeField] [Min(1)] private int octaves = 4;
    [SerializeField] private Texture2D texture;

    private float[,] heightMap;

    private Mesh mesh;
    private Vector3[] verts;
    private int[] tris;
    private Vector2[] uvs;


    void Start()
    {
        GenerateHeightMap();
        GenerateMeshData();

        mesh = new Mesh();
        mesh.name = "PerlinFloorMesh";

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv2 = uvs;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = mat;
    }

    // void Update()
    // {
        
    // }

    private void GenerateMeshData() {

        verts = new Vector3[(int) Mathf.Pow(resolution+1, 2)];
        tris = new int[6*resolution*resolution];
        uvs = new Vector2[(int) Mathf.Pow(resolution+1, 2)];

        for (int y=0; y<=resolution; y++) {
            for (int x=0; x<=resolution; x++) {
                int index = x + y*(resolution+1);

                uvs[index] = new Vector2(x,y) / (resolution+1);
                verts[index] = new Vector3(x, heightMap[x, y], y);

                if (x == resolution || y == resolution) continue;

                tris[6*(index-y)] = index+resolution+1;
                tris[6*(index-y)+1] = index+resolution+2;
                tris[6*(index-y)+2] = index+1;

                tris[6*(index-y)+3] = index+1;
                tris[6*(index-y)+4] = index;
                tris[6*(index-y)+5] = index+resolution+1;
            }
        }
    }

    private void GenerateHeightMap() {
        heightMap = new float[resolution+1, resolution+1];

        for (int x=0; x<=resolution; x++) {
            for (int y=0; y<=resolution; y++) {
                heightMap[x,y] = Mathf.PerlinNoise(x/(resolution+1.0f), y/(resolution+1.0f));
            }
        }
    }
}
