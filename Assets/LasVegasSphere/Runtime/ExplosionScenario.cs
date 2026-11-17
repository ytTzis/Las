using UnityEngine;

public sealed class ExplosionScenario : ExperimentScenarioBase
{
    [SerializeField] private Transform building;
    [SerializeField] private Renderer explosionFlash;
    [SerializeField] private Light explosionLight;

    private Vector3 buildingPosition;
    private Vector3 buildingScale;
    private Quaternion buildingRotation;

    public void Configure(Transform targetBuilding, Renderer flash, Light flashLight)
    {
        building = targetBuilding;
        explosionFlash = flash;
        explosionLight = flashLight;
        buildingPosition = building.localPosition;
        buildingScale = building.localScale;
        buildingRotation = building.localRotation;
        ResetVisual();
    }

    protected override void ResetVisual()
    {
        if (building != null)
        {
            building.localPosition = buildingPosition;
            building.localScale = buildingScale;
            building.localRotation = buildingRotation;
        }
        if (explosionFlash != null) explosionFlash.transform.localScale = Vector3.one * 0.05f;
        if (explosionLight != null) explosionLight.intensity = 0f;
    }

    protected override void UpdateVisual(double relativeDspTime)
    {
        if (building == null || explosionFlash == null) return;
        if (relativeDspTime < 0.0)
        {
            ResetVisual();
            return;
        }

        float t = Mathf.Clamp01((float)relativeDspTime / 1.4f);
        float flash = Mathf.Exp(-(float)relativeDspTime * 6f);
        explosionFlash.transform.localScale = Vector3.one * Mathf.Lerp(0.05f, 5.2f, Mathf.Clamp01(t * 3f));
        Color color = Color.Lerp(new Color(1f, 0.08f, 0.01f), new Color(1f, 0.75f, 0.08f), flash);
        explosionFlash.material.color = color;
        if (explosionFlash.material.HasProperty("_EmissionColor"))
            explosionFlash.material.SetColor("_EmissionColor", color * (2f + flash * 6f));
        if (explosionLight != null) explosionLight.intensity = flash * 8f;

        building.localRotation = buildingRotation * Quaternion.Euler(t * 9f, 0f, t * 7f);
        building.localPosition = buildingPosition + new Vector3(t * 0.5f, -t * 1.4f, 0f);
        building.localScale = Vector3.Scale(buildingScale, new Vector3(1f + t * 0.1f, 1f - t * 0.65f, 1f));
    }
}
