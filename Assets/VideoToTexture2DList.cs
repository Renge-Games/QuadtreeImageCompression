using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoToTexture2DList : MonoBehaviour {
	public Material renderMaterial;
	List<Texture2D> textures;
	VideoPlayer videoPlayer;
	bool done = false;

	public event Action<List<Texture2D>> onConversionComplete; 

	private void Start() {
		textures = new List<Texture2D>();
		videoPlayer = GetComponent<VideoPlayer>();
		videoPlayer.Stop();
		videoPlayer.renderMode = VideoRenderMode.APIOnly;
		videoPlayer.prepareCompleted += Prepared;
		videoPlayer.sendFrameReadyEvents = true;
		videoPlayer.frameReady += FrameReady;
		videoPlayer.Prepare();
	}

	private void Update() {
		if (videoPlayer.isPrepared && !videoPlayer.isPlaying) {
			videoPlayer.Play();
		}
		if (done && videoPlayer.isPlaying) {
			videoPlayer.Stop();
			onConversionComplete?.Invoke(textures);
		}
	}

	void Prepared(VideoPlayer vp) => vp.Pause();

	private void FrameReady(VideoPlayer source, long frameIndex) {

		RenderTexture renderTexture = source.texture as RenderTexture;
		Texture2D videoFrame = new Texture2D(renderTexture.width, renderTexture.height);

		if (videoFrame.width != renderTexture.width || videoFrame.height != renderTexture.height) {
			videoFrame.Resize(renderTexture.width, renderTexture.height);
		}
		RenderTexture.active = renderTexture;
		videoFrame.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
		videoFrame.Apply();
		RenderTexture.active = null;

		textures.Add(videoFrame);
		if(renderMaterial != null)
			renderMaterial.SetTexture(Shader.PropertyToID("_MainTex"), videoFrame);
		Debug.Log("Frame Ready " + frameIndex);
		if (frameIndex == (long)(source.frameCount - 1)) {
			done = true;
		}
	}
}
