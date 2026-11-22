using UnityEngine;
using UnityEngine.Video;

public enum SphericalVideoProjection
{
    FillVisibleDome = 0,
    Equirectangular360 = 1,
    FulldomeFisheye180 = 2
}

/// <summary>
/// Sends a VideoClip (or URL) to the complete spherical LED material.
/// With no video assigned, the material keeps showing its animated test pattern.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(VideoPlayer))]
public sealed class SphericalVideoScreen : MonoBehaviour
{
    [Header("Video Content (optional for now)")]
    [Tooltip("Import an MP4 into Assets, then drag its VideoClip here.")]
    [SerializeField] private VideoClip videoClip;

    [Tooltip("Optional alternative to Video Clip. A local file or web URL can be used.")]
    [SerializeField] private string videoUrl;

    [Header("Playback")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool flipVertically;
    [SerializeField, Range(0f, 4f)] private float videoBrightness = 1f;

    [Header("Projection")]
    [Tooltip("Normal video: Fill Visible Dome. 2:1 360 video: Equirectangular 360. Square dome master: Fulldome Fisheye 180.")]
    [SerializeField] private SphericalVideoProjection projection = SphericalVideoProjection.FillVisibleDome;

    [Tooltip("Polar angle from the zenith to the floor edge of this venue screen.")]
    [SerializeField, Range(30f, 180f)] private float visibleDomeAngle = 77.37f;

    [Header("Render Texture")]
    [Tooltip("A 2:1 texture is recommended for equirectangular 360-degree video.")]
    [SerializeField] private int textureWidth = 2048;
    [SerializeField] private int textureHeight = 1024;

    [Header("Audio")]
    [Tooltip("Keep this off for the synchronization experiment so its separately scheduled AudioSource remains authoritative.")]
    [SerializeField] private bool playEmbeddedVideoAudio;

    private static readonly int VideoTextureId = Shader.PropertyToID("_VideoTex");
    private static readonly int UseVideoId = Shader.PropertyToID("_UseVideo");
    private static readonly int VideoBrightnessId = Shader.PropertyToID("_VideoBrightness");
    private static readonly int FlipVideoYId = Shader.PropertyToID("_FlipVideoY");
    private static readonly int VideoProjectionModeId = Shader.PropertyToID("_VideoProjectionMode");
    private static readonly int VisibleDomeAngleId = Shader.PropertyToID("_VisibleDomeAngle");

    private VideoPlayer player;
    private Material screenMaterial;
    private RenderTexture videoTexture;

    public VideoPlayer Player => player;
    public bool HasVideo => videoClip != null || !string.IsNullOrWhiteSpace(videoUrl);

    private void Awake()
    {
        player = GetComponent<VideoPlayer>();
        screenMaterial = GetComponent<Renderer>().material;
        CreateTargetTexture();
        ConfigurePlayer();
        SetVideoVisible(false);

        if (playOnStart && HasVideo)
        {
            PrepareAndPlay();
        }
    }

    public void SetVideo(VideoClip clip, bool playImmediately = true)
    {
        videoClip = clip;
        videoUrl = string.Empty;
        if (player == null) return;

        player.Stop();
        ConfigureSource();
        SetVideoVisible(false);
        if (playImmediately && HasVideo) PrepareAndPlay();
    }

    public void PrepareAndPlay()
    {
        if (!HasVideo || player == null) return;
        ConfigureSource();
        player.Prepare();
    }

    public void Pause()
    {
        if (player != null && player.isPlaying) player.Pause();
    }

    public void StopVideo()
    {
        if (player != null) player.Stop();
        SetVideoVisible(false);
    }

    private void CreateTargetTexture()
    {
        int width = Mathf.Clamp(textureWidth, 256, 4096);
        int height = Mathf.Clamp(textureHeight, 128, 4096);
        videoTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = "Runtime Spherical Video Texture",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            useMipMap = false
        };
        videoTexture.Create();
        screenMaterial.SetTexture(VideoTextureId, videoTexture);
        screenMaterial.SetFloat(VideoBrightnessId, videoBrightness);
        screenMaterial.SetFloat(FlipVideoYId, flipVertically ? 1f : 0f);
        screenMaterial.SetFloat(VideoProjectionModeId, (float)projection);
        screenMaterial.SetFloat(VisibleDomeAngleId, visibleDomeAngle);
    }

    private void ConfigurePlayer()
    {
        player.playOnAwake = false;
        player.isLooping = loop;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = videoTexture;
        player.audioOutputMode = playEmbeddedVideoAudio
            ? VideoAudioOutputMode.Direct
            : VideoAudioOutputMode.None;
        player.prepareCompleted += HandlePrepared;
        player.errorReceived += HandleError;
        ConfigureSource();
    }

    private void ConfigureSource()
    {
        if (videoClip != null)
        {
            player.source = VideoSource.VideoClip;
            player.clip = videoClip;
        }
        else
        {
            player.source = VideoSource.Url;
            player.url = videoUrl ?? string.Empty;
        }
    }

    private void HandlePrepared(VideoPlayer source)
    {
        SetVideoVisible(true);
        source.Play();
    }

    private void HandleError(VideoPlayer source, string message)
    {
        SetVideoVisible(false);
        Debug.LogError("Spherical video could not be played: " + message, this);
    }

    private void SetVideoVisible(bool visible)
    {
        if (screenMaterial != null)
            screenMaterial.SetFloat(UseVideoId, visible ? 1f : 0f);
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.prepareCompleted -= HandlePrepared;
            player.errorReceived -= HandleError;
        }
        if (videoTexture != null)
        {
            videoTexture.Release();
            Destroy(videoTexture);
        }
        if (screenMaterial != null) Destroy(screenMaterial);
    }
}
