using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace NicolasMassara.CustomActionManager
{
    public enum ActionStatus
    {
        Idle,
        Running,
        Success,
        Failure
    }
    
    public sealed class ActionQueueTools
    {
        public static Queue<IQueueAction> CreateQueue(IEnumerable<IQueueAction> data)
        {
            return new Queue<IQueueAction>(data);
        }
        
        public static List<IQueueAction> CreateList(IEnumerable<IQueueAction> data)
        {
            return new List<IQueueAction>(data);
        }
    }
    
    #region Interfaces
    
    public interface IQueueAction
    {
        public ActionStatus CurrentStatus { get;}
        public void OnStart();
        public ActionStatus OnUpdate(float deltaTime);
        public void OnInterrupt();   
    }
    
    public interface ICommand
    {
        public ActionStatus OnExecute(float deltaTime);
        public void OnInterrupt();
    }
    
    public interface ICommand<T>
    {
        public ActionStatus OnExecute(float deltaTime, out T result);
        public void OnInterrupt(out T result);
    }
    
    public abstract class Command : ICommand
    {
        public abstract ActionStatus OnExecute(float deltaTime);
        public virtual void OnInterrupt() {}
    }
    
    public abstract class Command<T> : ICommand<T>
    {
        public abstract ActionStatus OnExecute(float deltaTime, out T result);

        public virtual void OnInterrupt(out T result)
        {
            result = default;
        }
    }

    #endregion

    #region Actions Helper
    
    // ==================== Fluent Builder ====================
    public class ActionBuilder
    {
        private readonly DynamicActionSequence _sequence = new DynamicActionSequence();

        private ActionBuilder() { }

        public static ActionBuilder Start() => new ActionBuilder();

        // Agrega una acción al final de la secuencia
        public ActionBuilder Do(IQueueAction action)
        {
            _sequence.InsertLast(action);
            return this;
        }

        public ActionBuilder Then(IQueueAction action) => Do(action);
        
        public ActionBuilder WrapLast(Func<IQueueAction, IQueueAction> wrapper)
        {
            var last = _sequence.GetLastAction();
            if (last == null)
                throw new InvalidOperationException("No action to wrap.");

            _sequence.ReplaceLast(wrapper(last));
            return this;
        }
        
        public ActionBuilder WrapAll(Func<IQueueAction, IQueueAction> wrapper)
        {
            _sequence.WrapAll(wrapper);
            return this;
        }

        public DynamicActionSequence Build() => _sequence;
    }
        
    public class DynamicActionSequence : IQueueAction
    {
        private readonly Queue<IQueueAction> _actions = new Queue<IQueueAction>();
        private IQueueAction _current;

        public ActionStatus CurrentStatus { get; private set; } = ActionStatus.Running;

        public void OnStart()
        {
            _current = null;
            CurrentStatus = ActionStatus.Running;
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            if (_current == null)
            {
                if (_actions.Count == 0)
                {
                    CurrentStatus = ActionStatus.Success;
                    return CurrentStatus;
                }

                _current = _actions.Dequeue();
                _current.OnStart();
            }

            var s = _current.OnUpdate(deltaTime);

            if (s == ActionStatus.Success)
                _current = null;
            else if (s == ActionStatus.Failure)
                CurrentStatus = ActionStatus.Failure;

            return CurrentStatus;
        }

        public void OnInterrupt()
        {
            _current?.OnInterrupt();
            _actions.Clear();
            CurrentStatus = ActionStatus.Failure;
        }

        // Inserta la acción justo después de la acción activa
        public void InsertNext(IQueueAction action)
        {
            var temp = new Queue<IQueueAction>();
            temp.Enqueue(action);
            while (_actions.Count > 0)
                temp.Enqueue(_actions.Dequeue());
            while (temp.Count > 0)
                _actions.Enqueue(temp.Dequeue());
        }

        // Inserta la acción al final
        public void InsertLast(IQueueAction action) => _actions.Enqueue(action);

        // Para WrapLast
        public IQueueAction GetLastAction()
        {
            if (_actions.Count == 0)
                return _current;
            return _actions.Last(); // System.Linq
        }

        public void ReplaceLast(IQueueAction newAction)
        {
            if (_actions.Count > 0)
            {
                var list = _actions.ToList();
                list[list.Count - 1] = newAction;
                _actions.Clear();
                foreach (var a in list)
                    _actions.Enqueue(a);
            }
            else if (_current != null)
            {
                _current = newAction;
            }
        }

        public void WrapAll(Func<IQueueAction, IQueueAction> wrapper)
        {
            if (_current != null)
                _current = wrapper(_current);

            var list = _actions.ToList();
            for (int i = 0; i < list.Count; i++)
                list[i] = wrapper(list[i]);
            _actions.Clear();
            foreach (var a in list)
                _actions.Enqueue(a);
        }
    }
    
    // ==================== Interrupt ====================
    
    public class InterruptibleSequence : IQueueAction
    {
        private readonly DynamicActionSequence _inner;
        private readonly Action _onInterrupt;
        public ActionStatus CurrentStatus { get; private set; }

        public InterruptibleSequence(DynamicActionSequence inner, Action onInterrupt = null)
        {
            _inner = inner;
            _onInterrupt = onInterrupt;
        }

        public void OnStart() => _inner.OnStart();

        public ActionStatus OnUpdate(float deltaTime) => _inner.OnUpdate(deltaTime);

        public void OnInterrupt()
        {
            _inner.OnInterrupt();  
            _onInterrupt?.Invoke();
        }
    }
    
    
    // ==================== Command Wrappers ====================
    
    #region Wrappers
    
    public class CallbackAction : IQueueAction
    {
        private readonly IQueueAction _inner;
        private readonly Action _startCallback;
        private readonly Action _endCallback;
        
        public ActionStatus CurrentStatus { get; private set; }

        public CallbackAction(IQueueAction inner,  Action startCallback, Action endCallback)
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

        public void OnInterrupt()
        {
            if (CurrentStatus == ActionStatus.Idle) return;
            
            _inner?.OnInterrupt();
        }
    }
    public class TimeoutAction : IQueueAction
    {
        private readonly IQueueAction _inner;
        private readonly float _maxTime;
        private readonly Action<ActionStatus> _onTimeout; 
        private float _elapsed;
        private ActionStatus _innerStatus;
        public ActionStatus CurrentStatus { get; private set; }

        public TimeoutAction(IQueueAction inner, float maxTime, Action<ActionStatus> onTimeout = null)
        {
            _inner = inner;
            _maxTime = maxTime;
            _onTimeout = onTimeout;
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
            {
                _onTimeout?.Invoke(ActionStatus.Failure);
                return ActionStatus.Failure;
            }

            return _inner.OnUpdate(deltaTime);
        }

        public void OnInterrupt()
        {
            if (CurrentStatus == ActionStatus.Idle) return;
            
            _inner.OnInterrupt();
        }
    }
    public class RetryAction : IQueueAction
    {
        private readonly IQueueAction _inner;
        private readonly int _maxRetries;
        private bool _doesRetryUntilSuccess;
        private int _currentRetries;
        public ActionStatus CurrentStatus { get; private set; }
        
        public RetryAction(IQueueAction inner, int maxRetries = 1)
        {
            this._inner = inner;
            this._maxRetries = maxRetries;
            _doesRetryUntilSuccess = _maxRetries <= 0;
        }

        public void OnStart()
        {
            _currentRetries = 0;
            _inner.OnStart();
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            var status = _inner.OnUpdate(deltaTime);
            
            if (status == ActionStatus.Failure)
            {
                if (_currentRetries < _maxRetries && !_doesRetryUntilSuccess)
                {
                    _currentRetries++;
                }
                
                _inner.OnStart();
                
                return ActionStatus.Running;
            }

            return status;
        }

        public void OnInterrupt()
        {
            if (CurrentStatus == ActionStatus.Idle) return;
            
            _inner.OnInterrupt();
        }
    }
    public class PriorityAction : IQueueAction
    {
        private readonly IQueueAction _inner;
        private readonly Func<bool> _shouldInterruptOthers;
        public ActionStatus CurrentStatus { get; private set; }

        public PriorityAction(IQueueAction inner, Func<bool> shouldInterruptOthers)
        {
            _inner = inner;
            _shouldInterruptOthers = shouldInterruptOthers;
        }

        public void OnStart()
        {
            _inner.OnStart();
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            if (_shouldInterruptOthers())
            {
                _inner.OnInterrupt();
                return ActionStatus.Failure;
            }
            return _inner.OnUpdate(deltaTime);
        }

        public void OnInterrupt()
        {
            if (CurrentStatus == ActionStatus.Idle) return;

            _inner.OnInterrupt();
        }
    }
    public class RepeatAction : IQueueAction
    {
        private readonly IQueueAction _inner;
        private readonly int _repeatCount;
        private int _currentCount;
        public ActionStatus CurrentStatus { get; private set; }

        public RepeatAction(IQueueAction inner, int repeatCount)
        {
            _inner = inner;
            _repeatCount = repeatCount;
        }

        public void OnStart()
        {
            _currentCount = 0;
            _inner.OnStart();
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            var status = _inner.OnUpdate(deltaTime);
            if (status == ActionStatus.Success)
            {
                _currentCount++;
                if (_currentCount >= _repeatCount)
                    return ActionStatus.Success;

                _inner.OnStart(); // reinicia
                return ActionStatus.Running;
            }
            return status;
        }

        public void OnInterrupt()
        {
            if (CurrentStatus == ActionStatus.Idle) return;
            
            _inner.OnInterrupt();
        }
    }
    public class DebugAction : IQueueAction
    {
        private readonly IQueueAction _inner;
        private readonly string _name;
        public ActionStatus CurrentStatus { get; private set; }

        public DebugAction(IQueueAction inner, string name)
        {
            _inner = inner;
            _name = name;
        }

        public void OnStart()
        {
            Debug.Log($"{_name} started");
            _inner.OnStart();
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            var status = _inner.OnUpdate(deltaTime);
            if (status == ActionStatus.Success)
                Debug.Log($"{_name} success");
            else if (status == ActionStatus.Failure)
                Debug.Log($"{_name} failed");
            return status;
        }
        
        public void OnInterrupt()
        {
            if (CurrentStatus == ActionStatus.Idle) return;
            
            Debug.Log($"{_name} interrupted");
            _inner.OnInterrupt();
        }
    }

    #endregion
    
    // ==================== Control Wrappers ====================
    
    #region Wait/Utility
    public class WaitFramesAction : IQueueAction
    {
        private int _framesToWait;
        private int _framesElapsed;
        public ActionStatus CurrentStatus { get; private set; }

        public WaitFramesAction(int frames)
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
            if (CurrentStatus == ActionStatus.Idle) return;
            
            _framesElapsed = _framesToWait;
        }
    }
    public class WaitSecondsAction : IQueueAction
    {
        private float _timeToWait;
        private float _timeElapsed;
        public ActionStatus CurrentStatus { get; private set; }

        public WaitSecondsAction(float seconds)
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
            if (CurrentStatus == ActionStatus.Idle) return;
            
            _timeElapsed = _timeToWait;
        }
    }
    public class FrameDelayAction : IQueueAction
    {
        private readonly IQueueAction _inner;
        private readonly WaitFramesAction _waitFrames;
        private bool _hasStarted;
        public ActionStatus CurrentStatus { get; private set; }
        
        public FrameDelayAction(IQueueAction inner, int frames)
        {
            _inner = inner;
            _waitFrames = new WaitFramesAction(frames);
        }

        public void OnStart()
        {
            _waitFrames.OnStart();
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            CurrentStatus = _waitFrames.OnUpdate(deltaTime);

            if (CurrentStatus == ActionStatus.Success)
            {
                _inner.OnStart();
                _hasStarted = true;
                CurrentStatus = ActionStatus.Running;
            }

            if (_hasStarted)
            {
                CurrentStatus = _inner.OnUpdate(deltaTime);
            }


            return CurrentStatus;
        }

        public void OnInterrupt()
        {
            if (CurrentStatus == ActionStatus.Idle) return;
            
            _waitFrames.OnInterrupt();
            _inner.OnInterrupt();
        }
    }
    public class WaitForExternalAction : IQueueAction
    {
        private bool _triggered = false;
        private readonly Action<Action> _externalActionSetter;
        public ActionStatus CurrentStatus { get; private set; }
        
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
            if (CurrentStatus == ActionStatus.Idle) return;
            
            _triggered = false;
        }
    }
    public class ConditionalAction : IQueueAction
    {
        private readonly Func<bool> _condition;
        public ActionStatus CurrentStatus { get; private set; }

        public ConditionalAction(Func<bool> condition)
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
    public class WaitSecondsWithCallBack : IQueueAction
    {
        private readonly WaitSecondsAction _wait;
        private readonly Action _onComplete;
        private readonly Action _onInterrupt;
        
        public ActionStatus CurrentStatus { get; private set; }

        public WaitSecondsWithCallBack(float seconds, Action onComplete, Action onInterrupt = null)
        {
            _wait = new WaitSecondsAction(seconds);
            _onComplete = onComplete;
            _onInterrupt = onInterrupt;
        }

        public void OnStart()
        {
            _wait.OnStart();
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            CurrentStatus = _wait.OnUpdate(deltaTime);

            if (CurrentStatus == ActionStatus.Success)
            {
                _onComplete?.Invoke();
            }

            return CurrentStatus;
        }

        public void OnInterrupt()
        {
            if (CurrentStatus == ActionStatus.Idle) return;
            
            _wait.OnInterrupt();
            _onInterrupt?.Invoke();
        }
    }
    public class WaitFramesWithCallBack : IQueueAction
    {
        private readonly WaitFramesAction _wait;
        private readonly Action _onComplete;
        private readonly Action _onInterrupt;
        
        public ActionStatus CurrentStatus { get; private set; }

        public WaitFramesWithCallBack(int frames, Action onComplete, Action onInterrupt = null)
        {
            _wait = new WaitFramesAction(frames);
            _onComplete = onComplete;
            _onInterrupt = onInterrupt;
        }

        public void OnStart()
        {
            _wait.OnStart();
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            CurrentStatus = _wait.OnUpdate(deltaTime);

            if (CurrentStatus == ActionStatus.Success)
            {
                _onComplete?.Invoke();
            }

            return CurrentStatus;
        }

        public void OnInterrupt()
        {
            if (CurrentStatus == ActionStatus.Idle) return;
            
            _wait.OnInterrupt();
            _onInterrupt?.Invoke();
        }
    }
    
    public class InterruptAwareAction : IQueueAction
    {
        private readonly IQueueAction _inner;
        private readonly Action _onInterrupted;
        public ActionStatus CurrentStatus { get; private set; }

        public InterruptAwareAction(IQueueAction inner, Action onInterrupted)
        {
            _inner = inner;
            _onInterrupted = onInterrupted;
        }

        public void OnStart()
        {
            _inner.OnStart();
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            return _inner.OnUpdate(deltaTime);
        }

        public void OnInterrupt()
        {
            _inner.OnInterrupt();
            _onInterrupted?.Invoke();
        }
    }

    #endregion
    
    // ==================== Base Composables ====================
    
    #region Composition
    
    public class FirstToFinishAction : IQueueAction
    {
        private readonly List<IQueueAction> _actions;
        private bool _finished;
        public ActionStatus CurrentStatus { get; private set; }

        public FirstToFinishAction(IEnumerable<IQueueAction> actions)
        {
            _actions = new List<IQueueAction>(actions);
        }

        public void OnStart()
        {
            _finished = false;
            foreach (var a in _actions) a.OnStart();
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            if (_finished) return ActionStatus.Success;

            foreach (var a in _actions)
            {
                var status = a.OnUpdate(deltaTime);
                if (status == ActionStatus.Success)
                {
                    _finished = true;
                    return ActionStatus.Success;
                }
            }

            return ActionStatus.Running;
        }

        public void OnInterrupt()
        {
            if (CurrentStatus == ActionStatus.Idle) return;
            
            foreach (var a in _actions) a.OnInterrupt();
        }
    }

    public class ActionSequence : IQueueAction
    {
        private readonly Queue<IQueueAction> _actions;
        private IQueueAction _current = null;
        public ActionStatus CurrentStatus { get; private set; }

        public ActionSequence(IEnumerable<IQueueAction> actions)
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
            if (CurrentStatus == ActionStatus.Idle) return;
            
            _current?.OnInterrupt();
            _actions.Clear();
        }
    }
    public class ActionParallel : IQueueAction
    {
        private readonly List<IQueueAction> _actions;
        private readonly List<IQueueAction> _finished = new List<IQueueAction>();
        public ActionStatus CurrentStatus { get; private set; }

        public ActionParallel(IEnumerable<IQueueAction> actions)
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
            if (CurrentStatus == ActionStatus.Idle) return;
            
            foreach (var item in _actions)
            {
                item.OnInterrupt();
            }
            
            _actions.Clear();
        }
    }

    #endregion
    
    // ===================== Command/Func =======================
    
    #region Command/Func

    public class SimpleCommandAction : IQueueAction
    {
        private readonly ICommand _command;
        private ActionStatus _status;
                public ActionStatus CurrentStatus { get; private set; }

        public SimpleCommandAction(ICommand command)
        {
            _command = command;
        }

        public void OnStart()
        {
            _status = ActionStatus.Running;
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            if (_status == ActionStatus.Running)
            {
                _status = _command.OnExecute(deltaTime);
            }
            return _status;
        }

        public void OnInterrupt()
        {
            if (CurrentStatus == ActionStatus.Idle) return;
            
            _command.OnInterrupt();
        }
    }
    public class CommandWithResultAction<T> : IQueueAction
    {
        private readonly ICommand<T> _command;
        private T _result;
        private ActionStatus _status = ActionStatus.Running;
        public ActionStatus CurrentStatus { get; private set; }

        public T Result => _result; // Expuesto para que otros lean el resultado

        public CommandWithResultAction(ICommand<T> command)
        {
            _command = command;
        }



        public void OnStart()
        {
            _status = ActionStatus.Running;
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            if (_status == ActionStatus.Running)
            {
                _status = _command.OnExecute(deltaTime, out _result);
            }

            return _status;
        }

        public void OnInterrupt()
        {
            if (CurrentStatus == ActionStatus.Idle) return;
            
            _command.OnInterrupt(out _result);
        }
    }
    public class FuncAction<T> : IQueueAction
    {
        private Action<T> _resultCallback;
        private Func<T> _actionFunc;
        private ActionStatus _status = ActionStatus.Failure;
        private T _result;
        public ActionStatus CurrentStatus { get; private set; }

        public FuncAction(Func<T> actionFunc, Action<T> resultCallback)
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
            if (CurrentStatus == ActionStatus.Idle) return;
            
            _status = ActionStatus.Success;
        }
    }

    #endregion
    
    // ========================= Async ==========================
    
    #region Async

    public class AsyncQueueAction : IQueueAction
    {
        private Task _task;
        private TaskCompletionSource<bool> _tcs;
        public ActionStatus CurrentStatus { get; private set; }
        
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
            if (CurrentStatus == ActionStatus.Idle) return;
            
            _tcs.TrySetCanceled();
        }
    }

    #endregion
    
    // ========================= Debug ==========================
    
    #region Debug
    public class LogDebugAction : IQueueAction
    {
        private readonly string _message;
        public ActionStatus CurrentStatus { get; private set; }

        public LogDebugAction(string message)
        {
            _message = message;
        }

        public void OnStart()
        {
            Debug.Log(_message);
        }

        public ActionStatus OnUpdate(float deltaTime)
        {
            return ActionStatus.Success;
        }

        public void OnInterrupt()
        {
            if (CurrentStatus == ActionStatus.Idle) return;
            
            Debug.Log($"Message interrupted");
        }
    }

    #endregion
    
    #endregion
}