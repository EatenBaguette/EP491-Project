using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MidiNoteSetDatabase",
    menuName = "Scriptable Objects/MIDI Note Set Database"
)]
public class MidiNoteSetDatabase : ScriptableObject
{
    [SerializeField] private List<MidiNoteSetDefinition> noteSets = new();

    private Dictionary<uint, MidiNoteSetDefinition> idToNoteSetLookup;

    public bool TryGetNoteSet(uint noteSetSwitchId, out MidiNoteSetDefinition noteSet)
    {
        EnsureLookup();

        if (noteSetSwitchId == 0)
        {
            noteSet = null;
            return false;
        }

        return idToNoteSetLookup.TryGetValue(noteSetSwitchId, out noteSet);
    }

    private void EnsureLookup()
    {
        if (idToNoteSetLookup != null)
        {
            return;
        }

        idToNoteSetLookup = new Dictionary<uint, MidiNoteSetDefinition>();

        foreach (MidiNoteSetDefinition noteSet in noteSets)
        {
            if (noteSet == null)
            {
                continue;
            }

            uint switchId = noteSet.NoteSetSwitchId;

            if (switchId == 0)
            {
                Debug.LogWarning($"{noteSet.name} has an empty/zero note-set switch ID.");
                continue;
            }

            if (!idToNoteSetLookup.ContainsKey(switchId))
            {
                idToNoteSetLookup.Add(switchId, noteSet);
            }
            else
            {
                Debug.LogWarning(
                    $"Duplicate MIDI note-set switch ID '{switchId}' found. Keeping the first one."
                );
            }
        }
    }
}
