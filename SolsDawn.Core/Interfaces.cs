using Microsoft.Xna.Framework;

namespace SolsDawn.Core;

public interface IStartable
{
    public void Start();
}

public interface IUpdatable 
{
    public void Update(GameTime gameTime);
    public void LateUpdate(GameTime gameTime);
}

public interface IDrawable
{
    public void Draw(GameTime gameTime);
}