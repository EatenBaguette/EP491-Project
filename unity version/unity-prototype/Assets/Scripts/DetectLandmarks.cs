using System;
using System.Collections.Generic;
using RTMLToolKit;
using Unity.InferenceEngine;
using UnityEngine;

public class DetectLandmarks : MonoBehaviour
{
    public enum Landmark
    {
        Nose = 0,
        LeftEye,
        RightEye,
        LeftEar,
        RightEar,
        LeftShoulder,
        RightShoulder,
        LeftElbow,
        RightElbow,
        LeftWrist,
        RightWrist,
        LeftHip,
        RightHip,
        LeftKnee,
        RightKnee,
        LeftAnkle,
        RightAnkle
    }

    [System.Serializable]
    public struct NamedLandmark
    {
        public string name;

        [Tooltip("Offset from this pose's position, not the raw model-space coordinate.")]
        public Vector2 position;
    }

    [System.Serializable]
    public class Pose
    {
        public float confidence;

        [Tooltip("Raw model-space pose position.")]
        public Vector2 position;

        [Tooltip("Each landmark is stored as an offset from pose.position.")]
        public NamedLandmark[] landmarks = new NamedLandmark[LandmarkCount];
    }

    private const int StrideFloatsPerPose = 57;
    private const int MaxPoses = 300;
    private const int LandmarkCount = 17;

    private const int PoseYIndex = 1;
    private const int PoseXIndex = 2;
    private const int PoseConfidenceIndex = 4;
    private const int LandmarksStartOffset = 6;
    private const int FloatsPerLandmark = 3;

    [SerializeField] private WebCam webCam;
    [SerializeField] private ModelAsset modelAsset;
    [SerializeField] private BackendType backendType = BackendType.GPUCompute;

    [Header("Confidence Thresholds")]
    [SerializeField] private float poseConfidenceThreshold = 0.5f;
    [SerializeField] private float landmarkConfidenceThreshold = 0.5f;

    [Header("Smoothing")]
    [SerializeField] private int smoothingWindow = 5;

    [Header("Optional RTML References")]
    [SerializeField] private RTMLCore[] rtmlCores = new RTMLCore[MaxPoses];

    private Model _runtimeModel;
    private Worker _worker;
    private Tensor<float> _inputTensor;

    private Queue<Vector2>[] _posePositionHistory = new Queue<Vector2>[MaxPoses];
    private Queue<Vector2>[][] _landmarkHistory = new Queue<Vector2>[MaxPoses][];
    private Queue<float>[] _poseConfidenceHistory = new Queue<float>[MaxPoses];

    [Header("Debug")]
    [SerializeField] private float[] _rawLandmarks;

    [SerializeField]
    public List<Pose> poses = new List<Pose>();

    void Start()
    {
        if (webCam == null)
            webCam = FindFirstObjectByType<WebCam>();

        _runtimeModel = ModelLoader.Load(modelAsset);
        _worker = new Worker(_runtimeModel, backendType);

        for (int i = 0; i < MaxPoses; i++)
        {
            _posePositionHistory[i] = new Queue<Vector2>();
            _landmarkHistory[i] = new Queue<Vector2>[LandmarkCount];
            _poseConfidenceHistory[i] = new Queue<float>();
            for (int j = 0; j < LandmarkCount; j++)
            {
                _landmarkHistory[i][j] = new Queue<Vector2>();
            }
        }
    }

    void Update()
    {
        if (webCam == null || webCam.webcamTexture == null || !webCam.webcamTexture.isPlaying)
            return;

        if (webCam.webcamTexture.width <= 16 || webCam.webcamTexture.height <= 16)
            return;

        int width = webCam.webcamTexture.width;
        int height = webCam.webcamTexture.height;

        if (_inputTensor == null || _inputTensor.shape[3] != width || _inputTensor.shape[2] != height)
        {
            _inputTensor?.Dispose();
            _inputTensor = new Tensor<float>(new TensorShape(1, 3, height, width));
        }

        TextureConverter.ToTensor(webCam.webcamTexture, _inputTensor, new TextureTransform());

        _worker.Schedule(_inputTensor);

        using Tensor<float> outputTensor = _worker.PeekOutput() as Tensor<float>;
        if (outputTensor == null)
            return;

        _rawLandmarks = outputTensor.DownloadToArray();
        ParsePoses(_rawLandmarks);
    }

    private void ParsePoses(float[] raw)
    {
        poses.Clear();

        if (raw == null || raw.Length < StrideFloatsPerPose)
            return;

        int poseCount = Mathf.Min(MaxPoses, raw.Length / StrideFloatsPerPose);

        for (int poseIndex = 0; poseIndex < MaxPoses; poseIndex++)
        {
            if (poseIndex >= poseCount)
            {
                _posePositionHistory[poseIndex].Clear();
                _poseConfidenceHistory[poseIndex].Clear();
                for (int k = 0; k < LandmarkCount; k++) _landmarkHistory[poseIndex][k].Clear();
                continue;
            }

            int baseIndex = poseIndex * StrideFloatsPerPose;
            float poseConfidence = raw[baseIndex + PoseConfidenceIndex];
            
            poseConfidence = SmoothFloat(_poseConfidenceHistory[poseIndex], poseConfidence);

            if (poseConfidence < poseConfidenceThreshold)
            {
                _poseConfidenceHistory[poseIndex].Clear();
                _posePositionHistory[poseIndex].Clear();
                for (int k = 0; k < LandmarkCount; k++) _landmarkHistory[poseIndex][k].Clear();
                continue;
            }

            Vector2 rawPosePosition = ReadPosePosition(raw, baseIndex);
            
            Pose pose = new Pose
            {
                confidence = poseConfidence,
                position = rawPosePosition // Temporary, will be updated after calculating weighted center
            };

            FillPoseLandmarks(raw, baseIndex, pose, poseIndex);

            // Calculate centered position based on landmarks
            Vector2 centeredPosition = CalculateCenteredPosition(pose, rawPosePosition);
            
            // Apply smoothing to the centered position
            pose.position = SmoothVector2(_posePositionHistory[poseIndex], centeredPosition);

            // Convert landmark positions to relative offsets from the final smoothed pose position
            for (int k = 0; k < LandmarkCount; k++)
            {
                if (pose.landmarks[k].position != Vector2.zero)
                {
                    pose.landmarks[k].position = pose.position - pose.landmarks[k].position;
                }
            }

            poses.Add(pose);
        }
    }

    private Vector2 CalculateCenteredPosition(Pose pose, Vector2 fallbackPosition)
    {
        float centerX = 0f;
        float totalWeightX = 0f;

        // X axis: hips 60, shoulders 20, eyes 10, nose 10
        AddWeightX(pose, Landmark.LeftHip, Landmark.RightHip, 60f, ref centerX, ref totalWeightX);
        AddWeightX(pose, Landmark.LeftShoulder, Landmark.RightShoulder, 20f, ref centerX, ref totalWeightX);
        AddWeightX(pose, Landmark.LeftEye, Landmark.RightEye, 10f, ref centerX, ref totalWeightX);
        
        // Nose is a single landmark
        Vector2 nosePos = pose.landmarks[(int)Landmark.Nose].position;
        if (nosePos != Vector2.zero)
        {
            centerX += nosePos.x * 10f;
            totalWeightX += 10f;
        }

        float finalX = (totalWeightX > 0) ? (centerX / totalWeightX) : fallbackPosition.x;

        // Y axis: average of shoulders, eyes, nose
        float centerY = 0f;
        int countY = 0;

        AddAverageY(pose, Landmark.LeftShoulder, ref centerY, ref countY);
        AddAverageY(pose, Landmark.RightShoulder, ref centerY, ref countY);
        AddAverageY(pose, Landmark.LeftEye, ref centerY, ref countY);
        AddAverageY(pose, Landmark.RightEye, ref centerY, ref countY);
        AddAverageY(pose, Landmark.Nose, ref centerY, ref countY);

        float finalY = (countY > 0) ? (centerY / countY) : fallbackPosition.y;

        return new Vector2(finalX, finalY);
    }

    private void AddWeightX(Pose pose, Landmark left, Landmark right, float weight, ref float centerX, ref float totalWeightX)
    {
        Vector2 lp = pose.landmarks[(int)left].position;
        Vector2 rp = pose.landmarks[(int)right].position;

        if (lp != Vector2.zero && rp != Vector2.zero)
        {
            centerX += ((lp.x + rp.x) * 0.5f) * weight;
            totalWeightX += weight;
        }
        else if (lp != Vector2.zero)
        {
            centerX += lp.x * weight;
            totalWeightX += weight;
        }
        else if (rp != Vector2.zero)
        {
            centerX += rp.x * weight;
            totalWeightX += weight;
        }
    }

    private void AddAverageY(Pose pose, Landmark landmark, ref float centerY, ref int countY)
    {
        Vector2 p = pose.landmarks[(int)landmark].position;
        if (p != Vector2.zero)
        {
            centerY += p.y;
            countY++;
        }
    }

    private Vector2 SmoothVector2(Queue<Vector2> history, Vector2 newValue)
    {
        if (smoothingWindow <= 1)
        {
            history.Clear();
            return newValue;
        }

        history.Enqueue(newValue);
        while (history.Count > smoothingWindow)
        {
            history.Dequeue();
        }

        Vector2 sum = Vector2.zero;
        foreach (Vector2 v in history)
        {
            sum += v;
        }

        return sum / history.Count;
    }

    private float SmoothFloat(Queue<float> history, float newValue)
    {
        if (smoothingWindow <= 1)
        {
            history.Clear();
            return newValue;
        }
        history.Enqueue(newValue);
        while (history.Count > smoothingWindow)
        {
            history.Dequeue();
        }

        float sum = 0f;
        foreach (float f in history)
        {
            sum += f;
        }
        return sum / history.Count;
    }

    private Vector2 ReadPosePosition(float[] raw, int baseIndex)
    {
        float poseX = raw[baseIndex + PoseXIndex];
        float poseY = raw[baseIndex + PoseYIndex];

        return new Vector2(poseX, poseY);
    }

    private void FillPoseLandmarks(float[] raw, int baseIndex, Pose pose, int poseIndex)
    {
        int landmarksStart = baseIndex + LandmarksStartOffset;

        for (int landmarkIndex = 0; landmarkIndex < LandmarkCount; landmarkIndex++)
        {
            int rawLandmarkIndex = landmarksStart + landmarkIndex * FloatsPerLandmark;

            float rawX = raw[rawLandmarkIndex];
            float rawY = raw[rawLandmarkIndex + 1];
            float confidence = raw[rawLandmarkIndex + 2];

            Vector2 landmarkPosition = Vector2.zero;

            if (confidence >= landmarkConfidenceThreshold)
            {
                Vector2 rawLandmarkPos = new Vector2(rawX, rawY);
                landmarkPosition = SmoothVector2(_landmarkHistory[poseIndex][landmarkIndex], rawLandmarkPos);
            }
            else
            {
                _landmarkHistory[poseIndex][landmarkIndex].Clear();
            }

            pose.landmarks[landmarkIndex] = new NamedLandmark
            {
                name = ((Landmark)landmarkIndex).ToString(),
                position = landmarkPosition
            };
        }
    }

    void OnDisable()
    {
        _inputTensor?.Dispose();
        _worker?.Dispose();
    }
}