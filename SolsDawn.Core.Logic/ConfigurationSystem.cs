using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic;

[AttributeUsage(AttributeTargets.Field)]
public abstract class CategoryAttribute : Attribute
{
    public virtual string GetTagsDescription() => GetType().Name.Replace("CategoryAttribute", "");
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public abstract class ModelAttribute : Attribute
{
    public abstract bool Satisfies(CategoryAttribute slot);

    public virtual int Breadth => 1;

    public virtual string GetTagsDescription() => GetType().Name.Replace("ModelAttribute", "");
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DefaultAttribute : Attribute;

public sealed record DefaultsReport(IReadOnlyList<string> Warnings, IReadOnlyList<string> Errors);

public interface ICategorySlot
{
    string? ModelGuid { get; set; }
    List<string> HistoryGuids { get; }
    Type ModelInterfaceType { get; }
    CategoryAttribute? CategoryAttribute { get; }
    string FieldName { get; }
    bool IsMissingModel { get; }

    void Configure(CategoryAttribute? attribute, string fieldName);
    object GetActiveInstanceObject();
    string GetActiveModelName();
    ModelEntry GetActiveModelEntry();

    ModelEntry CopyModel(ModelSearchResult source);
    void ReferenceModel(string targetGuid);
    void RecordHistory(string? guid);
    void RemoveFromHistory(string guid);
    void PruneDeletedHistory();
}

public class CategorySlot<T> : ICategorySlot where T : class
{
    public string? ModelGuid { get; set; }
    public List<string> HistoryGuids { get; set; } = new();

    private bool _missingLogged;
    private ModelEntry? _defaultEntry;

    [JsonIgnore] public CategoryAttribute? CategoryAttribute { get; private set; }
    [JsonIgnore] public string FieldName { get; private set; } = typeof(T).Name;
    [JsonIgnore] public Type ModelInterfaceType => typeof(T);

    [JsonIgnore]
    public bool IsMissingModel =>
        !string.IsNullOrWhiteSpace(ModelGuid) &&
        Library.GetModel<T>(ModelGuid) == null;

    public void Configure(CategoryAttribute? attribute, string fieldName)
    {
        CategoryAttribute = attribute;
        _defaultEntry = null;
        if (!string.IsNullOrWhiteSpace(fieldName))
            FieldName = fieldName;
    }

    public void RecordHistory(string? guid)
    {
        if (string.IsNullOrWhiteSpace(guid) || Library.IsDefaultGuid(guid))
            return;

        if (!HistoryGuids.Contains(guid, StringComparer.OrdinalIgnoreCase))
        {
            HistoryGuids.Add(guid);
        }
    }

    public void RemoveFromHistory(string guid)
    {
        if (string.IsNullOrWhiteSpace(guid))
            return;

        if (HistoryGuids.RemoveAll(g => string.Equals(g, guid, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            Library.NotifyHistoryOrSlotChanged();
        }
    }

    public void PruneDeletedHistory()
    {
        HistoryGuids.RemoveAll(g => string.IsNullOrWhiteSpace(g) || !Library.ContainsModel(g));
    }

    public T GetActiveInstance()
    {
        var instance = Library.GetModel<T>(ModelGuid);
        if (instance != null)
            return instance;

        if (!string.IsNullOrWhiteSpace(ModelGuid) && !_missingLogged)
        {
            Log.Logic.Warning($"CategorySlot '{FieldName}' missing model '{ModelGuid}'. Using default fallback.");
            _missingLogged = true;
        }

        return (T)GetDefaultEntry().Instance!;
    }

    public object GetActiveInstanceObject() => GetActiveInstance();

    public ModelEntry GetActiveModelEntry()
    {
        var entry = Library.GetModelEntry(ModelGuid);
        return entry?.Instance is T ? entry : GetDefaultEntry();
    }

    public string GetActiveModelName() => GetActiveModelEntry().Name;

    private ModelEntry GetDefaultEntry() =>
        _defaultEntry ??= CategoryRegistry.GetDefaultModelEntry(CategoryRegistry.GetDefaultModelType(typeof(T), CategoryAttribute));

    public ModelEntry CopyModel(ModelSearchResult source)
    {
        var created = Library.CopyExistingModel(source.Guid, CategoryAttribute)
                      ?? Library.CreateCopyFromDefault(source.ModelType, CategoryAttribute);

        ModelGuid = created.Guid;
        RecordHistory(created.Guid);
        _missingLogged = false;
        Library.NotifyHistoryOrSlotChanged();
        return created;
    }

    public void ReferenceModel(string targetGuid)
    {
        var entry = Library.GetModelEntry(targetGuid);
        if (entry == null)
            return;

        ModelGuid = entry.Guid;
        RecordHistory(entry.Guid);
        Library.TouchModel(entry.Guid);
        _missingLogged = false;
        Library.NotifyHistoryOrSlotChanged();
    }
}

public sealed record ModelDescriptor(
    Type ModelType,
    ModelAttribute Attribute,
    bool IsMarkedDefault,
    string Name,
    string TagsDescription,
    ModelEntry DefaultEntry);

public static class CategoryRegistry
{
    private static List<ModelDescriptor>? _models;
    private static readonly Dictionary<Type, ModelEntry> _defaultEntries = new();
    private static readonly Dictionary<Type, FieldInfo[]> _fieldCache = new();

    private static List<ModelDescriptor> EnsureModelsLoaded()
    {
        if (_models != null)
            return _models;

        _models = new List<ModelDescriptor>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
                continue;

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray()!;
            }

            foreach (var type in types)
            {
                if (!type.IsClass || type.IsAbstract)
                    continue;

                var modelAttr = type.GetCustomAttribute<ModelAttribute>(inherit: false);
                if (modelAttr != null)
                {
                    var defaultEntry = CreateDefaultEntry(type, modelAttr);
                    _defaultEntries[type] = defaultEntry;
                    _models.Add(new ModelDescriptor(
                        type,
                        modelAttr,
                        type.GetCustomAttribute<DefaultAttribute>(inherit: false) != null,
                        Library.SanitizeModelName(type.Name),
                        modelAttr.GetTagsDescription(),
                        defaultEntry));
                }
            }
        }

        return _models;
    }

    private static ModelEntry CreateDefaultEntry(Type modelType, ModelAttribute modelAttr)
    {
        var instance = Activator.CreateInstance(modelType)!;
        CategorySystem.Bind(instance);

        return new ModelEntry
        {
            Guid = $"{Library.DefaultGuidPrefix}{modelType.FullName ?? modelType.Name}",
            Name = Library.SanitizeModelName(typeName: modelType.Name),
            ModelType = modelType.FullName ?? modelType.Name,
            CategoryTags = modelAttr.GetTagsDescription(),
            LastUpdatedTicks = 0L,
            IsDefault = true,
            Instance = instance
        };
    }

    public static IReadOnlyList<ModelDescriptor> GetAllModels() => EnsureModelsLoaded();

    public static ModelEntry GetDefaultModelEntry(Type modelType)
    {
        EnsureModelsLoaded();
        if (_defaultEntries.TryGetValue(modelType, out var entry))
            return entry;

        var instance = Activator.CreateInstance(modelType)!;
        CategorySystem.Bind(instance);
        var fallbackEntry = new ModelEntry
        {
            Guid = $"{Library.DefaultGuidPrefix}{modelType.FullName ?? modelType.Name}",
            Name = Library.SanitizeModelName(modelType.Name),
            ModelType = modelType.FullName ?? modelType.Name,
            CategoryTags = modelType.Name,
            LastUpdatedTicks = 0L,
            IsDefault = true,
            Instance = instance
        };
        _defaultEntries[modelType] = fallbackEntry;
        return fallbackEntry;
    }

    public static IReadOnlyList<ModelDescriptor> GetAllowedModels(CategoryAttribute? category, Type modelInterfaceType)
    {
        var all = EnsureModelsLoaded();
        if (category == null)
            return all.Where(m => modelInterfaceType.IsAssignableFrom(m.ModelType)).ToList();

        return all
            .Where(m => modelInterfaceType.IsAssignableFrom(m.ModelType) && m.Attribute.Satisfies(category))
            .ToList();
    }

    public static bool IsModelTypeAllowed(Type modelType, CategoryAttribute? category, Type modelInterfaceType)
    {
        if (!modelInterfaceType.IsAssignableFrom(modelType))
            return false;

        if (category == null)
            return true;

        var modelAttr = modelType.GetCustomAttribute<ModelAttribute>(inherit: false);
        return modelAttr != null && modelAttr.Satisfies(category);
    }

    public static Type? FindModelType(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        var all = EnsureModelsLoaded();
        var match = all.FirstOrDefault(m =>
            string.Equals(m.ModelType.FullName, typeName, StringComparison.Ordinal) ||
            string.Equals(m.ModelType.Name, typeName, StringComparison.Ordinal));

        if (match != null)
            return match.ModelType;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var found = asm.GetType(typeName);
            if (found != null)
                return found;
        }

        return null;
    }

    public static Type GetDefaultModelType(Type modelInterfaceType, CategoryAttribute? category) =>
        ResolveDefault(modelInterfaceType, category, out _)
        ?? throw new InvalidOperationException($"No model type fits {Describe(modelInterfaceType, category)}.");

    public static Type? ResolveDefault(Type modelInterfaceType, CategoryAttribute? category, out IReadOnlyList<Type> tied)
    {
        tied = Array.Empty<Type>();
        var candidates = GetAllowedModels(category, modelInterfaceType);
        if (candidates.Count == 0)
            return null;

        var marked = candidates.Where(m => m.IsMarkedDefault).ToList();
        var pool = marked.Count > 0 ? marked : candidates.ToList();
        var breadth = pool.Min(m => m.Attribute.Breadth);
        var narrowest = pool
            .Where(m => m.Attribute.Breadth == breadth)
            .OrderBy(m => m.ModelType.Name, StringComparer.Ordinal)
            .ThenBy(m => m.ModelType.FullName, StringComparer.Ordinal)
            .Select(m => m.ModelType)
            .ToList();

        if (narrowest.Count > 1)
            tied = narrowest;
        return narrowest[0];
    }

    public static DefaultsReport CheckDefaults(IEnumerable<Type> types)
    {
        var warnings = new List<string>();
        var errors = new List<string>();
        var visited = new HashSet<Type>();

        foreach (var type in types)
            Walk(type, type.Name);

        return new DefaultsReport(warnings, errors);

        void Walk(Type type, string path)
        {
            if (!visited.Add(type))
                return;

            foreach (var field in GetPublicFields(type))
            {
                var fieldType = field.FieldType;
                var fieldPath = $"{path}.{field.Name}";

                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(CategorySlot<>))
                {
                    var modelType = fieldType.GetGenericArguments()[0];
                    var category = field.GetCustomAttribute<CategoryAttribute>();
                    var chosen = ResolveDefault(modelType, category, out var tied);
                    if (chosen == null)
                        errors.Add($"{fieldPath}: no model type fits {Describe(modelType, category)}");
                    else if (tied.Count > 1)
                        warnings.Add($"{fieldPath}: {string.Join(", ", tied.Select(t => t.Name))} tie as defaults, {chosen.Name} is chosen by name");
                }
                else if (fieldType.IsClass && fieldType != typeof(string))
                {
                    Walk(fieldType, fieldPath);
                }
            }
        }
    }

    public static void ValidateDefaults(IEnumerable<Type> types)
    {
        var report = CheckDefaults(types);
        if (report.Warnings.Count > 0)
            Log.Logic.Warning("Default model ties:\n" + string.Join("\n", report.Warnings));
        if (report.Errors.Count > 0)
            throw new InvalidOperationException("Slots without a default model:\n" + string.Join("\n", report.Errors));
    }

    private static string Describe(Type modelInterfaceType, CategoryAttribute? category) =>
        $"{modelInterfaceType.Name} ({category?.GetTagsDescription() ?? "any category"})";

    public static FieldInfo[] GetPublicFields(Type type)
    {
        if (!_fieldCache.TryGetValue(type, out var fields))
        {
            fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            _fieldCache[type] = fields;
        }
        return fields;
    }
}

public static class CategorySystem
{
    public static void Bind(object instance) => GetOwnedCategorySlots(instance);

    public static List<ICategorySlot> GetOwnedCategorySlots(object? modelInstance)
    {
        var slots = new List<ICategorySlot>();
        if (modelInstance == null)
            return slots;

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        CollectSlots(modelInstance, visited, slots);
        return slots;

        static void CollectSlots(object current, HashSet<object> visitedSet, List<ICategorySlot> result)
        {
            var type = current.GetType();
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum || type.IsValueType)
                return;

            if (!visitedSet.Add(current))
                return;

            foreach (var field in CategoryRegistry.GetPublicFields(type))
            {
                if (typeof(ICategorySlot).IsAssignableFrom(field.FieldType))
                {
                    var slot = field.GetValue(current) as ICategorySlot;
                    if (slot == null)
                    {
                        slot = (ICategorySlot)Activator.CreateInstance(field.FieldType)!;
                        field.SetValue(current, slot);
                    }

                    if (slot.CategoryAttribute == null)
                        slot.Configure(field.GetCustomAttribute<CategoryAttribute>(), field.Name);

                    result.Add(slot);
                }
                else if (field.FieldType.IsClass && field.FieldType != typeof(string))
                {
                    var val = field.GetValue(current);
                    if (val != null)
                        CollectSlots(val, visitedSet, result);
                }
            }
        }
    }
}

public sealed class ModelEdit
{
    private readonly ModelEntry? _entry;
    private readonly ModelEdit? _parent;
    private readonly FieldInfo? _field;
    private readonly bool _isSlot;

    private ModelEdit(ModelEntry? entry, ModelEdit? parent, FieldInfo? field, bool isSlot)
    {
        _entry = entry;
        _parent = parent;
        _field = field;
        _isSlot = isSlot;
    }

    public static ModelEdit ForModel(ModelEntry entry) => new(entry, null, null, false);

    public ModelEdit Slot(FieldInfo field) => new(null, this, field, true);

    public ModelEdit Container(FieldInfo field) => new(null, this, field, false);

    public (object Target, ModelEntry Entry) Current() => Walk(materialize: false);

    public (object Target, ModelEntry Entry) Writable() => Walk(materialize: true);

    public void SetField(FieldInfo field, object? value)
    {
        var (target, entry) = Writable();
        field.SetValue(target, value);
        Library.MarkDirty(entry.Guid);
    }

    public ModelEntry? ApplySearchResult(FieldInfo field, ModelSearchResult result, bool copy)
    {
        var (owner, ownerEntry) = Writable();
        var slot = (ICategorySlot)field.GetValue(owner)!;

        ModelEntry? created = null;
        if (copy)
            created = slot.CopyModel(result);
        else
            slot.ReferenceModel(result.Guid);

        Library.SaveModel(ownerEntry.Guid);
        return created;
    }

    private (object Target, ModelEntry Entry) Walk(bool materialize)
    {
        if (_parent == null)
            return (_entry!.Instance!, _entry);

        var (owner, ownerEntry) = _parent.Walk(materialize);
        var value = _field!.GetValue(owner)!;
        if (!_isSlot)
            return (value, ownerEntry);

        var slot = (ICategorySlot)value;
        if (materialize && slot.IsMissingModel)
            throw new InvalidOperationException($"Slot '{slot.FieldName}' holds a phantom model, which is read-only.");

        var active = slot.GetActiveModelEntry();
        if (!materialize || !active.IsDefault)
            return (active.Instance!, active);

        var copy = Library.CreateCopyFromDefault(
            active.Instance!.GetType(),
            slot.CategoryAttribute,
            $"{ownerEntry.Name}_{slot.FieldName}");
        slot.ModelGuid = copy.Guid;
        slot.RecordHistory(copy.Guid);
        Library.SaveModel(ownerEntry.Guid);
        return (copy.Instance!, copy);
    }
}

public sealed class ModelEntry
{
    public string Guid { get; set; } = System.Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New_Model";
    public string ModelType { get; set; } = string.Empty;
    public string CategoryTags { get; set; } = string.Empty;
    public long LastUpdatedTicks { get; set; } = DateTime.UtcNow.Ticks;

    [JsonIgnore]
    public bool IsDefault { get; set; }

    [JsonIgnore]
    public object? Instance { get; set; }

    public JsonElement? RawData { get; set; }
}

public sealed record ModelSearchResult(
    string Guid,
    string ModelName,
    Type ModelType,
    bool IsDefault,
    bool IsOrphaned,
    long LastUpdatedTicks,
    string? ParentGuid = null,
    string? ParentModelName = null,
    int ParentMatchScore = 0,
    int PatternMatchScore = 0)
{
    public string DisplayLabel =>
        ParentModelName != null
            ? $"{ModelName} [{ParentModelName}]"
            : IsDefault
                ? $"default: {ModelName}"
                : ModelName;

    public bool WillCopy(bool shiftHeld) => !IsDefault && (IsOrphaned ? shiftHeld : !shiftHeld);
}

public static class Library
{
    private static readonly Dictionary<string, ModelEntry> _models = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> _dirty = new(StringComparer.OrdinalIgnoreCase);

    private static bool _initialized;
    private static bool _isLoading;

    public static int Version { get; private set; } = 1;

    public const string RootGuid = "root";
    public const string DefaultGuidPrefix = "default:";

    public static string PresetsDirectory { get; private set; } = string.Empty;

    public static Action? OnLibraryChanged { get; set; }

    public static JsonSerializerOptions JsonOptions { get; } = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var opts = new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        opts.Converters.Add(new JsonStringEnumConverter());
        opts.Converters.Add(new ColorJsonConverter());
        opts.Converters.Add(new CategorySlotJsonConverterFactory());
        return opts;
    }

    public static void NotifyHistoryOrSlotChanged()
    {
        Version++;
        OnLibraryChanged?.Invoke();
    }

    public static string SanitizeModelName(string? typeName, string fallback = "Model")
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return fallback.Replace(' ', '_');

        return typeName.Trim().Replace(' ', '_');
    }

    public static bool IsDefaultGuid(string? guid) =>
        guid != null && guid.StartsWith(DefaultGuidPrefix, StringComparison.OrdinalIgnoreCase);

    public static void Initialize(string directory)
    {
        CategoryRegistry.ValidateDefaults(CategoryRegistry.GetAllModels().Select(m => m.ModelType));
        PresetsDirectory = directory;

        _isLoading = true;
        _models.Clear();
        _dirty.Clear();

        try
        {
            if (!Directory.Exists(PresetsDirectory))
            {
                Directory.CreateDirectory(PresetsDirectory);
            }

            foreach (var file in Directory.GetFiles(PresetsDirectory, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var entry = JsonSerializer.Deserialize<ModelEntry>(json, JsonOptions);
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Guid))
                        continue;

                    var modelType = CategoryRegistry.FindModelType(entry.ModelType);
                    if (modelType == null)
                    {
                        Log.Logic.Warning($"Model file '{file}' references unknown ModelType '{entry.ModelType}'. Skipping.");
                        continue;
                    }

                    entry.Name = SanitizeModelName(entry.Name, modelType.Name);
                    if (entry.LastUpdatedTicks <= 0)
                    {
                        entry.LastUpdatedTicks = File.GetLastWriteTimeUtc(file).Ticks;
                    }

                    if (entry.RawData.HasValue)
                    {
                        entry.Instance = entry.RawData.Value.Deserialize(modelType, JsonOptions);
                    }

                    entry.Instance ??= Activator.CreateInstance(modelType)!;
                    CategorySystem.Bind(entry.Instance);

                    if (string.IsNullOrWhiteSpace(entry.CategoryTags))
                    {
                        var modelAttr = modelType.GetCustomAttribute<ModelAttribute>(false);
                        entry.CategoryTags = modelAttr?.GetTagsDescription() ?? modelType.Name;
                    }

                    _models[entry.Guid] = entry;
                }
                catch (Exception ex)
                {
                    Log.Logic.Error($"Failed to load model file '{file}': {ex.Message}");
                }
            }

            _initialized = true;
        }
        finally
        {
            _isLoading = false;
            Version++;
        }

        Log.Logic.Info($"Model Library initialized with {_models.Count} model(s) from '{PresetsDirectory}'.");
    }

    public static void EnsureInitialized()
    {
        if (!_initialized && !_isLoading)
            throw new InvalidOperationException("Library.Initialize must be called before using the Library.");
    }

    public static T LoadRoot<T>() where T : class, new()
    {
        EnsureInitialized();
        CategoryRegistry.ValidateDefaults([typeof(T)]);
        if (_models.TryGetValue(RootGuid, out var existing) && existing.Instance is T loaded)
            return loaded;

        var path = GetModelFilePath(RootGuid);
        if (File.Exists(path))
            throw new InvalidOperationException($"Root model file '{path}' exists but could not be loaded as {typeof(T).Name}.");

        var root = new T();
        CategorySystem.Bind(root);
        var entry = new ModelEntry
        {
            Guid = RootGuid,
            Name = SanitizeModelName(typeof(T).Name),
            ModelType = typeof(T).FullName ?? typeof(T).Name,
            CategoryTags = typeof(T).Name,
            Instance = root
        };

        _models[RootGuid] = entry;
        SaveModelToFile(entry);
        Version++;
        return root;
    }

    public static bool ContainsModel(string? guid)
    {
        EnsureInitialized();
        return !string.IsNullOrWhiteSpace(guid) && _models.ContainsKey(guid);
    }

    public static IReadOnlyCollection<ModelEntry> GetAllModels()
    {
        EnsureInitialized();
        return _models.Values;
    }

    public static T? GetModel<T>(string? guid) where T : class => GetModelEntry(guid)?.Instance as T;

    public static ModelEntry? GetModelEntry(string? guid)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(guid))
            return null;

        if (IsDefaultGuid(guid))
        {
            var defaultType = CategoryRegistry.FindModelType(guid[DefaultGuidPrefix.Length..]);
            return defaultType == null ? null : CategoryRegistry.GetDefaultModelEntry(defaultType);
        }

        return _models.GetValueOrDefault(guid);
    }

    public static ModelEntry CreateCopyFromDefault(Type modelType, CategoryAttribute? slotCategory = null, string? customName = null)
    {
        EnsureInitialized();
        var instance = Activator.CreateInstance(modelType)!;
        CategorySystem.Bind(instance);

        var modelAttr = modelType.GetCustomAttribute<ModelAttribute>(false);
        var tags = modelAttr?.GetTagsDescription() ?? slotCategory?.GetTagsDescription() ?? modelType.Name;

        var entry = new ModelEntry
        {
            Guid = Guid.NewGuid().ToString("N"),
            Name = SanitizeModelName(customName ?? modelType.Name, modelType.Name),
            ModelType = modelType.FullName ?? modelType.Name,
            CategoryTags = tags,
            LastUpdatedTicks = DateTime.UtcNow.Ticks,
            IsDefault = false,
            Instance = instance
        };

        _models[entry.Guid] = entry;
        SaveModelToFile(entry);
        Version++;
        OnLibraryChanged?.Invoke();
        return entry;
    }

    public static ModelEntry? CopyExistingModel(string sourceGuid, CategoryAttribute? slotCategory = null)
    {
        EnsureInitialized();
        if (!_models.TryGetValue(sourceGuid, out var sourceEntry) || sourceEntry.Instance == null)
            return null;

        var runtimeType = sourceEntry.Instance.GetType();
        var clonedInstance = DeepCloneObject(sourceEntry.Instance, runtimeType);

        foreach (var childSlot in CategorySystem.GetOwnedCategorySlots(clonedInstance))
        {
            if (!ContainsModel(childSlot.ModelGuid))
                continue;

            var childCopy = CopyExistingModel(childSlot.ModelGuid!, childSlot.CategoryAttribute);
            if (childCopy != null)
            {
                childSlot.ModelGuid = childCopy.Guid;
                childSlot.RecordHistory(childCopy.Guid);
            }
        }

        var modelAttr = runtimeType.GetCustomAttribute<ModelAttribute>(false);
        var tags = modelAttr?.GetTagsDescription() ?? slotCategory?.GetTagsDescription() ?? sourceEntry.CategoryTags;

        var newEntry = new ModelEntry
        {
            Guid = Guid.NewGuid().ToString("N"),
            Name = SanitizeModelName(sourceEntry.Name, runtimeType.Name),
            ModelType = runtimeType.FullName ?? runtimeType.Name,
            CategoryTags = tags,
            LastUpdatedTicks = DateTime.UtcNow.Ticks,
            IsDefault = false,
            Instance = clonedInstance
        };

        _models[newEntry.Guid] = newEntry;
        SaveModelToFile(newEntry);
        Version++;
        OnLibraryChanged?.Invoke();
        return newEntry;
    }

    public static ModelEntry RegisterMigratedInlineModel(object inlineInstance, CategoryAttribute? slotCategory = null)
    {
        EnsureInitialized();
        CategorySystem.Bind(inlineInstance);
        var runtimeType = inlineInstance.GetType();
        var modelAttr = runtimeType.GetCustomAttribute<ModelAttribute>(false);
        var tags = modelAttr?.GetTagsDescription() ?? slotCategory?.GetTagsDescription() ?? runtimeType.Name;

        var entry = new ModelEntry
        {
            Guid = Guid.NewGuid().ToString("N"),
            Name = SanitizeModelName(runtimeType.Name),
            ModelType = runtimeType.FullName ?? runtimeType.Name,
            CategoryTags = tags,
            LastUpdatedTicks = DateTime.UtcNow.Ticks,
            IsDefault = false,
            Instance = inlineInstance
        };

        _models[entry.Guid] = entry;
        SaveModelToFile(entry);
        Version++;
        return entry;
    }

    public static void TouchModel(string guid)
    {
        EnsureInitialized();
        if (_models.TryGetValue(guid, out var entry))
        {
            entry.LastUpdatedTicks = DateTime.UtcNow.Ticks;
            SaveModelToFile(entry);
            Version++;
        }
    }

    public static void SaveModel(string? guid)
    {
        EnsureInitialized();
        if (!string.IsNullOrWhiteSpace(guid) && _models.TryGetValue(guid, out var entry))
        {
            entry.LastUpdatedTicks = DateTime.UtcNow.Ticks;
            SaveModelToFile(entry);
            Version++;
        }
    }

    public static void MarkDirty(string guid)
    {
        EnsureInitialized();
        if (_models.TryGetValue(guid, out var entry))
        {
            entry.LastUpdatedTicks = DateTime.UtcNow.Ticks;
            _dirty.Add(guid);
            Version++;
        }
    }

    public static void FlushDirty()
    {
        foreach (var guid in _dirty)
        {
            if (_models.TryGetValue(guid, out var entry))
                SaveModelToFile(entry);
        }

        _dirty.Clear();
    }

    public static void RenameModel(string? guid, string newName)
    {
        EnsureInitialized();
        if (!string.IsNullOrWhiteSpace(guid) && _models.TryGetValue(guid, out var entry))
        {
            var sanitized = SanitizeModelName(newName, entry.Name);
            if (!string.IsNullOrWhiteSpace(sanitized))
            {
                entry.Name = sanitized;
                MarkDirty(guid);
                OnLibraryChanged?.Invoke();
            }
        }
    }

    public static void DeleteModel(string guid)
    {
        EnsureInitialized();
        if (!string.Equals(guid, RootGuid, StringComparison.OrdinalIgnoreCase) && _models.Remove(guid))
        {
            var path = GetModelFilePath(guid);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            Version++;
            OnLibraryChanged?.Invoke();
        }
    }

    public static T DeepClone<T>(T source) where T : class
    {
        return (T)DeepCloneObject(source, source.GetType());
    }

    public static object DeepCloneObject(object source, Type runtimeType)
    {
        var json = JsonSerializer.Serialize(source, runtimeType, JsonOptions);
        return JsonSerializer.Deserialize(json, runtimeType, JsonOptions)
               ?? Activator.CreateInstance(runtimeType)!;
    }

    private static void SaveModelToFile(ModelEntry entry)
    {
        if (entry.IsDefault)
            return;

        if (!Directory.Exists(PresetsDirectory))
            Directory.CreateDirectory(PresetsDirectory);

        if (entry.Instance != null)
        {
            var element = JsonSerializer.SerializeToElement(entry.Instance, entry.Instance.GetType(), JsonOptions);
            entry.RawData = element;
        }

        var json = JsonSerializer.Serialize(entry, JsonOptions);
        var path = GetModelFilePath(entry.Guid);
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, path, overwrite: true);
    }

    private static string GetModelFilePath(string guid) => Path.Combine(PresetsDirectory, $"{guid}.json");

    private static HashSet<string> CollectOwnedModelGuids()
    {
        var ownedGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in _models.Values)
        {
            if (entry.Instance == null)
                continue;

            var childSlots = CategorySystem.GetOwnedCategorySlots(entry.Instance);
            foreach (var slot in childSlots)
            {
                if (!string.IsNullOrWhiteSpace(slot.ModelGuid))
                {
                    ownedGuids.Add(slot.ModelGuid);
                }
            }
        }

        return ownedGuids;
    }

    public static IReadOnlyList<ModelSearchResult> SearchModels(
        CategoryAttribute? category,
        Type modelInterfaceType,
        string? rawQuery,
        string? ownerGuid = null)
    {
        EnsureInitialized();
        ParseSearchQuery(rawQuery, out var hasFilters, out var hadFilters, out var onlyDefaults, out var pattern);

        var ownedGuids = CollectOwnedModelGuids();
        var results = new List<ModelSearchResult>();

        bool hasParentFilter = hasFilters.Count > 0 || hadFilters.Count > 0;

        if (!hasParentFilter)
        {
            var allowedDescriptors = CategoryRegistry.GetAllowedModels(category, modelInterfaceType);
            foreach (var desc in allowedDescriptors)
            {
                var defEntry = desc.DefaultEntry;
                if (!TryScoreMatch(defEntry.Name, pattern, out int patternScore))
                    continue;

                results.Add(new ModelSearchResult(
                    Guid: defEntry.Guid,
                    ModelName: defEntry.Name,
                    ModelType: desc.ModelType,
                    IsDefault: true,
                    IsOrphaned: false,
                    LastUpdatedTicks: 0L,
                    ParentGuid: null,
                    ParentModelName: null,
                    ParentMatchScore: 0,
                    PatternMatchScore: patternScore));
            }
        }

        if (onlyDefaults)
        {
            return SortSearchResults(results);
        }

        if (!hasParentFilter)
        {
            foreach (var entry in _models.Values)
            {
                if (entry.Instance == null)
                    continue;

                if (IsHiddenFromSearch(entry, ownerGuid))
                    continue;

                var instanceType = entry.Instance.GetType();
                if (!CategoryRegistry.IsModelTypeAllowed(instanceType, category, modelInterfaceType))
                    continue;

                if (!TryScoreMatch(entry.Name, pattern, out int patternScore))
                    continue;

                bool isOrphaned = !ownedGuids.Contains(entry.Guid);

                results.Add(new ModelSearchResult(
                    Guid: entry.Guid,
                    ModelName: entry.Name,
                    ModelType: instanceType,
                    IsDefault: false,
                    IsOrphaned: isOrphaned,
                    LastUpdatedTicks: entry.LastUpdatedTicks,
                    ParentGuid: null,
                    ParentModelName: null,
                    ParentMatchScore: 0,
                    PatternMatchScore: patternScore));
            }

            return SortSearchResults(results);
        }

        string primaryParentFilter = hasFilters.Count > 0 ? hasFilters[0] : hadFilters[0];
        bool primaryIsHad = hasFilters.Count == 0;

        foreach (var parentEntry in _models.Values)
        {
            if (parentEntry.Instance == null)
                continue;

            if (!TryScoreMatch(parentEntry.Name, primaryParentFilter, out int parentScore))
                continue;

            var descendants = primaryIsHad
                ? GetTransitiveDescendants(parentEntry, includeHistory: true)
                : GetTransitiveDescendants(parentEntry, includeHistory: false);

            foreach (var childEntry in descendants)
            {
                if (childEntry.Instance == null)
                    continue;

                if (IsHiddenFromSearch(childEntry, ownerGuid))
                    continue;

                var childType = childEntry.Instance.GetType();
                if (!CategoryRegistry.IsModelTypeAllowed(childType, category, modelInterfaceType))
                    continue;

                if (!TryScoreMatch(childEntry.Name, pattern, out int patternScore))
                    continue;

                if (!SatisfiesAdditionalParentFilters(childEntry.Guid, hasFilters, hadFilters))
                    continue;

                bool isOrphaned = !ownedGuids.Contains(childEntry.Guid);

                results.Add(new ModelSearchResult(
                    Guid: childEntry.Guid,
                    ModelName: childEntry.Name,
                    ModelType: childType,
                    IsDefault: false,
                    IsOrphaned: isOrphaned,
                    LastUpdatedTicks: childEntry.LastUpdatedTicks,
                    ParentGuid: parentEntry.Guid,
                    ParentModelName: parentEntry.Name,
                    ParentMatchScore: parentScore,
                    PatternMatchScore: patternScore));
            }
        }

        return SortSearchResults(results
            .GroupBy(r => r.Guid, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(r => r.ParentMatchScore).First())
            .ToList());
    }

    private static bool IsHiddenFromSearch(ModelEntry entry, string? ownerGuid) =>
        string.Equals(entry.Guid, RootGuid, StringComparison.OrdinalIgnoreCase) ||
        (ownerGuid != null &&
         (string.Equals(entry.Guid, ownerGuid, StringComparison.OrdinalIgnoreCase) ||
          GetTransitiveDescendants(entry, includeHistory: false)
              .Any(d => string.Equals(d.Guid, ownerGuid, StringComparison.OrdinalIgnoreCase))));

    private static bool SatisfiesAdditionalParentFilters(
        string candidateGuid,
        List<string> hasFilters,
        List<string> hadFilters)
    {
        for (int i = 0; i < hasFilters.Count; i++)
        {
            string filter = hasFilters[i];
            bool matchedAnyParent = _models.Values.Any(p =>
                p.Instance != null &&
                TryScoreMatch(p.Name, filter, out _) &&
                GetTransitiveDescendants(p, includeHistory: false).Any(d =>
                    string.Equals(d.Guid, candidateGuid, StringComparison.OrdinalIgnoreCase)));

            if (!matchedAnyParent)
                return false;
        }

        for (int i = 0; i < hadFilters.Count; i++)
        {
            string filter = hadFilters[i];
            bool matchedAnyParent = _models.Values.Any(p =>
                p.Instance != null &&
                TryScoreMatch(p.Name, filter, out _) &&
                GetTransitiveDescendants(p, includeHistory: true).Any(d =>
                    string.Equals(d.Guid, candidateGuid, StringComparison.OrdinalIgnoreCase)));

            if (!matchedAnyParent)
                return false;
        }

        return true;
    }

    private static List<ModelEntry> GetTransitiveDescendants(ModelEntry rootParent, bool includeHistory)
    {
        var result = new List<ModelEntry>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rootParent.Guid };
        var queue = new Queue<ModelEntry>();
        queue.Enqueue(rootParent);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var childGuids = GetDirectChildGuids(current, includeHistory);

            foreach (var childGuid in childGuids)
            {
                if (!visited.Add(childGuid))
                    continue;

                var childEntry = GetModelEntry(childGuid);
                if (childEntry != null)
                {
                    result.Add(childEntry);
                    queue.Enqueue(childEntry);
                }
            }
        }

        return result;
    }

    private static List<string> GetDirectChildGuids(ModelEntry parent, bool includeHistory)
    {
        var guids = new List<string>();
        if (parent.Instance == null)
            return guids;

        var slots = CategorySystem.GetOwnedCategorySlots(parent.Instance);
        bool anyPruned = false;

        foreach (var slot in slots)
        {
            if (includeHistory)
            {
                int countBefore = slot.HistoryGuids.Count;
                slot.PruneDeletedHistory();
                if (slot.HistoryGuids.Count != countBefore)
                {
                    anyPruned = true;
                }

                foreach (var histGuid in slot.HistoryGuids)
                {
                    if (!guids.Contains(histGuid, StringComparer.OrdinalIgnoreCase))
                        guids.Add(histGuid);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(slot.ModelGuid) && ContainsModel(slot.ModelGuid))
                {
                    if (!guids.Contains(slot.ModelGuid, StringComparer.OrdinalIgnoreCase))
                        guids.Add(slot.ModelGuid);
                }
            }
        }

        if (anyPruned)
        {
            SaveModelToFile(parent);
        }

        return guids;
    }

    private static void ParseSearchQuery(
        string? rawQuery,
        out List<string> hasFilters,
        out List<string> hadFilters,
        out bool onlyDefaults,
        out string pattern)
    {
        hasFilters = new List<string>();
        hadFilters = new List<string>();
        onlyDefaults = false;

        if (string.IsNullOrWhiteSpace(rawQuery))
        {
            pattern = string.Empty;
            return;
        }

        var patternParts = new List<string>();
        var tokens = rawQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            if (token.StartsWith("has:", StringComparison.OrdinalIgnoreCase))
            {
                hasFilters.Add(token[4..]);
            }
            else if (token.StartsWith("had:", StringComparison.OrdinalIgnoreCase))
            {
                hadFilters.Add(token[4..]);
            }
            else if (token.StartsWith("default:", StringComparison.OrdinalIgnoreCase))
            {
                onlyDefaults = true;
                var remainder = token[8..];
                if (!string.IsNullOrWhiteSpace(remainder))
                    patternParts.Add(remainder);
            }
            else
            {
                patternParts.Add(token);
            }
        }

        pattern = string.Join("_", patternParts);
    }

    private static bool TryScoreMatch(string targetName, string queryPattern, out int score)
    {
        if (string.IsNullOrEmpty(queryPattern))
        {
            score = 1000;
            return true;
        }

        int index = targetName.IndexOf(queryPattern, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            score = 0;
            return false;
        }

        score = 10000;

        if (string.Equals(targetName, queryPattern, StringComparison.Ordinal))
        {
            score += 50000;
        }
        else if (string.Equals(targetName, queryPattern, StringComparison.OrdinalIgnoreCase))
        {
            score += 40000;
        }
        else if (index == 0)
        {
            score += 25000;
        }
        else if (targetName[index - 1] == '_' ||
                 (char.IsUpper(targetName[index]) && char.IsLower(targetName[index - 1])))
        {
            score += 12000;
        }

        score -= index * 50;

        int lengthDiff = Math.Max(0, targetName.Length - queryPattern.Length);
        score -= lengthDiff * 15;

        return true;
    }

    private static IReadOnlyList<ModelSearchResult> SortSearchResults(List<ModelSearchResult> results)
    {
        return results
            .OrderBy(r => r.IsDefault ? 1 : 0)
            .ThenByDescending(r => r.ParentMatchScore)
            .ThenByDescending(r => r.PatternMatchScore)
            .ThenByDescending(r => r.LastUpdatedTicks)
            .ThenBy(r => r.ModelName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

public sealed class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            return Color.White;

        byte r = 255, g = 255, b = 255, a = 255;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var prop = reader.GetString();
            reader.Read();
            switch (prop)
            {
                case "R": r = reader.GetByte(); break;
                case "G": g = reader.GetByte(); break;
                case "B": b = reader.GetByte(); break;
                case "A": a = reader.GetByte(); break;
            }
        }
        return new Color(r, g, b, a);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("R", value.R);
        writer.WriteNumber("G", value.G);
        writer.WriteNumber("B", value.B);
        writer.WriteNumber("A", value.A);
        writer.WriteEndObject();
    }
}

public sealed class CategorySlotJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(CategorySlot<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var modelType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(CategorySlotJsonConverter<>).MakeGenericType(modelType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class CategorySlotJsonConverter<T> : JsonConverter<CategorySlot<T>> where T : class
    {
        public override CategorySlot<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var slot = new CategorySlot<T>();
            if (reader.TokenType != JsonTokenType.StartObject)
                return slot;

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (root.TryGetProperty("ModelGuid", out var modelGuidProp))
            {
                slot.ModelGuid = modelGuidProp.GetString();
            }
            else if (root.TryGetProperty("PresetGuid", out var legacyGuidProp))
            {
                slot.ModelGuid = legacyGuidProp.GetString();
            }

            if (root.TryGetProperty("HistoryGuids", out var historyProp) && historyProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in historyProp.EnumerateArray())
                {
                    var guid = item.GetString();
                    slot.RecordHistory(guid);
                }
            }

            if (string.IsNullOrWhiteSpace(slot.ModelGuid) &&
                root.TryGetProperty("ModelType", out var mtProp) &&
                root.TryGetProperty("InlineData", out var inlineElement) &&
                inlineElement.ValueKind == JsonValueKind.Object)
            {
                var concreteType = CategoryRegistry.FindModelType(mtProp.GetString());
                if (concreteType != null && typeof(T).IsAssignableFrom(concreteType))
                {
                    var instance = inlineElement.Deserialize(concreteType, options) ?? Activator.CreateInstance(concreteType)!;
                    var migrated = Library.RegisterMigratedInlineModel(instance);
                    slot.ModelGuid = migrated.Guid;
                }
            }

            slot.RecordHistory(slot.ModelGuid);
            return slot;
        }

        public override void Write(Utf8JsonWriter writer, CategorySlot<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("ModelGuid", value.ModelGuid);

            writer.WritePropertyName("HistoryGuids");
            writer.WriteStartArray();
            foreach (var guid in value.HistoryGuids)
            {
                if (!string.IsNullOrWhiteSpace(guid))
                    writer.WriteStringValue(guid);
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}