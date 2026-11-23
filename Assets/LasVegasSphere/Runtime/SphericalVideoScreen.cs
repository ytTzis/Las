using System;
using UnityEngine;
using UnityEngine.Video;

public enum SphericalVideoProjection
{
    FillVisibleDome = 0,
    Equirectangular360 = 1,
    FulldomeFisheye180 = 2
}

[Serializable]
public sealed class ExperimentVideoCue
{
    public VideoClip clip;
    [Min(0f)] public float eventTimeSeconds;
    [Min(0f)] public float stopTimeSeconds;
    [Tooltip("Moves this clip inside the dome. Negative Y moves the picture upward toward screen centre.")]
    public Vector2 framingOffset;

    public ExperimentVideoCue(VideoClip source, float eventTime, float stopTime, Vector2 offset)
    {
        clip = source;
        eventTimeSeconds = eventTime;
        stopTimeSeconds = stopTime;
        framingOffset = offset;
    }
}

/// <summary>
/// Presents scene-specific video on the spherical LED surface. Visual frames
/// and the embedded soundtrack use separate VideoPlayers so the soundtrack can
/// receive the experiment's -100..+100 ms offset without moving the picture.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(VideoPlayer))]
public sealed class SphericalVideoScreen : MonoBehaviour
{
    [Header("Experiment Videos")]
    [SerializeField] private ExperimentVideoCue clap = new ExperimentVideoCue(null, 0.40f, 0.82f, new Vector2(0.16f, 0.20f));
    [SerializeField] private ExperimentVideoCue explosion = new ExperimentVideoCue(null, 1.36f, 3.86f, Vector2.zero);
    [SerializeField] private ExperimentVideoCue aircraft = new ExperimentVideoCue(null, 1.00f, 2.10f, Vector2.zero);

    [Header("Playback")]
    [SerializeField] private bool useEmbeddedAudioForExperiment = true;
    [SerializeField] private bool flipVertically;
    [SerializeField, Range(0f, 4f)] private float videoBrightness = 1f;

    [Header("Projection")]
    [Tooltip("These three supplied clips are ordinary 16:9 video, so use Fill Visible Dome.")]
    [SerializeField] private SphericalVideoProjection projection = SphericalVideoProjection.FillVisibleDome;
    [SerializeField, Range(30f, 180f)] private float visibleDomeAngle = 77.37f;

    [Header("Render Texture")]
    [SerializeField] private int textureWidth = 1920;
    [SerializeField] private int textureHeight = 1080;

    private static readonly int VideoTextureId = Shader.PropertyToID("_VideoTex");
    private static readonly int UseVideoId = Shader.PropertyToID("_UseVideo");
    private static readonly int VideoBrightnessId = Shader.PropertyToID("_VideoBrightness");
    private static readonly int FlipVideoYId = Shader.PropertyToID("_FlipVideoY");
    private static readonly int VideoProjectionModeId = Shader.PropertyToID("_VideoProjectionMode");
    private static readonly int VisibleDomeAngleId = Shader.PropertyToID("_VisibleDomeAngle");
    private static readonly int VideoFramingOffsetId = Shader.PropertyToID("_VideoFramingOffset");

    private VideoPlayer visualPlayer;
    private VideoPlayer audioPlayer;
    private Material screenMaterial;
    private RenderTexture videoTexture;
    private ExperimentSceneType selectedScene;
    private ExperimentVideoCue activeCue;
    private AudioSource activeAudioSource;
    private bool trialScheduled;
    private bool visualStarted;
    private bool audioStarted;
    private bool visualFinished;
    private bool audioFinished;
    private double visualStartDspTime;
    private double audioStartDspTime;
    private double scheduledStopTime;

    public VideoPlayer Player => visualPlayer;
    public bool HasVideo => activeCue != null && activeCue.clip != null;

    public void ConfigureExperimentVideos(VideoClip clapClip, VideoClip explosionClip, VideoClip aircraftClip)
    {
        clap = new ExperimentVideoCue(clapClip, 0.40f, 0.82f, new Vector2(0.16f, 0.20f));
        explosion = new ExperimentVideoCue(explosionClip, 1.36f, 3.86f, Vector2.zero);
        aircraft = new ExperimentVideoCue(aircraftClip, 1.00f, 2.10f, Vector2.zero);
    }

    public bool HasSceneVideo(ExperimentSceneType scene)
    {
        ExperimentVideoCue cue = GetCue(scene);
        return cue != null && cue.clip != null;
    }

    public bool HasEmbeddedAudio(ExperimentSceneType scene)
    {
        ExperimentVideoCue cue = GetCue(scene);
        return useEmbeddedAudioForExperiment && cue != null && cue.clip != null && cue.clip.audioTrackCount > 0;
    }

    private void Awake()
    {
        visualPlayer = GetComponent<VideoPlayer>();
        audioPlayer = gameObject.AddComponent<VideoPlayer>();
        screenMaterial = GetComponent<Renderer>().material;
        CreateTargetTexture();
        ConfigurePlayers();
        SetVideoVisible(false);
    }

    private void Update()
    {
        if (!trialScheduled || activeCue == null || activeCue.clip == null) return;
        double dspTime = AudioSettings.dspTime;
        UpdateVisualClock(dspTime);
        UpdateAudioClock(dspTime);
        if (visualFinished && audioFinished) trialScheduled = false;
    }

    public void SelectScene(ExperimentSceneType scene)
    {
        ExperimentVideoCue requestedCue = GetCue(scene);
        // Seat changes call ExperimentManager.SelectCondition again with the
        // same scene. Keep both players and their DSP clocks running in that
        // case; only a real Clap/Explosion/Aircraft change reloads the media.
        if (activeCue != null && selectedScene == scene && activeCue == requestedCue)
            return;

        selectedScene = scene;
        activeCue = requestedCue;
        ApplyCueFraming();
        CancelPlayback();
        ConfigureSources();
        if (HasVideo)
        {
            visualPlayer.Prepare();
            if (HasEmbeddedAudio(scene)) audioPlayer.Prepare();
        }
    }

    public double ScheduleTrial(ExperimentSceneType scene, double eventDspTime,
        int offsetMilliseconds, AudioSource spatialAudioSource, bool playFullClip)
    {
        if (scene != selectedScene || activeCue != GetCue(scene)) SelectScene(scene);
        if (!HasVideo) return eventDspTime;

        activeAudioSource = spatialAudioSource;
        visualStartDspTime = eventDspTime - activeCue.eventTimeSeconds;
        audioStartDspTime = visualStartDspTime + offsetMilliseconds / 1000.0;
        scheduledStopTime = playFullClip ? activeCue.clip.length : GetStopTime(activeCue);
        trialScheduled = true;
        visualStarted = false;
        audioStarted = false;
        visualFinished = false;
        audioFinished = !HasEmbeddedAudio(scene);

        visualPlayer.Stop();
        visualPlayer.timeReference = VideoTimeReference.ExternalTime;
        visualPlayer.externalReferenceTime = 0.0;
        visualPlayer.time = 0.0;
        if (!visualPlayer.isPrepared) visualPlayer.Prepare();

        if (!audioFinished)
        {
            audioPlayer.Stop();
            audioPlayer.timeReference = VideoTimeReference.ExternalTime;
            audioPlayer.externalReferenceTime = 0.0;
            audioPlayer.time = 0.0;
            audioPlayer.controlledAudioTrackCount = 1;
            audioPlayer.EnableAudioTrack(0, true);
            audioPlayer.SetTargetAudioSource(0, activeAudioSource);
            if (!audioPlayer.isPrepared) audioPlayer.Prepare();
        }
        SetVideoVisible(false);
        double visualEnd = visualStartDspTime + scheduledStopTime;
        double audioEnd = audioFinished ? visualEnd : audioStartDspTime + scheduledStopTime;
        return Math.Max(visualEnd, audioEnd);
    }

    public void StopVideo()
    {
        CancelPlayback();
        SetVideoVisible(false);
    }

    private void UpdateVisualClock(double dspTime)
    {
        double contentTime = dspTime - visualStartDspTime;
        if (!visualStarted && contentTime >= 0.0 && visualPlayer.isPrepared)
        {
            visualPlayer.externalReferenceTime = contentTime;
            visualPlayer.time = contentTime;
            visualPlayer.Play();
            visualStarted = true;
            SetVideoVisible(true);
        }
        if (!visualStarted) return;
        visualPlayer.externalReferenceTime = Math.Max(0.0, contentTime);
        if (contentTime >= scheduledStopTime)
        {
            visualPlayer.Stop();
            visualFinished = true;
            SetVideoVisible(false);
        }
    }

    private void UpdateAudioClock(double dspTime)
    {
        if (audioFinished) return;
        double contentTime = dspTime - audioStartDspTime;
        if (!audioStarted && contentTime >= 0.0 && audioPlayer.isPrepared)
        {
            audioPlayer.externalReferenceTime = contentTime;
            audioPlayer.time = contentTime;
            audioPlayer.Play();
            audioStarted = true;
        }
        if (!audioStarted) return;
        audioPlayer.externalReferenceTime = Math.Max(0.0, contentTime);
        if (contentTime >= scheduledStopTime)
        {
            audioPlayer.Stop();
            audioFinished = true;
        }
    }

    private void ConfigurePlayers()
    {
        visualPlayer.playOnAwake = false;
        visualPlayer.isLooping = false;
        visualPlayer.skipOnDrop = false;
        visualPlayer.renderMode = VideoRenderMode.RenderTexture;
        visualPlayer.targetTexture = videoTexture;
        visualPlayer.audioOutputMode = VideoAudioOutputMode.None;
        visualPlayer.errorReceived += HandleError;

        audioPlayer.playOnAwake = false;
        audioPlayer.isLooping = false;
        audioPlayer.skipOnDrop = false;
        audioPlayer.renderMode = VideoRenderMode.APIOnly;
        audioPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        audioPlayer.errorReceived += HandleError;
    }

    private void ConfigureSources()
    {
        VideoClip clip = activeCue != null ? activeCue.clip : null;
        visualPlayer.clip = clip;
        audioPlayer.clip = clip;
    }

    private ExperimentVideoCue GetCue(ExperimentSceneType scene)
    {
        if (scene == ExperimentSceneType.Clap) return clap;
        if (scene == ExperimentSceneType.Explosion) return explosion;
        return aircraft;
    }

    private static double GetStopTime(ExperimentVideoCue cue)
    {
        if (cue == null || cue.clip == null) return 0.0;
        if (cue.stopTimeSeconds <= 0f) return cue.clip.length;
        return Math.Min(cue.stopTimeSeconds, cue.clip.length);
    }

    private void CancelPlayback()
    {
        trialScheduled = false;
        visualStarted = false;
        audioStarted = false;
        visualFinished = true;
        audioFinished = true;
        if (visualPlayer != null) visualPlayer.Stop();
        if (audioPlayer != null) audioPlayer.Stop();
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
        screenMaterial.SetVector(VideoFramingOffsetId, Vector4.zero);
    }

    private void ApplyCueFraming()
    {
        if (screenMaterial != null)
        {
            Vector2 offset = activeCue != null ? activeCue.framingOffset : Vector2.zero;
            screenMaterial.SetVector(VideoFramingOffsetId, new Vector4(offset.x, offset.y, 0f, 0f));
        }
    }

    private void HandleError(VideoPlayer source, string message)
    {
        Debug.LogError("Spherical experiment video could not be played: " + message, this);
    }

    private void SetVideoVisible(bool visible)
    {
        if (screenMaterial != null)
            screenMaterial.SetFloat(UseVideoId, visible ? 1f : 0f);
    }

    private void OnDestroy()
    {
        if (visualPlayer != null) visualPlayer.errorReceived -= HandleError;
        if (audioPlayer != null) audioPlayer.errorReceived -= HandleError;
        if (videoTexture != null)
        {
            videoTexture.Release();
            Destroy(videoTexture);
        }
        if (screenMaterial != null) Destroy(screenMaterial);
    }
}
