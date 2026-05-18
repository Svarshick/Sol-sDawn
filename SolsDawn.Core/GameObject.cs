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

    public T GetComponent<T>() where T : Component<T>
    {
        foreach (var component in _components)
        {
            if (component is T match)
                return match;
        }

        return null;
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
    
    public void Update(GameTime gameTime)
    {
        foreach (var component in _components)
        {
            if (component is IUpdatable updatable)
                updatable.Update(gameTime);
        }
    }

    public void LateUpdate(GameTime gameTime)
    {
        foreach (var component in _components)
        {
            if (component is IUpdatable updatable)
                updatable.LateUpdate(gameTime);
        }
    }

    public void Draw(GameTime gameTime)
    {
        foreach (var component in _components)
        {
            if (component is IDrawable drawable)
                drawable.Draw(gameTime);
        }
    }

    public void Dispose()
    {
        foreach(var component in _components)
            component.Dispose();
    } 
}