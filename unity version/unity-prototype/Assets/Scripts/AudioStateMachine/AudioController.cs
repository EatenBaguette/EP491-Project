using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance;

    public AudioPaintState Paint;
    private AudioState currentState;

    public AudioState EdgeOrbit;
    public AudioState Idle;
    public AudioState Lines;
    public AudioState Squares;
    public AudioState Scatter;
    public AudioState Collapse;

    public enum StartState { EdgeOrbit, Idle, Lines, Squares, Scatter, Collapse, Paint }

    [Header("Start Settings")]
    [SerializeField] private StartState startState = StartState.Paint;

    [Header("Paint Settings")] 
    public float flourishMovementThreshold = 250f;
    public AK.Wwise.Event playPaintPlaylist;
    public AK.Wwise.Event playChordVoicingPoll;

    [Header("Wwise Fields")]
    public AK.Wwise.Switch chordQualityGroup;
    public AK.Wwise.Switch chordVoicingGroup;

    [Header("MIDI Player")]
    public WwiseMidiNoteSetPlayer midiPlayer;
    public MidiNoteSetDatabase noteSetDatabase;

    [Header("Debug")] public float debugValue;
    
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

    public uint GetCurrentChordQualityId()
    {
        return GetSwitchId(chordQualityGroup);
    }

    public uint GetCurrentChordVoicingId()
    {
        return GetSwitchId(chordVoicingGroup);
    }

    private uint GetSwitchId(AK.Wwise.Switch switchGroup)
    {
        if (switchGroup == null || !switchGroup.IsValid())
        {
            Debug.Log("Invalid switch group");
            return 0;
        }

        uint switchId;
        AKRESULT result = AkUnitySoundEngine.GetSwitch(switchGroup.GroupId, gameObject, out switchId);

        if (result != AKRESULT.AK_Success)
        {
            Debug.Log("Failed to get switch ID");
            return 0;
        }

        return switchId;
    }

    private void InitializeStates()
    {
        // EdgeOrbit = new AudioEdgeOrbitState(this);
        // Idle = new AudioIdleState(this);
        // Lines = new AudioLinesState(this);
        // Squares = new AudioSquaresState(this);
        // Scatter = new AudioScatterState(this);
        // Collapse = new AudioCollapseState(this);
        Paint = new AudioPaintState(this);
    }
    
    void Start()
    {
        ChangeState(GetStartState());
    }

    private AudioState GetStartState()
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
                return Paint;
        }
    }

    // Update is called once per frame
    void Update()
    {
        currentState?.Update();
    }
    
    public void ApplySignals(InteractionSignals signals)
    {
        if (signals == null) return;
        currentState?.ApplySignals(signals);
        CheckSwitchState(signals);
    }


    public void ChangeState(AudioState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }
    
    void CheckSwitchState(InteractionSignals signals)   
    {
        if (signals.GestureChanged)
        {
            // Example:
            // chordQualityGroup.SetValue(gameObject);
        }
    }
}