using System.Collections.Generic;
using UnityEngine;

public class EdgeOrbitState : VisualState
{
    private Dictionary<VisualObject, float> orbitRadii = new Dictionary<VisualObject, float>();
    private Dictionary<VisualObject, Vector3> orbitAxes = new Dictionary<VisualObject, Vector3>();
    private Dictionary<VisualObject, float> orbitDirections = new Dictionary<VisualObject, float>();
    private Dictionary<VisualObject, float> orbitSpeedMultipliers = new Dictionary<VisualObject, float>();

    public EdgeOrbitState(VisualController controller) : base(controller) { }

    private float arrivalOrbitGravity;
    private float arrivalCenterRepelForce;
    
    public override void Enter()
    {
        Debug.Log("Entering EdgeOrbit State");

        orbitRadii.Clear();
        orbitAxes.Clear();
        orbitDirections.Clear();
        orbitSpeedMultipliers.Clear();
        
        arrivalOrbitGravity = controller.orbitGravity;
        arrivalCenterRepelForce = controller.centerRepelForce;

        foreach (var obj in controller.VisualObjects)
        {
            if (obj == null || !obj.gameObject.activeSelf) continue;

            orbitRadii[obj] = Random.Range(controller.minOrbitDistance, controller.maxOrbitDistance);

            Vector3 axis = Random.onUnitSphere;
            if (Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.95f)
            {
                axis = Vector3.forward;
            }

            orbitAxes[obj] = axis.normalized;
            orbitDirections[obj] = Random.value < 0.5f ? -1f : 1f;
            orbitSpeedMultipliers[obj] = Random.Range(0.75f, 1.35f);

            Vector3 startDirection = Random.onUnitSphere;
            obj.ApplyForce(startDirection * Random.Range(controller.minOrbitVelocity, controller.maxOrbitVelocity));
        }
    }

    public override void Update()
{
    Camera cam = Camera.main;

    foreach (var obj in controller.VisualObjects)
    {
        if (obj == null || !obj.gameObject.activeSelf) continue;
        if (!orbitRadii.ContainsKey(obj)) continue;

        Vector3 position = obj.transform.position;

        if (position.sqrMagnitude < 0.001f)
        {
            obj.ApplyForce(Random.onUnitSphere * controller.minOrbitVelocity);
            continue;
        }

        Vector3 radialDirection = position.normalized;

        float currentRadius = position.magnitude;
        float targetRadius = orbitRadii[obj];

        // Pull inward if too far, push outward if too close.
        float radiusError = currentRadius - targetRadius;
        Vector3 radiusCorrection = -radialDirection * (radiusError * controller.orbitGravity * Time.deltaTime);

        obj.ApplyForce(radiusCorrection);

        if (cam != null)
        {
            Vector3 objectFromCamera = position - cam.transform.position;
            float objectDepth = Vector3.Dot(objectFromCamera, cam.transform.forward);

            if (objectDepth > 0.001f)
            {
                Vector3 viewportCenterWorld = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, objectDepth));
                Vector3 fromCenter = position - viewportCenterWorld;

                // Project onto the camera plane so depth does not affect the direction.
                Vector3 planarFromCenter = Vector3.ProjectOnPlane(fromCenter, cam.transform.forward);
                float distanceFromCenter = planarFromCenter.magnitude;

                // Make the mask widen with the camera frustum as depth increases.
                float safeReferenceDepth = Mathf.Max(0.001f, controller.centerRepelReferenceDepth);
                float depthScale = objectDepth / safeReferenceDepth;
                float repelRadiusAtDepth = controller.centerRepelRadius * depthScale;

                if (distanceFromCenter < repelRadiusAtDepth)
                {
                    Vector3 repelDirection = planarFromCenter.normalized;

                    if (repelDirection.sqrMagnitude < 0.001f)
                    {
                        repelDirection = Random.insideUnitSphere;
                        repelDirection = Vector3.ProjectOnPlane(repelDirection, cam.transform.forward).normalized;
                    }

                    float normalizedDistance = distanceFromCenter / repelRadiusAtDepth;
                    float repelStrength = Mathf.Pow(1f - normalizedDistance, controller.centerRepelFalloff);

                    Vector3 repelForce = repelDirection * (controller.centerRepelForce * repelStrength * Time.deltaTime);
                    obj.ApplyForce(repelForce);
                }
            }
        }

        Vector3 orbitAxis = orbitAxes[obj];
        float orbitDirection = orbitDirections[obj];

        // Each object gets its own orbit plane/direction.
        Vector3 tangent = Vector3.Cross(orbitAxis, radialDirection) * orbitDirection;

        if (tangent.sqrMagnitude < 0.001f)
        {
            tangent = Vector3.Cross(Vector3.forward, radialDirection) * orbitDirection;
        }

        tangent.Normalize();

        float baseTargetSpeed = (controller.minOrbitVelocity + controller.maxOrbitVelocity) * 0.5f;
        float targetSpeed = baseTargetSpeed * orbitSpeedMultipliers[obj];

        float currentSpeedAlongTangent = Vector3.Dot(obj.Velocity, tangent);

        // Continuously push toward the desired tangential speed.
        float speedError = targetSpeed - currentSpeedAlongTangent;
        Vector3 tangentialCorrection = tangent * (speedError * Time.deltaTime);

        obj.ApplyForce(tangentialCorrection);

        // Tiny wander force so paths do not perfectly synchronize.
        Vector3 randomWander = Random.insideUnitSphere * (controller.orbitGravity * 0.03f * Time.deltaTime);
        obj.ApplyForce(randomWander);
    }
}

    public override void Exit()
    {
        orbitRadii.Clear();
        orbitAxes.Clear();
        orbitDirections.Clear();
        orbitSpeedMultipliers.Clear();
        
        controller.centerRepelForce = arrivalCenterRepelForce;
        controller.orbitGravity = arrivalOrbitGravity;
    }

    public override void ApplySignals(InteractionSignals signals)
    {
        float gravity = 2 * controller.handDistanceCurve.Evaluate(signals.HandDistance) - 1;
        controller.centerRepelForce = arrivalCenterRepelForce + gravity * controller.handDistanceMultiplier;
        controller.orbitGravity = arrivalOrbitGravity + gravity;
    }
    

    //void NotUsed()
    //{
        //foreach (var obj in controller.VisualObjects)
        //{
            //if (!orbitTargets.ContainsKey(obj)) continue;
//
            //Vector3 targetPos = orbitTargets[obj];
            //Vector3 toTarget = targetPos - obj.transform.position;
            //float distToTarget = toTarget.magnitude;
//
            //if (distToTarget > arrivalThreshold)
            //{
                //// Move towards orbit position
                //obj.ApplyForce(toTarget.normalized * (controller.orbitGravity * Time.deltaTime));
            //}
            //else
            //{
                //// Smooth orbit around world origin (0,0,0)
                //// Gravity pulls toward origin
                //Vector3 toOrigin = -obj.transform.position;
                //obj.ApplyForce(toOrigin.normalized * (controller.orbitGravity * Time.deltaTime));
//
                //// Maintain tangential velocity to keep orbiting
                //// Cross product with a consistent up vector (or local up) to get tangent
                //Vector3 tangent = Vector3.Cross(obj.transform.position, Vector3.up).normalized;
                //if (tangent.sqrMagnitude < 0.01f) // Handle case where pos is aligned with Vector3.up
                    //tangent = Vector3.Cross(obj.transform.position, Vector3.forward).normalized;
//
                //// Push along tangent to maintain orbit speed
                //float currentSpeed = obj.Velocity.magnitude;
                //float targetSpeed = (controller.minOrbitVelocity + controller.maxOrbitVelocity) * 0.5f;
                //
                //if (currentSpeed < targetSpeed)
                //{
                    //obj.ApplyForce(tangent * (controller.orbitGravity * 0.5f * Time.deltaTime));
                //}
            //}
        //}
    //}
}
