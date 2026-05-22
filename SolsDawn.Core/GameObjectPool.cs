using Microsoft.Xna.Framework;

namespace SolsDawn.Core;

public static class GameObjectPool
{
    private static readonly HashSet<GameObject> GameObjects = new();

    public static void Update()
    {
        foreach (var go in GameObjects)
            go.Update();
    }

    public static void LateUpdate()
    {
        foreach (var go in GameObjects)
            go.LateUpdate();
    }

    public static void Draw()
    {
        foreach (var go in GameObjects)
            go.Draw();
    }
    
    internal static void Add(GameObject gameObject) => GameObjects.Add(gameObject);
    internal static void Remove(GameObject gameObject) => GameObjects.Remove(gameObject);
}