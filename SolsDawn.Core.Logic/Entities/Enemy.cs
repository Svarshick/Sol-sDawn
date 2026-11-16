using Microsoft.Xna.Framework;
using SolsDawn.Core.Logic.Animations;
using SolsDawn.Core.Logic.Combat;
using SolsDawn.Core.Logic.Gameplay.Pipeline;
using Collider = SolsDawn.Core.Logic.Gameplay.Collider;
using static SolsDawn.Core.Logic.Gameplay.GameplayAPI;

namespace SolsDawn.Core.Logic.Entities;

public class Enemy : Core.Logic.Gameplay.Entity<EnemyBoard, EnemyAnimations>
{
    public readonly Hurtbox Hurtbox;
    private (float Width, float Height) _bodySize;

    public Enemy(GameObject go, EnemyBoard board, EnemyAnimations animation)
        : base(go, null, board, animation, BodyShape(board.Specs.GetActiveInstance()), Layer.Enemy)
    {
        var hurtboxObject = CreateObject();
        hurtboxObject.Transform.Parent = go.Transform;
        Hurtbox = new Hurtbox(hurtboxObject, BodyShape(board.Specs.GetActiveInstance()), Side.Enemy);
        Hurtbox.Hit.OnFire(() => Animator.Player.TryPlay(AnimationSet.Hit));
        var specs = board.Specs.GetActiveInstance();
        _bodySize = (specs.Width, specs.Height);
    }

    public override void Update()
    {
        var specs = Board.Specs.GetActiveInstance();
        if ((specs.Width, specs.Height) == _bodySize)
            return;
        _bodySize = (specs.Width, specs.Height);
        Collider.Shape = BodyShape(specs);
        Hurtbox.Collider.Shape = BodyShape(specs);
    }

    public static nkast.Aether.Physics2D.Collision.Shapes.Shape BodyShape(EnemySpecs specs) =>
        Shapes.Rectangle(specs.Width, specs.Height);

    internal Job? Behaviour;

    public override void OnDestroyImmediate()
    {
        base.OnDestroyImmediate();
        Behaviour?.Kill();
    }
}

public class EnemyCategoryAttribute : CategoryAttribute
{
    public override string GetTagsDescription() => "Entity: Enemy";
}

public class EnemyModelAttribute : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is EnemyCategoryAttribute;

    public override string GetTagsDescription() => "Entity: Enemy";
}

public record EnemyBoard
{
    public required CategorySlot<EnemySpecs> Specs;
    public Color? Tint;
}

[EnemyModel]
public record EnemySpecs
{
    [Inspect] public Color Color = Color.DarkRed;
    [Inspect(0.05f, 0.1f, 20f)] public float Width = 1.5f;
    [Inspect(0.05f, 0.1f, 20f)] public float Height = 2.5f;
    [Inspect(0.01f, 0f, 5f)] public float HitDuration = 0.2f;
    [Inspect] public Color HitBlinkColor = Color.White;
    [Inspect(0.05f, 0f, 10f)] public float AttackGap = 1f;

    [Inspect(0.05f, 0f, 10f)] public float BladeTelegraphDuration = 1f;
    [Inspect] public Color BladeTelegraphColor = Color.Yellow;
    [Inspect(0.05f, 0.1f, 20f)] public float BladeLength = 4f;
    [Inspect(0.05f, 0.1f, 20f)] public float BladeWidth = 0.6f;
    [Inspect(0.01f, 0f, 5f)] public float BladeAttackDuration = 0.3f;
    [Inspect] public Color BladeAttackColor = Color.Red;
    [Inspect(0.05f, 0f, 10f)] public float BladeParriedDuration = 1f;
    [Inspect] public Color BladeParriedColor = Color.LightBlue;

    [Inspect(0.05f, 0f, 10f)] public float FireTelegraphDuration = 1.5f;
    [Inspect(0.05f, 0f, 10f)] public float FireAimingDuration = 1f;
    [Inspect] public Color FireTelegraphColor = Color.Orange;
    [Inspect(0.5f, 0.1f, 200f)] public float FireDistance = 20f;
    [Inspect(0.01f, 0.01f, 10f)] public float FireWidth = 0.3f;
    [Inspect(0.01f, 0f, 5f)] public float FireTraceDuration = 0.4f;
    [Inspect] public Color FireTraceColor = Color.Red;
    [Inspect(0.05f, 0f, 10f)] public float FireParriedDuration = 1f;
    [Inspect] public Color FireParriedColor = Color.LightBlue;
}
