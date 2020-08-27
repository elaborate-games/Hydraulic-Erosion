using System.Collections.Generic;
using UnityEngine;

namespace TerrainTools
{
    [System.Serializable]
    public class HydraulicErosion
    {
        [SerializeField] private ComputeShader erosion;

        public int erosionBrushRadius = 5;
        public int numErosionIterations = 50000;

        public int maxLifetime = 30;
        public float sedimentCapacityFactor = 3;
        public float minSedimentCapacity = .01f;
        public float depositSpeed = 0.3f;
        public float erodeSpeed = 0.3f;

        public float evaporateSpeed = .01f;
        public float gravity = 4;
        public float startSpeed = 1;
        public float startWater = 1;
        [Range(0, 1)] public float inertia = 0.3f;

        private ComputeBuffer brushIndexBuffer, brushWeightBuffer, randomIndexBuffer, heightBuffer;
        private List<int> brushIndexOffsets;
        private List<float> brushWeights;

        /// <param name="erosionBrushRadius"></param>
        /// <param name="mapSize">Square map size</param>
        /// <param name="heightmap"></param>
        /// <param name="numErosionIterations"></param>
        public void Erode(RenderTexture heightmap)
        {
            var mapSize = heightmap.width;// * heightmap.height;

            var erodeKernel = erosion.FindKernel("CSMain");
            var packKernel = erosion.FindKernel("CSPackHeight");
            var unpackKernel = erosion.FindKernel("CSUnPackHeight");

            // Create brush
            if (brushIndexOffsets == null) brushIndexOffsets = new List<int>();
            else brushIndexOffsets.Clear();

            if (brushWeights == null) brushWeights = new List<float>();
            brushWeights.Clear();

            float weightSum = 0;
            for (var brushY = -erosionBrushRadius; brushY <= erosionBrushRadius; brushY++)
            {
                for (var brushX = -erosionBrushRadius; brushX <= erosionBrushRadius; brushX++)
                {
                    float sqrDst = brushX * brushX + brushY * brushY;
                    if (sqrDst < erosionBrushRadius * erosionBrushRadius)
                    {
                        var offset = brushY * mapSize + brushX;
                        Debug.Log(offset);
                        brushIndexOffsets.Add(offset);
                        var brushWeight = 1 - Mathf.Sqrt(sqrDst) / erosionBrushRadius;
                        weightSum += brushWeight;
                        brushWeights.Add(brushWeight);
                    }
                }
            }

            for (var i = 0; i < brushWeights.Count; i++) 
                brushWeights[i] /= weightSum;

            // Send brush data to compute shader
            if (brushIndexBuffer == null || !brushIndexBuffer.IsValid() || brushIndexBuffer.count != brushIndexOffsets.Count)
            {
                if (brushIndexBuffer != null && brushIndexBuffer.IsValid()) brushIndexBuffer.Release();
                brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int));
            }
            brushIndexBuffer.SetData(brushIndexOffsets);
            erosion.SetBuffer(0, "brushIndices", brushIndexBuffer);

            if (brushWeightBuffer == null || !brushWeightBuffer.IsValid() || brushWeightBuffer.count != brushWeights.Count)
            {
                if (brushWeightBuffer != null && brushWeightBuffer.IsValid()) brushWeightBuffer.Release();
                brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(int));
            }
            brushWeightBuffer.SetData(brushWeights);
            erosion.SetBuffer(0, "brushWeights", brushWeightBuffer);

            // Generate random indices for droplet placement
            var randomIndices = new int[numErosionIterations];
            for (var i = 0; i < numErosionIterations; i++)
            {
                var randomX = Random.Range(erosionBrushRadius, mapSize + erosionBrushRadius);
                var randomY = Random.Range(erosionBrushRadius, mapSize + erosionBrushRadius);
                randomIndices[i] = randomY * mapSize + randomX;
            }

            // Send random indices to compute shader
            if (randomIndexBuffer == null || !randomIndexBuffer.IsValid() || randomIndexBuffer.count != randomIndices.Length)
            {
                if (randomIndexBuffer != null && randomIndexBuffer.IsValid()) randomIndexBuffer.Release();
                randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int));
            }
            randomIndexBuffer.SetData(randomIndices);
            erosion.SetBuffer(erodeKernel, "randomIndices", randomIndexBuffer);

            // Heightmap buffer
            var heightBufferSize = heightmap.width * heightmap.height;
            if (heightBuffer == null || !heightBuffer.IsValid() || heightBuffer.count != heightBufferSize)
            {
                if (heightBuffer != null && heightBuffer.IsValid()) heightBuffer.Release();
                heightBuffer = new ComputeBuffer(heightBufferSize, sizeof(float));
            }

            erosion.SetBuffer(erodeKernel, "map", heightBuffer);

            // Settings
            erosion.SetInt("borderSize", erosionBrushRadius);
            erosion.SetInt("mapSize", mapSize);
            erosion.SetInt("brushLength", brushIndexOffsets.Count);
            erosion.SetInt("maxLifetime", maxLifetime);
            erosion.SetFloat("inertia", inertia);
            erosion.SetFloat("sedimentCapacityFactor", sedimentCapacityFactor);
            erosion.SetFloat("minSedimentCapacity", minSedimentCapacity);
            erosion.SetFloat("depositSpeed", depositSpeed);
            erosion.SetFloat("erodeSpeed", erodeSpeed);
            erosion.SetFloat("evaporateSpeed", evaporateSpeed);
            erosion.SetFloat("gravity", gravity);
            erosion.SetFloat("startSpeed", startSpeed);
            erosion.SetFloat("startWater", startWater);
            
            erosion.SetBuffer(packKernel, "map", heightBuffer);
            erosion.SetTexture(packKernel, "heightTexture", heightmap);
            erosion.SetBuffer(unpackKernel, "map", heightBuffer);
            erosion.SetTexture(unpackKernel, "heightTexture", heightmap);

            var threadsPackingX = Mathf.CeilToInt(heightmap.width / 32f);
            var threadsPackingY = Mathf.CeilToInt(heightmap.height / 32f);
            erosion.Dispatch(packKernel, threadsPackingX, threadsPackingY, 1);
            var erodeThreads = Mathf.CeilToInt(numErosionIterations / 1024f);
            erosion.Dispatch(erodeKernel, erodeThreads, 1, 1);
            erosion.Dispatch(unpackKernel, threadsPackingX, threadsPackingY, 1);
        }

        public void ReleaseBuffers()
        {
            // Release buffers
            if (heightBuffer != null && heightBuffer.IsValid())
                heightBuffer.Release();
            if (randomIndexBuffer != null && randomIndexBuffer.IsValid())
                randomIndexBuffer.Release();
            if (brushIndexBuffer != null && brushIndexBuffer.IsValid())
                brushIndexBuffer.Release();
            if (brushWeightBuffer != null && brushWeightBuffer.IsValid())
                brushWeightBuffer.Release();
        }
    }
}