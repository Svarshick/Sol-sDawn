namespace SolsDawn.Core;

public static class MonoTask 
{
    private static List<(TaskCompletionSource<bool> CompletionSource, int Frames, CancellationToken Token)> _pendingFrameTasks = new();
    private static List<(TaskCompletionSource<bool> CompletionSource, int Frames, CancellationToken Token)> _pendingFrameTasksBuff = new();
    private static List<(TaskCompletionSource<bool> CompletionSource, int Frames, CancellationToken Token)> _newFrameTasks = new();
   
    public static void Update()
    {
        for (int i = 0; i < _pendingFrameTasks.Count; i++)
        {
            var completionSource = _pendingFrameTasks[i].CompletionSource;
            var frames = _pendingFrameTasks[i].Frames;
            var token = _pendingFrameTasks[i].Token;

            if (token.IsCancellationRequested)
            {
                completionSource.TrySetResult(false);
                continue;
            }
            
            frames--;
            if (frames <= 0)
            {
                completionSource.SetResult(true);
            }
            else
            {
                _pendingFrameTasksBuff.Add((completionSource, frames, token));
            }
        }
        
        _pendingFrameTasksBuff.AddRange(_newFrameTasks);
        _newFrameTasks.Clear();
        _pendingFrameTasks.Clear();
        (_pendingFrameTasks, _pendingFrameTasksBuff) = (_pendingFrameTasksBuff, _pendingFrameTasks);
    }

    public static Task<bool> NextFrame() => DelayFrames(1);

    public static Task<bool> DelayFrames(int frames, CancellationToken token = default)
    {
        if (token.IsCancellationRequested) 
            return Task.FromResult(false);
        
        var completionSource = new TaskCompletionSource<bool>();
        _newFrameTasks.Add((completionSource, frames, token));
        return completionSource.Task;
    }
}