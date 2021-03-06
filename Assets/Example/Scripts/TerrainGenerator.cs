﻿using System.Diagnostics;
using TerrainTools;
using UnityEngine;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(HydraulicErosion))]
[ExecuteAlways]
public class TerrainGenerator : MonoBehaviour {

    public bool printTimers;

    [Header ("Mesh Settings")]
    public int meshResolution = 255;
    public int mapSize = 255;
    public float scale = 20;
    public Material material;
    
    [Header("Erosion Settings")] 
    public int numErosionIterations = 50000;
    public int erosionBrushRadius = 3;

    // Internal
    RenderTexture map;
    Mesh mesh;
    int mapSizeWithBorder;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    public RenderTexture NormalMap { get; private set; }
    
    [Header("Normal")]
    [Range(0,1)]
    public GaussianBlurFilter Blur;
    public NormalMapFilterRFloat Normal;

    [Header("Runtime")] 
    public int StepsPerFrame = 5000;


    private HydraulicErosion erosion;

    private HydraulicErosion Erosion
    {
        get
        {
            if (erosion) return erosion;
            TryGetComponent(out erosion);
            return erosion;
        }
    }

    
    private void Start()
    {
        GenerateHeightMap();
        ContructMesh();
        Erode();
    }
    
    private int totalSteps;
    private Stopwatch sw = new Stopwatch();
    
    private void Update()
    {
        if (!Application.isPlaying) return;
        if (StepsPerFrame <= 0) return;
        numErosionIterations = StepsPerFrame;
        sw.Start();
        Erode();
        var ms = sw.ElapsedMilliseconds;
        sw.Reset();
        totalSteps += numErosionIterations;
        Debug.Log("Total: " + totalSteps + " in " + ms + "ms");
    }

    public void GenerateHeightMap ()
    {
        totalSteps = 0;
        mapSizeWithBorder = mapSize + erosionBrushRadius * 2;
        map = FindObjectOfType<HeightMapGenerator> ().GenerateHeightMap (mapSize);
        Blur.Apply(map, map);
        material.SetTexture("_heightMap", map);
        UpdateNormalMap();
    }

    public void Erode()
    {
        Erosion.Erode(map, erosionBrushRadius, numErosionIterations);
        UpdateNormalMap();
    }

    private void UpdateNormalMap()
    {
        if (NormalMap == null || NormalMap.width != map.width || NormalMap.height != map.height)
        {
            if(NormalMap != null && NormalMap.IsCreated()) NormalMap.Release();
            NormalMap = new RenderTexture(map.width, map.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            NormalMap.hideFlags = HideFlags.DontSave;
            NormalMap.Create();
        }
        Normal.Apply(map, NormalMap);
        material.SetTexture("_NormalMap", NormalMap);
    }

    

    public void ContructMesh () 
    {
        var verts = new Vector3[meshResolution * meshResolution];
        var triangles = new int[(meshResolution - 1) * (meshResolution-1) * 6];
        var t = 0;
        
        var uvs = new Vector2[verts.Length];

        for (var i = 0; i < meshResolution * meshResolution; i++) {
            var x = i % meshResolution;
            var y = i / meshResolution;
            var borderedMapIndex = (y + erosionBrushRadius) * mapSizeWithBorder + x + erosionBrushRadius;
            var meshMapIndex = y * meshResolution + x;

            var percent = new Vector2 (x / (meshResolution - 1f), y / (meshResolution - 1f));
            var pos = new Vector3 (percent.x * 2 - 1, 0, percent.y * 2 - 1) * scale;

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
    }

    private void AssignMeshComponents () {
        // Find/creator mesh holder object in children
        var meshHolderName = "Mesh Holder";
        var meshHolder = transform.Find (meshHolderName);
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