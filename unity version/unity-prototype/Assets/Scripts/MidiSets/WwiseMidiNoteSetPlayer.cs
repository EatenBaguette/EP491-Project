using System.Collections.Generic;
using UnityEngine;

public class WwiseMidiNoteSetPlayer : MonoBehaviour
{
    [Header("Wwise")]
    [SerializeField] private AK.Wwise.Event synthEvent;

    [Header("MIDI")]
    [SerializeField, Range(0, 15)] private byte midiChannel = 0;
    [SerializeField, Range(0, 127)] private byte noteOnVelocity = 127;

    [Header("Note Sets")]
    [SerializeField] private MidiNoteSetDatabase noteSetDatabase;

    private readonly List<byte> activeNotes = new();
    private readonly HashSet<byte> manualActiveNotes = new();

    private uint midiPlayingId;

    public void StartNote(byte note)
    {
        if (synthEvent == null) return;
        
        AkMIDIPostArray posts = new AkMIDIPostArray(1);
        posts[0] = CreateMidiPost(AkMIDIEventTypes.NOTE_ON, note, noteOnVelocity, 0);
        
        midiPlayingId = PostMidiToWwise(posts, 1, midiPlayingId);
        manualActiveNotes.Add(note);
    }

    public void StopNote(byte note)
    {
        if (synthEvent == null) return;
        if (!manualActiveNotes.Contains(note)) return;

        AkMIDIPostArray posts = new AkMIDIPostArray(1);
        posts[0] = CreateMidiPost(AkMIDIEventTypes.NOTE_OFF, note, 0, 0);

        PostMidiToWwise(posts, 1, midiPlayingId);
        manualActiveNotes.Remove(note);
    }

    public void PlayFromSwitches(uint noteSetSwitchId, uint voicingSwitchId)
    {
        if (synthEvent == null)
        {
            Debug.LogWarning($"{nameof(WwiseMidiNoteSetPlayer)} has no synth event assigned.");
            return;
        }

        if (noteSetDatabase == null)
        {
            Debug.LogWarning($"{nameof(WwiseMidiNoteSetPlayer)} has no note-set database assigned.");
            return;
        }

        if (!noteSetDatabase.TryGetNoteSet(noteSetSwitchId, out MidiNoteSetDefinition noteSet))
        {
            Debug.LogWarning($"No MIDI note set found for switch ID '{noteSetSwitchId}'.");
            SendNoteOffsForActiveNotes();
            return;
        }

        List<byte> notesToPlay = noteSet.GetMidiNotesForVoicing(voicingSwitchId);

        ReplaceActiveNotes(notesToPlay);
    }

    public void StopActiveNotes()
    {
        SendNoteOffsForActiveNotes();
        StopManualNotes();
    }

    private void StopManualNotes()
    {
        if (manualActiveNotes.Count == 0 || synthEvent == null)
        {
            manualActiveNotes.Clear();
            return;
        }

        AkMIDIPostArray posts = new AkMIDIPostArray(manualActiveNotes.Count);
        int i = 0;
        foreach (byte note in manualActiveNotes)
        {
            posts[i++] = CreateMidiPost(AkMIDIEventTypes.NOTE_OFF, note, 0, 0);
        }

        PostMidiToWwise(posts, manualActiveNotes.Count, midiPlayingId);
        manualActiveNotes.Clear();
    }

    [ContextMenu("Panic Stop MIDI")]
    public void PanicStopMidi()
    {
        activeNotes.Clear();
        manualActiveNotes.Clear();
        midiPlayingId = 0;

        if (synthEvent != null)
        {
            synthEvent.StopMIDI(gameObject);
        }
    }
    
    [ContextMenu("Post Note On Off 60")]
    public void PostNoteOnOff60()
    {
        AkMIDIPostArray midiPostArray = new AkMIDIPostArray(2);
        AkMIDIPost midiPost = new AkMIDIPost();
    
        midiPost.byType = AkMIDIEventTypes.NOTE_ON;
        midiPost.byChan = midiChannel;
        midiPost.byOnOffNote = 60;
        midiPost.byVelocity = noteOnVelocity;
        midiPost.uOffset = 0;
        midiPostArray[0] = midiPost;
    
        midiPost.byType = AkMIDIEventTypes.NOTE_OFF;
        midiPost.byChan = midiChannel;
        midiPost.byOnOffNote = 60;
        midiPost.byVelocity = 0;
        midiPost.uOffset = 48000 * 2;
        midiPostArray[1] = midiPost;
    
        synthEvent.PostMIDI(gameObject, midiPostArray);
    }


    private void ReplaceActiveNotes(List<byte> newNotes)
    {
        SendNoteOffsForActiveNotes();

        if (newNotes == null || newNotes.Count == 0)
        {
            return;
        }

        AkMIDIPostArray noteOnPosts = new AkMIDIPostArray(newNotes.Count);

        for (int i = 0; i < newNotes.Count; i++)
        {
            byte note = newNotes[i];

            noteOnPosts[i] = CreateMidiPost(
                AkMIDIEventTypes.NOTE_ON,
                note,
                noteOnVelocity,
                offset: 0
            );

            activeNotes.Add(note);
        }

        midiPlayingId = PostMidiToWwise(noteOnPosts, newNotes.Count, midiPlayingId);
    }

    private void SendNoteOffsForActiveNotes()
    {
        if (synthEvent == null)
        {
            activeNotes.Clear();
            midiPlayingId = 0;
            return;
        }

        if (activeNotes.Count == 0)
        {
            return;
        }

        AkMIDIPostArray noteOffPosts = new AkMIDIPostArray(activeNotes.Count);

        for (int i = 0; i < activeNotes.Count; i++)
        {
            noteOffPosts[i] = CreateMidiPost(
                AkMIDIEventTypes.NOTE_OFF,
                activeNotes[i],
                velocity: 0,
                offset: 0
            );
        }

        PostMidiToWwise(noteOffPosts, activeNotes.Count, midiPlayingId);

        activeNotes.Clear();
    }

    private uint PostMidiToWwise(AkMIDIPostArray midiPosts, int count, uint targetPlayingId)
    {
        if (synthEvent == null || !synthEvent.IsValid())
        {
            return 0;
        }

        ulong gameObjectId = AkUnitySoundEngine.GetAkGameObjectID(gameObject);

        AkUnitySoundEngine.PreGameObjectAPICall(gameObject, gameObjectId);

        return AkUnitySoundEngine.PostMIDIOnEvent(
            synthEvent.Id,
            gameObjectId,
            midiPosts,
            (ushort)count,
            false,
            0,
            null,
            null,
            targetPlayingId
        );
    }

    private AkMIDIPost CreateMidiPost(
        AkMIDIEventTypes eventType,
        byte note,
        byte velocity,
        uint offset
    )
    {
        AkMIDIPost midiPost = new AkMIDIPost
        {
            byType = eventType,
            byChan = midiChannel,
            byOnOffNote = note,
            byVelocity = velocity,
            uOffset = offset
        };

        return midiPost;
    }

    private void OnDisable()
    {
        StopActiveNotes();
    }

    private void OnDestroy()
    {
        StopActiveNotes();
    }
}