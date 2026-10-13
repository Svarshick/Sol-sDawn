//Vibecoded
using Microsoft.Xna.Framework;

namespace SolsDawn.Core;

public class Transform2
{
    private Vector2 _position = Vector2.Zero;
    private float _rotation = 0f;
    private Vector2 _scale = Vector2.One;

    private Matrix _localMatrix = Matrix.Identity;
    private Matrix _worldMatrix = Matrix.Identity;
    
    private bool _isDirty = true;

    private Transform2? _parent;
    private readonly List<Transform2> _children = new();

    public Vector2 Position
    {
        get => _position;
        set { _position = value; SetDirty(); }
    }

    public float Rotation
    {
        get => _rotation;
        set { _rotation = value; SetDirty(); }
    }

    public Vector2 Scale
    {
        get => _scale;
        set { _scale = value; SetDirty(); }
    }

    public Transform2? Parent
    {
        get => _parent;
        set
        {
            if (_parent == value) return;
            _parent?._children.Remove(this);
            _parent = value;
            _parent?._children.Add(this);
            SetDirty();
        }
    }

    public Matrix LocalMatrix
    {
        get
        {
            if (_isDirty) UpdateMatrices();
            return _localMatrix;
        }
    }

    public Matrix WorldMatrix
    {
        get
        {
            if (_isDirty) UpdateMatrices();
            return _worldMatrix;
        }
    }

    public Vector2 WorldPosition
    {
        get
        {
            var wm = WorldMatrix;
            return new Vector2(wm.Translation.X, wm.Translation.Y);
        }
    }

    public float WorldRotation
    {
        get
        {
            var wm = WorldMatrix;
            return (float)Math.Atan2(wm.M12, wm.M11);
        }
    }

    public Vector2 WorldScale
    {
        get
        {
            var wm = WorldMatrix;
            float scaleX = (float)Math.Sqrt(wm.M11 * wm.M11 + wm.M12 * wm.M12);
            float scaleY = (float)Math.Sqrt(wm.M21 * wm.M21 + wm.M22 * wm.M22);
            return new Vector2(scaleX, scaleY);
        }
    }

    private void SetDirty()
    {
        if (_isDirty)
            return;
        _isDirty = true;
        
        for (int i = 0; i < _children.Count; i++)
        {
            _children[i].SetDirty();
        }
    }

    private void UpdateMatrices()
    {
        _localMatrix = Matrix.CreateScale(new Vector3(_scale, 1f)) *
                       Matrix.CreateRotationZ(_rotation) *
                       Matrix.CreateTranslation(new Vector3(_position, 0f));

        if (_parent != null)
        {
            _worldMatrix = _localMatrix * _parent.WorldMatrix;
        }
        else
        {
            _worldMatrix = _localMatrix;
        }

        _isDirty = false;
    }
}