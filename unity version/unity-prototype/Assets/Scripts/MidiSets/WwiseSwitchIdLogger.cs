using UnityEngine;

public class WwiseSwitchIdDebugPrinter : MonoBehaviour
{
    [SerializeField] private AK.Wwise.Switch switchValue;

    [ContextMenu("Print Switch ID")]
    private void PrintSwitchId()
    {
        if (switchValue == null)
        {
            Debug.LogWarning("No switch assigned.");
            return;
        }

        Debug.Log(
            $"Switch Name: {switchValue.Name}, " +
            $"Switch ID: {switchValue.Id}, " +
            $"Group ID: {switchValue.GroupId}"
        );
    }
}
