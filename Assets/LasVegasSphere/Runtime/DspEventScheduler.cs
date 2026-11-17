using System;
using UnityEngine;

/// <summary>
/// Schedules every key sound against the same DSP clock. Seat position never
/// changes the scheduled onset; it only changes spatial direction.
/// </summary>
public sealed class DspEventScheduler : MonoBehaviour
{
    public static readonly int[] OffsetsMilliseconds = { -100, -50, 0, 50, 100 };

    [SerializeField, Min(0.5f)] private float scheduleLeadSeconds = 2.2f;

    public double LastVisualDspTime { get; private set; }
    public double LastAudioDspTime { get; private set; }
    public int LastOffsetMilliseconds { get; private set; }

    public double Schedule(ExperimentScenarioBase scenario, int offsetIndex)
    {
        if (scenario == null) throw new ArgumentNullException("scenario");
        offsetIndex = Mathf.Clamp(offsetIndex, 0, OffsetsMilliseconds.Length - 1);

        double now = AudioSettings.dspTime;
        double minimumLead = Math.Abs(OffsetsMilliseconds[0]) / 1000.0 + 0.25;
        LastVisualDspTime = now + Math.Max(scheduleLeadSeconds, minimumLead);
        LastOffsetMilliseconds = OffsetsMilliseconds[offsetIndex];
        double requestedAudioDspTime = LastVisualDspTime + LastOffsetMilliseconds / 1000.0;

        scenario.PrepareTrial();
        scenario.ScheduleVisual(LastVisualDspTime);
        LastAudioDspTime = scenario.ScheduleAudio(requestedAudioDspTime);

        Debug.LogFormat(
            "Trial scheduled | scene={0} | seat-independent visual={1:F6} | audio={2:F6} | offset={3:+#;-#;0} ms",
            scenario.SceneType, LastVisualDspTime, LastAudioDspTime, LastOffsetMilliseconds);
        return LastVisualDspTime;
    }
}
