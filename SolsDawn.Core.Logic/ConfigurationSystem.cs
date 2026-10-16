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

// --- 2. SLOT STATE & WRAPPER ---

public enum SlotState
{
    Inline,        // The Model data is unique to this parent and serialized within it.
    Linked,        // The Model data is a shared Preset. The slot holds a string GUID.
    Error_Missing  // The slot holds a GUID, but the file was deleted/missing.
}

public interface ICategorySlot
{
    bool IsPreset { get; set; }
    string? PresetGuid { get; set; }
    object? InlineDataObject { get; set; }
    Type ModelInterfaceType { get; }
    Type? FallbackType { get; }
    CategoryAttribute? CategoryAttribute { get; }
    string FieldName { get; }
    SlotState State { get; }

    void Configure(CategoryAttribute? attribute, string fieldName);
    void Resolve();
    object GetActiveInstanceObject();
    void SelectModel(Type modelType);
    PresetEntry SaveAsPreset(string presetName);
    void LinkPreset(string presetGuid);
    void MakeUnique();
}

public class CategorySlot<T> : ICategorySlot where T : class
{
    public bool IsPreset { get; set; }
    public string? PresetGuid { get; set; }
    public T? InlineData { get; set; }

    private T? _fallbackData;
    private bool _missingLogged;

    [JsonIgnore] public CategoryAttribute? CategoryAttribute { get; private set; }
    [JsonIgnore] public string FieldName { get; private set; } = typeof(T).Name;
    [JsonIgnore] public Type ModelInterfaceType => typeof(T);
    [JsonIgnore] public Type? FallbackType => CategoryAttribute?.DefaultFallbackType;

    [JsonIgnore]
    public object? InlineDataObject
    {
        get => InlineData;
        set => InlineData = value as T;
    }

    [JsonIgnore]
    public SlotState State
    {
        get
        {
            if (IsPreset)
            {
                if (!string.IsNullOrWhiteSpace(PresetGuid) && Library.GetPreset<T>(PresetGuid) != null)
                    return SlotState.Linked;

                return SlotState.Error_Missing;
            }

            return SlotState.Inline;
        }
    }

    public void Configure(CategoryAttribute? attribute, string fieldName)
    {
        CategoryAttribute = attribute;
        if (!string.IsNullOrWhiteSpace(fieldName))
            FieldName = fieldName;

        if (!IsPreset && InlineData == null && FallbackType != null)
        {
            InlineData = (T)Activator.CreateInstance(FallbackType)!;
            CategorySystem.BindAndResolveRecursive(InlineData);
        }
    }

    public void Resolve()
    {
        _missingLogged = false;
        if (IsPreset)
        {
            var preset = Library.GetPreset<T>(PresetGuid);
            if (preset == null)
            {
                var fb = GetOrCreateFallback(FallbackType);
                Log.Logic.Warning($"CategorySlot '{FieldName}' failed to resolve Preset GUID '{PresetGuid}'. Flagged as Error_Missing and using fallback '{fb.GetType().Name}'.");
                _missingLogged = true;
            }
        }
        else if (InlineData == null)
        {
            InlineData = GetOrCreateFallback(FallbackType);
        }
    }

    // Called by the engine. GUARANTEED to return a valid object.
    public T GetActiveInstance()
    {
        if (IsPreset)
        {
            var preset = Library.GetPreset<T>(PresetGuid);
            if (preset != null)
                return preset; // Linked

            if (!_missingLogged)
            {
                Log.Logic.Warning($"CategorySlot '{FieldName}' missing preset '{PresetGuid}'. Using fallback.");
                _missingLogged = true;
            }

            return GetOrCreateFallback(FallbackType); // Error_Missing
        }

        if (InlineData != null)
            return InlineData; // Inline

        InlineData = GetOrCreateFallback(FallbackType); // Safety catch
        return InlineData;
    }

    public object GetActiveInstanceObject() => GetActiveInstance();

    private T GetOrCreateFallback(Type? fallbackType)
    {
        var resolvedType = fallbackType ?? FallbackType ?? CategoryRegistry.GetDefaultModelType(typeof(T), CategoryAttribute);
        if (_fallbackData == null || _fallbackData.GetType() != resolvedType)
        {
            _fallbackData = (T)Activator.CreateInstance(resolvedType)!;
            CategorySystem.BindAndResolveRecursive(_fallbackData);
        }
        return _fallbackData;
    }

    public void SelectModel(Type modelType)
    {
        if (!typeof(T).IsAssignableFrom(modelType))
            throw new ArgumentException($"Type {modelType.Name} does not implement {typeof(T).Name}");

        InlineData = (T)Activator.CreateInstance(modelType)!;
        CategorySystem.BindAndResolveRecursive(InlineData);
        PresetGuid = null;
        IsPreset = false;
        _missingLogged = false;
    }

    public PresetEntry SaveAsPreset(string presetName)
    {
        var active = GetActiveInstance();
        var cloned = Library.DeepClone(active);
        var entry = Library.CreatePreset(presetName, cloned, CategoryAttribute);
        PresetGuid = entry.Guid;
        IsPreset = true;
        InlineData = null;
        _missingLogged = false;
        return entry;
    }

    public void LinkPreset(string presetGuid)
    {
        var preset = Library.GetPreset<T>(presetGuid);
        if (preset != null)
        {
            Library.EnsureClonedInMemory(presetGuid);
        }

        PresetGuid = presetGuid;
        IsPreset = true;
        InlineData = null;
        _missingLogged = false;
    }

    public void MakeUnique()
    {
        var active = GetActiveInstance();
        InlineData = Library.DeepClone(active);
        CategorySystem.BindAndResolveRecursive(InlineData);
        PresetGuid = null;
        IsPreset = false;
        _missingLogged = false;
    }
}

// --- 3. MODEL DISCOVERY & RESOLUTION HANDSHAKE ---

public sealed record ModelDescriptor(
    Type ModelType,
    ModelAttribute Attribute,
    string Name,
    string TagsDescription);

public static class CategoryRegistry
{
    private static List<ModelDescriptor>? _models;
    private static readonly Dictionary<Type, PropertyInfo[]> _slotOwnerCache = new();
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
                    _models.Add(new ModelDescriptor(
                        type,
                        modelAttr,
                        type.Name,
                        modelAttr.GetTagsDescription()));
                }
            }
        }

        return _models;
    }

    public static IReadOnlyList<ModelDescriptor> GetAllModels() => EnsureModelsLoaded();

    public static IReadOnlyList<ModelDescriptor> GetAllowedModels(CategoryAttribute? category, Type modelInterfaceType)
    {
        var all = EnsureModelsLoaded();
        if (category == null)
            return all.Where(m => modelInterfaceType.IsAssignableFrom(m.ModelType)).ToList();

        return all
            .Where(m => modelInterfaceType.IsAssignableFrom(m.ModelType) && m.Attribute.Satisfies(category))
            .ToList();
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

        throw new InvalidOperationException($"No valid Model found for interface {modelInterfaceType.Name}");
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

    public static void BindAndResolveRecursive(object? root)
    {
        if (root == null)
            return;

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        Visit(root, visited);

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
                    if (slot.InlineDataObject != null)
                        Visit(slot.InlineDataObject, visitedSet);
                }
                else if (field.FieldType.IsClass && field.FieldType != typeof(string))
                {
                    Visit(val, visitedSet);
                }
            }
        }
    }
}

// --- 4. GLOBAL PRESET LIBRARY & JSON SERIALIZATION ---

public sealed class PresetEntry
{
    public string Guid { get; set; } = System.Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New Preset";
    public string ModelType { get; set; } = string.Empty;
    public string CategoryTags { get; set; } = string.Empty;

    [JsonIgnore]
    public object? Instance { get; set; }

    public JsonElement? RawData { get; set; }
}

public static class Library
{
    private static readonly Dictionary<string, PresetEntry> _presets = new(StringComparer.OrdinalIgnoreCase);
    private static bool _initialized;

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

    public static void Initialize(string? customDirectory = null)
    {
        if (customDirectory != null)
            PresetsDirectory = customDirectory;

        _presets.Clear();

        if (!Directory.Exists(PresetsDirectory))
        {
            Directory.CreateDirectory(PresetsDirectory);
        }

        foreach (var file in Directory.GetFiles(PresetsDirectory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var entry = JsonSerializer.Deserialize<PresetEntry>(json, JsonOptions);
                if (entry == null || string.IsNullOrWhiteSpace(entry.Guid))
                    continue;

                var modelType = CategoryRegistry.FindModelType(entry.ModelType);
                if (modelType == null)
                {
                    Log.Logic.Warning($"Preset '{file}' references unknown ModelType '{entry.ModelType}'. Skipping.");
                    continue;
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

                _presets[entry.Guid] = entry;
            }
            catch (Exception ex)
            {
                Log.Logic.Error($"Failed to load preset file '{file}': {ex.Message}");
            }
        }

        _initialized = true;
        Log.Logic.Info($"Preset Library initialized with {_presets.Count} preset(s) from '{PresetsDirectory}'.");
    }

    public static void EnsureInitialized()
    {
        if (!_initialized)
            Initialize();
    }

    public static IReadOnlyCollection<PresetEntry> GetAllPresets()
    {
        EnsureInitialized();
        return _presets.Values;
    }

    public static T? GetPreset<T>(string? guid) where T : class
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(guid))
            return null;

        if (_presets.TryGetValue(guid, out var entry) && entry.Instance is T typedInstance)
            return typedInstance;

        return null;
    }

    public static PresetEntry? GetPresetEntry(string? guid)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(guid))
            return null;

        return _presets.GetValueOrDefault(guid);
    }

    public static IReadOnlyList<PresetEntry> GetAllowedPresets(CategoryAttribute? category, Type modelInterfaceType)
    {
        EnsureInitialized();
        var result = new List<PresetEntry>();

        foreach (var entry in _presets.Values)
        {
            if (entry.Instance == null)
                continue;

            var instanceType = entry.Instance.GetType();
            if (!modelInterfaceType.IsAssignableFrom(instanceType))
                continue;

            if (category != null)
            {
                var modelAttr = instanceType.GetCustomAttribute<ModelAttribute>(false);
                if (modelAttr == null || !modelAttr.Satisfies(category))
                    continue;
            }

            result.Add(entry);
        }

        return result;
    }

    public static PresetEntry CreatePreset(string name, object modelInstance, CategoryAttribute? slotCategory = null)
    {
        EnsureInitialized();
        var modelType = modelInstance.GetType();
        var modelAttr = modelType.GetCustomAttribute<ModelAttribute>(false);
        var tags = modelAttr?.GetTagsDescription() ?? slotCategory?.GetTagsDescription() ?? modelType.Name;

        var entry = new PresetEntry
        {
            Guid = Guid.NewGuid().ToString("N"),
            Name = string.IsNullOrWhiteSpace(name) ? $"{modelType.Name}_Preset" : name.Trim(),
            ModelType = modelType.FullName ?? modelType.Name,
            CategoryTags = tags,
            Instance = DeepCloneObject(modelInstance, modelType)
        };

        _presets[entry.Guid] = entry;
        SavePresetToFile(entry);
        OnLibraryChanged?.Invoke();
        return entry;
    }

    public static void EnsureClonedInMemory(string guid)
    {
        if (_presets.TryGetValue(guid, out var entry) && entry.Instance != null)
        {
            entry.Instance = DeepCloneObject(entry.Instance, entry.Instance.GetType());
        }
    }

    public static void SavePreset(string guid)
    {
        EnsureInitialized();
        if (_presets.TryGetValue(guid, out var entry))
        {
            SavePresetToFile(entry);
        }
    }

    public static void RenamePreset(string guid, string newName)
    {
        EnsureInitialized();
        if (_presets.TryGetValue(guid, out var entry) && !string.IsNullOrWhiteSpace(newName))
        {
            entry.Name = newName.Trim();
            SavePresetToFile(entry);
            OnLibraryChanged?.Invoke();
        }
    }

    public static void DeletePreset(string guid)
    {
        EnsureInitialized();
        if (_presets.Remove(guid))
        {
            var path = GetPresetFilePath(guid);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
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

    private static void SavePresetToFile(PresetEntry entry)
    {
        if (!Directory.Exists(PresetsDirectory))
            Directory.CreateDirectory(PresetsDirectory);

        if (entry.Instance != null)
        {
            var element = JsonSerializer.SerializeToElement(entry.Instance, entry.Instance.GetType(), JsonOptions);
            entry.RawData = element;
        }

        var json = JsonSerializer.Serialize(entry, JsonOptions);
        File.WriteAllText(GetPresetFilePath(entry.Guid), json);
    }

    private static string GetPresetFilePath(string guid) => Path.Combine(PresetsDirectory, $"{guid}.json");
}

// --- 5. CUSTOM JSON CONVERTERS ---

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

            bool isPreset = root.TryGetProperty("IsPreset", out var isPresetProp) && isPresetProp.GetBoolean();
            string? presetGuid = root.TryGetProperty("PresetGuid", out var guidProp) ? guidProp.GetString() : null;

            slot.IsPreset = isPreset;
            slot.PresetGuid = presetGuid;

            if (!isPreset)
            {
                string? modelTypeName = root.TryGetProperty("ModelType", out var mtProp) ? mtProp.GetString() : null;
                var concreteType = CategoryRegistry.FindModelType(modelTypeName);
                if (concreteType != null && typeof(T).IsAssignableFrom(concreteType))
                {
                    if (root.TryGetProperty("InlineData", out var inlineElement) && inlineElement.ValueKind == JsonValueKind.Object)
                    {
                        slot.InlineData = (T?)inlineElement.Deserialize(concreteType, options);
                    }
                    slot.InlineData ??= (T)Activator.CreateInstance(concreteType)!;
                }
            }

            return slot;
        }

        public override void Write(Utf8JsonWriter writer, CategorySlot<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteBoolean("IsPreset", value.IsPreset);

            if (value.IsPreset)
            {
                writer.WriteString("PresetGuid", value.PresetGuid);
            }
            else
            {
                var inline = value.InlineData ?? value.GetActiveInstance();
                var runtimeType = inline.GetType();
                writer.WriteString("ModelType", runtimeType.FullName ?? runtimeType.Name);
                writer.WritePropertyName("InlineData");
                JsonSerializer.Serialize(writer, inline, runtimeType, options);
            }

            writer.WriteEndObject();
        }
    }
}