using UnityEngine;

public class VisualPaint : VisualObject
{
    private float revealAmount = 0f;
    private float targetRevealAmount = 0f;
    private float revealAcceleration = 0.05f;

    private Vector3 constantVelocity;

    private static readonly int RevealAmountID = Shader.PropertyToID("_RevealAmount");

    public float RevealAmount => revealAmount;
    public float TargetRevealAmount => targetRevealAmount;

    public void SetConstantVelocity(Vector3 velocity)
    {
        constantVelocity = velocity;
    }

    public override void ResetObject(Vector3 position)
    {
        base.ResetObject(position);
        constantVelocity = Vector3.zero;
    }

    public void SetRevealTarget(float target)
    {
        targetRevealAmount = Mathf.Clamp01(target);
    }

    public void SetRevealAcceleration(float acceleration)
    {
        revealAcceleration = acceleration;
    }

    public void SetRevealImmediate(float value)
    {
        revealAmount = value;
        targetRevealAmount = value;
        if (material != null)
        {
            material.SetFloat(RevealAmountID, revealAmount);
        }
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        // Apply constant velocity continuously
        transform.position += constantVelocity * deltaTime;

        if (material != null)
        {
            // Interpolate revealAmount towards targetRevealAmount
            // Using a similar approach to SliderController
            revealAmount = Mathf.Lerp(revealAmount, targetRevealAmount, revealAcceleration * deltaTime * 60f);
            material.SetFloat(RevealAmountID, revealAmount);
        }
    }
}
