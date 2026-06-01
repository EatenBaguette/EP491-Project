using System.Collections.Generic;
using UnityEngine;
using RTMLToolKit;

public class DetectGestures : MonoBehaviour
{
    public static DetectGestures Instance;
    
    private const int KeypointCount = 17;
    private const int CoordinatesPerKeypoint = 2;
    private const int LandmarkFrameSize = KeypointCount * CoordinatesPerKeypoint;

    [Header("RTML Settings")]
    [Tooltip("A reference to your RTMLCore component.")]
    public RTMLCore rtmlCore;

    [Header("Landmark Source")]
    [Tooltip("The DetectLandmarks component that provides pose keypoints.")]
    public DetectLandmarks detectLandmarks;

    [Tooltip("If true, landmark coordinates are normalized around the hips/shoulders to make gestures less dependent on screen position and scale.")]
    public bool normalisePose = true;

    [Header("Gesture Training")]
    [Tooltip("The integer label to record for the next gesture sample. Change this for each gesture class.")]
    public int gestureIndex = 0;

    [Tooltip("Minimum number of frames required to save a recorded gesture.")]
    [Min(1)]
    public int minimumRecordedFrames = 5;

    [Tooltip("If true, missing pose data is recorded as zero frames. This allows training a 'silence' or 'no gesture' class.")]
    public bool recordZeroFramesWhenPoseMissing = true;

    [Header("Gesture Prediction")]
    [Tooltip("Maximum number of recent frames to keep for live DTW prediction.")]
    [Min(1)]
    public int maxPredictionFrames = 90;

    [Tooltip("Predicted gesture index from the model.")]
    public int predictedGestureIndex = -1;

    [Tooltip("Raw predicted output value from the model.")]
    public float predictedGestureValue = 0f;

    [SerializeField] private bool predictOnStart;

    [Header("Keyboard Shortcuts")]
    public KeyCode startRecordKey = KeyCode.R;
    public KeyCode stopRecordKey = KeyCode.S;
    public KeyCode trainKey = KeyCode.T;
    public KeyCode predictKey = KeyCode.P;

    [Header("Debug")]
    [SerializeField] private bool isRecordingGesture;
    [SerializeField] private int recordedFrameCount;
    [SerializeField] private int predictionFrameCount;

    private readonly List<float[]> recordedGestureFrames = new List<float[]>();
    private readonly List<float[]> predictionGestureFrames = new List<float[]>();
    
    void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (detectLandmarks == null)
        {
            detectLandmarks = FindFirstObjectByType<DetectLandmarks>();
        }

        if (rtmlCore == null)
        {
            Debug.LogError("[DetectGestures] Missing RTMLCore reference.");
            enabled = false;
            return;
        }

        rtmlCore.enableRun = false;
        rtmlCore.enableRecord = true;

        // For true DTW, each frame has 34 values.
        // Full gesture inputs are variable-length flattened sequences of these frames.
        rtmlCore.inputSize = LandmarkFrameSize;
        rtmlCore.outputSize = 1;
        rtmlCore.dtwFrameSize = LandmarkFrameSize;

        Debug.Log($"[DetectGestures] Gesture frame size set to {LandmarkFrameSize}. Output size set to 1.");
        
        if (predictOnStart)
        {
            EnablePredict();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(startRecordKey) && !isRecordingGesture)
        {
            StartGestureRecording();
        }

        if (Input.GetKeyDown(stopRecordKey) && isRecordingGesture)
        {
            StopAndRecordGesture();
        }

        if (Input.GetKeyDown(trainKey))
        {
            rtmlCore.TrainModel();
            Debug.Log("[DetectGestures] Gesture model training requested.");
        }

        if (Input.GetKeyDown(predictKey))
        {
            EnablePredict();
        }

        float[] currentFrame = GetCurrentLandmarkFrameOrZeros();

        if (isRecordingGesture)
        {
            recordedGestureFrames.Add(currentFrame);
            recordedFrameCount = recordedGestureFrames.Count;
        }

        if (rtmlCore.enableRun)
        {
            UpdateGesturePrediction(currentFrame);
        }
    }

    private void StartGestureRecording()
    {
        recordedGestureFrames.Clear();
        recordedFrameCount = 0;
        isRecordingGesture = true;

        Debug.Log($"[DetectGestures] Gesture recording started for gesture index {gestureIndex}.");
    }

    private void StopAndRecordGesture()
    {
        isRecordingGesture = false;

        if (recordedGestureFrames.Count < minimumRecordedFrames)
        {
            Debug.LogWarning($"[DetectGestures] Gesture was too short. Captured {recordedGestureFrames.Count} frames, but at least {minimumRecordedFrames} are required.");
            recordedGestureFrames.Clear();
            recordedFrameCount = 0;
            return;
        }

        float[] gestureInput = FlattenGesture(recordedGestureFrames);
        float[] output = { gestureIndex };

        rtmlCore.RecordSample(gestureInput, output);

        Debug.Log($"[DetectGestures] Recorded gesture index {gestureIndex} with {recordedGestureFrames.Count} frames / {gestureInput.Length} floats.");

        recordedGestureFrames.Clear();
        recordedFrameCount = 0;
    }

    private void UpdateGesturePrediction(float[] currentFrame)
    {
        predictionGestureFrames.Add(currentFrame);

        while (predictionGestureFrames.Count > maxPredictionFrames)
        {
            predictionGestureFrames.RemoveAt(0);
        }

        predictionFrameCount = predictionGestureFrames.Count;

        if (predictionGestureFrames.Count < minimumRecordedFrames)
        {
            return;
        }

        float[] gestureInput = FlattenGesture(predictionGestureFrames);
        float[] prediction = rtmlCore.PredictSample(gestureInput);

        if (prediction == null || prediction.Length < 1)
        {
            predictedGestureIndex = -1;
            predictedGestureValue = 0f;
            return;
        }

        predictedGestureValue = prediction[0];
        predictedGestureIndex = Mathf.RoundToInt(predictedGestureValue);
    }

    private float[] GetCurrentLandmarkFrameOrZeros()
    {
        if (TryGetFirstPoseLandmarkFrame(out float[] frame))
        {
            return frame;
        }

        if (recordZeroFramesWhenPoseMissing)
        {
            return new float[LandmarkFrameSize];
        }

        return new float[LandmarkFrameSize];
    }

    private bool TryGetFirstPoseLandmarkFrame(out float[] frame)
    {
        frame = null;

        if (detectLandmarks == null)
        {
            return false;
        }

        if (detectLandmarks.poses == null || detectLandmarks.poses.Count == 0)
        {
            return false;
        }

        DetectLandmarks.Pose pose = detectLandmarks.poses[0];

        if (pose == null || pose.keypoints == null || pose.keypoints.Length < KeypointCount)
        {
            return false;
        }

        frame = new float[LandmarkFrameSize];

        if (normalisePose)
        {
            FillNormalisedLandmarkFrame(pose, frame);
        }
        else
        {
            FillRawLandmarkFrame(pose, frame);
        }

        return true;
    }

    private void FillRawLandmarkFrame(DetectLandmarks.Pose pose, float[] frame)
    {
        for (int i = 0; i < KeypointCount; i++)
        {
            Vector2 position = pose.keypoints[i].position;
            int frameIndex = i * CoordinatesPerKeypoint;

            frame[frameIndex] = position.x;
            frame[frameIndex + 1] = position.y;
        }
    }

    private void FillNormalisedLandmarkFrame(DetectLandmarks.Pose pose, float[] frame)
    {
        Vector2 leftHip = pose.keypoints[(int)DetectLandmarks.Keypoint.LeftHip].position;
        Vector2 rightHip = pose.keypoints[(int)DetectLandmarks.Keypoint.RightHip].position;
        Vector2 leftShoulder = pose.keypoints[(int)DetectLandmarks.Keypoint.LeftShoulder].position;
        Vector2 rightShoulder = pose.keypoints[(int)DetectLandmarks.Keypoint.RightShoulder].position;

        Vector2 hipCentre = GetAverageNonZero(leftHip, rightHip);
        Vector2 shoulderCentre = GetAverageNonZero(leftShoulder, rightShoulder);

        Vector2 origin = hipCentre != Vector2.zero ? hipCentre : shoulderCentre;

        float bodyScale = Vector2.Distance(hipCentre, shoulderCentre);

        if (bodyScale < 1e-5f)
        {
            bodyScale = Vector2.Distance(leftShoulder, rightShoulder);
        }

        if (bodyScale < 1e-5f)
        {
            bodyScale = 1f;
        }

        for (int i = 0; i < KeypointCount; i++)
        {
            Vector2 position = pose.keypoints[i].position;
            int frameIndex = i * CoordinatesPerKeypoint;

            if (position == Vector2.zero)
            {
                frame[frameIndex] = 0f;
                frame[frameIndex + 1] = 0f;
                continue;
            }

            Vector2 normalised = (position - origin) / bodyScale;

            frame[frameIndex] = normalised.x;
            frame[frameIndex + 1] = normalised.y;
        }
    }

    private float[] FlattenGesture(List<float[]> gestureFrames)
    {
        float[] flattenedGesture = new float[gestureFrames.Count * LandmarkFrameSize];

        for (int frameIndex = 0; frameIndex < gestureFrames.Count; frameIndex++)
        {
            int flattenedOffset = frameIndex * LandmarkFrameSize;
            float[] frame = gestureFrames[frameIndex];

            for (int feature = 0; feature < LandmarkFrameSize; feature++)
            {
                flattenedGesture[flattenedOffset + feature] = frame[feature];
            }
        }

        return flattenedGesture;
    }

    private Vector2 GetAverageNonZero(Vector2 a, Vector2 b)
    {
        bool hasA = a != Vector2.zero;
        bool hasB = b != Vector2.zero;

        if (hasA && hasB)
        {
            return (a + b) * 0.5f;
        }

        if (hasA)
        {
            return a;
        }

        if (hasB)
        {
            return b;
        }

        return Vector2.zero;
    }

    void EnablePredict()
    {
        rtmlCore.enableRun = !rtmlCore.enableRun;
        predictionGestureFrames.Clear();
        predictedGestureIndex = -1;
        predictedGestureValue = 0f;
        
        Debug.Log($"[DetectGestures] Real-time gesture prediction toggled to: {rtmlCore.enableRun}");
    }
}