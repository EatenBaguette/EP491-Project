using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PaintState : VisualState
{
    private Dictionary<VisualPaint, bool> poolAvailability = new Dictionary<VisualPaint, bool>();

    public PaintState(VisualController controller) : base(controller) { }

    public override void Enter()
    {
        Debug.Log("Entering Paint State");
        
        poolAvailability.Clear();
        foreach (var obj in controller.VisualPaints)
        {
            poolAvailability[obj] = true;
            obj.SetActive(false);
            obj.SetRevealImmediate(0f);
        }
    }

    public override void Update()
    {
        Camera cam = Camera.main;
        // Monitor active objects and return them to pool if they are no longer rendered
        foreach (var obj in controller.VisualPaints)
        {
            if (!poolAvailability[obj])
            {
                // If target is 0 and it's almost invisible, return to pool
                if (obj.TargetRevealAmount <= 0.001f && obj.RevealAmount < 0.01f)
                {
                    ReturnToPool(obj);
                    continue;
                }

                // Distance threshold fadeout
                if (cam != null)
                {
                    float dist = Vector3.Distance(cam.transform.position, obj.transform.position);
                    if (dist > controller.paintFadeDistance)
                    {
                        obj.SetRevealTarget(0f);
                    }
                }
            }
        }
    }

    private void ReturnToPool(VisualPaint obj)
    {
        obj.SetRevealImmediate(0f);
        obj.SetActive(false);
        poolAvailability[obj] = true;
    }

    public override void Exit()
    {
    }

    public override void ApplySignals(InteractionSignals signals)
    {
        if (signals.MovementEnergy > controller.paintEnergyThreshold)
        {
            PaintObjects(signals);
        }
        else
        {
            // If energy is low, start fading out active objects
            //FadeOutActiveObjects();
        }
    }

    private void FadeOutActiveObjects()
    {
        foreach (var obj in controller.VisualPaints)
        {
            if (!poolAvailability[obj])
            {
                obj.SetRevealTarget(0f);
            }
        }
    }

    private void PaintObjects(InteractionSignals signals)
    {
        int paintObjectCount = (int) Mathf.Round(controller.paintObjectCount * signals.MovementEnergy / 100f);
        
        List<VisualPaint> selectedObjects = new List<VisualPaint>();
        int count = 0;
        foreach (var obj in controller.VisualPaints)
        {
            if (poolAvailability[obj])
            {
                selectedObjects.Add(obj);
                poolAvailability[obj] = false;
                count++;
                if (count >= paintObjectCount) break;
            }
        }

        if (selectedObjects.Count == 0) return;

        // Direction: average direction of hand movement
        Vector3 direction = (Vector3)(signals.LeftHandDirection + signals.RightHandDirection).normalized;
        if (direction == Vector3.zero) direction = Vector3.forward;

        // Origin: mapped based on hand signals
        Vector3 origin = Vector3.zero;
        Camera cam = Camera.main;

        if (cam != null)
        {
            float targetZ = 10f; // Distance in front of camera
            
            // Map average hand X to viewport X
            // Average Hand X: x/2 = right (1.0 viewport), -x/2 = left (0.0 viewport)
            float avgX = (signals.LeftHandPosition.x + signals.RightHandPosition.x) / 2f;
            float xRange = controller.maxHandDistance / 2f;
            float viewportX = Mathf.InverseLerp(-xRange / 2f, xRange / 2f, avgX);
            
            // Map HandHeight (average hand Y) to viewport Y
            // HandHeight: y = top (1.0 viewport), -y = bottom (0.0 viewport)
            float yMax = controller.maxHandHeight;
            float yMin = controller.minHandHeight;
            float viewportY = Mathf.InverseLerp(yMin, yMax, signals.HandHeight);
            
            Vector3 viewportPos = new Vector3(viewportX, viewportY, targetZ);
            origin = cam.ViewportToWorldPoint(viewportPos);
        }

        float lineLength = controller.paintLineLength * (signals.MovementEnergy/100f);
        float spacing = lineLength / selectedObjects.Count;

        Camera mainCam = Camera.main;

        for (int i = 0; i < selectedObjects.Count; i++)
        {
            VisualPaint obj = selectedObjects[i];
            Vector3 pos = origin + direction * (i + 1) * spacing;
            obj.ResetObject(pos);
            obj.SetActive(true);

            // Set reveal amount based on hand distance
            float t = Mathf.InverseLerp(controller.minHandDistance, controller.maxHandDistance, signals.HandDistance);
            float targetReveal = Mathf.Lerp(0.01f, 1f, t);
            obj.SetRevealTarget(targetReveal);

            // Set reveal acceleration based on movement energy
            // Map movement energy to min/max reveal acceleration. 
            float energyT = Mathf.Clamp01(signals.MovementEnergy / controller.maxMovementEnergy);
            float revealAcc = Mathf.Lerp(controller.minRevealAcceleration, controller.maxRevealAcceleration, energyT);
            obj.SetRevealAcceleration(revealAcc);

            // Set continuous velocity: randomized direction within a cone shape aligned with camera forward
            //if (mainCam != null)
            //{
            //    // Center direction: camera forward
            //    Vector3 centerDirection = mainCam.transform.forward;
            //    
            //    // Randomize within a cone. Use half of FOV as the max angle to stay within view roughly
            //    float maxAngle = mainCam.fieldOfView * 0.5f;
            //    float randomAngle = Random.Range(0f, maxAngle);
            //    float randomRotation = Random.Range(0f, 360f);
            //    
            //    Quaternion coneRotation = Quaternion.AngleAxis(randomAngle, Vector3.up);
            //    Quaternion spinRotation = Quaternion.AngleAxis(randomRotation, Vector3.forward);
            //    
            //    // Combine rotations to point in the direction 'centerDirection'
            //    Quaternion lookRotation = Quaternion.LookRotation(centerDirection);
            //    Vector3 randomizedDirection = lookRotation * coneRotation * spinRotation * Vector3.forward;
            //    
            //    obj.SetConstantVelocity(randomizedDirection * controller.paintVelocitySpeed);
            //}
            //else
            //{
            //    obj.SetConstantVelocity(controller.paintVelocityDirection.normalized * controller.paintVelocitySpeed);
            //}
            obj.SetConstantVelocity(controller.paintVelocityDirection.normalized * controller.paintVelocitySpeed);
        }
    }
}