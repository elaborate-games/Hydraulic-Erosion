using System;
using System.Collections.Generic;
using System.Security.Principal;
using Height2NormalMap;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Random = UnityEngine.Random;

[ExecuteAlways]
public class TerrainGenerator : MonoBehaviour {

    public bool printTimers;

    [Header ("Mesh Settings")]
    public int meshResolution = 255;
    public int mapSize = 255;
    public float scale = 20;
    public float elevationScale = 10;
    public Material material;

    [Header("Erosion Settings")] 
    public ComputeShader erosion;
    public int numErosionIterations = 50000;
    public int erosionBrushRadius = 3;

    public int maxLifetime = 30;
    public float sedimentCapacityFactor = 3;
    public float minSedimentCapacity = .01f;
    public float depositSpeed = 0.3f;
    public float erodeSpeed = 0.3f;

    public float evaporateSpeed = .01f;
    public float gravity = 4;
    public float startSpeed = 1;
    public float startWater = 1;
    [Range (0, 1)]
    public float inertia = 0.3f;

    // Internal
    RenderTexture map;
    Mesh mesh;
    int mapSizeWithBorder;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    private RenderTexture NormalMap;
    public Renderer Rend;
    public Texture HeightMap;
    public bool UseHeight2NormalSoebel = false;
    [Range(0,1)]
    public float BumpEffect = .5f;

    public GaussianBlurFilter Blur;

    private void Start()
    {
        GenerateHeightMap();
        ContructMesh();
        Erode();
    }

    public void GenerateHeightMap () 
    {
        mapSizeWithBorder = mapSize + erosionBrushRadius * 2;
        map = FindObjectOfType<HeightMapGenerator> ().GenerateHeightMap (mapSize);
        if (HeightMap) Graphics.Blit(HeightMap, map);
        Blur.Apply(map, map);
        material.SetTexture("_heightMap", map);
        UpdateNormalMap();
    }

    private void UpdateNormalMap()
    {
        var mat = new Material(Shader.Find("Hidden/NormalMap"));
        var bumpEffect = BumpEffect;
        float v = bumpEffect * 2f - 1f;
        float z = 1f - v;
        float xy = 1f + v;
        mat.SetVector("_Factor", new Vector4(xy, xy, z, 1));
        NormalMap = new RenderTexture(map.width, map.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        NormalMap.hideFlags = HideFlags.DontSave;
        NormalMap.Create();
        if (UseHeight2NormalSoebel)
        {
            Debug.Log("sobel");
            var sobel = new SobelNormalMapFilter() {bumpEffect = BumpEffect};
            sobel.Apply(map, NormalMap);
        }
        else
        {
            Debug.Log(mat.shader.name);
            Graphics.Blit(map, NormalMap, mat);
        }
        Rend.sharedMaterial.mainTexture = NormalMap;
        material.SetTexture("_NormalMap", NormalMap);
    }

    public void Erode ()
    {
        var erodeKernel = erosion.FindKernel("CSMain");
        var packKernel = erosion.FindKernel("CSPackHeight");
        var unpackKernel = erosion.FindKernel("CSUnPackHeight");
        
        // Create brush
        List<int> brushIndexOffsets = new List<int> ();
        List<float> brushWeights = new List<float> ();

        float weightSum = 0;
        for (int brushY = -erosionBrushRadius; brushY <= erosionBrushRadius; brushY++) {
            for (int brushX = -erosionBrushRadius; brushX <= erosionBrushRadius; brushX++) {
                float sqrDst = brushX * brushX + brushY * brushY;
                if (sqrDst < erosionBrushRadius * erosionBrushRadius) {
                    brushIndexOffsets.Add (brushY * mapSize + brushX);
                    float brushWeight = 1 - Mathf.Sqrt (sqrDst) / erosionBrushRadius;
                    weightSum += brushWeight;
                    brushWeights.Add (brushWeight);
                }
            }
        }
        for (int i = 0; i < brushWeights.Count; i++) {
            brushWeights[i] /= weightSum;
        }

        // Send brush data to compute shader
        ComputeBuffer brushIndexBuffer = new ComputeBuffer (brushIndexOffsets.Count, sizeof (int));
        ComputeBuffer brushWeightBuffer = new ComputeBuffer (brushWeights.Count, sizeof (int));
        brushIndexBuffer.SetData (brushIndexOffsets);
        brushWeightBuffer.SetData (brushWeights);
        erosion.SetBuffer (0, "brushIndices", brushIndexBuffer);
        erosion.SetBuffer (0, "brushWeights", brushWeightBuffer);

        // Generate random indices for droplet placement
        int[] randomIndices = new int[numErosionIterations];
        for (int i = 0; i < numErosionIterations; i++) {
            int randomX = Random.Range (erosionBrushRadius, mapSize + erosionBrushRadius);
            int randomY = Random.Range (erosionBrushRadius, mapSize + erosionBrushRadius);
            randomIndices[i] = randomY * mapSize + randomX;
        }

        // Send random indices to compute shader
        ComputeBuffer randomIndexBuffer = new ComputeBuffer (randomIndices.Length, sizeof (int));
        randomIndexBuffer.SetData (randomIndices);
        // erosion.SetBuffer (packKernel, "randomIndices", randomIndexBuffer);
        erosion.SetBuffer (erodeKernel, "randomIndices", randomIndexBuffer);
        // erosion.SetBuffer (unpackKernel, "randomIndices", randomIndexBuffer);

        // Heightmap buffer
        ComputeBuffer mapBuffer = new ComputeBuffer (map.width*map.height, sizeof (float));
        erosion.SetBuffer (erodeKernel, "map", mapBuffer);

        // Settings
        erosion.SetInt ("borderSize", erosionBrushRadius);
        erosion.SetInt ("mapSize", mapSize);
        erosion.SetInt ("brushLength", brushIndexOffsets.Count);
        erosion.SetInt ("maxLifetime", maxLifetime);
        erosion.SetFloat ("inertia", inertia);
        erosion.SetFloat ("sedimentCapacityFactor", sedimentCapacityFactor);
        erosion.SetFloat ("minSedimentCapacity", minSedimentCapacity);
        erosion.SetFloat ("depositSpeed", depositSpeed);
        erosion.SetFloat ("erodeSpeed", erodeSpeed);
        erosion.SetFloat ("evaporateSpeed", evaporateSpeed);
        erosion.SetFloat ("gravity", gravity);
        erosion.SetFloat ("startSpeed", startSpeed);
        erosion.SetFloat ("startWater", startWater);

        erosion.SetBuffer (packKernel, "map", mapBuffer);
        erosion.SetTexture(packKernel, "heightTexture", map);
        erosion.SetBuffer (unpackKernel, "map", mapBuffer);
        erosion.SetTexture(unpackKernel, "heightTexture", map);
        
        Debug.Log(map.width + ", " + map.height + ", " + mapSize + ", " + mapSizeWithBorder);
        
        var threadsPackingX = Mathf.CeilToInt(map.width / 32f);
        var threadsPackingY = Mathf.CeilToInt(map.height / 32f);
        
        // pack
        erosion.Dispatch(packKernel, threadsPackingX, threadsPackingY, 1);
        
        var erodeThreads = Mathf.CeilToInt(numErosionIterations / 1024f);
        erosion.Dispatch (erodeKernel, erodeThreads, 1, 1);
        
        // unpack
        erosion.Dispatch(unpackKernel, threadsPackingX, threadsPackingY, 1);

        // Release buffers
        mapBuffer.Release ();
        randomIndexBuffer.Release ();
        brushIndexBuffer.Release ();
        brushWeightBuffer.Release ();
        
        UpdateNormalMap();
    }

    public void ContructMesh () 
    {
        Vector3[] verts = new Vector3[meshResolution * meshResolution];
        int[] triangles = new int[(meshResolution - 1) * (meshResolution-1) * 6];
        int t = 0;
        
        Vector2[] uvs = new Vector2[verts.Length];

        for (int i = 0; i < meshResolution * meshResolution; i++) {
            int x = i % meshResolution;
            int y = i / meshResolution;
            int borderedMapIndex = (y + erosionBrushRadius) * mapSizeWithBorder + x + erosionBrushRadius;
            int meshMapIndex = y * meshResolution + x;

            Vector2 percent = new Vector2 (x / (meshResolution - 1f), y / (meshResolution - 1f));
            Vector3 pos = new Vector3 (percent.x * 2 - 1, 0, percent.y * 2 - 1) * scale;

            // float normalizedHeight = map[borderedMapIndex];
            // pos += Vector3.up * normalizedHeight * elevationScale;
            verts[meshMapIndex] = pos;
            uvs[meshMapIndex] = percent;// new Vector2(percent.y, percent.x);

            // Construct triangles
            if (x != meshResolution - 1 && y != meshResolution - 1) {
                t = (y * (meshResolution - 1) + x) * 3 * 2;

                triangles[t + 0] = meshMapIndex + meshResolution;
                triangles[t + 1] = meshMapIndex + meshResolution + 1;
                triangles[t + 2] = meshMapIndex;

                triangles[t + 3] = meshMapIndex + meshResolution + 1;
                triangles[t + 4] = meshMapIndex + 1;
                triangles[t + 5] = meshMapIndex;
                t += 6;
            }
        }

        if (mesh == null) {
            mesh = new Mesh ();
        } else {
            mesh.Clear ();
        }
        mesh.hideFlags = HideFlags.DontSaveInEditor;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals ();

        AssignMeshComponents ();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;

        material.SetFloat ("_MaxHeight", elevationScale);
    }

    private void AssignMeshComponents () {
        // Find/creator mesh holder object in children
        string meshHolderName = "Mesh Holder";
        Transform meshHolder = transform.Find (meshHolderName);
        if (meshHolder == null) {
            meshHolder = new GameObject (meshHolderName).transform;
            meshHolder.transform.parent = transform;
            meshHolder.transform.localPosition = Vector3.zero;
            meshHolder.transform.localRotation = Quaternion.identity;
        }

        // Ensure mesh renderer and filter components are assigned
        if (!meshHolder.gameObject.GetComponent<MeshFilter> ()) {
            meshHolder.gameObject.AddComponent<MeshFilter> ();
        }
        if (!meshHolder.GetComponent<MeshRenderer> ()) {
            meshHolder.gameObject.AddComponent<MeshRenderer> ();
        }

        meshRenderer = meshHolder.GetComponent<MeshRenderer> ();
        meshFilter = meshHolder.GetComponent<MeshFilter> ();
    }
}