using System.Reflection;
using System.Text.Json;
using Xunit;

namespace SolsDawn.Tests;

[Collection("Library")]
public sealed class ConfigurationSystemTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "solsdawn-" + Guid.NewGuid().ToString("N"));

    public ConfigurationSystemTests() => Library.Initialize(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private static FieldInfo Field<T>(string name) => typeof(T).GetField(name)!;

    private static ModelEntry Create<T>(string name) => Library.CreateCopyFromDefault(typeof(T), null, name);

    private static T Instance<T>(ModelEntry entry) => (T)entry.Instance!;

    private static ModelSearchResult Result(string guid, IEnumerable<ModelSearchResult> results) =>
        results.Single(r => r.Guid == guid);

    private static IReadOnlyList<ModelSearchResult> SearchWeapons(string? query = null, string category = "one-handed") =>
        Library.SearchModels(new TagCategory(category), typeof(IWeapon), query);

    private string FilePath(string guid) => Path.Combine(_directory, guid + ".json");

    private static void Reference<T>(ModelEntry owner, string slotField, ModelEntry target)
    {
        var slot = (ICategorySlot)typeof(T).GetField(slotField)!.GetValue(owner.Instance)!;
        slot.ReferenceModel(target.Guid);
        Library.MarkDirty(owner.Guid);
    }

    [Fact]
    public void CFG02_SlotStoresGuidOfReferencedModel()
    {
        var hero = Create<Hero>("H");
        var sword = Create<Sword>("S");
        Reference<Hero>(hero, "Weapon", sword);

        Assert.Equal(sword.Guid, Instance<Hero>(hero).Weapon.ModelGuid);
        Assert.Same(sword.Instance, Instance<Hero>(hero).Weapon.GetActiveInstance());
    }

    [Fact]
    public void CFG04_ContainerIsSavedInsideOwnerWithoutOwnFile()
    {
        var hero = Create<Hero>("H");
        Instance<Hero>(hero).Gear.Weight = 7f;
        Library.MarkDirty(hero.Guid);
        Library.Save();
        var filesBefore = Directory.GetFiles(_directory).Length;

        Library.Initialize(_directory);

        Assert.Equal(1, filesBefore);
        Assert.Equal(7f, Library.GetModel<Hero>(hero.Guid)!.Gear.Weight);
    }

    [Fact]
    public void CFG05_RuntimeStateIsNotSaved()
    {
        var hero = Create<Hero>("H");
        Instance<Hero>(hero).Gear.RuntimeCounter = 9;
        Library.MarkDirty(hero.Guid);
        Library.Save();

        Assert.DoesNotContain("RuntimeCounter", File.ReadAllText(FilePath(hero.Guid)));
    }

    [Fact]
    public void CFG06_NewSlotReferencesCategoryDefaultWithoutFiles()
    {
        var hero = new Hero();
        CategorySystem.Bind(hero);

        Assert.IsType<Sword>(hero.Weapon.GetActiveInstance());
        Assert.True(hero.Weapon.GetActiveModelEntry().IsDefault);
        Assert.IsType<Spear>(hero.Gear.Backup.GetActiveInstance());
        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public void CFG10_SearchReturnsOnlyFittingCategory()
    {
        var sword = Create<Sword>("Blade");
        var spear = Create<Spear>("Pike");

        var results = SearchWeapons();

        Assert.Contains(results, r => r.Guid == sword.Guid);
        Assert.DoesNotContain(results, r => r.Guid == spear.Guid);
        Assert.DoesNotContain(results, r => r.ModelType == typeof(Spear));
    }

    [Fact]
    public void CFG15_RootPersistsAcrossLaunchesAsRootJson()
    {
        var root = Library.LoadRoot<TestRoot>();
        var squad = Create<Squad>("Alpha");
        root.Team.ReferenceModel(squad.Guid);
        Library.MarkDirty(Library.RootGuid);
        Library.Save();

        Library.Initialize(_directory);

        Assert.True(File.Exists(FilePath("root")));
        Assert.Equal(squad.Guid, Library.LoadRoot<TestRoot>().Team.ModelGuid);
    }

    [Fact]
    public void CFG16_RootIsNotOfferedAndCannotBeDeleted()
    {
        Library.LoadRoot<TestRoot>();

        Assert.DoesNotContain(Library.SearchModels(null, typeof(object), null), r => r.Guid == Library.RootGuid);

        Library.DeleteModel(Library.RootGuid);
        Assert.True(Library.ContainsModel(Library.RootGuid));
    }

    [Fact]
    public void CFG18_LoadRootNeverOverwritesAnUnreadableRootFile()
    {
        File.WriteAllText(FilePath("root"), "{ not json");
        Library.Initialize(_directory);

        Assert.Throws<InvalidOperationException>(Library.LoadRoot<TestRoot>);
        Assert.Equal("{ not json", File.ReadAllText(FilePath("root")));
    }

    [Fact]
    public void CFG21_DefaultModelsCreateNoFileAndCannotBeDeleted()
    {
        var entry = CategoryRegistry.GetDefaultModelEntry(typeof(Sword));

        Library.DeleteModel(entry.Guid);

        Assert.Same(entry, Library.GetModelEntry(entry.Guid));
        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public void CFG22_SelectingDefaultReferencesItEvenWithShift()
    {
        var hero = Create<Hero>("H");
        var result = Result(CategoryRegistry.GetDefaultModelEntry(typeof(Sword)).Guid, SearchWeapons());

        Assert.False(result.WillCopy(shiftHeld: false));
        Assert.False(result.WillCopy(shiftHeld: true));

        var created = ModelEdit.ForModel(hero).ApplySearchResult(Field<Hero>("Weapon"), result, copy: false);

        Assert.Null(created);
        Assert.Equal(result.Guid, Instance<Hero>(hero).Weapon.ModelGuid);
        Library.Save();
        Assert.Single(Directory.GetFiles(_directory));
    }

    [Fact]
    public void CFG23_FirstEditOfDefaultCreatesNamedCopyAndLeavesDefaultUnchanged()
    {
        var hero = Create<Hero>("Hero1");
        var edit = ModelEdit.ForModel(hero).Slot(Field<Hero>("Weapon"));

        edit.SetField(Field<Sword>("Damage"), 42f);

        var weapon = Instance<Hero>(hero).Weapon;
        Assert.Equal(42f, ((Sword)weapon.GetActiveInstance()).Damage);
        Assert.False(weapon.GetActiveModelEntry().IsDefault);
        Assert.Equal("Hero1_Weapon", weapon.GetActiveModelName());
        Assert.Equal(1f, ((Sword)CategoryRegistry.GetDefaultModelEntry(typeof(Sword)).Instance!).Damage);
    }

    [Fact]
    public void CFG23_SecondEditReusesTheCopy()
    {
        var hero = Create<Hero>("H");
        var edit = ModelEdit.ForModel(hero).Slot(Field<Hero>("Weapon"));

        edit.SetField(Field<Sword>("Damage"), 2f);
        var count = Library.GetAllModels().Count;
        edit.SetField(Field<Sword>("Damage"), 3f);

        Assert.Equal(count, Library.GetAllModels().Count);
    }

    [Fact]
    public void CFG23_EditInsideNestedDefaultsCopiesWholeChain()
    {
        var root = Library.LoadRoot<TestRoot>();
        var rootEntry = Library.GetModelEntry(Library.RootGuid)!;
        var edit = ModelEdit.ForModel(rootEntry)
            .Slot(Field<TestRoot>("Team"))
            .Slot(Field<Squad>("Leader"))
            .Slot(Field<Hero>("Weapon"));

        edit.SetField(Field<Sword>("Damage"), 9f);
        Library.Save();
        Library.Initialize(_directory);

        var loaded = Library.LoadRoot<TestRoot>();
        var squad = loaded.Team.GetActiveInstance();
        var hero = squad.Leader.GetActiveInstance();
        Assert.Equal(9f, ((Sword)hero.Weapon.GetActiveInstance()).Damage);
        Assert.Equal("TestRoot_Team", loaded.Team.GetActiveModelName());
        Assert.Equal("TestRoot_Team_Leader", squad.Leader.GetActiveModelName());
        Assert.Equal("TestRoot_Team_Leader_Weapon", hero.Weapon.GetActiveModelName());
    }

    [Fact]
    public void CFG23_EditThroughContainerInsideDefault()
    {
        var hero = Create<Hero>("H");
        var edit = ModelEdit.ForModel(hero).Container(Field<Hero>("Gear")).Slot(Field<Loadout>("Backup"));

        edit.SetField(Field<Spear>("Reach"), 8f);

        Assert.Equal(8f, ((Spear)Instance<Hero>(hero).Gear.Backup.GetActiveInstance()).Reach);
        Assert.Equal("H_Backup", Instance<Hero>(hero).Gear.Backup.GetActiveModelName());
    }

    [Fact]
    public void CFG23_ChangingSlotInsideDefaultCountsAsEdit()
    {
        var squad = Create<Squad>("S");
        var sword = Create<Sword>("Blade");
        var edit = ModelEdit.ForModel(squad).Slot(Field<Squad>("Leader"));

        edit.ApplySearchResult(Field<Hero>("Weapon"), Result(sword.Guid, SearchWeapons()), copy: false);

        var leader = Instance<Squad>(squad).Leader;
        Assert.False(leader.GetActiveModelEntry().IsDefault);
        Assert.Equal(sword.Guid, leader.GetActiveInstance().Weapon.ModelGuid);
    }

    [Fact]
    public void CFG33_CFG34_ActionDependsOnOrphanStateAndShift()
    {
        var hero = Create<Hero>("H");
        var used = Create<Sword>("Used");
        var orphan = Create<Sword>("Lone");
        Reference<Hero>(hero, "Weapon", used);

        var results = SearchWeapons();

        Assert.True(Result(used.Guid, results).WillCopy(false));
        Assert.False(Result(used.Guid, results).WillCopy(true));
        Assert.False(Result(orphan.Guid, results).WillCopy(false));
        Assert.True(Result(orphan.Guid, results).WillCopy(true));
    }

    [Fact]
    public void CFG35_SearchHidesModelsThatWouldContainTheirOwner()
    {
        var parent = Create<Node>("Parent");
        var child = Create<Node>("Child");
        var other = Create<Node>("Other");
        Reference<Node>(parent, "Child", child);

        var results = Library.SearchModels(new TagCategory("node"), typeof(Node), null, child.Guid);

        Assert.DoesNotContain(results, r => r.Guid == parent.Guid);
        Assert.DoesNotContain(results, r => r.Guid == child.Guid);
        Assert.Contains(results, r => r.Guid == other.Guid);
    }

    [Fact]
    public void CFG40_HasReturnsTransitiveDescendants()
    {
        var squad = Create<Squad>("Alpha");
        var hero = Create<Hero>("H");
        var sword = Create<Sword>("Blade");
        Reference<Squad>(squad, "Leader", hero);
        Reference<Hero>(hero, "Weapon", sword);

        var results = SearchWeapons("has:Alpha");

        Assert.Equal(sword.Guid, Assert.Single(results).Guid);
    }

    [Fact]
    public void CFG41_HadIncludesPastReferences()
    {
        var hero = Create<Hero>("Holder");
        var old = Create<Sword>("Old");
        var current = Create<Sword>("New");
        Reference<Hero>(hero, "Weapon", old);
        Reference<Hero>(hero, "Weapon", current);

        Assert.Equal(current.Guid, Assert.Single(SearchWeapons("has:Holder")).Guid);
        Assert.Contains(SearchWeapons("had:Holder"), r => r.Guid == old.Guid);
    }

    [Fact]
    public void CFG42_DefaultTagReturnsOnlyDefaultModels()
    {
        Create<Sword>("Sword");

        var result = Assert.Single(SearchWeapons("default:Sword"));

        Assert.True(result.IsDefault);
        Assert.Equal("default: Sword", result.DisplayLabel);
    }

    [Fact]
    public void CFG43_SameNameMostRecentlyUsedFirst_DefaultsLast()
    {
        var first = Create<Sword>("Twin");
        var second = Create<Sword>("Twin");
        Library.MarkDirty(first.Guid);

        var results = SearchWeapons();

        Assert.Equal(first.Guid, results[0].Guid);
        Assert.Equal(second.Guid, results[1].Guid);
        Assert.True(results[^1].IsDefault);
    }

    [Fact]
    public void CFG44_ResultReachedThroughSeveralParentsIsListedOnce()
    {
        var a = Create<Hero>("Pair_A");
        var b = Create<Hero>("Pair_B");
        var sword = Create<Sword>("Blade");
        Reference<Hero>(a, "Weapon", sword);
        Reference<Hero>(b, "Weapon", sword);

        Assert.Equal(sword.Guid, Assert.Single(SearchWeapons("has:Pair")).Guid);
    }

    [Fact]
    public void CFG50_CFG51_HistoryIsKeptAfterDeleteAndCleanedLazilyByHad()
    {
        var hero = Create<Hero>("Holder");
        var old = Create<Sword>("Old");
        Reference<Hero>(hero, "Weapon", old);
        var slot = Instance<Hero>(hero).Weapon;
        slot.ReferenceModel(CategoryRegistry.GetDefaultModelEntry(typeof(Sword)).Guid);

        Library.DeleteModel(old.Guid);
        Assert.Contains(old.Guid, slot.HistoryGuids);

        Library.MarkDirty(hero.Guid);
        Library.Save();
        Library.Initialize(_directory);
        var reloaded = Library.GetModel<Hero>(hero.Guid)!.Weapon;
        Assert.Contains(old.Guid, reloaded.HistoryGuids);

        Assert.Empty(SearchWeapons("had:Holder"));
        Assert.DoesNotContain(old.Guid, reloaded.HistoryGuids);
    }

    [Fact]
    public void CFG52_CFG53_DeletedReferenceBecomesPhantomUntilChanged()
    {
        var hero = Create<Hero>("H");
        var sword = Create<Sword>("Blade");
        var replacement = Create<Sword>("Other");
        Reference<Hero>(hero, "Weapon", sword);
        var slot = Instance<Hero>(hero).Weapon;

        Library.DeleteModel(sword.Guid);

        Assert.True(slot.IsMissingModel);
        Assert.IsType<Sword>(slot.GetActiveInstance());
        Assert.Equal(sword.Guid, slot.ModelGuid);

        slot.ReferenceModel(replacement.Guid);
        Assert.False(slot.IsMissingModel);
    }

    [Fact]
    public void CFG54_PhantomCannotBeEdited()
    {
        var hero = Create<Hero>("H");
        var sword = Create<Sword>("Blade");
        Reference<Hero>(hero, "Weapon", sword);
        Library.DeleteModel(sword.Guid);
        var edit = ModelEdit.ForModel(hero).Slot(Field<Hero>("Weapon"));

        Assert.Throws<InvalidOperationException>(() => edit.SetField(Field<Sword>("Damage"), 5f));
        Assert.Equal(1f, ((Sword)CategoryRegistry.GetDefaultModelEntry(typeof(Sword)).Instance!).Damage);
    }

    [Fact]
    public void CFG61_CopyIsIndependentAndKeepsDefaultsReferenced()
    {
        var hero = Create<Hero>("H");
        var sword = Create<Sword>("Blade");
        Reference<Hero>(hero, "Weapon", sword);

        var copy = Library.CopyExistingModel(hero.Guid)!;

        var copiedHero = Instance<Hero>(copy);
        Assert.NotEqual(hero.Guid, copy.Guid);
        Assert.NotEqual(sword.Guid, copiedHero.Weapon.ModelGuid);
        Assert.True(copiedHero.Gear.Backup.GetActiveModelEntry().IsDefault);

        ((Sword)copiedHero.Weapon.GetActiveInstance()).Damage = 50f;
        Assert.Equal(1f, Instance<Sword>(sword).Damage);
        Assert.Equal(4, Library.GetAllModels().Count);
    }

    [Fact]
    public void CFG65_SpacesInNamesBecomeUnderscores()
    {
        var sword = Create<Sword>("Old");

        Library.RenameModel(sword.Guid, "Big Blade");

        Assert.Equal("Big_Blade", sword.Name);
    }

    [Fact]
    public void CFG68_MissingFieldTakesCodeDefault()
    {
        var hero = Create<Hero>("H");
        Library.Save();
        var path = FilePath(hero.Guid);
        var json = File.ReadAllText(path).Replace("\"Speed\": 5,", "");
        Assert.DoesNotContain("Speed", json);
        File.WriteAllText(path, json);

        Library.Initialize(_directory);

        Assert.Equal(5f, Library.GetModel<Hero>(hero.Guid)!.Speed);
    }

    [Fact]
    public void CFG69_FileOfUnknownTypeIsLeftUntouchedAndSlotBecomesPhantom()
    {
        var hero = Create<Hero>("H");
        var sword = Create<Sword>("Blade");
        Reference<Hero>(hero, "Weapon", sword);
        Library.Save();
        var path = FilePath(sword.Guid);
        var json = File.ReadAllText(path).Replace("Tests.Sword", "Tests.Removed");
        File.WriteAllText(path, json);

        Library.Initialize(_directory);

        Assert.Equal(json, File.ReadAllText(path));
        Assert.True(Library.GetModel<Hero>(hero.Guid)!.Weapon.IsMissingModel);
    }

    [Fact]
    public void CFG90_UsingLibraryBeforeInitializeFails()
    {
        var field = typeof(Library).GetField("_initialized", BindingFlags.NonPublic | BindingFlags.Static)!;
        field.SetValue(null, false);

        Assert.Throws<InvalidOperationException>(() => Library.ContainsModel("x"));

        field.SetValue(null, true);
    }

    [Fact]
    public void CFG93_EditWritesFileOnlyOnSaveAndOnlyForEditedModel()
    {
        var hero = Create<Hero>("H");
        var sword = Create<Sword>("Blade");
        Reference<Hero>(hero, "Weapon", sword);
        Library.Save();
        var heroWrite = File.ReadAllText(FilePath(hero.Guid));
        var edit = ModelEdit.ForModel(hero).Slot(Field<Hero>("Weapon"));

        edit.SetField(Field<Sword>("Damage"), 11f);

        Assert.Equal(11f, Instance<Sword>(sword).Damage);
        Assert.True(Library.HasUnsavedChanges);
        Assert.Contains("\"Damage\": 1", File.ReadAllText(FilePath(sword.Guid)));

        Library.Save();

        Assert.False(Library.HasUnsavedChanges);
        Assert.Contains("\"Damage\": 11", File.ReadAllText(FilePath(sword.Guid)));
        Assert.Equal(heroWrite, File.ReadAllText(FilePath(hero.Guid)));
    }

    [Fact]
    public void CFG95_ChangesStayInMemoryUntilSave()
    {
        var hero = Create<Hero>("H");
        Assert.Empty(Directory.GetFiles(_directory));
        Assert.True(Library.HasUnsavedChanges);

        Library.Save();
        Assert.True(File.Exists(FilePath(hero.Guid)));

        Library.RenameModel(hero.Guid, "Renamed");
        Assert.True(Library.HasUnsavedChanges);
        Assert.DoesNotContain("Renamed", File.ReadAllText(FilePath(hero.Guid)));

        Library.Save();
        Assert.Contains("Renamed", File.ReadAllText(FilePath(hero.Guid)));
    }

    [Fact]
    public void CFG95_DeleteRemovesFileOnSave()
    {
        var sword = Create<Sword>("Blade");
        Library.Save();

        Library.DeleteModel(sword.Guid);

        Assert.True(File.Exists(FilePath(sword.Guid)));
        Assert.True(Library.HasUnsavedChanges);
        Library.Save();
        Assert.False(File.Exists(FilePath(sword.Guid)));
        Assert.False(Library.HasUnsavedChanges);
    }

    [Fact]
    public void CFG95_DeletingUnsavedModelWritesNothing()
    {
        var sword = Create<Sword>("Blade");

        Library.DeleteModel(sword.Guid);
        Library.Save();

        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public void CFG95_RootCreatedOnFirstLaunchIsWrittenAtOnce()
    {
        Library.LoadRoot<TestRoot>();

        Assert.True(File.Exists(FilePath("root")));
        Assert.False(Library.HasUnsavedChanges);
    }

    [Fact]
    public void CFG95_PruningHistoryIsNotAnUnsavedChange()
    {
        var hero = Create<Hero>("Holder");
        var old = Create<Sword>("Old");
        Reference<Hero>(hero, "Weapon", old);
        Library.DeleteModel(old.Guid);
        Library.Save();

        SearchWeapons("had:Holder");

        Assert.False(Library.HasUnsavedChanges);
    }

    [Fact]
    public void SAV06_UninitializedLibraryHasNoUnsavedChanges()
    {
        Create<Sword>("Blade");
        var field = typeof(Library).GetField("_initialized", BindingFlags.NonPublic | BindingFlags.Static)!;
        field.SetValue(null, false);

        Assert.False(Library.HasUnsavedChanges);

        field.SetValue(null, true);
    }

    [Fact]
    public void CFG94_SaveLeavesNoTemporaryFileAndValidJson()
    {
        var sword = Create<Sword>("Blade");

        Library.MarkDirty(sword.Guid);
        Library.Save();

        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
        using var document = JsonDocument.Parse(File.ReadAllText(FilePath(sword.Guid)));
        Assert.Equal("Blade", document.RootElement.GetProperty("Name").GetString());
    }

    private static PickHolder BoundPicks()
    {
        var holder = new PickHolder();
        CategorySystem.Bind(holder);
        return holder;
    }

    [Fact]
    public void CFG25_NarrowerCandidateWins()
    {
        Assert.IsType<NarrowPick>(BoundPicks().Narrow.GetActiveInstance());
    }

    [Fact]
    public void CFG25_MarkedCandidateBeatsNarrowerUnmarkedOne()
    {
        Assert.IsType<MarkedWidePick>(BoundPicks().Marked.GetActiveInstance());
    }

    [Fact]
    public void CFG25_CFG26_TieIsDecidedByNameWithWarning()
    {
        Assert.IsType<TieA>(BoundPicks().Tie.GetActiveInstance());

        var report = CategoryRegistry.CheckDefaults([typeof(PickHolder)]);

        var warning = Assert.Single(report.Warnings);
        Assert.StartsWith("PickHolder.Tie:", warning);
        Assert.Contains("TieA", warning);
        Assert.Contains("TieB", warning);
    }

    [Fact]
    public void CFG26_CFG27_SlotWithoutCandidateIsAnErrorFoundInsideContainers()
    {
        var report = CategoryRegistry.CheckDefaults([typeof(PickHolder)]);

        var error = Assert.Single(report.Errors);
        Assert.StartsWith("PickHolder.Inner.Missing:", error);
        Assert.Throws<InvalidOperationException>(() => CategoryRegistry.ValidateDefaults([typeof(PickHolder)]));
    }

    [Fact]
    public void CFG27_LoadingRootChecksRootType()
    {
        Assert.Throws<InvalidOperationException>(() => Library.LoadRoot<PickHolder>());
        Assert.False(File.Exists(FilePath(Library.RootGuid)));
    }

    [Fact]
    public void CFG27_GameModelTypesAndRootHaveDefaults()
    {
        var types = CategoryRegistry.GetAllModels().Select(m => m.ModelType).Append(typeof(SolsDawn.Gameplay.GameConfig));
        var report = CategoryRegistry.CheckDefaults(types);

        Assert.Empty(report.Errors);
        Assert.Empty(report.Warnings);
    }
}
