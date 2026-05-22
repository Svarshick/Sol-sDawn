using Microsoft.Xna.Framework;

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

public sealed class GameObject : IUpdatable, IDrawable, IDisposable
{
    private readonly List<IDisposable> _components = new();
    
    public readonly Transform Transform = new();
    
    public GameObject()
    {
        GameObjectPool.Add(this);
    }

    public T? GetComponent<T>() where T : Component<T>
    {
        foreach (var component in _components)
        {
            if (component is T match)
                return match;
        }

        return null;
    }

    public bool TryGetComponents<T1, T2>(out T1? component1, out T2? component2) 
        where T1 : Component<T1>
        where T2 : Component<T2>
    {
        component1 = null;
        component2 = null;
        foreach (var component in _components)
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
        foreach (var component in _components)
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
        
        _components.Add(component);
    }
    
    public void RemoveComponent<T>() where T : Component<T>
    {
        for (int i = 0; i < _components.Count; i++)
        {
            if (_components[i] is T)
            {
                _components[i].Dispose();
                _components.RemoveAt(i);
                return;
            }
        }

        throw new ArgumentException($"The component {typeof(T).Name} isn't attached");
    }

    public void Update()
    {
        foreach (var component in _components)
        {
            if (component is IUpdatable updatable)
                updatable.Update();
        }
    }

    public void LateUpdate()
    {
        foreach (var component in _components)
        {
            if (component is IUpdatable updatable)
                updatable.LateUpdate();
        }
    }

    public void Draw()
    {
        foreach (var component in _components)
        {
            if (component is IDrawable drawable)
                drawable.Draw();
        }
    }

    public void Dispose()
    {
        foreach(var component in _components)
            component.Dispose();
        GameObjectPool.Remove(this);
    }
}