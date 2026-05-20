using Microsoft.Xna.Framework;

namespace SolsDawn.Core;

public static class GameObjectPool
{
    private static readonly HashSet<GameObject> GameObjects = new();

    public static void Update(GameTime gameTime)
    {
        foreach (var go in GameObjects)
            go.Update(gameTime);
    }

    public static void LateUpdate(GameTime gameTime)
    {
        foreach (var go in GameObjects)
            go.LateUpdate(gameTime);
    }

    public static void Draw(GameTime gameTime)
    {
        foreach (var go in GameObjects)
            go.Draw(gameTime);
    }
    
    internal static void Add(GameObject gameObject) => GameObjects.Add(gameObject);
    internal static void Remove(GameObject gameObject) => GameObjects.Remove(gameObject);
}