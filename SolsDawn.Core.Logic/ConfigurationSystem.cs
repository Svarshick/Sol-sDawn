using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace SolsDawn.Core.Logic;

// --- 1. GENERIC ATTRIBUTE INFRASTRUCTURE ---

[AttributeUsage(AttributeTargets.Field)]
public abstract class CategoryAttribute : Attribute
{
    public Type DefaultFallbackType { get; }

    protected CategoryAttribute(Type defaultFallbackType)
    {
        DefaultFallbackType = defaultFallbackType;
    }

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
    bool IsMissingModel { get; }

    void Configure(CategoryAttribute? attribute, string fieldName);
    void ConfigureAsDefaultTemplate(CategoryAttribute? attribute, string fieldName);
    void Resolve();
    object GetActiveInstanceObject();
    string GetActiveModelName();
    ModelEntry? GetActiveModelEntry();

    ModelEntry CopyModel(ModelSearchResult source);
    ModelEntry CopyModelByGuidOrDefault(string? sourceGuid, Type? fallbackModelType = null);
    void ReferenceModel(string targetGuid);
    void RemoveFromHistory(string guid);
    void PruneDeletedHistory();
}

public class CategorySlot<T> : ICategorySlot where T : class
{
    public string? ModelGuid { get; set; }
    public List<string> HistoryGuids { get; set; } = new();

    private bool _isDefaultTemplateSlot;
    private bool _missingLogged;

    [JsonIgnore] public CategoryAttribute? CategoryAttribute { get; private set; }
    [JsonIgnore] public string FieldName { get; private set; } = typeof(T).Name;
    [JsonIgnore] public Type ModelInterfaceType => typeof(T);
    [JsonIgnore] public Type? FallbackType => CategoryAttribute?.DefaultFallbackType;

    [JsonIgnore]
    public bool IsMissingModel =>
        !_isDefaultTemplateSlot &&
        !string.IsNullOrWhiteSpace(ModelGuid) &&
        Library.GetModel<T>(ModelGuid) == null;

    public void Configure(CategoryAttribute? attribute, string fieldName)
    {
        CategoryAttribute = attribute;
        if (!string.IsNullOrWhiteSpace(fieldName))
            FieldName = fieldName;

        if (!_isDefaultTemplateSlot && string.IsNullOrWhiteSpace(ModelGuid))
        {
            EnsureInitializedWithCopy();
        }
    }

    public void ConfigureAsDefaultTemplate(CategoryAttribute? attribute, string fieldName)
    {
        _isDefaultTemplateSlot = true;
        CategoryAttribute = attribute;
        if (!string.IsNullOrWhiteSpace(fieldName))
            FieldName = fieldName;
    }

    public void Resolve()
    {
        if (_isDefaultTemplateSlot)
            return;

        _missingLogged = false;
        PruneDeletedHistory();

        if (string.IsNullOrWhiteSpace(ModelGuid))
        {
            EnsureInitializedWithCopy();
            return;
        }

        var model = Library.GetModel<T>(ModelGuid);
        if (model == null)
        {
            var fb = GetDefaultFallbackInstance();
            Log.Logic.Warning(
                $"CategorySlot '{FieldName}' could not find model GUID '{ModelGuid}'. Using default fallback '{fb.GetType().Name}'.");
            _missingLogged = true;
        }
        else
        {
            RecordHistory(ModelGuid);
        }
    }

    private void EnsureInitializedWithCopy()
    {
        var defaultType = CategoryRegistry.GetDefaultModelType(typeof(T), CategoryAttribute);
        var created = Library.CreateCopyFromDefault(defaultType, CategoryAttribute);
        ModelGuid = created.Guid;
        RecordHistory(created.Guid);
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

    // Guaranteed never to return null
    public T GetActiveInstance()
    {
        if (_isDefaultTemplateSlot)
            return GetDefaultFallbackInstance();

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
        if (!string.IsNullOrWhiteSpace(ModelGuid))
        {
            var entry = Library.GetModelEntry(ModelGuid);
            if (entry != null)
                return entry;
        }

        var defaultType = CategoryRegistry.GetDefaultModelType(typeof(T), CategoryAttribute);
        return CategoryRegistry.GetDefaultModelEntry(defaultType);
    }

    public string GetActiveModelName()
    {
        var entry = GetActiveModelEntry();
        if (entry != null)
            return entry.Name;

        return GetActiveInstance().GetType().Name;
    }

    private T GetDefaultFallbackInstance()
    {
        var defaultType = CategoryRegistry.GetDefaultModelType(typeof(T), CategoryAttribute);
        return (T)CategoryRegistry.GetDefaultModelEntry(defaultType).Instance!;
    }

    // Copy action: creates a new unique model instance with its own GUID
    public ModelEntry CopyModel(ModelSearchResult source)
    {
        ModelEntry created;
        if (source.IsDefault)
        {
            created = Library.CreateCopyFromDefault(source.ModelType, CategoryAttribute);
        }
        else
        {
            created = Library.CopyExistingModel(source.Guid, CategoryAttribute)
                      ?? Library.CreateCopyFromDefault(source.ModelType, CategoryAttribute);
        }

        ModelGuid = created.Guid;
        RecordHistory(created.Guid);
        _missingLogged = false;
        Library.NotifyHistoryOrSlotChanged();
        return created;
    }

    public ModelEntry CopyModelByGuidOrDefault(string? sourceGuid, Type? fallbackModelType = null)
    {
        ModelEntry? created = null;
        if (!string.IsNullOrWhiteSpace(sourceGuid))
        {
            created = Library.CopyExistingModel(sourceGuid, CategoryAttribute);
        }

        if (created == null)
        {
            var targetType = fallbackModelType ?? CategoryRegistry.GetDefaultModelType(typeof(T), CategoryAttribute);
            created = Library.CreateCopyFromDefault(targetType, CategoryAttribute);
        }

        ModelGuid = created.Guid;
        RecordHistory(created.Guid);
        _missingLogged = false;
        Library.NotifyHistoryOrSlotChanged();
        return created;
    }

    // Reference action: reuses the existing GUID
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

// --- 3. MODEL DISCOVERY, DEFAULT MODELS & RESOLUTION HANDSHAKE ---

public sealed record ModelDescriptor(
    Type ModelType,
    ModelAttribute Attribute,
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
        CategorySystem.BindDefaultTemplateRecursive(instance);

        return new ModelEntry
        {
            Guid = $"default:{modelType.FullName ?? modelType.Name}",
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
        CategorySystem.BindDefaultTemplateRecursive(instance);
        var fallbackEntry = new ModelEntry
        {
            Guid = $"default:{modelType.FullName ?? modelType.Name}",
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

    public static Type GetDefaultModelType(Type modelInterfaceType, CategoryAttribute? category)
    {
        if (category?.DefaultFallbackType != null && modelInterfaceType.IsAssignableFrom(category.DefaultFallbackType))
            return category.DefaultFallbackType;

        var allowed = GetAllowedModels(category, modelInterfaceType);
        if (allowed.Count > 0)
            return allowed[0].ModelType;

        var anyAssignable = EnsureModelsLoaded().FirstOrDefault(m => modelInterfaceType.IsAssignableFrom(m.ModelType));
        if (anyAssignable != null)
            return anyAssignable.ModelType;

        throw new InvalidOperationException($"No valid Model Type found for {modelInterfaceType.Name}");
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

// --- 4. CATEGORY SYSTEM & HIERARCHY TRAVERSAL ---

public static class CategorySystem
{
    public static void BindSlots(object owner)
    {
        if (owner == null)
            return;

        var fields = CategoryRegistry.GetPublicFields(owner.GetType());
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
            slot.Configure(attr, field.Name);
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

    public static void BindAndResolveRecursive(object? root)
    {
        if (root == null)
            return;

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        Visit(root, visited);

        // Register root configuration slots (e.g., PlayerSpecs) in Library._rootSlots
        if (root.GetType().GetCustomAttribute<ModelAttribute>(inherit: false) == null)
        {
            Library.RegisterRootOwner(root);
        }

        static void Visit(object current, HashSet<object> visitedSet)
        {
            var type = current.GetType();
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum || type.IsValueType)
                return;

            if (!visitedSet.Add(current))
                return;

            BindSlots(current);

            foreach (var field in CategoryRegistry.GetPublicFields(type))
            {
                var val = field.GetValue(current);
                if (val == null)
                    continue;

                if (val is ICategorySlot slot)
                {
                    slot.Resolve();
                    var activeObj = slot.GetActiveInstanceObject();
                    if (activeObj != null)
                        Visit(activeObj, visitedSet);
                }
                else if (field.FieldType.IsClass && field.FieldType != typeof(string))
                {
                    Visit(val, visitedSet);
                }
            }
        }
    }

    // Collects all ICategorySlot fields belonging to an object (traversing through Nested Fields like MotionSolver,
    // without crossing into child Models).
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
                var val = field.GetValue(current);
                if (val == null)
                    continue;

                if (val is ICategorySlot slot)
                {
                    var attr = field.GetCustomAttribute<CategoryAttribute>();
                    if (slot.CategoryAttribute == null && attr != null)
                    {
                        slot.Configure(attr, field.Name);
                    }
                    result.Add(slot);
                }
                else if (field.FieldType.IsClass && field.FieldType != typeof(string))
                {
                    CollectSlots(val, visitedSet, result);
                }
            }
        }
    }
}

// --- 5. GLOBAL MODEL LIBRARY, OWNERSHIP TRACKING & SEARCH ENGINE ---

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
                ? $"{ModelName} [Default]"
                : ModelName;
}

public static class Library
{
    private static readonly Dictionary<string, ModelEntry> _models = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<ICategorySlot> _rootSlots = new(ReferenceEqualityComparer.Instance);
    private static readonly Dictionary<Type, List<ICategorySlot>> _rootSlotsByOwnerType = new();

    private static bool _initialized;
    private static bool _isLoading;

    public static int Version { get; private set; } = 1;

    public static string PresetsDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Presets");

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

    public static void RegisterRootOwner(object rootOwner)
    {
        if (rootOwner == null)
            return;

        var ownerType = rootOwner.GetType();
        var slots = CategorySystem.GetOwnedCategorySlots(rootOwner);
        _rootSlotsByOwnerType[ownerType] = slots;

        _rootSlots.Clear();
        foreach (var list in _rootSlotsByOwnerType.Values)
        {
            foreach (var slot in list)
            {
                _rootSlots.Add(slot);
            }
        }

        Version++;
    }

    public static void RegisterRootSlot(ICategorySlot slot)
    {
        if (slot != null && _rootSlots.Add(slot))
        {
            Version++;
        }
    }

    public static void NotifyHistoryOrSlotChanged()
    {
        Version++;
        OnLibraryChanged?.Invoke();
    }

    // Spaces in model names are replaced with underscores (_)
    public static string SanitizeModelName(string? typeName, string fallback = "Model")
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return fallback.Replace(' ', '_');

        return typeName.Trim().Replace(' ', '_');
    }

    public static void Initialize(string? customDirectory = null)
    {
        if (customDirectory != null)
            PresetsDirectory = customDirectory;

        _isLoading = true;
        _models.Clear();

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

            foreach (var entry in _models.Values.ToList())
            {
                if (entry.Instance != null)
                {
                    foreach (var slot in CategorySystem.GetOwnedCategorySlots(entry.Instance))
                    {
                        slot.PruneDeletedHistory();
                    }
                }
            }
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
            Initialize();
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

    public static T? GetModel<T>(string? guid) where T : class
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(guid))
            return null;

        if (_models.TryGetValue(guid, out var entry) && entry.Instance is T typedInstance)
            return typedInstance;

        return null;
    }

    public static ModelEntry? GetModelEntry(string? guid)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(guid))
            return null;

        return _models.GetValueOrDefault(guid);
    }

    public static ModelEntry CreateCopyFromDefault(Type modelType, CategoryAttribute? slotCategory = null, string? customName = null)
    {
        EnsureInitialized();
        var instance = Activator.CreateInstance(modelType)!;

        var childSlots = CategorySystem.GetOwnedCategorySlots(instance);
        foreach (var childSlot in childSlots)
        {
            if (string.IsNullOrWhiteSpace(childSlot.ModelGuid))
            {
                childSlot.CopyModelByGuidOrDefault(null, childSlot.FallbackType);
            }
        }

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

        var clonedSlots = CategorySystem.GetOwnedCategorySlots(clonedInstance);
        foreach (var childSlot in clonedSlots)
        {
            childSlot.PruneDeletedHistory();
            if (!string.IsNullOrWhiteSpace(childSlot.ModelGuid))
            {
                childSlot.CopyModelByGuidOrDefault(childSlot.ModelGuid, childSlot.FallbackType);
            }
            else
            {
                childSlot.CopyModelByGuidOrDefault(null, childSlot.FallbackType);
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

    public static void RenameModel(string? guid, string newName)
    {
        EnsureInitialized();
        if (!string.IsNullOrWhiteSpace(guid) && _models.TryGetValue(guid, out var entry))
        {
            var sanitized = SanitizeModelName(newName, entry.Name);
            if (!string.IsNullOrWhiteSpace(sanitized))
            {
                entry.Name = sanitized;
                entry.LastUpdatedTicks = DateTime.UtcNow.Ticks;
                SaveModelToFile(entry);
                Version++;
                OnLibraryChanged?.Invoke();
            }
        }
    }

    public static void DeleteModel(string guid)
    {
        EnsureInitialized();
        if (_models.Remove(guid))
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
        File.WriteAllText(GetModelFilePath(entry.Guid), json);
    }

    private static string GetModelFilePath(string guid) => Path.Combine(PresetsDirectory, $"{guid}.json");

    // --- 6. OWNERSHIP CALCULATION & SEARCH ENGINE ---

    // Collects all model GUIDs currently bound to any slot in _rootSlots or Library models.
    // A model in Library is Owned if its GUID is in this set, and Orphaned otherwise.
    private static HashSet<string> CollectOwnedModelGuids()
    {
        var ownedGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rootSlot in _rootSlots)
        {
            if (!string.IsNullOrWhiteSpace(rootSlot.ModelGuid))
            {
                ownedGuids.Add(rootSlot.ModelGuid);
            }
        }

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
        string? rawQuery)
    {
        EnsureInitialized();
        ParseSearchQuery(rawQuery, out var hasFilters, out var hadFilters, out var onlyDefaults, out var pattern);

        // Pre-calculate Owned vs. Orphaned status once per search execution (Section 6)
        var ownedGuids = CollectOwnedModelGuids();
        var results = new List<ModelSearchResult>();

        bool hasParentFilter = hasFilters.Count > 0 || hadFilters.Count > 0;

        // 1. Default Templates (New)
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

        // 2. Library Models without parent filter
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

        // 3. Library Models with has: / had: parent filter (supports transitive parenting)
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

        return SortSearchResults(results);
    }

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