using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctreeVideoCompressor : MonoBehaviour {
	VideoToTexture2DList source;

	private void Start() {
		source = GetComponent<VideoToTexture2DList>();
		source.onConversionComplete += OnTextureListReceived;
	}

	void OnTextureListReceived(List<Texture2D> textures) {
		//TODO
	}
}
