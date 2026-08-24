using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SolsDawn.Core.Logic.Animations;

namespace SolsDawn.Core.Logic.Gameplay.Pipeline;

public class JobException(string message = null) : Exception(FullMessage(message))
{
    private static string FullMessage(string message)
    {
        const string prefix = "[Job]";
        return string.IsNullOrEmpty(message) ? $"{prefix}" : $"{prefix}: {message}";
    }
}

public enum JobStatus : byte
{
    Pending,
    Completed,
    Killed
}

public static class JobContext
{
    [field: ThreadStatic]
    public static Job? CurrentJob { get; internal set; }

    public static JobScope Use(Job job) => new(job);

    public struct JobScope : IDisposable
    {
        private readonly Job? _previous;

        public JobScope(Job currentJob)
        {
            _previous = CurrentJob;
            CurrentJob = currentJob;
        }

        public void Dispose()
        {
            CurrentJob = _previous;
        }
    }
}

[AsyncMethodBuilder(typeof(JobAsyncMethodBuilder))]
public class Job
{
    public JobStatus Status { get; private set; }
    internal Job? BlockJob;
    internal Action? Continuation;
    internal Event Completed;
    
    public bool IsEnded => Status != JobStatus.Pending;

    private readonly List<Job> _subjobs = new();
    private readonly List<Job> _subjobsToAdd = new();
    private readonly List<Timer> _timers = new();
    private readonly List<Timer> _timersToAdd = new();
    private readonly List<object> _trackedResources = new();

    public static Job CompletedJob
    {
        get
        {
            var job = new Job();
            job.Status = JobStatus.Completed;
            return job;
        }
    }
    
    internal Job()
    {
        Status = JobStatus.Pending;
        Completed = new(this);
    }

    public JobAwaiter GetAwaiter()
    {
        if (JobContext.CurrentJob is null)
            throw new JobException();
        return new (JobContext.CurrentJob, this);
    } 

    internal void AddChild(Job child)
    {
        if (IsEnded) //for cases when subjob is created in Kill process of parent job
        {
            child.Kill();
            return;
        }
        
        _subjobsToAdd.Add(child);
    }
    
    internal void TrackResource(object resource)
    {
        if (IsEnded)
        {
            FreeResource(resource);
            return;
        }
            
        _trackedResources.Add(resource);
    }

    internal void StartTimer(Timer timer)
    {
        if (IsEnded)
        {
            timer.Cancel();
            return;
        }

        _timersToAdd.Add(timer);
        _trackedResources.Add(timer);
    }

    internal void Update()
    {
        if (IsEnded)
            return;
        using (JobContext.Use(this))
        {
            UpdateTimers();
            UpdateSubjobs();
            UpdateSelf();
        }

        return;
        
        void UpdateTimers()
        {
            if (IsEnded)
                return;
            
            foreach (var timer in _timersToAdd)
            {
                if (!timer.IsEnded)
                {
                    _timers.Add(timer);
                }
            }
            _timersToAdd.Clear();
            
            int offset = 0;
            for (int i = 0; i + offset < _timers.Count;)
            {
                var timer = _timers[i + offset];
                timer.TimeRemaining -= GameplayAPI.ElapsedSeconds;
                if (timer.IsEnded)
                {
                    offset++;
                }
                else if (timer.TimeRemaining <= 0)
                {
                    timer.Fire();
                    if (IsEnded)
                        return;
                    offset++;
                }
                else
                {
                    _timers[i] = timer;
                    i++;
                }
            }
            _timers.RemoveRange(_timers.Count - offset, offset);
        }

        void UpdateSubjobs()
        {
            if (IsEnded)
                return;
            
            foreach (var subjob in _subjobsToAdd)
            {
                if (!subjob.IsEnded && subjob != BlockJob)
                {
                    _subjobs.Add(subjob);
                }
            }
            _subjobsToAdd.Clear();
            
            int offset = 0;
            for (int i = 0; i + offset < _subjobs.Count;)
            {
                var subjob = _subjobs[i + offset];
                subjob.Update();
                if (IsEnded)
                    return;
                
                if (subjob.IsEnded)
                {
                    offset++;
                }
                else
                {
                    _subjobs[i] = subjob;
                    i++;
                }
            }
            _subjobs.RemoveRange(_subjobs.Count - offset, offset);
        }

        void UpdateSelf()
        {
            if (IsEnded)
                return;
            
            if (BlockJob != null)
            {
                BlockJob.Update();
            }
            else if (Continuation != null) //event awaiter doesn't set Continuation until event ended
            {
                var continuation = Continuation;
                Continuation = null;
                continuation();
            }
            else
            {
                return;
            }

            while (!IsEnded && BlockJob != null)
            {
                if (BlockJob.IsEnded)
                {
                    BlockJob = null;
                    if (Continuation != null)
                    {
                        var continuation = Continuation!;
                        Continuation = null;
                        continuation();
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }

    internal void Complete()
    {
        if (IsEnded) 
            return;
        using (JobContext.Use(this))
        {
            Status = JobStatus.Completed;
            Completed.Fire();
            Cleanup();
        }
    }
    
    public void Kill()
    {    
        if (IsEnded) 
            return;
        using (JobContext.Use(this))
        {
            Status = JobStatus.Killed;
            Completed.Cancel();
            Cleanup();
        }
    }

    private void Cleanup()
    {
        foreach (var subjob in _subjobs)
        {
            subjob.Kill();
        }

        foreach (var subjob in _subjobsToAdd)
        {
            subjob.Kill();
        }
        
        BlockJob?.Kill();
        
        foreach (var resource in _trackedResources)
        {
            FreeResource(resource);
        }

        _timers.Clear();
        _timersToAdd.Clear();
        _subjobs.Clear();
        _subjobsToAdd.Clear();
        BlockJob = null;
        _trackedResources.Clear();
    }

    private void FreeResource(object resource)
    {
        switch (resource)
        {
            case GameObject go:
                go.Destroy();
                break;
            case Component comp:
                comp.Destroy();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
            case Event @event:
                @event.Cancel();
                break;
            case EventRace eventRace:
                eventRace.Finished.Cancel();
                break;
            case Animation animation:
                animation.Kill();
                break;
            default:
                throw new NotImplementedException($"Can't free {resource.GetType()} resource");
        }
    }
}