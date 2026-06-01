using System.Collections.Generic;
using UnityEngine;

public class IdleState : VisualState
{
    private Dictionary<VisualObject, Vector3> gravityPoints = new Dictionary<VisualObject, Vector3>();

    public IdleState(VisualController controller) : base(controller) { }

    public override void Enter()
    {
        Debug.Log("Entering Idle State");
        gravityPoints.Clear();

        foreach (var obj in controller.VisualObjects)
        {
            if (obj == null || !obj.gameObject.activeSelf) continue;
            gravityPoints[obj] = obj.transform.position;
        }
    }

    public override void Update()
    {
        foreach (var obj in controller.VisualObjects)
        {
            if (obj == null || !obj.gameObject.activeSelf) continue;
            if (!gravityPoints.ContainsKey(obj)) continue;

            Vector3 targetPos = gravityPoints[obj];
            Vector3 toTarget = targetPos - obj.transform.position;

            // Apply gravity force toward the stored position
            float gravityStrength = controller.idleGravity; 
            obj.ApplyForce(toTarget * (gravityStrength * Time.deltaTime));

            // Add random wander force for variation
            Vector3 wanderForce = Random.insideUnitSphere * (gravityStrength * 0.1f * Time.deltaTime);
            obj.ApplyForce(wanderForce);
        }
    }

    public override void Exit()
    {
        gravityPoints.Clear();
    }

    public override void ApplySignals(InteractionSignals signals)
    {
        
    }
}

