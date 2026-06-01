using System.Collections.Generic;
using RTMLToolKit;
using Unity.InferenceEngine;
using UnityEngine;

public class DetectLandmarks : MonoBehaviour
{
    public enum Keypoint
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

    [System.Serializable] public struct NamedKeypoint
    {
        public string name;
        public Vector2 position;
    }

    [System.Serializable] public class Pose
    {
        public float confidence;
        public NamedKeypoint[] keypoints = new NamedKeypoint[17];
    }

    [SerializeField] private WebCam webCam;
    [SerializeField] private ModelAsset modelAsset;
    [SerializeField] private BackendType backendType = BackendType.GPUCompute;
    [SerializeField] private float poseConfidenceThreshold = 0.5f;
    [SerializeField] private float keypointConfidenceThreshold = 0.5f;
    [SerializeField] private RTMLCore[] rtmlCores = new RTMLCore[MaxPoses];
    

    private const int StrideFloatsPerPose = 57;
    private const int MaxPoses = 10;
    private const int KeypointCount = 17;

    private Model _runtimeModel;
    private Worker _worker;
    private Tensor<float> _inputTensor;
    [SerializeField]  float[] _rawLandmarks;

    [SerializeField] public List<Pose> poses = new List<Pose>();

    void Start()
    {
        if (webCam == null)
            webCam = FindFirstObjectByType<WebCam>();

        _runtimeModel = ModelLoader.Load(modelAsset);
        _worker = new Worker(_runtimeModel, backendType);
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

        int maxPoses = Mathf.Min(MaxPoses, raw.Length / StrideFloatsPerPose);

        for (int p = 0; p < maxPoses; p++)
        {
            int baseIndex = p * StrideFloatsPerPose;
            float poseConfidence = raw[baseIndex + 4];

            if (poseConfidence < poseConfidenceThreshold)
                continue;

            Pose pose = new Pose { confidence = poseConfidence };

            // Keypoints start after the 6 header values
            int keypointsStart = baseIndex + 6;

            for (int k = 0; k < KeypointCount; k++)
            {
                int kIndex = keypointsStart + k * 3;
                float x = raw[kIndex];
                float y = raw[kIndex + 1];
                float c = raw[kIndex + 2];

                Vector2 position = c < keypointConfidenceThreshold
                    ? Vector2.zero
                    : new Vector2(x, y);

                pose.keypoints[k] = new NamedKeypoint
                {
                    name = ((Keypoint)k).ToString(),
                    position = position
                };
            }

            poses.Add(pose);
        }
    }

    void OnDisable()
    {
        _inputTensor?.Dispose();
        _worker?.Dispose();
    }
}