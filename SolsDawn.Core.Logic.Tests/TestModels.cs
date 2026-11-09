using System.Text.Json.Serialization;
using SolsDawn.Core.Logic;

namespace SolsDawn.Core.Logic.Tests;

public class TagCategory(string tag, Type fallback) : CategoryAttribute(fallback)
{
    public string Tag { get; } = tag;
}

public class TagModel(params string[] tags) : ModelAttribute
{
    public override bool Satisfies(CategoryAttribute slot) => slot is TagCategory c && tags.Contains(c.Tag);
}

public interface IWeapon;

[TagModel("one-handed", "melee")]
public class Sword : IWeapon
{
    public float Damage = 1f;
}

[TagModel("two-handed", "melee")]
public class Spear : IWeapon
{
    public float Reach = 2f;
}

public class Loadout
{
    public float Weight = 1f;

    [TagCategory("melee", typeof(Spear))]
    public CategorySlot<IWeapon> Backup = new();

    [JsonIgnore]
    public int RuntimeCounter { get; set; }
}

[TagModel("player")]
public class Hero
{
    public float Speed = 5f;

    [TagCategory("one-handed", typeof(Sword))]
    public CategorySlot<IWeapon> Weapon = new();

    public Loadout Gear = new();
}

[TagModel("squad")]
public class Squad
{
    [TagCategory("player", typeof(Hero))]
    public CategorySlot<Hero> Leader = new();
}

[TagModel("node")]
public class Node
{
    [TagCategory("node", typeof(Node))]
    public CategorySlot<Node> Child = new();
}

public class TestRoot
{
    [TagCategory("squad", typeof(Squad))]
    public CategorySlot<Squad> Team = new();
}
