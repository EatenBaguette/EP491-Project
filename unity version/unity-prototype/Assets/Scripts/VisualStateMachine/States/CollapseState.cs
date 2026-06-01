using UnityEngine;

public class CollapseState : VisualState
{
    public CollapseState(VisualController controller) : base(controller) { }

    public override void Enter()
    {
        Debug.Log("Entering Collapse State");
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
