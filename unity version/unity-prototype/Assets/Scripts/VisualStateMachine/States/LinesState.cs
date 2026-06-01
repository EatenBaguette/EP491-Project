using UnityEngine;

public class LinesState : VisualState
{
    public LinesState(VisualController controller) : base(controller) { }

    public override void Enter()
    {
        Debug.Log("Entering Lines State");
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
