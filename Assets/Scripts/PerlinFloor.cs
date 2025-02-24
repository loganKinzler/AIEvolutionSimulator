using System;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using Unity.VisualScripting;
using UnityEditor.Timeline;
using UnityEngine;

public class PerlinFloor : MonoBehaviour
{
    [Header("Mesh Data")]
    [SerializeField] [Min(0.0001f)] private float resolution = 1f;
    [SerializeField] private Material mat;

    [Header("Noise")]
    [SerializeField] [Min(0.0001f)] private float octavesPerUnit = 1;
    [SerializeField] [Min(1)] private int depth = 1;
    [SerializeField] [Min(1)] private float depthScaling = 2;

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
        GetComponent<MeshCollider>().sharedMesh = mesh;
        GetComponent<MeshRenderer>().material = mat;
    }

    // BASIC LINEAR ALGEBRA
    public Vector3 GetNormal(Vector3 u, Vector3 v) {
        return Vector3.Cross(u, v) / u.magnitude / v.magnitude;
    }

    private float ProjectionScalar(Vector2 u, Vector2 v) {
        return Vector2.Dot(u, v) / v.SqrMagnitude();
    }


    // INDEXING FUNCTIONS
    public float GetHeightFromPlanePos(Vector2 planePos) {
        Vector2 relativePos = Vector2.Scale(planePos, new Vector2(xRes-1, yRes-1));
        Vector2Int mapIndex = Vector2Int.FloorToInt(relativePos);

        Vector2 localCellPos = relativePos - mapIndex;
        int sideDirection = (localCellPos.x >= localCellPos.y)? 1:-1;// 1 = down left
        Vector2Int sideIndex = mapIndex + (sideDirection == 1? Vector2Int.right:Vector2Int.up);

        float fowardScale = ProjectionScalar(localCellPos, Vector2.one);
        float sidewaysScale = ProjectionScalar(localCellPos, new Vector2(0.70710678118f,-0.70710678118f)*sideDirection);

        float forwardLerp = Mathf.Lerp(heightMap[mapIndex.x, mapIndex.y]-0.5f, heightMap[mapIndex.x+1, mapIndex.y+1]-0.5f, fowardScale);
        float sidewaysLerp = Mathf.Lerp(forwardLerp, heightMap[sideIndex.x, sideIndex.y]-0.5f, sidewaysScale);
        return sidewaysLerp - 0.5f;
    }

    public Vector3 GetNormalAt(Vector2 planePos) {
        Vector2 relativePos = Vector2.Scale(planePos, new Vector2(xRes-1, yRes-1));
        Vector2Int mapIndex = Vector2Int.FloorToInt(relativePos);

        Vector2 localCellPos = relativePos - mapIndex;
        int sideDirection = (localCellPos.x >= localCellPos.y)? 1:-1;// 1 = down left
        Vector2Int sideIndex = mapIndex + (sideDirection == 1? Vector2Int.right:Vector2Int.up);

        Vector3 zeroIndex3 = new Vector3(mapIndex.x, heightMap[mapIndex.x, mapIndex.y], mapIndex.y);
        Vector3 oneIndex3 = new Vector3(mapIndex.x+1, heightMap[mapIndex.x+1, mapIndex.y+1], mapIndex.y+1);
        Vector3 sideIndex3 = new Vector3(sideIndex.x, heightMap[sideIndex.x, sideIndex.y], sideIndex.y);

        return sideDirection * GetNormal(sideIndex3 - zeroIndex3, oneIndex3 - sideIndex3);
    }


    // MESH FUNCTIONS
    private void GenerateMeshData() {

        verts = new Vector3[xRes*yRes];
        tris = new int[6*(xRes-1)*(yRes-1)];
        uvs = new Vector2[xRes*yRes];

        for (int y=0; y<yRes; y++) {
            for (int x=0; x<xRes; x++) {
                int index = x + y*xRes;

                uvs[index] = new Vector2(x/(xRes-1f), y/(yRes-1f));
                verts[index] = new Vector3(x/(xRes-1f), heightMap[x, y] - 0.5f, y/(yRes-1f)) - 0.5f*Vector3.one;

                if (x == xRes-1 || y == yRes-1) continue;

                tris[6*(index-y)] = index;
                tris[6*(index-y)+1] = index+xRes;
                tris[6*(index-y)+2] = index+xRes+1;

                tris[6*(index-y)+3] = index+xRes+1;
                tris[6*(index-y)+4] = index+1;
                tris[6*(index-y)+5] = index;
            }
        }
    }

    private void GenerateHeightMap() {
        heightMap = new float[xRes, yRes];

        for (int d=0; d<depth; d++) {
            for (int y=0; y<yRes; y++) {
                for (int x=0; x<xRes; x++) {

                    heightMap[x,y] += Mathf.PerlinNoise(
                        x/(xRes-1f) * transform.localScale.x*octavesPerUnit * d,
                        y/(yRes-1f) * transform.localScale.z*octavesPerUnit * d) /
                            Mathf.Pow(depthScaling, d);
                }
            }
        }
    }
}
