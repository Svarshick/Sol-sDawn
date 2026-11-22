using System.Text.Json.Serialization;

namespace SolsDawn.Tests;

public class TagCategory(string tag) : CategoryAttribute
{
    public string Tag { get; } = tag;
}

public class TagModel(params string[] tags) : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is TagCategory c && tags.Contains(c.Tag);

    public override int Breadth => tags.Length;
}

public interface IWeapon;

[TagModel("one-handed", "melee")]
public class Sword : IWeapon
{
    public float Damage = 1f;
}

[TagModel("two-handed", "melee")]
[Default]
public class Spear : IWeapon
{
    public float Reach = 2f;
}

public class Loadout
{
    public float Weight = 1f;

    [TagCategory("melee")]
    public CategorySlot<IWeapon> Backup = new();

    [JsonIgnore]
    public int RuntimeCounter { get; set; }
}

[TagModel("player")]
public class Hero
{
    public float Speed = 5f;

    [TagCategory("one-handed")]
    public CategorySlot<IWeapon> Weapon = new();

    public Loadout Gear = new();
}

[TagModel("squad")]
public class Squad
{
    [TagCategory("player")]
    public CategorySlot<Hero> Leader = new();
}

[TagModel("node")]
public class Node
{
    [TagCategory("node")]
    public CategorySlot<Node> Child = new();
}

public class TestRoot
{
    [TagCategory("squad")]
    public CategorySlot<Squad> Team = new();
}

public interface IPick;

[TagModel("spread", "narrow")]
public class WidePick : IPick;

[TagModel("narrow")]
public class NarrowPick : IPick;

[TagModel("marked", "spread")]
[Default]
public class MarkedWidePick : IPick;

[TagModel("marked")]
public class MarkedNarrowRival : IPick;

[TagModel("tie", "a")]
[Default]
public class TieB : IPick;

[TagModel("tie", "c")]
[Default]
public class TieA : IPick;

[TagModel("a")]
public class OnlyA : IPick;

public class PickHolder
{
    [TagCategory("narrow")]
    public CategorySlot<IPick> Narrow = new();

    [TagCategory("marked")]
    public CategorySlot<IPick> Marked = new();

    [TagCategory("tie")]
    public CategorySlot<IPick> Tie = new();

    public PickContainer Inner = new();
}

public class PickContainer
{
    [TagCategory("nothing fits")]
    public CategorySlot<IPick> Missing = new();
}
