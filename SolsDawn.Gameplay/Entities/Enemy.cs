using SolsDawn.Core.Logic;
using SolsDawn.Gameplay.Combat;

namespace SolsDawn.Gameplay.Entities;

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

    private static nkast.Aether.Physics2D.Collision.Shapes.Shape BodyShape(EnemySpecs specs) =>
        Shapes.Rectangle(specs.Width, specs.Height);

    public async Job Run()
    {
        while (true)
        {
            await BladeAttack();
            await Timer(Board.Specs.GetActiveInstance().AttackGap);
            await FireAttack();
            await Timer(Board.Specs.GetActiveInstance().AttackGap);
        }
    }

    private Vector2 AimAtPlayer() => CombatMath.Direction(Transform.Position, G.Player.Transform.Position);

    private async Job BladeAttack()
    {
        var s = Board.Specs.GetActiveInstance();
        var position = Transform.Position;
        var aim = AimAtPlayer();
        var bladeCenter = position + aim * s.BladeLength / 2;
        var bladeShape = Shapes.Rectangle(s.BladeLength, s.BladeWidth);

        var parrybox = new Parrybox(CreateObject(bladeCenter, aim.Angle()), bladeShape, Side.Enemy, ParryKind.Blade);
        Board.Tint = s.BladeTelegraphColor;
        await Race(Timer(s.BladeTelegraphDuration), parrybox.Parried).Finished;
        var parried = parrybox.Parried.IsFired;
        parrybox.Destroy();

        if (parried)
        {
            Board.Tint = s.BladeParriedColor;
            await Timer(s.BladeParriedDuration);
            Board.Tint = null;
            return;
        }

        Board.Tint = null;
        var attack = new Attack(CreateObject(bladeCenter, aim.Angle()), bladeShape, Side.Player, ParryKind.None);
        Game.AnimationsPool.Add(new LineTraceAnimation(position, position + aim * s.BladeLength, s.BladeWidth, s.BladeAttackDuration, s.BladeAttackColor));
        await Timer(s.BladeAttackDuration);
        attack.Destroy();
    }

    private async Job FireAttack()
    {
        var s = Board.Specs.GetActiveInstance();
        var position = Transform.Position;
        var aim = AimAtPlayer();

        var parrybox = new Parrybox(CreateObject(position), BodyShape(s), Side.Enemy, ParryKind.Fire);
        Board.Tint = s.FireTelegraphColor;
        var telegraph = Timer(s.FireTelegraphDuration);
        var elapsed = 0f;
        while (!telegraph.IsEnded && !parrybox.Parried.IsFired)
        {
            if (elapsed < s.FireAimingDuration)
                aim = AimAtPlayer();
            elapsed += DeltaTime;
            await NextFrame();
        }

        var parried = parrybox.Parried.IsFired;
        var parriedBy = parrybox.ParriedBy;
        parrybox.Destroy();

        if (parried)
        {
            var target = parriedBy?.ParryPoint ?? (position + G.Player.Transform.Position) / 2;
            Game.AnimationsPool.Add(new LineTraceAnimation(position, target, s.FireWidth, s.FireTraceDuration, s.FireTraceColor));
            Board.Tint = s.FireParriedColor;
            await Timer(s.FireParriedDuration);
            Board.Tint = null;
            return;
        }

        Board.Tint = null;
        var attack = new Attack(
            CreateObject(position + aim * s.FireDistance / 2, aim.Angle()),
            Shapes.Rectangle(s.FireDistance, s.FireWidth),
            Side.Player,
            ParryKind.None);
        await attack.Resolved;
        Game.AnimationsPool.Add(new LineTraceAnimation(position, position + aim * s.FireDistance, s.FireWidth, s.FireTraceDuration, s.FireTraceColor));
        attack.Destroy();
    }
}

public class EnemyCategoryAttribute() : CategoryAttribute(typeof(EnemySpecs))
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
