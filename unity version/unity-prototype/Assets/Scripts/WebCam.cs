using UnityEngine;
using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

[RequireComponent(typeof(Renderer))]
public class WebCam : MonoBehaviour
{
#if UNITY_IOS || UNITY_WEBGL
    private bool CheckPermissionAndRaiseCallbackIfGranted(UserAuthorization authenticationType, Action authenticationGrantedAction)
    {
        if (Application.HasUserAuthorization(authenticationType))
        {
            if (authenticationGrantedAction != null)
                authenticationGrantedAction();

            return true;
        }
        return false;
    }

    private IEnumerator AskForPermissionIfRequired(UserAuthorization authenticationType, Action authenticationGrantedAction)
    {
        if (!CheckPermissionAndRaiseCallbackIfGranted(authenticationType, authenticationGrantedAction))
        {
            yield return Application.RequestUserAuthorization(authenticationType);
            if (!CheckPermissionAndRaiseCallbackIfGranted(authenticationType, authenticationGrantedAction))
                Debug.LogWarning($"Permission {authenticationType} Denied");
        }
    }
#elif UNITY_ANDROID
    private void PermissionCallbacksPermissionGranted(string permissionName)
    {
        StartCoroutine(DelayedCameraInitialization());
    }

    private IEnumerator DelayedCameraInitialization()
    {
        yield return null;
        InitializeCamera();
    }

    private void PermissionCallbacksPermissionDenied(string permissionName)
    {
        Debug.LogWarning($"Permission {permissionName} Denied");
    }

    private void AskCameraPermission()
    {
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionDenied += PermissionCallbacksPermissionDenied;
        callbacks.PermissionGranted += PermissionCallbacksPermissionGranted;
        Permission.RequestUserPermission(Permission.Camera, callbacks);
    }
#endif

    public WebCamTexture webcamTexture;

    [SerializeField] private string preferredCameraName = "FaceTime";
    [SerializeField] private float scaler = 10f;

    void Start()
    {
#if UNITY_IOS || UNITY_WEBGL
        StartCoroutine(AskForPermissionIfRequired(UserAuthorization.WebCam, () => { InitializeCamera(); }));
        return;
#elif UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            AskCameraPermission();
            return;
        }
#endif
        InitializeCamera();
    }

    private void InitializeCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        foreach (WebCamDevice device in devices)
        {
            Debug.Log($"Camera found: {device.name}");
        }

        string selectedCameraName = null;

        foreach (WebCamDevice device in devices)
        {
            if (device.name.Contains(preferredCameraName, StringComparison.OrdinalIgnoreCase))
            {
                selectedCameraName = device.name;
                break;
            }
        }

        if (string.IsNullOrEmpty(selectedCameraName) && devices.Length > 0)
        {
            selectedCameraName = devices[0].name;
        }

        if (string.IsNullOrEmpty(selectedCameraName))
        {
            Debug.LogWarning("No webcam devices found.");
            return;
        }

        Debug.Log($"Using camera: {selectedCameraName}");

        webcamTexture = new WebCamTexture(selectedCameraName);
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.mainTexture = webcamTexture;
        webcamTexture.Play();

        StartCoroutine(FitQuadToWebcam());
    }

    private IEnumerator FitQuadToWebcam()
    {
        while (webcamTexture.width <= 16 || webcamTexture.height <= 16)
            yield return null;

        float aspect = (float)webcamTexture.width / webcamTexture.height;

        Transform t = transform;
        Vector3 scale = t.localScale;
        scale.x = aspect;

        // Mirror horizontally like a selfie camera.
        scale.x = -Mathf.Abs(scale.x);

        // Keep height at 1 and adjust width to match aspect ratio.
        scale.y = 1f;
        scale *= scaler;
        t.localScale = scale;
    }
}