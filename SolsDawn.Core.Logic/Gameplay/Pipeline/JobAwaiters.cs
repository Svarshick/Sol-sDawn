using System;
using System.Runtime.CompilerServices;

namespace SolsDawn.Core.Logic.Gameplay.Pipeline;

public static class AwaiterUtils
{
    public static bool ValidContext(Job job) => job == JobContext.CurrentJob &&
                                                job is { IsEnded: false, Continuation: null, BlockJob: null };
    
    public static bool ValidContext() => JobContext.CurrentJob is { IsEnded: false, Continuation: null, BlockJob: null };
}

public readonly struct JobAwaiter(Job awaiterJob, Job awaitedJob) : INotifyCompletion
{
    public bool IsCompleted => awaitedJob.IsEnded;

    public void OnCompleted(Action continuation)
    {
        if (!AwaiterUtils.ValidContext(awaiterJob))
            throw new JobException();
        awaiterJob.Continuation = continuation;
        awaiterJob.BlockJob = awaitedJob;
    }

    public void GetResult() { }
}

public readonly struct YieldAwaiter(Job awaiterJob) : INotifyCompletion
{
    public bool IsCompleted => false;

    public void OnCompleted(Action continuation)
    {
        if (!AwaiterUtils.ValidContext(awaiterJob))
            throw new JobException();
        awaiterJob.Continuation = continuation;
    }

    public void GetResult() { }

    public YieldAwaiter GetAwaiter() => this;
}

public readonly struct EventAwaiter(Job awaiterJob, Event @event) : INotifyCompletion
{
    public bool IsCompleted => @event.IsEnded;
    
    public void OnCompleted(Action continuation)
    {
        var job = awaiterJob;
        @event.OnEnd(() =>
        {
            using (JobContext.Use(job))
            {
                if (!AwaiterUtils.ValidContext(job))
                    throw new JobException();
                continuation();
            }
        });
    }
    
    public void GetResult() { }
}