using System.Collections.Generic;
using UnityEngine;

public class VisualController : MonoBehaviour
{
    public static VisualController Instance;

    public List<VisualCube> VisualCubes = new List<VisualCube>();
    public List<VisualPaint> VisualPaints = new List<VisualPaint>();

    [Header("Pool Settings")]
    [SerializeField] private VisualCube visualCubePrefab;
    [SerializeField] private int cubeCount = 50;
    [SerializeField] private VisualPaint visualPaintPrefab;
    [SerializeField] private int paintCount = 50;

    [SerializeField] private bool activateVisualCubesOnStart = false;
    [SerializeField] private bool activateVisualPaintsOnStart = false;
    
    public enum StartState { EdgeOrbit, Idle, Lines, Squares, Scatter, Collapse, Paint }
    public enum StartMode { Center, Field }
    
    [Header("Start Settings")]
    [SerializeField] private StartState startState = StartState.Idle;
    [SerializeField] private StartMode startMode = StartMode.Center;
    [SerializeField] private float fieldStartDepth = 5f;
    
    public Vector3 PrefabScale => visualCubePrefab != null
        ? visualCubePrefab.transform.localScale
        : Vector3.one;
    public Vector3 PaintPrefabScale => visualPaintPrefab != null
        ? visualPaintPrefab.transform.localScale
        : Vector3.one;
    

    [Header("Force Settings")]
    [SerializeField] private float edgePushForce = 5f;
    [SerializeField] private float attractionForce = 0.5f;
    [SerializeField] private float repulsionForce = 0.5f;
    [SerializeField] private float noiseForce = 0.2f;

    [Header("Edge Orbit Settings")]
    public float minOrbitDistance = 2f;
    public float maxOrbitDistance = 10f;
    public float minOrbitVelocity = 1f;
    public float maxOrbitVelocity = 5f;
    public float orbitGravity = 5f;
    public AnimationCurve handDistanceCurve;

    [Header("Idle Settings")]
    public float idleGravity = 5f;

    [Header("Paint State Settings")]
    public float paintEnergyThreshold = 0.5f;
    public int paintObjectCount = 5;
    public float paintLineLength = 2f;
    public float minHandDistance = 0.1f;
    public float maxHandDistance = 1.0f;
    public float minHandHeight = -350f;
    public float maxHandHeight = 100f;
    public float minRevealAcceleration = 0.01f;
    public float maxRevealAcceleration = 0.2f;
    public Vector3 paintVelocityDirection = new Vector3(0, 0, 1);
    public float paintVelocitySpeed = 1f;
    public float paintConeAngle = 30f;
    public float paintFadeDistance = 20f;
    public float maxMovementEnergy = 300;

    [Header("Viewport Repel Mask")]
    public float centerRepelRadius = 2.5f;
    public float centerRepelForce = 12f;
    public float centerRepelFalloff = 2f;
    public float centerRepelReferenceDepth = 10f;
    public float handDistanceMultiplier = 600f;

    [Header("Smooth Motion Settings")]
    [SerializeField] private float friction = 0.98f; // Higher friction for smoother, floating motion

    private VisualState currentState;

    public EdgeOrbitState EdgeOrbit;
    public IdleState Idle;
    public LinesState Lines;
    public SquaresState Squares;
    public ScatterState Scatter;
    public CollapseState Collapse;
    public PaintState Paint;
    
    
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeStates();
    }

    private void InitializeStates()
    {
        EdgeOrbit = new EdgeOrbitState(this);
        Idle = new IdleState(this);
        Lines = new LinesState(this);
        Squares = new SquaresState(this);
        Scatter = new ScatterState(this);
        Collapse = new CollapseState(this);
        Paint = new PaintState(this);
    }

    void Start()
    {
        SpawnObjects();
        // Initially objects are inactive. We activate them when we start the first state.
        ActivateObjects();
        ChangeState(GetStartState());
    }
    
    private VisualState GetStartState()
    {
        switch (startState)
        {
            case StartState.EdgeOrbit:
                return EdgeOrbit;

            case StartState.Idle:
                return Idle;

            case StartState.Lines:
                return Lines;

            case StartState.Squares:
                return Squares;

            case StartState.Scatter:
                return Scatter;

            case StartState.Collapse:
                return Collapse;

            case StartState.Paint:
                return Paint;

            default:
                return Idle;
        }
    }

    private void SpawnObjects()
    {
        if (visualCubePrefab == null)
        {
            Debug.LogError("[VisualController] VisualCube prefab not assigned!");
            return;
        }

        for (int i = 0; i < cubeCount; i++)
        {
            VisualCube obj = Instantiate(visualCubePrefab, Vector3.zero, Quaternion.identity, transform);
            obj.SetFriction(friction);
            obj.SetActive(false);
            VisualCubes.Add(obj);
        }
        for (int i = 0; i < paintCount; i++)
        {
            VisualPaint obj = Instantiate(visualPaintPrefab, Vector3.zero, Random.rotation, transform);
            obj.SetFriction(friction);
            obj.SetActive(false);
            VisualPaints.Add(obj);
        }
    }

    private void ActivateObjects()
    {
        Camera cam = Camera.main;
        Vector3 spawnCenter = Vector3.zero;
        if (cam != null)
        {
            spawnCenter = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
        }

        foreach (var obj in VisualCubes)
        {
            if (obj != null)
            {
                if (startMode == StartMode.Center)
                {
                    obj.ResetObject(spawnCenter);
                }
                else if (startMode == StartMode.Field)
                {
                    float halfSize = maxOrbitDistance * 0.5f;
                    Vector3 randomPos = new Vector3(
                        Random.Range(-halfSize, halfSize),
                        Random.Range(-halfSize, halfSize),
                        Random.Range(-fieldStartDepth, fieldStartDepth)
                    );
                    obj.ResetObject(spawnCenter + randomPos);
                }
                if (activateVisualCubesOnStart) obj.SetActive(true);
            }
        }
        foreach (var obj in VisualPaints)
        {
            if (obj != null)
            {
               if (activateVisualPaintsOnStart) obj.SetActive(true);
            }
        }
    }

    void Update()
    {
        currentState?.Update();

        foreach (var obj in VisualCubes)
        {
            if (obj != null && obj.gameObject.activeSelf)
                obj.Tick(Time.deltaTime);
        }
        foreach (var obj in VisualPaints)
        {
            if (obj != null && obj.gameObject.activeSelf)
                obj.Tick(Time.deltaTime);
        }
    }

    public void ChangeState(VisualState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public void ApplySignals(InteractionSignals signals)
    {
        if (signals == null) return;
        currentState?.ApplySignals(signals);
        CheckSwitchState(signals);
    }

    private void ApplySignalsUnused(InteractionSignals signals)
    {
        if (signals == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // Convert normalized hand/body positions to world space if available
        Vector3 leftHandWorld = Vector3.zero;
        Vector3 rightHandWorld = Vector3.zero;
        Vector3 bodyCenterWorld = Vector3.zero;

        if (signals.IsPoseAvailable)
        {
            leftHandWorld = cam.ViewportToWorldPoint(new Vector3(signals.LeftHandPosition.x, signals.LeftHandPosition.y, 10f));
            rightHandWorld = cam.ViewportToWorldPoint(new Vector3(signals.RightHandPosition.x, signals.RightHandPosition.y, 10f));
            bodyCenterWorld = cam.ViewportToWorldPoint(new Vector3(signals.BodyCenter.x, signals.BodyCenter.y, 10f));
        }

        foreach (var obj in VisualCubes)
        {
            if (obj == null || !obj.gameObject.activeSelf) continue;
            
            Vector3 pos = obj.transform.position;
            Vector3 combinedForce = Vector3.zero;

            // 1. Base force: gently toward edges
            Vector3 screenPos = cam.WorldToViewportPoint(pos);
            Vector3 directionFromCenter = new Vector3(screenPos.x - 0.5f, screenPos.y - 0.5f, 0);
            
            // If we are exactly at center, give a tiny random push to start movement
            if (directionFromCenter.sqrMagnitude < 0.0001f)
            {
                directionFromCenter = Random.insideUnitSphere.normalized * 0.1f;
            }
            
            Vector3 edgeForce = directionFromCenter.normalized * edgePushForce;
            combinedForce += edgeForce;

            // 2. VisualAttraction: pull toward hand positions
            if (signals.IsPoseAvailable && signals.VisualAttraction > 0)
            {
                Vector3 toLeft = leftHandWorld - pos;
                Vector3 toRight = rightHandWorld - pos;
                combinedForce += toLeft.normalized * (signals.VisualAttraction * attractionForce);
                combinedForce += toRight.normalized * (signals.VisualAttraction * attractionForce);
            }

            // 3. VisualRepulsion: push away from body center
            if (signals.IsPoseAvailable && signals.VisualRepulsion > 0)
            {
                Vector3 fromBody = pos - bodyCenterWorld;
                combinedForce += fromBody.normalized * (signals.VisualRepulsion * repulsionForce);
            }

            // 4. VisualNoise: random movement
            if (signals.VisualNoise > 0)
            {
                Vector3 noise = Random.insideUnitSphere;
                combinedForce += noise * (signals.VisualNoise * noiseForce);
            }

            // 5. VisualPulse: outward push
            if (signals.VisualPulse > 0)
            {
                Vector3 outward = Vector3.zero;
                if (signals.IsPoseAvailable)
                {
                    outward = (pos - bodyCenterWorld).normalized;
                }
                else
                {
                    // Fallback to center of screen if no pose
                    Vector3 worldCenter = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
                    outward = (pos - worldCenter).normalized;
                    
                    // If exactly at center, push in a random direction
                    if (outward.sqrMagnitude < 0.0001f)
                        outward = Random.insideUnitSphere.normalized;
                }
                
                combinedForce += outward * (signals.VisualPulse * 10.0f); // Stronger kick for pulse
                
                // Also brief scale effect
                obj.transform.localScale = Vector3.one * (1.0f + signals.VisualPulse * 0.5f);
            }
            else
            {
                obj.transform.localScale = Vector3.one;
            }

            obj.ApplyForce(combinedForce);
        }
    }

    void CheckSwitchState(InteractionSignals signals)   
    {
        if (signals.GestureChanged)
        {
            //if (signals.GestureIndex == 0) ChangeState(Idle);
            //else if (signals.GestureIndex == 1) ChangeState(EdgeOrbit);
        }
    }
}
