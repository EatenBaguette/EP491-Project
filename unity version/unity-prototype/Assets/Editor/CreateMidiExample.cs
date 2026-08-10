using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class CreateMidiExample
{
    [MenuItem("Tools/Create MIDI Example Asset")]
    public static void CreateAsset()
    {
        string path = "Assets/Scripts/MidiSets/MidiNoteSet_LydianExample.asset";
        
        MidiNoteSetDefinition asset = ScriptableObject.CreateInstance<MidiNoteSetDefinition>();
        
        // Use reflection or serialized object to set private fields if necessary, 
        // but noteSetSwitchValue and baseMidiNote are private serialized fields.
        // We'll use SerializedObject for safety.
        SerializedObject so = new SerializedObject(asset);
        so.FindProperty("noteSetSwitchValue").stringValue = "Lydian";
        so.FindProperty("baseMidiNote").intValue = 36;
        
        // voicings is a private field too
        SerializedProperty voicingsProp = so.FindProperty("voicings");
        
        // Add Voicing 1: Low_Outer
        voicingsProp.InsertArrayElementAtIndex(0);
        SerializedProperty v1 = voicingsProp.GetArrayElementAtIndex(0);
        v1.FindPropertyRelative("voicingSwitchValue").stringValue = "Low_Outer";
        v1.FindPropertyRelative("octaveOffset").intValue = 0;
        SerializedProperty notes1 = v1.FindPropertyRelative("relativeNotes");
        notes1.ClearArray();
        int[] n1 = { 0, 9 }; // Root and 6th (Lydian usually has #4, but following user example)
        for(int i=0; i<n1.Length; i++) {
            notes1.InsertArrayElementAtIndex(i);
            notes1.GetArrayElementAtIndex(i).intValue = n1[i];
        }
        
        // Add Voicing 2: Upper_Middle_Range
        voicingsProp.InsertArrayElementAtIndex(1);
        SerializedProperty v2 = voicingsProp.GetArrayElementAtIndex(1);
        v2.FindPropertyRelative("voicingSwitchValue").stringValue = "Upper_Middle_Range";
        v2.FindPropertyRelative("octaveOffset").intValue = 2;
        SerializedProperty notes2 = v2.FindPropertyRelative("relativeNotes");
        notes2.ClearArray();
        int[] n2 = { 6, 9, 12, 14 }; // #4, 6, 8, 9
        for(int i=0; i<n2.Length; i++) {
            notes2.InsertArrayElementAtIndex(i);
            notes2.GetArrayElementAtIndex(i).intValue = n2[i];
        }
        
        so.ApplyModifiedProperties();

        AssetDatabase.CreateAsset(asset, path);

        // Try to add it to the database
        string dbPath = "Assets/ScriptableObjects/MidiNoteSetDatabase.asset";
        MidiNoteSetDatabase db = AssetDatabase.LoadAssetAtPath<MidiNoteSetDatabase>(dbPath);
        if (db != null)
        {
            SerializedObject dbSo = new SerializedObject(db);
            SerializedProperty noteSetsProp = dbSo.FindProperty("noteSets");
            bool alreadyExists = false;
            for (int i = 0; i < noteSetsProp.arraySize; i++)
            {
                if (noteSetsProp.GetArrayElementAtIndex(i).objectReferenceValue == asset)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                noteSetsProp.InsertArrayElementAtIndex(noteSetsProp.arraySize);
                noteSetsProp.GetArrayElementAtIndex(noteSetsProp.arraySize - 1).objectReferenceValue = asset;
                dbSo.ApplyModifiedProperties();
                Debug.Log("Added example to MidiNoteSetDatabase.");
            }
        }

        AssetDatabase.SaveAssets();
        
        Debug.Log("Created Lydian MIDI Note Set Example at " + path);
    }
}
