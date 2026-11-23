using UnityEngine;

public abstract class ExperimentScenarioBase : MonoBehaviour
{
    [SerializeField] private ExperimentSceneType sceneType;
    [SerializeField] private AudioSource eventAudioSource;
    [SerializeField] private AudioClip eventClip;
    [SerializeField] private Transform focusPoint;

    protected double EventDspTime = double.NegativeInfinity;
    protected bool TrialScheduled;

    public ExperimentSceneType SceneType { get { return sceneType; } }
    public Transform FocusPoint { get { return focusPoint != null ? focusPoint : transform; } }
    public AudioSource EventAudioSource { get { return eventAudioSource; } }
    protected bool UsesExternalMediaAudio { get; private set; }

    public void ConfigureBase(ExperimentSceneType type, AudioSource source, Transform focus)
    {
        sceneType = type;
        eventAudioSource = source;
        focusPoint = focus;
        ConfigureSpatialAudio();
    }

    public virtual void PrepareTrial()
    {
        TrialScheduled = false;
        EventDspTime = double.NegativeInfinity;
        ResetVisual();
        ConfigureSpatialAudio();
        if (eventAudioSource != null) eventAudioSource.Stop();
    }

    public void ScheduleVisual(double eventDspTime)
    {
        EventDspTime = eventDspTime;
        TrialScheduled = true;
        OnTrialScheduled();
    }

    public virtual double ScheduleAudio(double audioDspTime)
    {
        ConfigureSpatialAudio();
        if (UsesExternalMediaAudio) return audioDspTime;
        if (eventAudioSource == null) return audioDspTime;
        if (eventClip == null) eventClip = CreateFallbackClip(sceneType);
        eventAudioSource.clip = eventClip;
        eventAudioSource.PlayScheduled(audioDspTime);
        return audioDspTime;
    }

    public void SetEventClip(AudioClip clip) { eventClip = clip; }

    public void SetExternalMediaAudio(bool enabled)
    {
        bool changed = UsesExternalMediaAudio != enabled;
        UsesExternalMediaAudio = enabled;
        // Seat changes re-apply the same condition so the AudioListener can
        // move. Do not stop an already scheduled WAV unless the audio mode
        // itself actually changed.
        if (changed && enabled && eventAudioSource != null) eventAudioSource.Stop();
    }

    public void SetPlaceholderVisualsVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = visible;
        Light[] lights = GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++) lights[i].enabled = visible;
    }

    protected virtual void Update()
    {
        if (TrialScheduled) UpdateVisual(AudioSettings.dspTime - EventDspTime);
    }

    protected virtual void OnTrialScheduled() { }
    protected abstract void ResetVisual();
    protected abstract void UpdateVisual(double relativeDspTime);

    private void ConfigureSpatialAudio()
    {
        if (eventAudioSource == null) return;
        eventAudioSource.playOnAwake = false;
        eventAudioSource.spatialBlend = 1f;
        eventAudioSource.dopplerLevel = 0f;
        eventAudioSource.rolloffMode = AudioRolloffMode.Custom;
        eventAudioSource.minDistance = 1f;
        eventAudioSource.maxDistance = 100f;
        eventAudioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
            AnimationCurve.Linear(0f, 1f, 1f, 1f));
    }

    private static AudioClip CreateFallbackClip(ExperimentSceneType type)
    {
        const int sampleRate = 48000;
        float duration = type == ExperimentSceneType.Explosion ? 0.75f :
            type == ExperimentSceneType.Aircraft ? 4.0f : 0.09f;
        int count = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)sampleRate;
            if (type == ExperimentSceneType.Clap)
            {
                float envelope = Mathf.Exp(-t * 55f);
                samples[i] = (Mathf.Sin(2f * Mathf.PI * 820f * t) * 0.55f +
                              Mathf.Sin(2f * Mathf.PI * 2400f * t) * 0.25f) * envelope;
            }
            else if (type == ExperimentSceneType.Explosion)
            {
                float envelope = Mathf.Exp(-t * 4.8f);
                float rumble = Mathf.Sin(2f * Mathf.PI * 58f * t) +
                               Mathf.Sin(2f * Mathf.PI * 83f * t) * 0.45f;
                float deterministicNoise = Mathf.Sin(i * 12.9898f) * Mathf.Sin(i * 0.071f);
                samples[i] = (rumble * 0.32f + deterministicNoise * 0.18f) * envelope;
            }
            else
            {
                float fadeIn = Mathf.Clamp01(t * 8f);
                float fadeOut = Mathf.Clamp01((duration - t) * 3f);
                float engine = Mathf.Sin(2f * Mathf.PI * 105f * t) * 0.35f +
                               Mathf.Sin(2f * Mathf.PI * 210f * t) * 0.14f;
                samples[i] = engine * fadeIn * fadeOut;
            }
        }

        AudioClip clip = AudioClip.Create("Generated_" + type + "_48k", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
