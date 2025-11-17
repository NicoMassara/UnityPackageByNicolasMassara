using System;
using System.Threading.Tasks;
using NicolasMassara.TimedActionManager;
using UnityEditor;
using UnityEngine;

namespace NicolasMassara.CustomActionManager.Scripts.Test
{
#if UNITY_EDITOR
    public class LinkedActionTest : MonoBehaviour
    {
        private LinkedActionManager.GeneratedId _generatedId;
        private InterruptibleSequence _sequence;
        private bool _hasStarted;

        private bool _canContinue;

        private void Start()
        {
            _hasStarted = true;
        }
        
        async Task ExternalActionAsync()
        {
            await Task.Delay(5000);
            Debug.Log("External Event Finished!");
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
                .WrapAll(a => new TimeoutAction(a, 10,
                    status =>
                    {
                        Debug.Log("Action Timed Out, Removed!");
                        Remove();
                    }))
                .WrapAll(a => new InterruptAwareAction(a, () =>
                {
                    Debug.Log("Sequence Interrupted!");
                }));
            
            var action = builder.Build();

            _sequence = new InterruptibleSequence(action, () =>
            {
                Debug.Log("Action Interrupted, Removed!");
            });
            
            _generatedId = LinkedActionManager.Add(action, PriorityTick.High);
        }

        public void Remove()
        {
            if(_hasStarted == false) return;
            
            LinkedActionManager.Remove(_generatedId);
        }
        
        public void AllowContinue()
        {
            if(_hasStarted == false) return;
            
            _canContinue = true;
        }

        public void InterruptSequence()
        {
            if(_hasStarted == false) return;
            
            LinkedActionManager.Clear();
            
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
    }

#endif
    
    
#if UNITY_EDITOR
    
    [CustomEditor(typeof(LinkedActionTest))]
    public class ComponentsNameChangeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Dibuja el inspector normal
            DrawDefaultInspector();

            // Agrega el botón
            LinkedActionTest script = (LinkedActionTest)target;
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