using UnityEngine;

[CreateAssetMenu(fileName = "GestureSO", menuName = "Scriptable Objects/GestureSO")]
public class GestureSO : ScriptableObject
{
    public int gestureIndex;
    public string gestureName;

    [Header("Visual")]
    public float attraction;
    public float repulsion;
    public float pulse;
    public float noise;
    public float brightness;
    public float trailAmount;

    [Header("Audio")]
    public float density;
    public float audioBrightness;
    public float reverb;
    public float tension;
    public float audioPulse;
}
