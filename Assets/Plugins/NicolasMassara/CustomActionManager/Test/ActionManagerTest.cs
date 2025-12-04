using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace NicolasMassara.CustomActionManager.Test
{
#if UNITY_EDITOR
    public class ActionManagerTest : MonoBehaviour
    {
        private ActionManager.GeneratedId _generatedId;
        private InterruptibleSequence _sequence;
        private bool _hasStarted;

        private bool _canContinue;

        private void Start()
        {
            _hasStarted = true;
        }
        
        public void RunQueue()
        {
            if(_hasStarted == false) return;

            var builder = ActionBuilder.Start()
                .Do(new LogDebugAction("Be Fast!"))
                .Then(new WaitSecondsAction(0.5f))
                .Then(new LogDebugAction("Prepare for Key"))
                .Then(new WaitForKeyAction(KeyCode.W))
                .Then(new LogDebugAction("Success!"))
                .Then(new WaitSecondsAction(0.5f))
                .Then(new LogDebugAction("Prepare for Key"))
                .Then(new WaitForKeyAction(KeyCode.W))
                .Then(new LogDebugAction("Success!"))
                .Then(new WaitSecondsAction(0.25f))
                .Then(new LogDebugAction("Allow to Continue"))
                .Then(new ConditionalAction(() => _canContinue))
                .Then(new LogDebugAction("Finished"))
                .WrapAll(a => new TimeoutWrapperAction(a, 10,
                    status =>
                    {
                        Debug.Log("Action Timed Out, Removed!");
                        Remove();
                    }))
                .WrapAll(a => new InterruptAwareWrapperAction(a, () =>
                {
                    Debug.Log("Sequence Interrupted!");
                }));
            
            var action = builder.Build();

            _sequence = new InterruptibleSequence(action, () =>
            {
                Debug.Log("Action Interrupted, Removed!");
            });
            
            _generatedId = ActionManager.Add(_sequence, ActionManager.UpdateType.Update, ActionManager.PriorityTick.EveryFrame);
        }

        public void Remove()
        {
            if(_hasStarted == false) return;
            
            ActionManager.Remove(_generatedId);
        }
        
        public void AllowContinue()
        {
            if(_hasStarted == false) return;
            
            _canContinue = true;
        }

        public void InterruptSequence()
        {
            if(_hasStarted == false) return;
            
            ActionManager.Clear(ActionManager.UpdateType.Update);
        }
    }
    
    public class WaitForKeyAction : IQueueAction
    {
        readonly KeyCode _key;

        public ActionStatus CurrentStatus { get; private set; }
        public WaitForKeyAction(KeyCode key) => _key = key;

        public void OnStart() => Debug.Log("Waiting For: " + _key);

        public ActionStatus OnUpdate(float deltaTime) => Input.GetKey(_key) ? ActionStatus.Success : ActionStatus.Running;

        public void OnInterrupt()
        {
            if (CurrentStatus == ActionStatus.Idle) return;
            
            Debug.Log("Interrupted " + _key);
        }

        public IQueueAction Copy()
        {
            return new WaitForKeyAction(_key);
        }
    }

#endif
    
    
#if UNITY_EDITOR
    
    [CustomEditor(typeof(ActionManagerTest))]
    public class ComponentsNameChangeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Dibuja el inspector normal
            DrawDefaultInspector();

            // Agrega el botón
            ActionManagerTest script = (ActionManagerTest)target;
            if (GUILayout.Button("Run Queue"))
            {
                // Llama al método normalmente
                script.RunQueue();
            }
            
            if (GUILayout.Button("Clear Queue"))
            {
                // Llama al método normalmente
                script.Remove();
            }
            
            if (GUILayout.Button("Allow Continue"))
            {
                // Llama al método normalmente
                script.AllowContinue();
            }
            
            if (GUILayout.Button("Interrupt Sequence"))
            {
                // Llama al método normalmente
                script.InterruptSequence();
            }
        }
    }
    
#endif
}