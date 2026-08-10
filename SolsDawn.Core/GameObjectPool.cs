namespace SolsDawn.Core;

//first Update all alive GO (updates all alive GO.Components). Any new GO added to the GameObjectsToAdd
//then clears: delayed destroy, dirty GO (added/removed components), merge GameObjectsToAdd
//
//clear process:
//Iterate over GameObjects. If destroyed, calls all OnDestroyDelayed. If not destroyed but dirty, iterates over
//components, OnDestroyDelayed, then merges Components with ComponentsToAdd.
//Iterate over GameObjectsToAdd and do the same.
//Even when new GO added/destroyed, nothing broken. When GO destroyed before it's reached, GO clear process completed.
//
//Rules:
//OnDestroyDelayed SHOULDN'T destroy any component/GO. Do it in OnDestroyImmediate or make sticky
//OnDestroyDelayed COULD add new GO
//OnDestroyDelayed SHOULDN't add components to existing GO (they might not be updated in the next frame)
//CAN'T add/remove/get component in destroyed GO

public static class GameObjectPool
{
    private readonly static List<GameObject> GameObjects = new();
    private readonly static List<GameObject> GameObjectsToAdd = new();

    public static void Update()
    {
        foreach (var go in GameObjects)
        {
            if (!go.IsDestroyed)
                go.Update();
        }

        int aliveCount = 0;
        for (int i = 0; i < GameObjects.Count; i++)
        {
            var go = GameObjects[i];
            ProcessGameObjectCleanup(go);
            if (!go.IsDestroyed)
            {
                GameObjects[aliveCount++] = go;
            }
        }
        GameObjects.RemoveRange(aliveCount, GameObjects.Count - aliveCount);

        //the same, but over GameObjectsToAdd and just use GameObjects.Add
        for (int i = 0; i < GameObjectsToAdd.Count; i++)
        {
            var go = GameObjectsToAdd[i];
            ProcessGameObjectCleanup(go);
            if (!go.IsDestroyed)
            {
                GameObjects.Add(go);
            }
        }

        GameObjectsToAdd.Clear();
    }

    private static void ProcessGameObjectCleanup(GameObject go)
    {
        if (go.IsDestroyed)
        {
            foreach (var component in go.Components)
                component.OnDestroyDelayed();

            foreach (var component in go.ComponentsToAdd)
                component.OnDestroyDelayed();

            go.Components.Clear();
            go.ComponentsToAdd.Clear();
        }
        else if (go.IsDirty)
        {
            int aliveCompCount = 0;
            for (int i = 0; i < go.Components.Count; i++)
            {
                var component = go.Components[i];
                if (component.IsDestroyed)
                {
                    component.OnDestroyDelayed();
                }
                else
                {
                    go.Components[aliveCompCount++] = component;
                }
            }
            go.Components.RemoveRange(aliveCompCount, go.Components.Count - aliveCompCount);
            
            for (int i = 0; i < go.ComponentsToAdd.Count; i++)
            {
                var component = go.ComponentsToAdd[i];
                if (component.IsDestroyed)
                {
                    component.OnDestroyDelayed();
                }
                else
                {
                    go.Components.Add(component);
                }
            }
            go.ComponentsToAdd.Clear();
            go.IsDirty = false;
        }
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
    
    internal static void Add(GameObject gameObject) => GameObjectsToAdd.Add(gameObject);
}