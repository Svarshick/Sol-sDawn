using Microsoft.Xna.Framework;

namespace SolsDawn.Core;

public static class MonoTask 
{
    private static List<(TaskCompletionSource CompletionSource, int Frames)> _pendingFrameTasks = new();
    private static List<(TaskCompletionSource CompletionSource, int Frames)> _pendingFrameTasksBuff = new();
    private static List<(TaskCompletionSource CompletionSource, int Frames)> _newFrameTasks = new();
   
    public static void Update(GameTime gameTime)
    {
        for (int i = 0; i < _pendingFrameTasks.Count; i++)
        {
            var completionSource = _pendingFrameTasks[i].CompletionSource;
            var frames = _pendingFrameTasks[i].Frames;
            frames--;
            if (frames <= 0)
            {
                completionSource.SetResult();
            }
            else
            {
                _pendingFrameTasksBuff.Add((completionSource, frames));
            }
        }
        
        _pendingFrameTasksBuff.AddRange(_newFrameTasks);
        _newFrameTasks.Clear();
        _pendingFrameTasks.Clear();
        (_pendingFrameTasks, _pendingFrameTasksBuff) = (_pendingFrameTasksBuff, _pendingFrameTasks);
    }

    public static Task NextFrame() => DelayFrames(1);

    public static Task DelayFrames(int frames)
    {
        var completionSource = new TaskCompletionSource();
        _newFrameTasks.Add((completionSource, frames));
        return completionSource.Task;
    }
}