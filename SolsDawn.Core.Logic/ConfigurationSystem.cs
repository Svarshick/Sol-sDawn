using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic;

// --- 1. GENERIC ATTRIBUTE INFRASTRUCTURE ---

[AttributeUsage(AttributeTargets.Field)]
public abstract class CategoryAttribute(Type defaultFallbackType) : Attribute
{
    public Type DefaultFallbackType { get; } = defaultFallbackType;

    public virtual string GetTagsDescription() => GetType().Name.Replace("CategoryAttribute", "");
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public abstract class ModelAttribute : Attribute
{
    public abstract bool Satisfies(CategoryAttribute slot);

    public virtual string GetTagsDescription() => GetType().Name.Replace("ModelAttribute", "");
}

// --- 2. CATEGORY SLOT ---

public interface ICategorySlot
{
    string? ModelGuid { get; set; }
    List<string> HistoryGuids { get; }
    Type ModelInterfaceType { get; }
    Type? FallbackType { get; }
    CategoryAttribute? CategoryAttribute { get; }
    string FieldName { get; }
    string? RootSlotKey { get; }
    bool IsMissingModel { get; }

    void ConfigureRoot(CategoryAttribute? attribute, string fieldName, string rootSlotKey);
    bool ConfigureChild(CategoryAttribute? attribute, string fieldName);
    void ConfigureAsDefaultTemplate(CategoryAttribute? attribute, string fieldName);
    object GetActiveInstanceObject();
    string GetActiveModelName();
    ModelEntry? GetActiveModelEntry();

    ModelEntry CopyModel(ModelSearchResult source);
    ModelEntry CopyModelByGuidOrDefault(string? sourceGuid, Type? fallbackModelType = null);
    void ReferenceModel(string targetGuid);
    void PruneDeletedHistory();
}

public class CategorySlot<T> : ICategorySlot where T : class
{
    public string? ModelGuid { get; set; }
    public List<string> HistoryGuids { get; set; } = new();

    private bool _isDefaultTemplateSlot;
    private bool _isChildSlot;
    private bool _missingLogged;

    [JsonIgnore] public CategoryAttribute? CategoryAttribute { get; private set; }
    [JsonIgnore] public string FieldName { get; private set; } = typeof(T).Name;
    [JsonIgnore] public string? RootSlotKey { get; private set; }
    [JsonIgnore] public Type ModelInterfaceType => typeof(T);
    [JsonIgnore] public Type? FallbackType => CategoryAttribute?.DefaultFallbackType;

    [JsonIgnore]
    public bool IsMissingModel =>
        !_isDefaultTemplateSlot &&
        !string.IsNullOrWhiteSpace(ModelGuid) &&
        Library.GetModel<T>(ModelGuid) == null;

    public static implicit operator T(CategorySlot<T> slot) => slot.GetActiveInstance();

    internal void MarkAsChildSlot() => _isChildSlot = true;

    public void ConfigureRoot(CategoryAttribute? attribute, string fieldName, string rootSlotKey)
    {
        _isChildSlot = false;
        if (attribute != null)
            CategoryAttribute = attribute;
        if (!string.IsNullOrWhiteSpace(fieldName))
            FieldName = fieldName;
        if (!string.IsNullOrWhiteSpace(rootSlotKey))
            RootSlotKey = rootSlotKey;

        if (_isDefaultTemplateSlot)
            return;

        EnsureResolved();
    }

    // Configures a slot that belongs to a Model (or nested container inside a Model).
    // Returns true if the slot had to initialize or repair its ModelGuid.
    public bool ConfigureChild(CategoryAttribute? attribute, string fieldName)
    {
        _isChildSlot = true;
        RootSlotKey = null;

        if (attribute != null)
            CategoryAttribute = attribute;
        if (!string.IsNullOrWhiteSpace(fieldName))
            FieldName = fieldName;

        if (_isDefaultTemplateSlot)
            return false;

        PruneDeletedHistory();

        // 1. If uninitialized, create a copy of this slot's specific DefaultFallbackType
        if (string.IsNullOrWhiteSpace(ModelGuid))
        {
            var defaultType = CategoryRegistry.GetDefaultModelType(typeof(T), CategoryAttribute);
            var created = Library.CreateCopyFromDefault(defaultType, CategoryAttribute);
            ModelGuid = created.Guid;
            RecordHistory(created.Guid);
            return true;
        }

        // 2. If the model exists in Library, verify it actually satisfies this slot's CategoryAttribute
        var existingInstance = Library.GetModel<T>(ModelGuid);
        if (existingInstance != null &&
            !CategoryRegistry.IsModelTypeAllowed(existingInstance.GetType(), CategoryAttribute, typeof(T)))
        {
            var defaultType = CategoryRegistry.GetDefaultModelType(typeof(T), CategoryAttribute);
            var created = Library.CreateCopyFromDefault(defaultType, CategoryAttribute);
            HistoryGuids.Remove(ModelGuid);
            ModelGuid = created.Guid;
            RecordHistory(created.Guid);
            return true;
        }

        return false;
    }

    public void ConfigureAsDefaultTemplate(CategoryAttribute? attribute, string fieldName)
    {
        _isDefaultTemplateSlot = true;
        _isChildSlot = true;
        CategoryAttribute = attribute;
        if (!string.IsNullOrWhiteSpace(fieldName))
            FieldName = fieldName;
    }

    private void EnsureResolved()
    {
        if (_isDefaultTemplateSlot)
            return;

        // Only non-child slots (such as PlayerBoard.Specs) use RootSlotKey and _RootSlots.json
        if (!_isChildSlot)
        {
            if (string.IsNullOrWhiteSpace(RootSlotKey))
            {
                RootSlotKey = typeof(T).FullName ?? typeof(T).Name;
            }

            if (string.IsNullOrWhiteSpace(ModelGuid) &&
                Library.TryRestoreRootSlot(RootSlotKey, out var savedGuid, out var savedHistory))
            {
                ModelGuid = savedGuid;
                foreach (var h in savedHistory)
                    RecordHistory(h);
            }
        }

        if (string.IsNullOrWhiteSpace(ModelGuid))
        {
            var defaultType = CategoryRegistry.GetDefaultModelType(typeof(T), CategoryAttribute);
            var created = Library.CreateCopyFromDefault(defaultType, CategoryAttribute);
            ModelGuid = created.Guid;
            RecordHistory(created.Guid);
            PersistRootSlotIfNeeded();
        }
    }

    private void PersistRootSlotIfNeeded()
    {
        if (!_isChildSlot && !string.IsNullOrWhiteSpace(RootSlotKey))
        {
            Library.SaveRootSlot(RootSlotKey, ModelGuid, HistoryGuids);
        }
    }

    public void RecordHistory(string? guid)
    {
        if (string.IsNullOrWhiteSpace(guid))
            return;

        if (!HistoryGuids.Contains(guid, StringComparer.OrdinalIgnoreCase))
        {
            HistoryGuids.Add(guid);
        }
    }

    public void PruneDeletedHistory()
    {
        int before = HistoryGuids.Count;
        HistoryGuids.RemoveAll(g => string.IsNullOrWhiteSpace(g) || !Library.ContainsModel(g));
        if (!string.IsNullOrWhiteSpace(ModelGuid) && Library.ContainsModel(ModelGuid))
        {
            RecordHistory(ModelGuid);
        }

        if (HistoryGuids.Count != before)
        {
            PersistRootSlotIfNeeded();
        }
    }

    public T GetActiveInstance()
    {
        if (_isDefaultTemplateSlot)
            return GetDefaultFallbackInstance();

        EnsureResolved();

        if (!string.IsNullOrWhiteSpace(ModelGuid))
        {
            var instance = Library.GetModel<T>(ModelGuid);
            if (instance != null)
                return instance;

            if (!_missingLogged)
            {
                Log.Logic.Warning($"CategorySlot '{FieldName}' missing model '{ModelGuid}'. Using default fallback.");
                _missingLogged = true;
            }
        }

        return GetDefaultFallbackInstance();
    }

    public object GetActiveInstanceObject() => GetActiveInstance();

    public ModelEntry? GetActiveModelEntry()
    {
        EnsureResolved();
        if (!string.IsNullOrWhiteSpace(ModelGuid))
        {
            var entry = Library.GetModelEntry(ModelGuid);
            if (entry != null)
                return entry;
        }

        var defaultType = CategoryRegistry.GetDefaultModelType(typeof(T), CategoryAttribute);
        return CategoryRegistry.GetDefaultModelEntry(defaultType);
    }

    public string GetActiveModelName() => GetActiveModelEntry()?.Name ?? GetActiveInstance().GetType().Name;

    private T GetDefaultFallbackInstance()
    {
        var defaultType = CategoryRegistry.GetDefaultModelType(typeof(T), CategoryAttribute);
        return (T)CategoryRegistry.GetDefaultModelEntry(defaultType).Instance!;
    }

    public ModelEntry CopyModel(ModelSearchResult source)
    {
        ModelEntry created = source.IsDefault
            ? Library.CreateCopyFromDefault(source.ModelType, CategoryAttribute)
            : Library.CopyExistingModel(source.Guid, CategoryAttribute)
              ?? Library.CreateCopyFromDefault(source.ModelType, CategoryAttribute);

        ModelGuid = created.Guid;
        RecordHistory(created.Guid);
        _missingLogged = false;
        PersistRootSlotIfNeeded();
        return created;
    }

    public ModelEntry CopyModelByGuidOrDefault(string? sourceGuid, Type? fallbackModelType = null)
    {
        ModelEntry? created = !string.IsNullOrWhiteSpace(sourceGuid)
            ? Library.CopyExistingModel(sourceGuid, CategoryAttribute)
            : null;

        if (created == null)
        {
            var targetType = fallbackModelType ?? CategoryRegistry.GetDefaultModelType(typeof(T), CategoryAttribute);
            created = Library.CreateCopyFromDefault(targetType, CategoryAttribute);
        }

        ModelGuid = created.Guid;
        RecordHistory(created.Guid);
        _missingLogged = false;
        PersistRootSlotIfNeeded();
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
        PersistRootSlotIfNeeded();
    }
}

// --- 3. STATIC METADATA INITIALIZATION (CATEGORY REGISTRY) ---

public sealed record ModelDescriptor(
    Type ModelType,
    ModelAttribute Attribute,
    string Name,
    string TagsDescription,
    ModelEntry DefaultEntry);

public static class CategoryRegistry
{
    private static readonly List<ModelDescriptor> _models = new();
    private static readonly Dictionary<Type, ModelEntry> _defaultEntries = new();
    private static readonly Dictionary<Type, FieldInfo[]> _fieldCache = new();

    static CategoryRegistry()
    {
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
                        SanitizeName(type.Name),
                        modelAttr.GetTagsDescription(),
                        defaultEntry));
                }
            }
        }
    }

    public static string SanitizeName(string? rawName, string fallback = "Model")
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return fallback.Replace(' ', '_');

        return rawName.Trim().Replace(' ', '_');
    }

    private static ModelEntry CreateDefaultEntry(Type modelType, ModelAttribute modelAttr)
    {
        var instance = Activator.CreateInstance(modelType)!;
        CategorySystem.BindDefaultTemplateRecursive(instance);

        return new ModelEntry
        {
            Guid = $"default:{modelType.FullName ?? modelType.Name}",
            Name = SanitizeName(modelType.Name),
            ModelType = modelType.FullName ?? modelType.Name,
            CategoryTags = modelAttr.GetTagsDescription(),
            LastUpdatedTicks = 0L,
            IsDefault = true,
            Instance = instance
        };
    }

    public static IReadOnlyList<ModelDescriptor> GetAllModels() => _models;

    public static ModelEntry GetDefaultModelEntry(Type modelType)
    {
        if (_defaultEntries.TryGetValue(modelType, out var entry))
            return entry;

        throw Log.Logic.FatalException($"Type '{modelType.Name}' is not registered with a [ModelAttribute].");
    }

    public static IReadOnlyList<ModelDescriptor> GetAllowedModels(CategoryAttribute? category, Type modelInterfaceType)
    {
        if (category == null)
            return _models.Where(m => modelInterfaceType.IsAssignableFrom(m.ModelType)).ToList();

        return _models
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

        var match = _models.FirstOrDefault(m =>
            string.Equals(m.ModelType.FullName, typeName, StringComparison.Ordinal) ||
            string.Equals(m.ModelType.Name, typeName, StringComparison.Ordinal));

        return match?.ModelType;
    }

    public static Type GetDefaultModelType(Type modelInterfaceType, CategoryAttribute? category)
    {
        if (category?.DefaultFallbackType != null && modelInterfaceType.IsAssignableFrom(category.DefaultFallbackType))
            return category.DefaultFallbackType;

        var allowed = GetAllowedModels(category, modelInterfaceType);
        if (allowed.Count > 0)
            return allowed[0].ModelType;

        var anyAssignable = _models.FirstOrDefault(m => modelInterfaceType.IsAssignableFrom(m.ModelType));
        if (anyAssignable != null)
            return anyAssignable.ModelType;

        throw Log.Logic.FatalException($"No valid Model Type found for '{modelInterfaceType.Name}'.");
    }

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

// --- 4. INTERNAL SLOT BINDING ---

public static class CategorySystem
{
    public static void BindSlots(object owner, bool isRootOwner = false)
    {
        if (owner == null)
            return;

        var ownerType = owner.GetType();
        var fields = CategoryRegistry.GetPublicFields(ownerType);
        foreach (var field in fields)
        {
            if (!typeof(ICategorySlot).IsAssignableFrom(field.FieldType))
                continue;

            var slot = field.GetValue(owner) as ICategorySlot;
            if (slot == null)
            {
                slot = (ICategorySlot)Activator.CreateInstance(field.FieldType)!;
                field.SetValue(owner, slot);
            }

            var attr = field.GetCustomAttribute<CategoryAttribute>();
            if (isRootOwner)
            {
                string rootKey = slot.ModelInterfaceType.FullName ?? slot.ModelInterfaceType.Name;
                slot.ConfigureRoot(attr, field.Name, rootKey);
            }
            else
            {
                slot.ConfigureChild(attr, field.Name);
            }
        }
    }

    public static void BindDefaultTemplateRecursive(object? root)
    {
        if (root == null)
            return;

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        VisitDefault(root, visited);

        static void VisitDefault(object current, HashSet<object> visitedSet)
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
                    var attr = field.GetCustomAttribute<CategoryAttribute>();
                    slot.ConfigureAsDefaultTemplate(attr, field.Name);
                }
                else if (field.FieldType.IsClass && field.FieldType != typeof(string))
                {
                    var nested = field.GetValue(current);
                    if (nested != null)
                        VisitDefault(nested, visitedSet);
                }
            }
        }
    }

    internal static bool BindModelInstanceSlots(object? modelInstance)
    {
        bool anyModified = false;
        GetOwnedCategorySlots(modelInstance, ref anyModified);
        return anyModified;
    }

    public static List<ICategorySlot> GetOwnedCategorySlots(object? modelInstance)
    {
        bool dummy = false;
        return GetOwnedCategorySlots(modelInstance, ref dummy);
    }

    private static List<ICategorySlot> GetOwnedCategorySlots(object? modelInstance, ref bool anyModified)
    {
        var slots = new List<ICategorySlot>();
        if (modelInstance == null)
            return slots;

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        CollectSlots(modelInstance, visited, slots, ref anyModified);
        return slots;

        static void CollectSlots(
            object current,
            HashSet<object> visitedSet,
            List<ICategorySlot> result,
            ref bool modified)
        {
            var type = current.GetType();
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum || type.IsValueType)
                return;

            if (!visitedSet.Add(current))
                return;

            foreach (var field in CategoryRegistry.GetPublicFields(type))
            {
                var val = field.GetValue(current);
                if (val == null)
                    continue;

                if (val is ICategorySlot slot)
                {
                    var attr = field.GetCustomAttribute<CategoryAttribute>();
                    if (slot.ConfigureChild(attr, field.Name))
                    {
                        modified = true;
                    }
                    result.Add(slot);
                }
                else if (field.FieldType.IsClass && field.FieldType != typeof(string))
                {
                    CollectSlots(val, visitedSet, result, ref modified);
                }
            }
        }
    }
}

// --- 5. CLIENT DATA INITIALIZATION & MODEL LIBRARY ---

public sealed class ModelEntry
{
    public string Guid { get; set; } = System.Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New_Model";
    public string ModelType { get; set; } = string.Empty;
    public string CategoryTags { get; set; } = string.Empty;
    public long LastUpdatedTicks { get; set; } = DateTime.UtcNow.Ticks;

    [JsonIgnore] public bool IsDefault { get; set; }
    [JsonIgnore] public object? Instance { get; set; }

    public JsonElement? RawData { get; set; }
}

public sealed class RootSlotRecord
{
    public string? ModelGuid { get; set; }
    public List<string> HistoryGuids { get; set; } = new();
}

public sealed record ModelSearchResult(
    string Guid,
    string ModelName,
    Type ModelType,
    bool IsDefault,
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
                ? $"{ModelName} [Default]"
                : ModelName;
}

public static class Library
{
    private const string RootSlotsFileName = "_RootSlots.json";

    private static readonly Dictionary<string, ModelEntry> _models = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, RootSlotRecord> _rootSlots = new(StringComparer.OrdinalIgnoreCase);
    private static bool _initialized;

    public static string StorageDirectory { get; private set; } = string.Empty;

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

    private static void RequireInitialized()
    {
        if (!_initialized)
        {
            Log.Logic.Fatal("Library is not initialized. Call Library.Initialize() in client startup first.");
        }
    }

    public static void Initialize(
        string relativeDataFolder = "_Data",
        [CallerFilePath] string callerFilePath = "")
    {
        if (string.IsNullOrWhiteSpace(callerFilePath))
            Log.Logic.Fatal("Library.Initialize requires a valid caller path to resolve the data directory.");

        var callerDir = Path.GetDirectoryName(callerFilePath)!;
        StorageDirectory = Path.GetFullPath(Path.Combine(callerDir, relativeDataFolder));

        _models.Clear();
        _rootSlots.Clear();

        if (!Directory.Exists(StorageDirectory))
            Directory.CreateDirectory(StorageDirectory);

        // 1. Load Root Slots index
        var rootSlotsPath = GetRootSlotsFilePath();
        if (File.Exists(rootSlotsPath))
        {
            try
            {
                var rootJson = File.ReadAllText(rootSlotsPath);
                var loadedRoots = JsonSerializer.Deserialize<Dictionary<string, RootSlotRecord>>(rootJson, JsonOptions);
                if (loadedRoots != null)
                {
                    foreach (var kvp in loadedRoots)
                        _rootSlots[kvp.Key] = kvp.Value;
                }
            }
            catch (Exception ex)
            {
                Log.Logic.Error($"Failed to load '{rootSlotsPath}': {ex.Message}");
            }
        }

        // 2. Load all Model JSON files
        foreach (var file in Directory.GetFiles(StorageDirectory, "*.json"))
        {
            if (string.Equals(Path.GetFileName(file), RootSlotsFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                var json = File.ReadAllText(file);
                var entry = JsonSerializer.Deserialize<ModelEntry>(json, JsonOptions);
                if (entry == null || string.IsNullOrWhiteSpace(entry.Guid))
                    continue;

                var modelType = CategoryRegistry.FindModelType(entry.ModelType);
                if (modelType == null)
                    continue;

                entry.Name = CategoryRegistry.SanitizeName(entry.Name, modelType.Name);
                if (entry.LastUpdatedTicks <= 0)
                    entry.LastUpdatedTicks = File.GetLastWriteTimeUtc(file).Ticks;

                if (entry.RawData.HasValue)
                    entry.Instance = entry.RawData.Value.Deserialize(modelType, JsonOptions);

                entry.Instance ??= Activator.CreateInstance(modelType)!;

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

        // 3. Bind and validate child slots on all loaded models (and auto-heal any mismatched slot types)
        foreach (var entry in _models.Values.ToList())
        {
            if (entry.Instance != null)
            {
                bool modified = CategorySystem.BindModelInstanceSlots(entry.Instance);
                if (modified)
                {
                    SaveModelToFile(entry);
                }
            }
        }

        Log.Logic.Info($"Model Library initialized with {_models.Count} model(s) in '{StorageDirectory}'.");
    }

    public static bool IsModelInstance(object? obj)
    {
        RequireInitialized();
        if (obj == null)
            return false;

        return _models.Values.Any(m => ReferenceEquals(m.Instance, obj));
    }

    public static string? GetModelGuidByInstance(object? obj)
    {
        RequireInitialized();
        if (obj == null)
            return null;

        return _models.Values.FirstOrDefault(m => ReferenceEquals(m.Instance, obj))?.Guid;
    }

    public static bool TryRestoreRootSlot(string rootSlotKey, out string? modelGuid, out List<string> historyGuids)
    {
        RequireInitialized();
        if (_rootSlots.TryGetValue(rootSlotKey, out var record) ||
            _rootSlots.TryGetValue(rootSlotKey.Split('.').Last(), out record))
        {
            modelGuid = record.ModelGuid;
            historyGuids = record.HistoryGuids ?? new List<string>();
            return !string.IsNullOrWhiteSpace(modelGuid) && ContainsModel(modelGuid);
        }

        modelGuid = null;
        historyGuids = new List<string>();
        return false;
    }

    public static void SaveRootSlot(string rootSlotKey, string? modelGuid, List<string> historyGuids)
    {
        RequireInitialized();
        if (string.IsNullOrWhiteSpace(rootSlotKey))
            return;

        _rootSlots[rootSlotKey] = new RootSlotRecord
        {
            ModelGuid = modelGuid,
            HistoryGuids = historyGuids
                .Where(g => !string.IsNullOrWhiteSpace(g) && ContainsModel(g))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        var json = JsonSerializer.Serialize(_rootSlots, JsonOptions);
        File.WriteAllText(GetRootSlotsFilePath(), json);
    }

    private static string GetRootSlotsFilePath() => Path.Combine(StorageDirectory, RootSlotsFileName);

    public static bool ContainsModel(string? guid)
    {
        RequireInitialized();
        return !string.IsNullOrWhiteSpace(guid) && _models.ContainsKey(guid);
    }

    public static IReadOnlyCollection<ModelEntry> GetAllModels()
    {
        RequireInitialized();
        return _models.Values;
    }

    public static T? GetModel<T>(string? guid) where T : class
    {
        RequireInitialized();
        if (string.IsNullOrWhiteSpace(guid))
            return null;

        if (_models.TryGetValue(guid, out var entry) && entry.Instance is T typedInstance)
            return typedInstance;

        return null;
    }

    public static ModelEntry? GetModelEntry(string? guid)
    {
        RequireInitialized();
        if (string.IsNullOrWhiteSpace(guid))
            return null;

        return _models.GetValueOrDefault(guid);
    }

    public static ModelEntry CreateCopyFromDefault(Type modelType, CategoryAttribute? slotCategory = null, string? customName = null)
    {
        RequireInitialized();
        var instance = Activator.CreateInstance(modelType)!;
        CategorySystem.BindModelInstanceSlots(instance);

        var modelAttr = modelType.GetCustomAttribute<ModelAttribute>(false);
        var tags = modelAttr?.GetTagsDescription() ?? slotCategory?.GetTagsDescription() ?? modelType.Name;

        var entry = new ModelEntry
        {
            Guid = Guid.NewGuid().ToString("N"),
            Name = CategoryRegistry.SanitizeName(customName ?? modelType.Name, modelType.Name),
            ModelType = modelType.FullName ?? modelType.Name,
            CategoryTags = tags,
            LastUpdatedTicks = DateTime.UtcNow.Ticks,
            IsDefault = false,
            Instance = instance
        };

        _models[entry.Guid] = entry;
        SaveModelToFile(entry);
        OnLibraryChanged?.Invoke();
        return entry;
    }

    public static ModelEntry? CopyExistingModel(string sourceGuid, CategoryAttribute? slotCategory = null)
    {
        RequireInitialized();
        if (!_models.TryGetValue(sourceGuid, out var sourceEntry) || sourceEntry.Instance == null)
            return null;

        var runtimeType = sourceEntry.Instance.GetType();
        var clonedInstance = DeepCloneObject(sourceEntry.Instance, runtimeType);

        var clonedSlots = CategorySystem.GetOwnedCategorySlots(clonedInstance);
        foreach (var childSlot in clonedSlots)
        {
            childSlot.PruneDeletedHistory();
            childSlot.CopyModelByGuidOrDefault(childSlot.ModelGuid, childSlot.FallbackType);
        }

        var modelAttr = runtimeType.GetCustomAttribute<ModelAttribute>(false);
        var tags = modelAttr?.GetTagsDescription() ?? slotCategory?.GetTagsDescription() ?? sourceEntry.CategoryTags;

        var newEntry = new ModelEntry
        {
            Guid = Guid.NewGuid().ToString("N"),
            Name = CategoryRegistry.SanitizeName(sourceEntry.Name, runtimeType.Name),
            ModelType = runtimeType.FullName ?? runtimeType.Name,
            CategoryTags = tags,
            LastUpdatedTicks = DateTime.UtcNow.Ticks,
            IsDefault = false,
            Instance = clonedInstance
        };

        _models[newEntry.Guid] = newEntry;
        SaveModelToFile(newEntry);
        OnLibraryChanged?.Invoke();
        return newEntry;
    }

    public static void TouchModel(string guid)
    {
        RequireInitialized();
        if (_models.TryGetValue(guid, out var entry))
        {
            entry.LastUpdatedTicks = DateTime.UtcNow.Ticks;
            SaveModelToFile(entry);
        }
    }

    public static void SaveModel(string? guid)
    {
        RequireInitialized();
        if (!string.IsNullOrWhiteSpace(guid) && _models.TryGetValue(guid, out var entry))
        {
            entry.LastUpdatedTicks = DateTime.UtcNow.Ticks;
            SaveModelToFile(entry);
        }
    }

    public static void RenameModel(string? guid, string newName)
    {
        RequireInitialized();
        if (!string.IsNullOrWhiteSpace(guid) && _models.TryGetValue(guid, out var entry))
        {
            var sanitized = CategoryRegistry.SanitizeName(newName, entry.Name);
            if (!string.IsNullOrWhiteSpace(sanitized))
            {
                entry.Name = sanitized;
                entry.LastUpdatedTicks = DateTime.UtcNow.Ticks;
                SaveModelToFile(entry);
                OnLibraryChanged?.Invoke();
            }
        }
    }

    public static void DeleteModel(string guid)
    {
        RequireInitialized();
        if (_models.Remove(guid))
        {
            var path = GetModelFilePath(guid);
            if (File.Exists(path))
                File.Delete(path);

            OnLibraryChanged?.Invoke();
        }
    }

    public static T DeepClone<T>(T source) where T : class
        => (T)DeepCloneObject(source, source.GetType());

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

        if (entry.Instance != null)
        {
            entry.RawData = JsonSerializer.SerializeToElement(entry.Instance, entry.Instance.GetType(), JsonOptions);
        }

        var json = JsonSerializer.Serialize(entry, JsonOptions);
        File.WriteAllText(GetModelFilePath(entry.Guid), json);
    }

    private static string GetModelFilePath(string guid) => Path.Combine(StorageDirectory, $"{guid}.json");

    // --- 6. SEARCH ENGINE ---

    public static IReadOnlyList<ModelSearchResult> SearchModels(
        CategoryAttribute? category,
        Type modelInterfaceType,
        string? rawQuery)
    {
        RequireInitialized();
        ParseSearchQuery(rawQuery, out var hasFilters, out var hadFilters, out var onlyDefaults, out var pattern);

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
                    LastUpdatedTicks: 0L,
                    ParentGuid: null,
                    ParentModelName: null,
                    ParentMatchScore: 0,
                    PatternMatchScore: patternScore));
            }
        }

        if (onlyDefaults)
            return SortSearchResults(results);

        if (!hasParentFilter)
        {
            foreach (var entry in _models.Values)
            {
                if (entry.Instance == null)
                    continue;

                var instanceType = entry.Instance.GetType();
                if (!CategoryRegistry.IsModelTypeAllowed(instanceType, category, modelInterfaceType))
                    continue;

                if (!TryScoreMatch(entry.Name, pattern, out int patternScore))
                    continue;

                results.Add(new ModelSearchResult(
                    Guid: entry.Guid,
                    ModelName: entry.Name,
                    ModelType: instanceType,
                    IsDefault: false,
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

                var childType = childEntry.Instance.GetType();
                if (!CategoryRegistry.IsModelTypeAllowed(childType, category, modelInterfaceType))
                    continue;

                if (!TryScoreMatch(childEntry.Name, pattern, out int patternScore))
                    continue;

                if (!SatisfiesAdditionalParentFilters(childEntry.Guid, hasFilters, hadFilters))
                    continue;

                results.Add(new ModelSearchResult(
                    Guid: childEntry.Guid,
                    ModelName: childEntry.Name,
                    ModelType: childType,
                    IsDefault: false,
                    LastUpdatedTicks: childEntry.LastUpdatedTicks,
                    ParentGuid: parentEntry.Guid,
                    ParentModelName: parentEntry.Name,
                    ParentMatchScore: parentScore,
                    PatternMatchScore: patternScore));
            }
        }

        return SortSearchResults(results);
    }

    private static bool SatisfiesAdditionalParentFilters(
        string candidateGuid,
        List<string> hasFilters,
        List<string> hadFilters)
    {
        foreach (var filter in hasFilters)
        {
            bool matched = _models.Values.Any(p =>
                p.Instance != null &&
                TryScoreMatch(p.Name, filter, out _) &&
                GetTransitiveDescendants(p, includeHistory: false).Any(d =>
                    string.Equals(d.Guid, candidateGuid, StringComparison.OrdinalIgnoreCase)));

            if (!matched)
                return false;
        }

        foreach (var filter in hadFilters)
        {
            bool matched = _models.Values.Any(p =>
                p.Instance != null &&
                TryScoreMatch(p.Name, filter, out _) &&
                GetTransitiveDescendants(p, includeHistory: true).Any(d =>
                    string.Equals(d.Guid, candidateGuid, StringComparison.OrdinalIgnoreCase)));

            if (!matched)
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
                    anyPruned = true;

                foreach (var histGuid in slot.HistoryGuids)
                {
                    if (!guids.Contains(histGuid, StringComparer.OrdinalIgnoreCase))
                        guids.Add(histGuid);
                }
            }
            else if (!string.IsNullOrWhiteSpace(slot.ModelGuid) && ContainsModel(slot.ModelGuid))
            {
                if (!guids.Contains(slot.ModelGuid, StringComparer.OrdinalIgnoreCase))
                    guids.Add(slot.ModelGuid);
            }
        }

        if (anyPruned)
            SaveModelToFile(parent);

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
                hasFilters.Add(token[4..]);
            else if (token.StartsWith("had:", StringComparison.OrdinalIgnoreCase))
                hadFilters.Add(token[4..]);
            else if (token.StartsWith("default:", StringComparison.OrdinalIgnoreCase))
            {
                onlyDefaults = true;
                var remainder = token[8..];
                if (!string.IsNullOrWhiteSpace(remainder))
                    patternParts.Add(remainder);
            }
            else
                patternParts.Add(token);
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
            score += 50000;
        else if (string.Equals(targetName, queryPattern, StringComparison.OrdinalIgnoreCase))
            score += 40000;
        else if (index == 0)
            score += 25000;
        else if (targetName[index - 1] == '_' ||
                 (char.IsUpper(targetName[index]) && char.IsLower(targetName[index - 1])))
            score += 12000;

        score -= index * 50;
        score -= Math.Max(0, targetName.Length - queryPattern.Length) * 15;
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

// --- 7. CUSTOM JSON CONVERTERS ---

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
            slot.MarkAsChildSlot();

            if (reader.TokenType != JsonTokenType.StartObject)
                return slot;

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (root.TryGetProperty("ModelGuid", out var modelGuidProp))
                slot.ModelGuid = modelGuidProp.GetString();

            if (root.TryGetProperty("HistoryGuids", out var historyProp) && historyProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in historyProp.EnumerateArray())
                    slot.RecordHistory(item.GetString());
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