using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Erosion
{
    public class HydraulicErosion : MonoBehaviour
    {
        [SerializeField] private ComputeShader erosion;
        
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

        public void Erode(RenderTexture map, int erosionBrushRadius, int mapSize, int numErosionIterations)
        {
            var erodeKernel = erosion.FindKernel("CSMain");
            var packKernel = erosion.FindKernel("CSPackHeight");
            var unpackKernel = erosion.FindKernel("CSUnPackHeight");

            // Create brush
            List<int> brushIndexOffsets = new List<int>();
            List<float> brushWeights = new List<float>();

            float weightSum = 0;
            for (int brushY = -erosionBrushRadius; brushY <= erosionBrushRadius; brushY++)
            {
                for (int brushX = -erosionBrushRadius; brushX <= erosionBrushRadius; brushX++)
                {
                    float sqrDst = brushX * brushX + brushY * brushY;
                    if (sqrDst < erosionBrushRadius * erosionBrushRadius)
                    {
                        brushIndexOffsets.Add(brushY * mapSize + brushX);
                        float brushWeight = 1 - Mathf.Sqrt(sqrDst) / erosionBrushRadius;
                        weightSum += brushWeight;
                        brushWeights.Add(brushWeight);
                    }
                }
            }

            for (int i = 0; i < brushWeights.Count; i++)
            {
                brushWeights[i] /= weightSum;
            }

            Debug.Log("Brush weights: " + brushWeights.Count);
            if (brushWeights.Count > 500)
            {
                Debug.Log("Reduce brush radius to < 500, current: " + brushWeights.Count);
                return;
            }

            // Send brush data to compute shader
            ComputeBuffer brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int));
            ComputeBuffer brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(int));
            brushIndexBuffer.SetData(brushIndexOffsets);
            brushWeightBuffer.SetData(brushWeights);
            erosion.SetBuffer(0, "brushIndices", brushIndexBuffer);
            erosion.SetBuffer(0, "brushWeights", brushWeightBuffer);

            // Generate random indices for droplet placement
            int[] randomIndices = new int[numErosionIterations];
            for (int i = 0; i < numErosionIterations; i++)
            {
                int randomX = Random.Range(erosionBrushRadius, mapSize + erosionBrushRadius);
                int randomY = Random.Range(erosionBrushRadius, mapSize + erosionBrushRadius);
                randomIndices[i] = randomY * mapSize + randomX;
            }

            // Send random indices to compute shader
            ComputeBuffer randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int));
            randomIndexBuffer.SetData(randomIndices);
            // erosion.SetBuffer (packKernel, "randomIndices", randomIndexBuffer);
            erosion.SetBuffer(erodeKernel, "randomIndices", randomIndexBuffer);
            // erosion.SetBuffer (unpackKernel, "randomIndices", randomIndexBuffer);

            // Heightmap buffer
            ComputeBuffer mapBuffer = new ComputeBuffer(map.width * map.height, sizeof(float));
            erosion.SetBuffer(erodeKernel, "map", mapBuffer);

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

            erosion.SetBuffer(packKernel, "map", mapBuffer);
            erosion.SetTexture(packKernel, "heightTexture", map);
            erosion.SetBuffer(unpackKernel, "map", mapBuffer);
            erosion.SetTexture(unpackKernel, "heightTexture", map);

            var threadsPackingX = Mathf.CeilToInt(map.width / 32f);
            var threadsPackingY = Mathf.CeilToInt(map.height / 32f);

            // pack
            erosion.Dispatch(packKernel, threadsPackingX, threadsPackingY, 1);

            var erodeThreads = Mathf.CeilToInt(numErosionIterations / 1024f);
            erosion.Dispatch(erodeKernel, erodeThreads, 1, 1);

            // unpack
            erosion.Dispatch(unpackKernel, threadsPackingX, threadsPackingY, 1);

            // Release buffers
            mapBuffer.Release();
            randomIndexBuffer.Release();
            brushIndexBuffer.Release();
            brushWeightBuffer.Release();
        }
    }
}