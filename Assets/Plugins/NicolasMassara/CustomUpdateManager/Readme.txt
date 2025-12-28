UpdateManager – Internal Documentation
Overview

UpdateManager is a centralized system to handle Update, FixedUpdate, and LateUpdate calls in Unity, with:

Custom time channels with scaling and pause/resume

Prioritized execution per update group

Tick intervals for performance optimization

Conditional updates (IUpdateConditional)

Safe runtime registration and unregistration

Setup

Include UpdateManager.cs in the project.

Attach UpdateManager to a persistent GameObject, or rely on its singleton (UpdateManager.Instance).

Optionally, add UpdateManagerDebug in the editor for visualization.

Key Concepts
Update Groups

Always (cannot be paused)

Gameplay

UI

Inputs

Priority Groups

Critical, High, Normal, Low, Background

Tick Intervals

Defines how frequently updates run:

EveryFrame

EveryHalfSecond

EveryQuarterSecond

EverySecond

Custom intervals supported

Custom Time

Each update group has a TimeChannel controlling deltaTime, fixedDeltaTime, and timeScale.

Global pause and per-channel pause supported.

Usage
Register Objects
UpdateManager.Instance.Register(myObject);
UpdateManager.Instance.Unregister(myObject);

Implement Updatable Object
public class PlayerMovement : MonoBehaviour, IUpdatable, IUpdateConditional
{
    public UpdateManager.UpdateGroup SelfUpdateGroup => UpdateManager.UpdateGroup.Gameplay;
    public UpdateManager.UpdatePriorityGroup SelfPriorityGroup => UpdateManager.UpdatePriorityGroup.Normal;
    public UpdateManager.TickGroup SelfTickGroup => UpdateManager.TickGroup.EveryFrame;

    public void ExecuteUpdate(float deltaTime)
    {
        transform.Translate(Vector3.forward * deltaTime);
    }

    public bool CanUpdate(UpdateManager.UpdateType type) => true;
}

Custom Time Control
UpdateManager.CustomTime.PauseChannel(UpdateManager.UpdateGroup.Gameplay);
UpdateManager.CustomTime.ResumeChannel(UpdateManager.UpdateGroup.Gameplay);
UpdateManager.CustomTime.SetChannelTimeScale(UpdateManager.UpdateGroup.Gameplay, 0.5f);

float dt = UpdateManager.CustomTime.GetDeltaTimeByChannel(UpdateManager.UpdateGroup.Gameplay);

Frame Rate Control
UpdateManager.SetTargetFrameRate(120);

Events

OnRegistered / OnUnregistered – Update objects

OnFixedRegistered / OnFixedUnregistered – FixedUpdate objects

OnLateRegistered / OnLateUnregistered – LateUpdate objects

Best Practices

Avoid pausing the Always group.

Use IUpdateConditional to skip unnecessary updates.

Apply lower-frequency TickGroup for non-critical objects to optimize performance.

------------------------------------
Created by Nicolás Federico Massara

Linkedin: in/nicolas-federico-massara-95818322a
Github: github.com/NicoMassara;
Itch.io: https://nicolasmassara.itch.io/
Portfolio: https://nicomassara.github.io/
Email: nicolasmassara@hotmail.com