using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace NicolasMassara.CustomActionManager.Scripts.Test
{
#if UNITY_EDITOR
    public class LinkedActionTest : MonoBehaviour
    {
        private ActionQueue _queue;
        private bool _hasStarted;

        private bool _canContinue;

        private void Start()
        {
            _hasStarted = true;
            _queue = new ActionQueue();
        }
        
        private void Update()
        {
            _queue.Execute(Time.deltaTime);
        }

        public void RunQueue()
        {
            if(_hasStarted == false) return;
            
            var temp = new QueueActionData[]
            {
                new()
                {
                    Action = new WaitForKeyAction(KeyCode.W)
                },
                new()
                {
                    Action = new WaitForSecondsAction(3f),
                    StartCallback = ()=> Debug.Log("Waiting for seconds")
                },
                new()
                {
                    Action = new AsyncQueueAction(ExternalActionAsync),
                    StartCallback = ()=> Debug.Log("Waiting for Async")
                },
                new()
                {
                    Action = new WaitForFramesAction(30),
                    StartCallback = ()=> Debug.Log("Waiting for frames")
                },
                new()
                {
                    Action = new WaitForConditionAction(() => _canContinue),
                    StartCallback = ()=> Debug.Log("Waiting for bool")
                },
                new()
                {
                    Action = new ActionSequence( new QueueActionData[]
                    {
                        new() { Action = new WaitForKeyAction(KeyCode.W) },
                        new() { Action = new WaitForKeyAction(KeyCode.A) },
                        new() { Action = new WaitForKeyAction(KeyCode.S) },
                        new() { Action = new WaitForKeyAction(KeyCode.D) },
                        new()
                        {
                            Action = new ActionWithResult<int>(
                                actionFunc: () => UnityEngine.Random.Range(0, 100),
                                resultCallback: result => Debug.Log("Random Number: " + result)),
                            StartCallback = () =>
                            {
                                Debug.Log("Action With Result Started");
                            },
                            EndCallback = () =>
                            {
                                Debug.Log("Action With Result Finished");
                            }
                    
                        }
                    }),
                    StartCallback = () =>
                    {
                        Debug.Log("Sequence Started");
                    },
                    EndCallback = () =>
                    {
                        Debug.Log("Sequence Finished");
                    }
                },
                new ()
                {
                    Action = new ActionParallel(new QueueActionData[]
                    {
                        new() { Action = new WaitForKeyAction(KeyCode.Q), EndCallback = ()=> Debug.Log("Key Pressed")},
                        new() { Action = new WaitForKeyAction(KeyCode.W), EndCallback = ()=> Debug.Log("Key Pressed")},
                    }),
                    StartCallback = () => Debug.Log("Parallel Started"),
                    EndCallback = () => Debug.Log("Parallel Finished")
                }
                
            };
            
            _queue.AddAction(temp);
        }
        
        async Task ExternalActionAsync()
        {
            await Task.Delay(5000);
            Console.WriteLine("Evento externo completado!");
        }

        public void AddInterrupter()
        {
            if(_hasStarted == false) return;
            
            _queue.AddUrgentAndInterrupt(new QueueActionData
            {
                Action = new WaitForKeyAction(KeyCode.Return),
                EndCallback = () => { Debug.Log("Nigga");}
            });
        }

        public void AllowContinue()
        {
            if(_hasStarted == false) return;
            
            _canContinue = true;
        }
    }
    
    public class WaitForKeyAction : IQueueAction
    {
        readonly KeyCode _key;

        public WaitForKeyAction(KeyCode key) => _key = key;

        public void OnStart() => Debug.Log("Waiting For: " + _key);

        public ActionStatus OnUpdate(float deltaTime) => Input.GetKey(_key) ? ActionStatus.Success : ActionStatus.Running;

        public void OnInterrupt() => Debug.Log("Interrupted " + _key);
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
            
            if (GUILayout.Button("Add Interrupter"))
            {
                // Llama al método normalmente
                script.AddInterrupter();
            }
            
            if (GUILayout.Button("Allow Continue"))
            {
                // Llama al método normalmente
                script.AllowContinue();
            }
        }
    }
    
#endif
}