using UnityEngine;

public class InteractionSignals
{
    // Gesture data
    public int GestureIndex = -1;
    public int PreviousGestureIndex = -1;
    public bool GestureChanged;
    public bool IsPoseAvailable;

    // Visual parameters
    public float VisualAttraction;
    public float VisualRepulsion;
    public float VisualPulse;
    public float VisualNoise;
    public float VisualBrightness;
    public float VisualTrailAmount;

    // Audio parameters
    public float AudioDensity;
    public float AudioBrightness;
    public float AudioReverb;
    public float AudioTension;
    public float AudioPulse;

    // Pose positions
    public Vector2 LeftHandPosition;
    public Vector2 RightHandPosition;
    public Vector2 BodyCenter;

    // Derived values
    public float HandDistance;
    public float HandHeight;
    public float MovementEnergy;

    public void ResetFrameFlags()
    {
        GestureChanged = false;
    }

    public void Clamp01()
    {
        VisualAttraction = Mathf.Clamp01(VisualAttraction);
        VisualRepulsion = Mathf.Clamp01(VisualRepulsion);
        VisualPulse = Mathf.Clamp01(VisualPulse);
        VisualNoise = Mathf.Clamp01(VisualNoise);
        VisualBrightness = Mathf.Clamp01(VisualBrightness);
        VisualTrailAmount = Mathf.Clamp01(VisualTrailAmount);

        AudioDensity = Mathf.Clamp01(AudioDensity);
        AudioBrightness = Mathf.Clamp01(AudioBrightness);
        AudioReverb = Mathf.Clamp01(AudioReverb);
        AudioTension = Mathf.Clamp01(AudioTension);
        AudioPulse = Mathf.Clamp01(AudioPulse);
        
        MovementEnergy = Mathf.Clamp01(MovementEnergy);
    }

    public void Decay(float deltaTime, float decaySpeed)
    {
        VisualAttraction = Mathf.MoveTowards(VisualAttraction, 0f, deltaTime * decaySpeed);
        VisualRepulsion = Mathf.MoveTowards(VisualRepulsion, 0f, deltaTime * decaySpeed);
        VisualPulse = Mathf.MoveTowards(VisualPulse, 0f, deltaTime * decaySpeed);
        VisualNoise = Mathf.MoveTowards(VisualNoise, 0f, deltaTime * decaySpeed);
        VisualBrightness = Mathf.MoveTowards(VisualBrightness, 0f, deltaTime * decaySpeed);
        VisualTrailAmount = Mathf.MoveTowards(VisualTrailAmount, 0f, deltaTime * decaySpeed);

        AudioDensity = Mathf.MoveTowards(AudioDensity, 0f, deltaTime * decaySpeed);
        AudioBrightness = Mathf.MoveTowards(AudioBrightness, 0f, deltaTime * decaySpeed);
        AudioReverb = Mathf.MoveTowards(AudioReverb, 0f, deltaTime * decaySpeed);
        AudioTension = Mathf.MoveTowards(AudioTension, 0f, deltaTime * decaySpeed);
        AudioPulse = Mathf.MoveTowards(AudioPulse, 0f, deltaTime * decaySpeed);
        
        MovementEnergy = Mathf.MoveTowards(MovementEnergy, 0f, deltaTime * decaySpeed);
    }
}
