using UnityEngine;


public class SliderController : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private string shaderValueName;
    
    private float targetValue;
    private float currentValue;
    private float acceleration = 0.01f;
    

    void Update()
    {
        currentValue = Mathf.Lerp(currentValue, targetValue, acceleration * Time.deltaTime);
        material.SetFloat(shaderValueName, currentValue);
    }

    public void SetMaterialValue(float value)
    {
        targetValue = value;
    }
    
    public void SetAcceleration(float value)
    {
        acceleration = value;
    }

}
