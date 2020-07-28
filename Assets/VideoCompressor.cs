using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public static class TextureExtentions {
    public static Texture2D ToTexture2D(this Texture texture) {
        return Texture2D.CreateExternalTexture(
            texture.width,
            texture.height,
            TextureFormat.RGB24,
            false, false,
            texture.GetNativeTexturePtr());
    }
}

public class VideoCompressor : MonoBehaviour {
    public Material mat;
    List<Texture2D> textures;
    VideoPlayer videoPlayer;
    bool done = false;
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
        if(done) {
            videoPlayer.Stop();
        }
    }

    void Prepared(VideoPlayer vp) => vp.Pause();

    private void FrameReady(VideoPlayer source, long frameIndex) {
        mat.SetTexture(Shader.PropertyToID("_MainTex"), source.texture);
        textures.Add(source.texture.ToTexture2D());
        Debug.Log("Frame Ready " + frameIndex);
        if(frameIndex == (long)(source.frameCount - 1)) {
            done = true;
        }
        source.frame = frameIndex + 1;
    }
}
