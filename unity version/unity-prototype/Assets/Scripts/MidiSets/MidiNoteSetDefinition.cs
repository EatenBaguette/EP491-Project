using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MidiNoteSet_",
    menuName = "Scriptable Objects/MIDI Note Set"
)]
public class MidiNoteSetDefinition : ScriptableObject
{
    [Serializable]
    public class Voicing
    {
        [Tooltip("The Wwise Switch ID that selects this voicing.")]
        public uint voicingSwitchId;
        
        [Tooltip("Octave offset for this voicing.")]
        public int octaveOffset;
        
        [Tooltip("Relative MIDI note values (intervals) for this voicing.")]
        public List<int> relativeNotes = new();
    }

    [Header("Selection")]
    [Tooltip("The Wwise Switch ID that selects this note set.")]
    [SerializeField] private uint noteSetSwitchId;

    [Header("Notes")]
    [Tooltip("Common base MIDI note for all voicings in this set.")]
    [SerializeField] private int baseMidiNote = 24;

    [Header("Voicings")]
    [SerializeField] private List<Voicing> voicings = new();

    public uint NoteSetSwitchId => noteSetSwitchId;
    public int BaseMidiNote => baseMidiNote;

    public List<byte> GetMidiNotesForVoicing(uint voicingSwitchId)
    {
        Voicing voicing = voicings.Find(v => v.voicingSwitchId == voicingSwitchId);
        
        if (voicing == null)
        {
            Debug.LogWarning($"{name}: No voicing found for switch ID '{voicingSwitchId}'.");
            return new List<byte>();
        }

        List<byte> notes = new();
        int octaveMidiOffset = voicing.octaveOffset * 12;

        foreach (int relativeNote in voicing.relativeNotes)
        {
            int midiNote = baseMidiNote + octaveMidiOffset + relativeNote;

            if (midiNote < 0 || midiNote > 127)
            {
                Debug.LogWarning($"{name}: MIDI note {midiNote} is outside valid MIDI range 0-127. Voicing switch ID: {voicingSwitchId}.");
                continue;
            }

            notes.Add((byte)midiNote);
        }

        return notes;
    }
}
