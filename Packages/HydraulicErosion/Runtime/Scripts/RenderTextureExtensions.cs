using UnityEngine;

namespace Scripts
{
	public static class RenderTextureExtensions
	{
		public static void Ensure(this RenderTexture tex, ref RenderTexture reference, RenderTexture other)
		{
			if (other)
				tex.Ensure(ref reference,
					other.width,
					other.height,
					other.depth,
					other.format,
					other.sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear,
					other.filterMode,
					other.enableRandomWrite
				);
		}

		public static void Ensure(this RenderTexture tex, ref RenderTexture reference, int width, int height,
			int depth, RenderTextureFormat format, RenderTextureReadWrite space, FilterMode filter, bool randomWrite = false)
		{
			if (format == RenderTextureFormat.Default) format = RenderTextureFormat.ARGB32;

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
				// Debug.Log(format + " + " + reference.format);
				if (reference && reference.IsCreated()) reference.Release();
				Debug.Log("Create new RenderTexture: " + reference, tex);
				reference = new RenderTexture(width, height, depth, format, space);
				reference.filterMode = filter;
				reference.enableRandomWrite = randomWrite;
				reference.Create();
			}
		}
	}
}