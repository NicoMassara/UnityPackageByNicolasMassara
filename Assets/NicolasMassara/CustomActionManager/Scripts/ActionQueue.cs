using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace NicolasMassara.CustomActionManager
{

    #region External Tools
    
    public interface IQueueAction
    {
        public void OnStart();
        public ActionStatus OnUpdate(float deltaTime);
        public void OnInterrupt();   
    }

    public class QueueActionData
    {
        public IQueueAction Action;
        public Action StartCallback;
        public Action EndCallback;
        public bool HasCallback => EndCallback != null || StartCallback != null;
    }
    
    public enum ActionStatus
    {
        Running,
        Success,
        Failure
    }
    
    public sealed class ActionQueueTools
    {
        public static IQueueAction FilterCallback(QueueActionData data)
        {
            return data.HasCallback ? 
                new ActionWithCallBack(data.Action, data.StartCallback, data.EndCallback)
                : data.Action;
        }

        public static Queue<IQueueAction> CreateQueue(IEnumerable<QueueActionData> data)
        {
            var tempQueue = new Queue<IQueueAction>();
            
            foreach (QueueActionData item in data)
            {
                tempQueue.Enqueue(FilterCallback(item));
            }
            
            return tempQueue;
        }
        
        public static List<IQueueAction> CreateList(IEnumerable<QueueActionData> data)
        {
            var temp = new List<IQueueAction>();
            
            foreach (QueueActionData item in data)
            {
                temp.Add(FilterCallback(item));
            }
            
            return temp;
        }
    }

    #region Wrappers
    
    public class ActionWithCallBack : IQueueAction
    {
        private readonly IQueueAction _inner;
        private readonly Action _startCallback;
        private readonly Action _endCallback;

        public ActionWithCallBack(IQueueAction inner,  Action startCallback, Action endCallback)
        {
            _inner = inner;
            _startCallback = startCallback;
            _endCallback = endCallback;
        }

        public void OnStart()
        {
            _startCallback?.Invoke();
            _inner?.OnStart();
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            var status = _inner.OnUpdate(deltaTime);
            if (status == ActionStatus.Success) _endCallback?.Invoke();
            return status;
        }

        public void OnInterrupt() => _inner?.OnInterrupt();
    }
    public class WaitForFramesAction : IQueueAction
    {
        private int _framesToWait;
        private int _framesElapsed;

        public WaitForFramesAction(int frames)
        {
            _framesToWait = frames;
            _framesElapsed = 0;
        }

        public void OnStart()
        {
            _framesElapsed = 0;
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            _framesElapsed++;
            return _framesElapsed >= _framesToWait ? ActionStatus.Success : ActionStatus.Running;
        }

        public void OnInterrupt()
        {
            _framesElapsed = _framesToWait;
        }
    }
    public class WaitForSecondsAction : IQueueAction
    {
        private float _timeToWait;
        private float _timeElapsed;

        public WaitForSecondsAction(float seconds)
        {
            _timeToWait = seconds;
        }

        public void OnStart()
        {
            _timeElapsed = _timeToWait;
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            _timeElapsed -= deltaTime;
            return _timeElapsed  <= 0 ? ActionStatus.Success : ActionStatus.Running;
        }

        public void OnInterrupt()
        {
            _timeElapsed = _timeToWait;
        }
    }
    
    public class WaitForExternalAction : IQueueAction
    {
        private bool _triggered = false;
        private readonly Action<Action> _externalActionSetter;
        
        public WaitForExternalAction(Action<Action> externalActionSetter)
        {
            _externalActionSetter = externalActionSetter;
        }

        public void OnStart()
        {
            _triggered = false;
            
            _externalActionSetter(() => _triggered = true);
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            return _triggered ? ActionStatus.Success : ActionStatus.Running;
        }

        public void OnInterrupt()
        {
            _triggered = false;
        }
    }
    
    public class TimeoutAction : IQueueAction
    {
        private readonly IQueueAction _inner;
        private readonly float _maxTime;
        private float _elapsed;

        public TimeoutAction(IQueueAction inner, float maxTime)
        {
            this._inner = inner;
            this._maxTime = maxTime;
        }

        public void OnStart()
        {
            _elapsed = 0f;
            _inner.OnStart();
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            _elapsed += deltaTime;
            if (_elapsed > _maxTime)
                return ActionStatus.Failure;

            return _inner.OnUpdate(deltaTime);
        }

        public void OnInterrupt()
        {
            _inner.OnInterrupt();
        }
    }
    
    public class RetryAction : IQueueAction
    {
        private readonly IQueueAction _inner;
        private readonly int _maxRetries;
        private int _currentRetries;

        public RetryAction(IQueueAction inner, int maxRetries)
        {
            this._inner = inner;
            this._maxRetries = maxRetries;
        }

        public void OnStart()
        {
            _currentRetries = 0;
            _inner.OnStart();
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            var status = _inner.OnUpdate(deltaTime);
            if (status == ActionStatus.Failure && _currentRetries < _maxRetries)
            {
                _currentRetries++;
                _inner.OnStart(); // Reinicia
                return ActionStatus.Running;
            }

            return status;
        }

        public void OnInterrupt()
        {
            _inner.OnInterrupt();
        }
    }
    public class WaitForConditionAction : IQueueAction
    {
        private readonly Func<bool> _condition;

        public WaitForConditionAction(Func<bool> condition)
        {
            this._condition = condition;
        }

        public void OnStart() { }

        public ActionStatus OnUpdate(float deltaTime)
        {
            return _condition() ? ActionStatus.Success : ActionStatus.Running;
        }

        public void OnInterrupt() { }
    }
    public class ActionWithResult<T> : IQueueAction
    {
        private Action<T> _resultCallback;
        private Func<T> _actionFunc;
        private ActionStatus _status = ActionStatus.Failure;
        private T _result;

        public ActionWithResult(Func<T> actionFunc, Action<T> resultCallback)
        {
            _actionFunc = actionFunc;
            _resultCallback = resultCallback;
        }

        public void OnStart()
        {
            _status = ActionStatus.Running;
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            if (_status == ActionStatus.Running)
            {
                _result = _actionFunc.Invoke();

                if (_result != null)
                {
                    _status = ActionStatus.Success;
                    _resultCallback?.Invoke(_result);
                }
            }

            return _status;
        }

        public void OnInterrupt()
        {
            _status = ActionStatus.Success;
        }
    }
    public class ActionSequence : IQueueAction
    {
        private readonly Queue<IQueueAction> _actions;
        private IQueueAction _current = null;

        public ActionSequence(IEnumerable<QueueActionData> actions)
        {
            _actions = ActionQueueTools.CreateQueue(actions);
        }

        public void OnStart()
        {
            _current = null;
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            if (_current == null && _actions.Count == 0)
            {
                // Sequence Finished
                return ActionStatus.Success;
            }

            if (_current == null)
            {
                _current = _actions.Dequeue();
                _current.OnStart();
            }
            else if(_current.OnUpdate(deltaTime) == ActionStatus.Success)
            {
                _current = null;
            }
            
            return ActionStatus.Running;
        }

        public void OnInterrupt()
        {
            _current?.OnInterrupt();
            _actions.Clear();
        }
    }
    public class ActionParallel : IQueueAction
    {
        private readonly List<IQueueAction> _actions;
        private readonly List<IQueueAction> _finished = new List<IQueueAction>();

        public ActionParallel(IEnumerable<QueueActionData> actions)
        {
            _actions = ActionQueueTools.CreateList(actions);
        }

        public void OnStart()
        {
            foreach (var item in _actions)
            {
                item.OnStart();
            }
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            if (_actions.Count == 0)
                return ActionStatus.Success;
            
            foreach (var action in _actions)
            {
                if (action.OnUpdate(deltaTime) == ActionStatus.Success)
                    _finished.Add(action);
            }
            
            foreach (var f in _finished)
                _actions.Remove(f);

            var doesFinish = _actions.Count == 0 ? ActionStatus.Success : ActionStatus.Running;
            
            if (doesFinish == ActionStatus.Success)
            {
                _finished.Clear();
            }

            return doesFinish;
        }

        public void OnInterrupt()
        {
            foreach (var item in _actions)
            {
                item.OnInterrupt();
            }
            
            _actions.Clear();
        }
    }
    public class AsyncQueueAction : IQueueAction
    {
        private Task _task;
        private TaskCompletionSource<bool> _tcs;
        
        public AsyncQueueAction(Func<Task> asyncAction)
        {
            _tcs = new TaskCompletionSource<bool>();

            _task = Task.Run(async () =>
            {
                try
                {
                    await asyncAction();
                    _tcs.TrySetResult(true);
                }
                catch
                {
                    _tcs.TrySetException(new Exception("Async action failed"));
                }
            });
        }

        public void OnStart() { }

        public ActionStatus OnUpdate(float deltaTime)
        {
            if (_tcs.Task.IsCompletedSuccessfully)
                return ActionStatus.Success;
            if (_tcs.Task.IsFaulted)
                return ActionStatus.Failure;

            return ActionStatus.Running;
        }

        public void OnInterrupt()
        {
            _tcs.TrySetCanceled();
        }
    }
    
    #endregion

    #endregion

    public class ActionQueue
    {
        private Queue<IQueueAction> _actions = new Queue<IQueueAction>();
        private Queue<IQueueAction> _urgentActions = new Queue<IQueueAction>();
        private IQueueAction _current = null;
        private bool _isPaused = false;
        private bool IsRunning => (_actions.Count > 0 || _urgentActions.Count > 0) && _current != null;
        
        #region Constructors

        public ActionQueue()
        {
        }

        public ActionQueue(QueueActionData queueAction)
        {
            AddAction(queueAction);
        }
        
        public ActionQueue(IEnumerable<QueueActionData> actions)
        {
            AddAction(actions);
        }

        #endregion

        #region Add

        public void AddAction(QueueActionData data)
        {
            _actions ??= new Queue<IQueueAction>();
            
            _actions.Enqueue(ActionQueueTools.FilterCallback(data));
        }

        public void AddAction(IEnumerable<QueueActionData> actions)
        {
            _actions = ActionQueueTools.CreateQueue(actions);
        }

        #endregion

        public void Pause() => _isPaused = true;
        public void Resume() => _isPaused = false;
        public void Clear()
        {
            _current?.OnInterrupt();
            _actions.Clear();
            _current = null;
        }

        #region Interrupt

        public void InterruptAction()
        {
            _current?.OnInterrupt();
            _current = null;
        }


        public void AddUrgentAndInterrupt(QueueActionData queueActionData)
        {
            if (IsRunning == false)
            {
                Debug.Log("Queue not running");
                return;
            }

            _current?.OnInterrupt();
            
            _current = ActionQueueTools.FilterCallback(queueActionData);

            _current.OnStart();
        }

        #endregion

        #region Urgent

        public void AddUrgentNext(IEnumerable<QueueActionData> queueActionData)
        {
            if (IsRunning == false)
            {
                Debug.Log("Queue not running");
                return;
            }
            
            _urgentActions = ActionQueueTools.CreateQueue(queueActionData);
        }

        public void AddUrgentNext(QueueActionData queueActionData)
        {
            if (IsRunning == false)
            {
                Debug.Log("Queue not running");
                return;
            }
            
            _urgentActions.Enqueue(ActionQueueTools.FilterCallback(queueActionData));
        }

        #endregion

        public void Execute(float deltaTime)
        {
            if(_isPaused) return; 
            
            if (_current == null)
            {
                if (_urgentActions.Count > 0)
                {
                    _current = _urgentActions.Dequeue();
                    _current.OnStart();
                }
                else if (_actions.Count > 0)
                {
                    _current = _actions.Dequeue();
                    _current.OnStart();
                }
                else
                {
                    //Debug.Log("Queue is empty");
                }
            }
            else if (_current.OnUpdate(deltaTime) == ActionStatus.Success)
            {
                _current = null;
            }
        }
    }
}