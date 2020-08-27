using UnityEditor;
using UnityEngine;

[CustomEditor (typeof (TerrainGenerator))]
public class MeshEditor : Editor {

    TerrainGenerator terrainGenerator;

    public override void OnInspectorGUI () {
        DrawDefaultInspector ();

        if (GUILayout.Button ("Generate Mesh")) {
            GenerateMesh();
        }

        string numIterationsString = terrainGenerator.numErosionIterations.ToString();
        if (terrainGenerator.numErosionIterations >= 1000) {
            numIterationsString = (terrainGenerator.numErosionIterations/1000) + "k";
        }

        if (GUILayout.Button("Generate Height"))
        {
            terrainGenerator.GenerateHeightMap();
            terrainGenerator.ContructMesh();
        }

        if (GUILayout.Button ("Erode (" + numIterationsString + " iterations)")) {
            var sw = new System.Diagnostics.Stopwatch ();
            
            sw.Start();
            terrainGenerator.Erode ();
            int erosionTimer = (int)sw.ElapsedMilliseconds;
            sw.Reset();


            if (terrainGenerator.printTimers) {
                // Debug.Log($"{terrainGenerator.mapSize}x{terrainGenerator.mapSize} heightmap generated in {heightMapTimer}ms");
                Debug.Log ($"{numIterationsString} erosion iterations completed in {erosionTimer}ms");
            }

        }
    }

    private void GenerateMesh()
    {
        terrainGenerator.GenerateHeightMap ();
        terrainGenerator.ContructMesh();
    }
    
    void OnEnable () {
        terrainGenerator = (TerrainGenerator) target;
        Tools.hidden = true;
        
        GenerateMesh();
    }

    void OnDisable () {
        Tools.hidden = false;
    }
}