using UnityEngine;

public abstract class AudioState
{
    protected AudioController controller;

    protected AudioState(AudioController controller)
    {
        this.controller = controller;
    }

    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
    public abstract void ApplySignals(InteractionSignals signals);
}