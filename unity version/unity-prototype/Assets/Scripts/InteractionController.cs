using System;
using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [Header("Detection References")]
    [SerializeField] private DetectGestures detectGestures;
    [SerializeField] private DetectLandmarks detectLandmarks;

    [Header("System References")]
    [SerializeField] private VisualController visualController;
    [SerializeField] private AudioController audioController;

    [Header("Settings")]
    [SerializeField] private float decaySpeed = 1f;
    [SerializeField] private System.Collections.Generic.List<GestureSO> gestures;
    [SerializeField] private float movementEnergyMultiplier = 1f;
    [SerializeField] private float movementEnergyDecayMultiplier = 1f;

    [Header("Debug")] 
    [SerializeField] private float movementEnergy = 0f;
    [SerializeField] private float handHeight = 0f;
    [SerializeField] private float maxMovementEnergy = 0f;

    private InteractionSignals signals = new InteractionSignals();
    private Vector2 lastLeftHandPos = Vector2.zero;
    private Vector2 lastRightHandPos = Vector2.zero;
    private bool leftHandTracked = false;
    private bool rightHandTracked = false;

    public InteractionSignals Signals => signals;

    private void Update()
    {
        if (signals == null) return;

        // 1. Reset frame flags
        signals.ResetFrameFlags();

        // 2. Read the current gesture index
        int currentGesture = -1;
        if (detectGestures != null)
        {
            currentGesture = detectGestures.predictedGestureIndex;
        }

        // 3. Detect gesture changes
        if (currentGesture != signals.GestureIndex)
        {
            signals.PreviousGestureIndex = signals.GestureIndex;
            signals.GestureIndex = currentGesture;
            signals.GestureChanged = true;

            Debug.Log($"[InteractionController] Gesture changed: {signals.PreviousGestureIndex} -> {signals.GestureIndex}");

            ApplyGestureInfluence(signals.GestureIndex);
        }

        // 4. Update pose availability
        if (detectLandmarks != null)
        {
            bool hasPose = detectLandmarks.poses != null && detectLandmarks.poses.Count > 0;
            signals.IsPoseAvailable = hasPose;
            if (hasPose)
            {
                UpdatePoseSignals();
            }
        }

        // 6. Clamp them
        signals.Clamp01();

        // 7. Decay them
        signals.Decay(Time.deltaTime, decaySpeed);

        // 8. Pass signals to VisualController
        if (visualController != null)
        {
            visualController.ApplySignals(signals);
        }
        // 9. Pass signals to AudioController
        if (audioController != null)
        {
            audioController.ApplySignals(signals);
        }
        
        // Debug
        movementEnergy = signals.MovementEnergy;
        if (movementEnergy > maxMovementEnergy) maxMovementEnergy = movementEnergy;
        handHeight = signals.HandHeight;
    }

    private void ApplyGestureInfluence(int index)
    {
        if (gestures == null) return;

        GestureSO gesture = gestures.Find(g => g.gestureIndex == index);
        if (gesture != null)
        {
            signals.VisualAttraction += gesture.attraction;
            signals.VisualRepulsion += gesture.repulsion;
            signals.VisualPulse += gesture.pulse;
            signals.VisualNoise += gesture.noise;
            signals.VisualBrightness += gesture.brightness;
            signals.VisualTrailAmount += gesture.trailAmount;

            signals.AudioDensity += gesture.density;
            signals.AudioBrightness += gesture.audioBrightness;
            signals.AudioReverb += gesture.reverb;
            signals.AudioTension += gesture.tension;
            signals.AudioPulse += gesture.audioPulse;
        }
    }

    private void UpdatePoseSignals()
    {
        if (detectLandmarks == null || detectLandmarks.poses == null || detectLandmarks.poses.Count == 0)
        {
            return;
        }

        // Get the first pose (assuming single user interaction for now)
        var pose = detectLandmarks.poses[0];
        if (pose == null || pose.landmarks == null) return;

        Vector2 leftWrist = GetLandmarkPosition(pose, DetectLandmarks.Landmark.LeftWrist);
        Vector2 rightWrist = GetLandmarkPosition(pose, DetectLandmarks.Landmark.RightWrist);

        // Update basic positions
        signals.LeftHandPosition = leftWrist;
        signals.RightHandPosition = rightWrist;
        signals.BodyCenter = pose.position;

        float deltaMovement = 0f;
        
        if (leftWrist != Vector2.zero && lastLeftHandPos != Vector2.zero)
        {
            float dist = Vector2.Distance(leftWrist, lastLeftHandPos);
            deltaMovement += Math.Abs(dist);
            if (dist > 0.0001f)
            {
                signals.LeftHandDirection = (leftWrist - lastLeftHandPos).normalized;
            }
        }
        else
        {
            signals.LeftHandDirection = Vector2.zero;
        }

        if (rightWrist != Vector2.zero && lastRightHandPos != Vector2.zero)
        {
            float dist = Vector2.Distance(rightWrist, lastRightHandPos);
            deltaMovement += Math.Abs(dist);
            if (dist > 0.0001f)
            {
                signals.RightHandDirection = (rightWrist - lastRightHandPos).normalized;
            }
        }
        else
        {
            signals.RightHandDirection = Vector2.zero;
        }
        
        lastLeftHandPos = leftWrist;
        lastRightHandPos = rightWrist;

        // Add total combined movement to energy
        signals.MovementEnergy = deltaMovement * movementEnergyMultiplier;

        // Maintain HandDistance signal for other systems (Visuals, etc)
        if (leftWrist != Vector2.zero && rightWrist != Vector2.zero)
        {
            signals.HandDistance = Vector2.Distance(leftWrist, rightWrist);
        }

        // Compute AverageHandHeight (normalized 0..1)
        // Assuming Y=0 is top and Y=1 is bottom or vice versa, Vector2.zero check handles missing data.
        if (leftWrist != Vector2.zero && rightWrist != Vector2.zero)
        {
            signals.HandHeight = (leftWrist.y + rightWrist.y) * 0.5f;
        }
        else if (leftWrist != Vector2.zero)
        {
            signals.HandHeight = leftWrist.y;
        }
        else if (rightWrist != Vector2.zero)
        {
            signals.HandHeight = rightWrist.y;
        }

        //if (Time.frameCount % 10 == 0)
        //{
        //    Debug.Log($"[InteractionController] Pose signals updated: " +
        //              $" Hand distance: {signals.HandDistance}" +
        //              $"Movement Energy: {signals.MovementEnergy}");
        //}
        
        signals.MovementEnergyDecayMultiplier = movementEnergyDecayMultiplier;
        
    }

    private Vector2 GetLandmarkPosition(DetectLandmarks.Pose pose, DetectLandmarks.Landmark landmark)
    {
        int index = (int)landmark;
        if (index >= 0 && index < pose.landmarks.Length)
        {
            return pose.landmarks[index].position;
        }
        return Vector2.zero;
    }
}
