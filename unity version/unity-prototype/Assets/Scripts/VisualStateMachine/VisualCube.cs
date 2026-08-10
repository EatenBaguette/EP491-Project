using UnityEngine;

public class VisualCube : VisualObject
{
    void Start()
    {
        material.SetTextureScale("_BaseMap", new Vector2(Random.Range(1f, 2.6f), Random.Range(0.25f, 2.6f)));
    }
}
