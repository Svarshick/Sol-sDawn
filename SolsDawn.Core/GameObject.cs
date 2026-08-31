using MonoGame.Extended;

namespace SolsDawn.Core;

public abstract class Component : IUpdatable, IDrawable
{
    public readonly GameObject GameObject;
    public readonly bool IsSticky;
    public bool IsDestroyed { get; internal set; }
    

    public Component(GameObject gameObject, bool isSticky = false)
    {
        GameObject = gameObject;
        IsSticky = isSticky;
        //NOTE exception issue https://aistudio.google.com/prompts/1vhcj4zRFCU6uZrlyz3pYAGzul-2SGJ9x
        gameObject.AddComponent(this);
    }

    public void Destroy()
    {
        if (IsDestroyed)
            return;

        IsDestroyed = true;
        OnDestroyImmediate();
        if (IsSticky && !GameObject.IsDestroyed)
        {
            GameObject.Destroy();
        }
    }

    public virtual void OnDestroyImmediate()
    {
    }

    public virtual void OnDestroyDelayed()
    {
    }

    public virtual void Update()
    {
    }

    public virtual void LateUpdate()
    {
    }

    public virtual void Draw()
    {
    }
}

public sealed class GameObject : IUpdatable, IDrawable, IComparable<GameObject>
{
    internal List<Component> Components = new();
    internal List<Component> ComponentsToAdd = new();
    internal bool IsDirty { get; set; }
    
    public bool IsDestroyed { get; private set; } = false;
    
    public readonly Transform2 Transform = new();
    
    public GameObject()
    {
        GameObjectPool.Add(this);
    }

    public void Destroy()
    {
        if (IsDestroyed)
            return;

        IsDestroyed = true;
        foreach (var component in Components)
        {
            if (!component.IsDestroyed)
            {
                component.IsDestroyed = true;
                component.OnDestroyImmediate();
            }
        }

        for (int i = 0; i < ComponentsToAdd.Count; i++)
        {
            var component = ComponentsToAdd[i];
            if (!component.IsDestroyed)
            {
                component.IsDestroyed = true;
                component.OnDestroyImmediate();
            }
        }
    }

    public int CompareTo(GameObject? other) => GetHashCode().CompareTo(other?.GetHashCode());

    public T? GetComponent<T>() where T : Component
    {
        if (IsDestroyed)
            throw new Exception("Access to destroyed GameObject");
        
        foreach (var component in Components)
        {
            if (!component.IsDestroyed && component.GetType() == typeof(T))
                return (T)component;
        }

        foreach (var component in ComponentsToAdd)
        {
            if (!component.IsDestroyed && component.GetType() == typeof(T))
                return (T)component;
        }

        return null;
    }

    public Component? GetComponent(Type ofType)
    { 
        if (IsDestroyed)
            throw new Exception("Access to destroyed GameObject");
        
        foreach (var component in Components)
        {
            if (!component.IsDestroyed && component.GetType() == ofType)
                return component;
        }
        
        foreach (var component in ComponentsToAdd)
        {
            if (!component.IsDestroyed && component.GetType() == ofType)
                return component;
        }

        return null;
    }

    public bool TryGetComponent<T>(out T component) where T : Component
    {
        if (IsDestroyed)
            throw new Exception("Access to destroyed GameObject");
        
        foreach (var currentComponent in Components)
        {
            if (!currentComponent.IsDestroyed && currentComponent.GetType() == typeof(T))
            {
                component = (T)currentComponent;
                return true;
            }
        }
        
        foreach (var currentComponent in ComponentsToAdd)
        {
            if (!currentComponent.IsDestroyed && currentComponent.GetType() == typeof(T))
            {
                component = (T)currentComponent;
                return true;
            }
        }

        component = null;
        return false;
    }

    public bool TryGetComponents<T1, T2>(out T1? component1, out T2? component2) 
        where T1 : Component
        where T2 : Component
    {
        if (IsDestroyed)
            throw new Exception("Access to destroyed GameObject");
        
        component1 = null;
        component2 = null;
        foreach (var component in Components)
        {
            if (!component.IsDestroyed && component.GetType() == typeof(T1))
                component1 = (T1)component;
            if (!component.IsDestroyed && component.GetType() == typeof(T2))
                component2 = (T2)component;

            if (component1 is not null && 
                component2 is not null)
                return true;
        }
        
        foreach (var component in ComponentsToAdd)
        {
            if (!component.IsDestroyed && component.GetType() == typeof(T1))
                component1 = (T1)component;
            if (!component.IsDestroyed && component.GetType() == typeof(T2))
                component2 = (T2)component;

            if (component1 is not null && 
                component2 is not null)
                return true;
        }

        return false;
    }
    
    public bool TryGetComponents<T1, T2, T3>(out T1? component1, out T2? component2, out T3? component3) 
        where T1 : Component
        where T2 : Component
        where T3 : Component
    {
        if (IsDestroyed)
            throw new Exception("Access to destroyed GameObject");
        
        component1 = null;
        component2 = null;
        component3 = null;
        foreach (var component in Components)
        {
            if (!component.IsDestroyed && component.GetType() == typeof(T1))
                component1 = (T1)component;
            if (!component.IsDestroyed && component.GetType() == typeof(T2))
                component2 = (T2)component;
            if (!component.IsDestroyed && component.GetType() == typeof(T3))
                component3 = (T3)component;

            if (component1 is not null && 
                component2 is not null &&
                component3 is not null)
                return true;
        }
        
        foreach (var component in ComponentsToAdd)
        {
            if (!component.IsDestroyed && component.GetType() == typeof(T1))
                component1 = (T1)component;
            if (!component.IsDestroyed && component.GetType() == typeof(T2))
                component2 = (T2)component;
            if (!component.IsDestroyed && component.GetType() == typeof(T3))
                component3 = (T3)component;

            if (component1 is not null && 
                component2 is not null &&
                component3 is not null)
                return true;
        }

        return false;
    }
    
    internal void AddComponent(Component component)
    {
        if (IsDestroyed)
            throw new Exception("Access to destroyed GameObject");
        
        var type = component.GetType();
        var existingComponent = GetComponent(type);
        if (existingComponent != null)
            throw new ArgumentException($"The component {type.Name} is already attached");
        
        ComponentsToAdd.Add(component);
        IsDirty = true;
    }
    
    public void RemoveComponent<T>() where T : Component
    {
        if (IsDestroyed)
            throw new Exception("Access to destroyed GameObject");
        
        foreach (var component in Components)
        {
            if (!component.IsDestroyed && component.GetType() == typeof(T))
            {
                component.Destroy();
                IsDirty = true;
                return;
            }
        }
        
        foreach (var component in ComponentsToAdd)
        {
            if (!component.IsDestroyed && component.GetType() == typeof(T))
            {
                component.Destroy();
                IsDirty = true;
                return;
            }
        }

        throw new ArgumentException($"The component {typeof(T).Name} isn't attached");
    }

    public void Update()
    {
        if (IsDestroyed)
            return;
        
        foreach (var component in Components)
        {
            if (!component.IsDestroyed)
                component.Update();
        }
    }

    public void LateUpdate()
    {
        if (IsDestroyed)
            return;
        
        foreach (var component in Components)
        {
            if (!component.IsDestroyed)
                component.LateUpdate();
        }
    }

    public void Draw()
    {
        if (IsDestroyed)
            return;
        
        foreach (var component in Components)
        {
            if (!component.IsDestroyed)
                component.Draw();
        }
    }
}