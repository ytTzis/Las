using System;
using UnityEngine;

/// <summary>
/// DSP-clocked audio/visual simultaneity experiment.
/// Audio is scheduled with PlayScheduled; visual motion samples the same DSP clock.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public sealed class AudioVisualSyncExperiment : MonoBehaviour
{
    public static readonly int[] OffsetsMilliseconds = { -100, -50, 0, 50, 100 };

    [Header("Exact Inspector values")]
    [SerializeField] private AudioClip impactClip;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;
    [SerializeField] private Renderer impactOrb;
    [SerializeField, Range(0, 4)] private int offsetIndex = 2;
    [SerializeField, Min(0.25f)] private float scheduleLeadSeconds = 0.8f;
    [SerializeField, Min(0.1f)] private float approachSeconds = 0.55f;
    [SerializeField, Min(0.1f)] private float recoverySeconds = 0.45f;

    private AudioSource source;
    private double eventDspTime = double.NegativeInfinity;
    private double lastAutoDspTime;
    private bool autoCycle;
    private Vector3 leftOuter;
    private Vector3 rightOuter;
    private Vector3 leftImpact;
    private Vector3 rightImpact;
    private Color orbBaseColor = new Color(0.05f, 0.25f, 0.4f, 1f);

    public int CurrentOffsetMilliseconds { get { return OffsetsMilliseconds[offsetIndex]; } }

    public void Configure(Transform left, Transform right, Renderer orb)
    {
        leftHand = left;
        rightHand = right;
        impactOrb = orb;
        CachePose();
    }

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        if (impactClip == null) impactClip = CreateFallbackClick();
        source.clip = impactClip;
        CachePose();
    }

    private void Start() { lastAutoDspTime = AudioSettings.dspTime; }

    private void CachePose()
    {
        if (leftHand == null || rightHand == null) return;
        leftOuter = leftHand.localPosition;
        rightOuter = rightHand.localPosition;
        leftImpact = new Vector3(-0.38f, 0f, 0f);
        rightImpact = new Vector3(0.38f, 0f, 0f);
    }

    private void Update()
    {
        ReadControls();
        double now = AudioSettings.dspTime;
        if (autoCycle && now - lastAutoDspTime >= 2.2)
        {
            offsetIndex = (offsetIndex + 1) % OffsetsMilliseconds.Length;
            ScheduleTrial();
            lastAutoDspTime = now;
        }
        UpdateVisuals(now);
    }

    private void ReadControls()
    {
        for (int i = 0; i < OffsetsMilliseconds.Length; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) offsetIndex = i;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) offsetIndex = Mathf.Max(0, offsetIndex - 1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) offsetIndex = Mathf.Min(4, offsetIndex + 1);
        if (Input.GetKeyDown(KeyCode.Space)) ScheduleTrial();
        if (Input.GetKeyDown(KeyCode.A))
        {
            autoCycle = !autoCycle;
            lastAutoDspTime = AudioSettings.dspTime - 3.0;
        }
    }

    public void ScheduleTrial()
    {
        double now = AudioSettings.dspTime;
        double minimumLead = Math.Abs(OffsetsMilliseconds[0]) / 1000.0 + 0.15;
        eventDspTime = now + Math.Max(scheduleLeadSeconds, minimumLead);
        double audioDspTime = eventDspTime + CurrentOffsetMilliseconds / 1000.0;
        source.Stop();
        source.clip = impactClip;
        source.PlayScheduled(audioDspTime);
        Debug.LogFormat("Trial scheduled: visual={0:F6}, audio={1:F6}, offset={2:+#;-#;0} ms",
            eventDspTime, audioDspTime, CurrentOffsetMilliseconds);
    }

    private void UpdateVisuals(double now)
    {
        if (leftHand == null || rightHand == null || double.IsNegativeInfinity(eventDspTime)) return;
        double relative = now - eventDspTime;
        float amount;
        if (relative < 0.0)
        {
            amount = 1f - Mathf.Clamp01((float)(-relative / approachSeconds));
            amount = Smooth(amount);
        }
        else amount = 1f - Smooth(Mathf.Clamp01((float)(relative / recoverySeconds)));

        leftHand.localPosition = Vector3.Lerp(leftOuter, leftImpact, amount);
        rightHand.localPosition = Vector3.Lerp(rightOuter, rightImpact, amount);
        float pulse = Mathf.Exp(-Mathf.Abs((float)relative) * 22f);
        impactOrb.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.45f, pulse);
        Color c = Color.Lerp(orbBaseColor, new Color(0.2f, 1f, 1f, 1f), pulse);
        impactOrb.material.color = c;
        if (impactOrb.material.HasProperty("_EmissionColor"))
            impactOrb.material.SetColor("_EmissionColor", c * (0.5f + pulse * 5f));
    }

    private static float Smooth(float x) { return x * x * (3f - 2f * x); }

    private static AudioClip CreateFallbackClick()
    {
        const int sampleRate = 48000;
        const float duration = 0.09f;
        int count = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Exp(-t * 55f);
            float body = Mathf.Sin(2f * Mathf.PI * 820f * t) * 0.55f;
            float snap = Mathf.Sin(2f * Mathf.PI * 2400f * t) * 0.25f;
            samples[i] = (body + snap) * envelope;
        }
        AudioClip clip = AudioClip.Create("GeneratedImpactClick_48k", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(22, 22, 440, 154), "LAS VEGAS SPHERE · AUDIO/VISUAL SYNC");
        GUI.Label(new Rect(40, 52, 390, 24), "1–5 / ← → : choose offset    SPACE : schedule trial");
        GUI.Label(new Rect(40, 78, 390, 24), "A : auto-cycle all offsets");
        GUI.Label(new Rect(40, 106, 390, 26), "Selected audio offset: " + CurrentOffsetMilliseconds.ToString("+0;-0;0") + " ms");
        GUI.Label(new Rect(40, 132, 390, 24), "Negative = sound before visual impact    Auto: " + (autoCycle ? "ON" : "OFF"));
    }
}
