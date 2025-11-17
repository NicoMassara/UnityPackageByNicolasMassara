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

        public void AddInterrupter()
        {
            if(_hasStarted == false) return;

            LinkedActionManager.AddUrgentAndInterrupt(_generatedId, new LogDebugAction("Interrupted By This Message"));
        }
        
        public void AddUrgentNext()
        {
            if(_hasStarted == false) return;

            LinkedActionManager.AddUrgentNext(_generatedId, new []
            {
                new LogDebugAction("This is an Urgent Message 1"),
                new LogDebugAction("This is an Urgent Message 2"),
                new LogDebugAction("This is an Urgent Message 3"),
            });
        }

        public void AllowContinue()
        {
            if(_hasStarted == false) return;
            
            _canContinue = true;
        }

        public void InterruptSequence()
        {
            _sequence.OnInterrupt();
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
            
            if (GUILayout.Button("Add Interrupter"))
            {
                // Llama al método normalmente
                script.AddInterrupter();
            }
            
            if (GUILayout.Button("Add Urgent Next"))
            {
                // Llama al método normalmente
                script.AddUrgentNext();
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