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
            
            var temp = new IQueueAction[]
            {
                new WaitForKeyAction(KeyCode.W),
                new WaitForSecondsAction(3f),
                new AsyncQueueAction(ExternalActionAsync),
                new WaitForFramesAction(30),
                new WaitForConditionAction(() => _canContinue),
                new ActionSequence( new IQueueAction[]
                {
                        
                    new WaitForKeyAction(KeyCode.W),
                    new WaitForKeyAction(KeyCode.A),
                    new WaitForKeyAction(KeyCode.S),
                    new WaitForKeyAction(KeyCode.D),
                    new ActionWithResult<int>(
                        actionFunc: () => UnityEngine.Random.Range(0, 100),
                        resultCallback: result => Debug.Log("Random Number: " + result))
                }),
                new ActionParallel(new IQueueAction[]
                {
                    new WaitForKeyAction(KeyCode.Q),
                    new WaitForKeyAction(KeyCode.W)
                }),
                new LogDebugAction("Queue Finished")
                
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

            _queue.AddUrgentAndInterrupt(new WaitForKeyAction(KeyCode.Return));
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