namespace SolsDawn.Core;

public interface IStartable
{
    public void Start();
}

public interface IUpdatable 
{
    public void Update();
    public void LateUpdate();
}

public interface IDrawable
{
    public void Draw();
}