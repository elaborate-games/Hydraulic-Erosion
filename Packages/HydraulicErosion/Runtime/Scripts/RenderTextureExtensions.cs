using UnityEngine;

namespace Scripts
{
    public static class RenderTextureExtensions
    {
        public static void Ensure(this RenderTexture tex, ref RenderTexture reference, int width, int height, 
            int depth, RenderTextureFormat format, RenderTextureReadWrite space, FilterMode filter, bool randomWrite = false)
        {
            if (reference == null || 
                !reference.IsCreated() || 
                reference.width != width || 
                reference.height != height || 
                reference.depth != depth || 
                reference.format != format || 
                (reference.sRGB && space != RenderTextureReadWrite.sRGB) || 
                reference.filterMode != filter ||
                randomWrite != reference.enableRandomWrite)
            { 
                if(reference && reference.IsCreated()) reference.Release();
                Debug.Log("Create new RenderTexture");
                reference = new RenderTexture(width, height, depth, format, space);
                reference.filterMode = filter;
                reference.enableRandomWrite = randomWrite;
                reference.Create();
            }
        }
    }
}