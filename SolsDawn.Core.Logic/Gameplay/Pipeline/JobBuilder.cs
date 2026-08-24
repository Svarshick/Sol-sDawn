using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SolsDawn.Core.Logic.Gameplay.Pipeline;

public struct JobAsyncMethodBuilder
{
    [ThreadStatic] 
    internal static Job? PreallocatedJob;
    
    private readonly Job _job;

    public static JobAsyncMethodBuilder Create()
    {
        Job job;
        if (PreallocatedJob is not null)
        {
            job = PreallocatedJob;
            PreallocatedJob = null;
        }
        else
        {
            job = new Job();
        }

        JobContext.CurrentJob?.AddChild(job);
        return new JobAsyncMethodBuilder(job);
    }

    private JobAsyncMethodBuilder(Job job)
    {
        _job = job;
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine) 
        where TStateMachine : IAsyncStateMachine
    {
        if (_job.IsEnded)
            return;
        using (JobContext.Use(_job)) 
        {
            stateMachine.MoveNext();
        }
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine) { }

    public void SetResult()
    {
        _job.Complete();
    }

    public void SetException(Exception exception)
    {
        Console.Error.WriteLine($"[JobTest Error] {exception}");
        _job.Kill();
    }

    public Job Task => _job;

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        var runner = JobStateMachineRunner<TStateMachine>.Rent(ref stateMachine, _job);
        //It is not "awaiter do X because completed". It is "awaiter do X AFTER completed"
        awaiter.OnCompleted(runner.MoveNextAction);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        AwaitOnCompleted(ref awaiter, ref stateMachine);
    }
}

internal sealed class JobStateMachineRunner<TStateMachine> 
    where TStateMachine : IAsyncStateMachine
{
    private static readonly Stack<JobStateMachineRunner<TStateMachine>> Pool = new(16);

    public TStateMachine StateMachine;
    public Job TargetJob;
    public Action MoveNextAction { get; }

    public JobStateMachineRunner()
    {
        // Cached once per instance creation - no delegate allocation on await!
        MoveNextAction = Run;
    }

    public static JobStateMachineRunner<TStateMachine> Rent(ref TStateMachine stateMachine, Job targetJob)
    {
        var runner = Pool.Count > 0 ? Pool.Pop() : new JobStateMachineRunner<TStateMachine>();
        runner.StateMachine = stateMachine;
        runner.TargetJob = targetJob;
        return runner;
    }

    private void Run()
    {
        using (JobContext.Use(TargetJob))
        {
            try
            {
                StateMachine.MoveNext();
            }
            finally
            {
                StateMachine = default;
                TargetJob = null;
                Pool.Push(this);
            }
        }
    }
}