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

        public static RenderTexture CreateNormalMapTexture(int width, int height)
        {
            var nm = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            nm.hideFlags = HideFlags.DontSave;
            nm.Create();
            return nm;
        }
    }
}