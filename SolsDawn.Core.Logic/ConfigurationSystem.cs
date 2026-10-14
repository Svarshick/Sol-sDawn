using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SolsDawn.Core.Logic;

public enum SlotState
{
    Inline,
    Linked,
    Error_Missing
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public abstract class CategoryAttribute : Attribute
{
    public Type DefaultFallbackType { get; }
    protected CategoryAttribute(Type defaultFallbackType) => DefaultFallbackType = defaultFallbackType;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public abstract class ModelAttribute : Attribute
{
    public abstract bool Satisfies(CategoryAttribute slot);
}

// Global Preset Library for JSON saving/loading
public static class Library
{
    private static readonly Dictionary<string, string> _presetsJson = new();

    public static void SavePreset<T>(string guid, T model) where T : class
    {
        _presetsJson[guid] = ConfigurationUtility.Serialize(model);
    }

    public static T? GetPreset<T>(string? guid) where T : class
    {
        if (string.IsNullOrEmpty(guid) || !_presetsJson.TryGetValue(guid, out var json))
            return null;
        return ConfigurationUtility.Deserialize<T>(json);
    }

    public static bool Exists(string? guid) => !string.IsNullOrEmpty(guid) && _presetsJson.ContainsKey(guid);
    public static IEnumerable<string> GetAllGuids() => _presetsJson.Keys;
}

public class CategorySlot<T> where T : class
{
    public bool IsPreset { get; set; }
    public string? PresetGuid { get; set; }
    
    // InlineData is serialized polymorphically by System.Text.Json
    public T? InlineData { get; set; }

    [JsonIgnore] public Type? FallbackType { get; internal set; }
    private T? _fallbackData;

    public SlotState GetState()
    {
        if (IsPreset) return Library.Exists(PresetGuid) ? SlotState.Linked : SlotState.Error_Missing;
        return SlotState.Inline;
    }

    public T GetActiveInstance()
    {
        if (IsPreset)
        {
            var preset = Library.GetPreset<T>(PresetGuid);
            if (preset != null) return preset; 
            return GetOrCreateFallback();      
        }

        if (InlineData != null) return InlineData; 
        
        return GetOrCreateFallback(); 
    }

    private T GetOrCreateFallback()
    {
        if (_fallbackData == null)
        {
            if (FallbackType == null) throw new InvalidOperationException("Slot fallback was not initialized by ConfigurationUtility.");
            _fallbackData = (T)Activator.CreateInstance(FallbackType)!;
        }
        return _fallbackData;
    }

    public void LinkPreset(string guid)
    {
        PresetGuid = guid;
        IsPreset = true;
        InlineData = null;
    }

    public void MakeUnique()
    {
        if (IsPreset && Library.Exists(PresetGuid))
            InlineData = Library.GetPreset<T>(PresetGuid);
        else
            InlineData = GetOrCreateFallback(); 
            
        IsPreset = false;
        PresetGuid = null;
    }
}

public static class ConfigurationUtility
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, JsonOptions);
    public static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonOptions)!;

    public static void InitializeConfig(object? configObj)
    {
        if (configObj == null) return;
        var type = configObj.GetType();

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var value = field.GetValue(configObj);
            if (value == null) continue;

            // 1. If it's a Slot, wire up the Fallback type
            if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(CategorySlot<>))
            {
                var catAttr = field.GetCustomAttribute<CategoryAttribute>();
                if (catAttr != null)
                {
                    var fallbackProp = field.FieldType.GetProperty(nameof(CategorySlot<object>.FallbackType));
                    fallbackProp?.SetValue(value, catAttr.DefaultFallbackType);
                }
            }
            // 2. If it's a nested class (like MotionSolver), recurse into it
            else if (field.FieldType.IsClass && field.FieldType != typeof(string))
            {
                InitializeConfig(value);
            }
        }
    }

    public static IEnumerable<Type> GetAllowedModelsForCategory(CategoryAttribute category)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
            {
                var modelAttr = type.GetCustomAttribute<ModelAttribute>();
                if (modelAttr != null && modelAttr.Satisfies(category))
                {
                    yield return type;
                }
            }
        }
    }
}