using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;

namespace SolsDawn.Core.Logic.Gameplay.Behaviour;

public delegate Task Callback();

public static class ExecutionContext
{
    [field: ThreadStatic]
    public static Routine CurrentRoutine { get; private set; }

    public static IDisposable Use(Routine routine)
    {
        return new ContextScope(routine);
    }

    private class ContextScope : IDisposable
    {
        private readonly Routine _previous;

        public ContextScope(Routine routine)
        {
            _previous = CurrentRoutine;
            CurrentRoutine = routine;
        }

        public void Dispose()
        {
            CurrentRoutine = _previous;
        }
    }
}

public enum RoutineStatus
{
    Alive,
    Completed,
    Killed,
}

public class Routine
{
    public Event Completed { get; }
    public RoutineStatus Status { get; private set; }
    public bool IsDead => Status is RoutineStatus.Completed or RoutineStatus.Killed;
    

    private Callback _callback;
    private Task _task;
    private Action _continuation;
    
    private List<Routine> _subroutines = new();
    private List<Routine> _subroutinesBuff = new();
    private List<Timer> _activeTimers = new();
    private List<Timer> _timersBuff = new();

    private readonly List<IDisposable> _disposables = new();

    public Routine(Callback callback)
    {
        Completed = new(this);
        _callback = callback;
        Status = RoutineStatus.Alive;
    }
    
    public void StartSubroutine(Routine subroutine) 
    {
        if (IsDead)
        {
            subroutine.Kill();
            return;
        }

        subroutine.Update();
        if (!IsDead)
        {
            _subroutinesBuff.Add(subroutine);
        }
    }

    public Timer After(double time)
    {
        var t = new Timer(this, time);
        switch (Status)
        {
            case RoutineStatus.Alive:
                Completed.ChainEvent(t);
                break;
            case RoutineStatus.Completed:
                t.Fire();
                break;
            case RoutineStatus.Killed:
                t.Cancel();
                break;
        }

        return t;
    }

    public void StartTimer(Timer timer)
    {
        if (IsDead)
        {
            timer.Cancel();
        }
        else
        {
            _timersBuff.Add(timer);
        }
    }

    public void Update()
    {
        if (IsDead)
            return;

        using (ExecutionContext.Use(this))
        {
            try
            {
                UpdateTimers();
                Resume();

                if (_task.IsCompleted)
                {
                    Completed.Fire(); //kill cancels FinishEvent, so we Fire it
                    Kill();
                }

                foreach (var subroutine in _subroutines)
                {
                    subroutine.Update();
                    if (!subroutine.IsDead)
                    {
                        _subroutinesBuff.Add(subroutine);
                    }
                }

                (_subroutines, _subroutinesBuff) = (_subroutinesBuff, _subroutines);
                _subroutinesBuff.Clear();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] {ex}");
                Kill();
            }
        }
    }

    private void UpdateTimers()
    {
        foreach (var timer in _activeTimers)
        {
            if (timer.State != EventState.Pending)
                continue;

            timer.TimeRemaining -= Time.ElapsedGameTime.TotalSeconds;
            if (timer.TimeRemaining <= 0)
            {
                timer.Fire();
            }
            else
            {
                _timersBuff.Add(timer);
            }
        }

        (_activeTimers, _timersBuff) = (_timersBuff, _activeTimers);
        _timersBuff.Clear();
    }

    private void Resume()
    {
        if (_task is null)
        {
            _task = _callback();
            _callback = null;
        }
        else if (_continuation is not null)
        {
            var continuation = _continuation;
            _continuation = null;
            continuation();
        }
    }

    public void Kill() => End(false);

    private void End(bool completed)
    {
        if (IsDead)
            return;

        using (ExecutionContext.Use(this))
        {
            if (completed)
            {
                Status = RoutineStatus.Completed;
                Completed.Fire();
            }
            else
            {
                Status = RoutineStatus.Killed;
                Completed.Cancel();
            }
            
            //CANCEL TIMERS
            foreach (var timer in _activeTimers)
            {
                timer.Cancel();
            }

            foreach (var timer in _timersBuff)
            {
                timer.Cancel();
            }

            //KILL ROUTINES
            foreach (var subroutine in _subroutines)
            {
                subroutine.End(false);
            }

            foreach (var subroutine in _subroutinesBuff)
            {
                subroutine.End(false);
            }

            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }

            _disposables.Clear();
        }
    }
    
    public struct NextFrameAwaiter : INotifyCompletion
    {
        public NextFrameAwaiter GetAwaiter() => this;

        public bool IsCompleted => false;

        public void OnCompleted(Action continuation)
        {
            ExecutionContext.CurrentRoutine._continuation = continuation;
        }

        public void GetResult()
        {
        }
    }
}