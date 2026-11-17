using UnityEngine;

public sealed class ClapScenario : ExperimentScenarioBase
{
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;
    [SerializeField] private Renderer impactFlash;
    [SerializeField, Min(0.1f)] private float approachSeconds = 0.55f;
    [SerializeField, Min(0.1f)] private float recoverySeconds = 0.45f;

    private Vector3 leftOuter;
    private Vector3 rightOuter;
    private readonly Vector3 leftImpact = new Vector3(-0.38f, 0f, 0f);
    private readonly Vector3 rightImpact = new Vector3(0.38f, 0f, 0f);

    public void Configure(Transform left, Transform right, Renderer flash)
    {
        leftHand = left;
        rightHand = right;
        impactFlash = flash;
        leftOuter = left.localPosition;
        rightOuter = right.localPosition;
        ResetVisual();
    }

    protected override void ResetVisual()
    {
        if (leftHand != null) leftHand.localPosition = leftOuter;
        if (rightHand != null) rightHand.localPosition = rightOuter;
        if (impactFlash != null) impactFlash.transform.localScale = Vector3.one * 0.2f;
    }

    protected override void UpdateVisual(double relativeDspTime)
    {
        if (leftHand == null || rightHand == null || impactFlash == null) return;
        float amount;
        if (relativeDspTime < 0.0)
        {
            amount = Smooth(1f - Mathf.Clamp01((float)(-relativeDspTime / approachSeconds)));
        }
        else
        {
            amount = 1f - Smooth(Mathf.Clamp01((float)(relativeDspTime / recoverySeconds)));
        }

        leftHand.localPosition = Vector3.Lerp(leftOuter, leftImpact, amount);
        rightHand.localPosition = Vector3.Lerp(rightOuter, rightImpact, amount);
        float pulse = Mathf.Exp(-Mathf.Abs((float)relativeDspTime) * 22f);
        impactFlash.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.5f, pulse);
        Color color = Color.Lerp(new Color(0.05f, 0.25f, 0.4f), Color.cyan, pulse);
        impactFlash.material.color = color;
        if (impactFlash.material.HasProperty("_EmissionColor"))
            impactFlash.material.SetColor("_EmissionColor", color * (0.5f + pulse * 5f));
    }

    private static float Smooth(float value) { return value * value * (3f - 2f * value); }
}
