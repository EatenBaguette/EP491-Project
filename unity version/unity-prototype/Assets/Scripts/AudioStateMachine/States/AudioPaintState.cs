using System.Collections.Generic;
using UnityEngine;

public class AudioPaintState : AudioState
{
    
    public AudioPaintState(AudioController controller) : base(controller) { }

    private bool flourishActive = false;

    public override void Enter()
    {
        Debug.Log("Entering Paint State");
        controller.playPaintPlaylist.Post(controller.gameObject);
        controller.playChordVoicingPoll.Post(controller.gameObject);

        if (controller.midiPlayer != null)
        {
            controller.midiPlayer.StartNote(67);
        }
    }

    public override void Update()
    {
        
    }

    public override void Exit()
    {
        if (controller.midiPlayer != null)
        {
            controller.midiPlayer.StopNote(67);
        }
    }

    public override void ApplySignals(InteractionSignals signals)
    {
        AkUnitySoundEngine.SetRTPCValue("HandHeight", signals.HandHeight);
        AkUnitySoundEngine.SetRTPCValue("MovementEnergy", signals.MovementEnergy);
        
        if (flourishActive && signals.MovementEnergy < controller.flourishMovementThreshold) flourishActive = false;
        if (signals.MovementEnergy > controller.flourishMovementThreshold && !flourishActive)
        {
            PaintChord();
        }
    }


    public void PaintChord()
    {
        //Debug.Log("Painting chord");
        flourishActive = true;
        
        if (controller.midiPlayer != null)
        {
            uint qualityId = controller.GetCurrentChordQualityId();
            uint voicingId = controller.GetCurrentChordVoicingId();
            //Debug.Log("CQ ID: " + qualityId + " CV ID: " + voicingId);
            controller.midiPlayer.PlayFromSwitches(qualityId, voicingId);
        }
    }
    
}