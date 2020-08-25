using UnityEngine;

public class HeightMapGenerator : MonoBehaviour
{
    public bool forceNewHeightTexture = true;
    public int seed;
    
    public int numOctaves = 7;
    public float persistence = .5f;
    public float lacunarity = 2;
    public float initialScale = 2;

    // public bool useComputeShader = true;
    public ComputeShader heightMapComputeShader;

    private RenderTexture heightMapTexture;
    public RenderTexture HeightMapTexture => heightMapTexture;

    public Renderer rend;


    public RenderTexture GenerateHeightMap (int mapSize) {
        return GenerateHeightMapGPU (mapSize);
    }

    private RenderTexture GenerateHeightMapGPU (int mapSize) {
        var prng = new System.Random (seed);

        Vector2[] offsets = new Vector2[numOctaves];
        for (int i = 0; i < numOctaves; i++) {
            offsets[i] = new Vector2 (prng.Next (-10000, 10000), prng.Next (-10000, 10000));
        }
        ComputeBuffer offsetsBuffer = new ComputeBuffer (offsets.Length, sizeof (float) * 2);
        offsetsBuffer.SetData (offsets);
        heightMapComputeShader.SetBuffer (0, "offsets", offsetsBuffer);

        int floatToIntMultiplier = 1000;
        // float[] map = new float[mapSize * mapSize];
        var size = new Vector2Int(mapSize, mapSize);
        if (forceNewHeightTexture || heightMapTexture == null || size.x != heightMapTexture.width || size.y != heightMapTexture.height)
        {
            if(heightMapTexture != null && heightMapTexture.IsCreated()) heightMapTexture.Release();
            heightMapTexture = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            heightMapTexture.hideFlags = HideFlags.DontSave;
            heightMapTexture.enableRandomWrite = true;
            heightMapTexture.Create();
        }

        // ComputeBuffer mapBuffer = new ComputeBuffer (map.Length, sizeof (int));
        // mapBuffer.SetData (map);
        // heightMapComputeShader.SetBuffer (0, "heightMap", mapBuffer);
        heightMapComputeShader.SetTexture(0, "heightMapTexture", heightMapTexture);

        int[] minMaxHeight = { floatToIntMultiplier * numOctaves, 0 };
        ComputeBuffer minMaxBuffer = new ComputeBuffer (minMaxHeight.Length, sizeof (int));
        minMaxBuffer.SetData (minMaxHeight);
        heightMapComputeShader.SetBuffer (0, "minMax", minMaxBuffer);

        heightMapComputeShader.SetInt ("mapSize", mapSize);
        heightMapComputeShader.SetInt ("octaves", numOctaves);
        heightMapComputeShader.SetFloat ("lacunarity", lacunarity);
        heightMapComputeShader.SetFloat ("persistence", persistence);
        heightMapComputeShader.SetFloat ("scaleFactor", initialScale);
        heightMapComputeShader.SetInt ("floatToIntMultiplier", floatToIntMultiplier);

        var tx = Mathf.CeilToInt(heightMapTexture.width / 32f);
        var ty = Mathf.CeilToInt(heightMapTexture.height / 32f);
        heightMapComputeShader.Dispatch (0, tx, ty,  1);

        // mapBuffer.GetData (map);
        minMaxBuffer.GetData (minMaxHeight);
        // mapBuffer.Release ();
        minMaxBuffer.Release ();
        offsetsBuffer.Release ();

        // float minValue = (float) minMaxHeight[0] / (float) floatToIntMultiplier;
        // float maxValue = (float) minMaxHeight[1] / (float) floatToIntMultiplier;

        // for (int i = 0; i < map.Length; i++) {
        //     map[i] = Mathf.InverseLerp (minValue, maxValue, map[i]);
        // }


        rend.sharedMaterial.mainTexture = heightMapTexture;
        
        return heightMapTexture;
    }
}