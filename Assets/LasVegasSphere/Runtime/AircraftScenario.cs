using UnityEngine;

public sealed class AircraftScenario : ExperimentScenarioBase
{
    [SerializeField] private Transform aircraft;
    [SerializeField] private Renderer overheadMarker;
    [SerializeField] private Vector3 startPoint = new Vector3(-24f, 12.5f, 10f);
    [SerializeField] private Vector3 overheadPoint = new Vector3(0f, 12.5f, 10f);
    [SerializeField] private Vector3 endPoint = new Vector3(24f, 12.5f, 10f);
    [SerializeField, Min(0.5f)] private float approachSeconds = 1.8f;
    [SerializeField, Min(0.5f)] private float departureSeconds = 1.8f;

    public void Configure(Transform aircraftTransform, Renderer marker)
    {
        aircraft = aircraftTransform;
        overheadMarker = marker;
        ResetVisual();
    }

    public override double ScheduleAudio(double audioDspTime)
    {
        // For the aircraft, the tested offset applies to the complete moving
        // sound track. At 0 ms the engine begins exactly when the aircraft
        // starts moving from the left side of the screen.
        return base.ScheduleAudio(audioDspTime - approachSeconds);
    }

    protected override void ResetVisual()
    {
        if (aircraft != null)
        {
            aircraft.localPosition = startPoint;
            OrientAlongRoute(startPoint, overheadPoint);
        }
        if (overheadMarker != null) overheadMarker.transform.localScale = Vector3.one * 0.08f;
    }

    protected override void UpdateVisual(double relativeDspTime)
    {
        if (aircraft == null || overheadMarker == null) return;
        if (relativeDspTime < 0.0)
        {
            float t = 1f - Mathf.Clamp01((float)(-relativeDspTime / approachSeconds));
            aircraft.localPosition = Vector3.Lerp(startPoint, overheadPoint, Smooth(t));
            OrientAlongRoute(startPoint, overheadPoint);
        }
        else
        {
            float t = Mathf.Clamp01((float)(relativeDspTime / departureSeconds));
            aircraft.localPosition = Vector3.Lerp(overheadPoint, endPoint, Smooth(t));
            OrientAlongRoute(overheadPoint, endPoint);
        }

        float pulse = Mathf.Exp(-Mathf.Abs((float)relativeDspTime) * 10f);
        overheadMarker.transform.localScale = Vector3.one * Mathf.Lerp(0.08f, 1.1f, pulse);
    }

    private void OrientAlongRoute(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        if (direction.sqrMagnitude > 0.001f) aircraft.localRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private static float Smooth(float value) { return value * value * (3f - 2f * value); }
}
