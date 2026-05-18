using Microsoft.Xna.Framework;

namespace SolsDawn.Core;

public static class Time
{
    public static TimeSpan TotalGameTime { get; private set; }
    public static TimeSpan ElapsedGameTime { get; private set; }
    public static bool IsRunningSlowly { get; private set; }

    public static void Update(GameTime gameTime)
    {
        TotalGameTime = gameTime.TotalGameTime;
        ElapsedGameTime = gameTime.ElapsedGameTime;
        IsRunningSlowly = gameTime.IsRunningSlowly;
    }
}