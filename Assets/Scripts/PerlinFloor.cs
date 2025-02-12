using System;
using UnityEngine;

public class PerlinFloor : MonoBehaviour
{
    [Header("Mesh Data")]
    [SerializeField] [Min(0.0001f)] private float resolution = 1f;
    [SerializeField] private Material mat;

    [Header("Noise")]
    [SerializeField] [Min(1)] private int octaves = 1;
    // [SerializeField] private Texture2D texture;

    private float[,] heightMap;
    private int xRes;
    private int yRes;


    private Mesh mesh;
    private Vector3[] verts;
    private int[] tris;
    private Vector2[] uvs;


    void Start()
    {
        xRes = (int) Math.Max(1, transform.localScale.x / resolution) + 1;
        yRes = (int) Math.Max(1, transform.localScale.z / resolution) + 1;

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

        verts = new Vector3[xRes*yRes];
        tris = new int[6*(xRes-1)*(yRes-1)];
        uvs = new Vector2[xRes*yRes];

        for (int y=0; y<yRes; y++) {
            for (int x=0; x<xRes; x++) {
                int index = x + y*xRes;

                uvs[index] = new Vector2(x/(xRes-1f), y/(yRes-1f));
                verts[index] = new Vector3(x/(xRes-1f), heightMap[x, y], y/(yRes-1f)) - 0.5f*Vector3.one;

                if (x == xRes-1 || y == yRes-1) continue;

                tris[6*(index-y)] = index+xRes;
                tris[6*(index-y)+1] = index+xRes+1;
                tris[6*(index-y)+2] = index+1;

                tris[6*(index-y)+3] = index+1;
                tris[6*(index-y)+4] = index;
                tris[6*(index-y)+5] = index+xRes;
            }
        }
    }

    private void GenerateHeightMap() {
        heightMap = new float[xRes, yRes];

        for (int y=0; y<yRes; y++) {
            for (int x=0; x<xRes; x++) {
                heightMap[x,y] = Mathf.PerlinNoise(x/(xRes-1f)*octaves, y/(yRes-1f)*octaves) / octaves*2;
            }
        }
    }
}
