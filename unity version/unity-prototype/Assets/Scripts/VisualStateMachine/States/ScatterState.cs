using UnityEngine;

public class ScatterState : VisualState
{
    public ScatterState(VisualController controller) : base(controller) { }

    public override void Enter()
    {
        Debug.Log("Entering Scatter State");
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
    }

    public override void ApplySignals(InteractionSignals signals)
    {
    }
}
