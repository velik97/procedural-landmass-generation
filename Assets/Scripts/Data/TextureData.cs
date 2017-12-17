using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class TextureData : UpdatableData {

	const int textureSize = 512;
	const TextureFormat textureFormat = TextureFormat.RGB565;

	public Layer[] layers;

	public void ApplyToMaterial (Material material) {

		material.SetInt ("layerCount", layers.Length);
		material.SetColorArray ("baseColors", layers.Select (x => x.tint).ToArray ());
		material.SetFloatArray ("baseStartHeights", layers.Select (x => x.startHeight).ToArray ());
		material.SetFloatArray ("baseBlends", layers.Select (x => x.blendStrength).ToArray ());
		material.SetFloatArray ("baseColorStrengths", layers.Select (x => x.tintStrength).ToArray ());
		material.SetFloatArray ("baseTextureScales", layers.Select (x => x.textureScale).ToArray ());

	}

	public void UpdateMeshHeights (Material material, float minHeight, float maxHeight) {

		material.SetFloat ("minHeight", minHeight);
		material.SetFloat ("maxHeight", maxHeight);
	}

//	Texture2DArray GenerateTextureArray (Texture[] textures) {
//		Texture2DArray textureArray = new Texture2DArray (textureSize, textureSize, textures.Length, textureFormat, true);
//		for
//	}

	[System.Serializable]
	public struct Layer {
		
		public Texture texture;
		public Color tint;
		[Range(0,1)]
		public float tintStrength;
		[Range(0,1)]
		public float startHeight;
		[Range(0,1)]
		public float blendStrength;
		public float textureScale;
	}
}
