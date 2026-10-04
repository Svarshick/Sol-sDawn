namespace SolsDawn.Gameplay;

public static class Main
{
    public static Job RootJob() => GameLoop.Start();
    
    //public static Job RootJob() => Test.GhostVertex();
    //public static Job RootJob() => Test.ShapeSkin();
    //public static Job RootJob() => Test.PhysicsAligning();
}