using MonoGame.Extended;

namespace SolsDawn.Core;

public abstract class Component<T> : IDisposable where T : Component<T>
{
    public readonly GameObject GameObject;

    public Component(GameObject gameObject)
    {
        GameObject = gameObject;
        gameObject.AddComponent((T)this);
    }

    public abstract void Dispose();
}

public sealed class GameObject : IUpdatable, IDrawable, IDisposable, IComparable<GameObject>
{
    internal readonly List<IDisposable> Components = new();
    
    public readonly Transform2 Transform = new();
    public bool IsDisposed { get; private set; } = false;
    
    public GameObject()
    {
        GameObjectPool.Add(this);
    }
    
    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        GameObjectPool.Destroy(this);
    }
    
    public int CompareTo(GameObject? other) => GetHashCode().CompareTo(other?.GetHashCode());

    public T? GetComponent<T>() where T : Component<T>
    {
        foreach (var component in Components)
        {
            if (component is T match)
                return match;
        }

        return null;
    }

    public bool TryGetComponent<T>(out T? component) where T : Component<T>
    {
        foreach (var currentComponent in Components)
        {
            if (currentComponent is T found)
            {
                component = found;
                return true;
            }
        }

        component = null;
        return false;
    }

    public bool TryGetComponents<T1, T2>(out T1? component1, out T2? component2) 
        where T1 : Component<T1>
        where T2 : Component<T2>
    {
        component1 = null;
        component2 = null;
        foreach (var component in Components)
        {
            if (component is T1 match1)
                component1 = match1;
            if (component is T2 match2)
                component2 = match2;

            if (component1 is not null && 
                component2 is not null)
                return true;
        }

        return false;
    }
    
    public bool TryGetComponents<T1, T2, T3>(out T1? component1, out T2? component2, out T3? component3) 
        where T1 : Component<T1>
        where T2 : Component<T2>
        where T3 : Component<T3>
    {
        component1 = null;
        component2 = null;
        component3 = null;
        foreach (var component in Components)
        {
            if (component is T1 match1)
                component1 = match1;
            if (component is T2 match2)
                component2 = match2;
            if (component is T3 match3)
                component3 = match3;

            if (component1 is not null && 
                component2 is not null &&
                component3 is not null)
                return true;
        }

        return false;
    }
    
    internal void AddComponent<T>(T component) where T : Component<T>
    {
        var existingComponent = GetComponent<T>();
        if (existingComponent != null)
            throw new ArgumentException($"The component {component.GetType().Name} is already attached");
        
        Components.Add(component);
    }
    
    public void RemoveComponent<T>() where T : Component<T>
    {
        for (int i = 0; i < Components.Count; i++)
        {
            if (Components[i] is T)
            {
                Components[i].Dispose();
                Components.RemoveAt(i);
                return;
            }
        }

        throw new ArgumentException($"The component {typeof(T).Name} isn't attached");
    }

    public void Update()
    {
        foreach (var component in Components)
        {
            if (component is IUpdatable updatable)
                updatable.Update();
        }
    }

    public void LateUpdate()
    {
        foreach (var component in Components)
        {
            if (component is IUpdatable updatable)
                updatable.LateUpdate();
        }
    }

    public void Draw()
    {
        foreach (var component in Components)
        {
            if (component is IDrawable drawable)
                drawable.Draw();
        }
    }
}