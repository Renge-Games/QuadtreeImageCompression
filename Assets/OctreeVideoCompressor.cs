using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctreeVideoCompressor : MonoBehaviour {
	VideoToTexture2DList source;
	OctreeTexture2DArray octree;
	private void Start() {
		source = GetComponent<VideoToTexture2DList>();
		source.onConversionComplete += OnTextureListReceived;
	}

	void OnTextureListReceived(List<Texture2D> textures) {
		octree = new OctreeTexture2DArray();
		octree.InsertTextureArr(textures.ToArray(), 1);
	}
}
