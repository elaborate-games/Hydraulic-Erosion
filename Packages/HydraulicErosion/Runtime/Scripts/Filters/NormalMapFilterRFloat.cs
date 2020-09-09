using Scripts;
using UnityEngine;

namespace TerrainTools
{
    /// <summary>
    /// the source texture is expected to be RFloat
    /// </summary>
    [System.Serializable]
    public struct NormalMapFilterRFloat
    {
        private static Material mat;

        [Range(0, 0.999f)]
        public float BumpEffect01;
        private static readonly int Factor = Shader.PropertyToID("_Factor");

        public void Apply(Texture source, RenderTexture target)
        {
            if (mat == null)
                mat = new Material(Shader.Find("Hidden/NormalMap"));

            var v = BumpEffect01 * 2f - 1f;
            var z = 1f - v;
            var xy = 1f + v;
            mat.SetVector(Factor, new Vector4(xy, xy, z, 1));

            Graphics.Blit(source, target, mat);
        }
    }
}