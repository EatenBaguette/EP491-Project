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
    [SerializeField] private float movementEnergyMultiplier = 10f;

    private InteractionSignals signals = new InteractionSignals();
    private float lastHandDistance = -1f;

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
        if (pose == null || pose.keypoints == null) return;

        Vector2 leftWrist = GetKeypointPosition(pose, DetectLandmarks.Keypoint.LeftWrist);
        Vector2 rightWrist = GetKeypointPosition(pose, DetectLandmarks.Keypoint.RightWrist);
        Vector2 leftHip = GetKeypointPosition(pose, DetectLandmarks.Keypoint.LeftHip);
        Vector2 rightHip = GetKeypointPosition(pose, DetectLandmarks.Keypoint.RightHip);
        Vector2 leftShoulder = GetKeypointPosition(pose, DetectLandmarks.Keypoint.LeftShoulder);
        Vector2 rightShoulder = GetKeypointPosition(pose, DetectLandmarks.Keypoint.RightShoulder);

        // Update basic positions
        signals.LeftHandPosition = leftWrist;
        signals.RightHandPosition = rightWrist;
        signals.BodyCenter = (leftHip + rightHip + leftShoulder + rightShoulder) * 0.25f;

        // Compute HandDistance (normalized 0..1)
        // Since we are in screen/normalized coordinates (0..1 usually from DetectLandmarks), 
        // a distance of 1.0 means across the whole screen.
        if (leftWrist != Vector2.zero && rightWrist != Vector2.zero)
        {
            float currentDistance = Vector2.Distance(leftWrist, rightWrist);
            signals.HandDistance = currentDistance;

            if (lastHandDistance >= 0)
            {
                float deltaDistance = Mathf.Abs(currentDistance - lastHandDistance);
                // Add to movement energy so it can accumulate and decay
                signals.MovementEnergy += deltaDistance * movementEnergyMultiplier;
            }
            lastHandDistance = currentDistance;
        }
        else
        {
            // If we lose tracking, we reset lastHandDistance to avoid huge jumps when it returns
            lastHandDistance = -1f;
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
    }

    private Vector2 GetKeypointPosition(DetectLandmarks.Pose pose, DetectLandmarks.Keypoint keypoint)
    {
        int index = (int)keypoint;
        if (index >= 0 && index < pose.keypoints.Length)
        {
            return pose.keypoints[index].position;
        }
        return Vector2.zero;
    }
}
