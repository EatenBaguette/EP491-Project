using UnityEngine;

public class SquaresState : VisualState
{
    public SquaresState(VisualController controller) : base(controller) { }

    public override void Enter()
    {
        Debug.Log("Entering Squares State");
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
