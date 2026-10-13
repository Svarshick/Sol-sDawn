using System;
using System.Runtime.CompilerServices;

namespace SolsDawn.Core.Logic.Gameplay.Pipeline;

public static class AwaiterUtils
{
    public static bool ValidContext(Job job) => job == JobContext.CurrentJob &&
                                                job is { IsEnded: false, NextFrameContinuation: null, BlockJob: null };
    
    public static bool ValidContext() => JobContext.CurrentJob is { IsEnded: false, NextFrameContinuation: null, BlockJob: null };
}

public readonly struct JobAwaiter(Job awaiterJob, Job awaitedJob) : INotifyCompletion
{
    public bool IsCompleted => awaitedJob.IsEnded;

    public void OnCompleted(Action continuation)
    {
        if (!AwaiterUtils.ValidContext(awaiterJob))
            Log.Job.Fatal("Invalid Job awaiter context");
        awaiterJob.BlockJob = awaitedJob;
        var awaiter = awaiterJob;
        awaitedJob.Completed.OnEnd(() =>
        {
            if (awaiter.IsEnded)
                return;
            using (JobContext.Use(awaiter))
            {
                awaiter.BlockJob = null;
                continuation();
            }
        });
    }

    public void GetResult() { }
}

public readonly struct YieldAwaiter(Job awaiterJob) : INotifyCompletion
{
    public bool IsCompleted => false;

    public void OnCompleted(Action continuation)
    {
        if (!AwaiterUtils.ValidContext(awaiterJob))
            Log.Job.Fatal("Invalid Job awaiter context");
        awaiterJob.NextFrameContinuation = continuation;
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
            if (job.IsEnded)
                return;
            using (JobContext.Use(job))
            {
                if (!AwaiterUtils.ValidContext(job))
                    Log.Job.Fatal("Invalid Job awaiter context");
                continuation();
            }
        });
    }
    
    public void GetResult() { }
}