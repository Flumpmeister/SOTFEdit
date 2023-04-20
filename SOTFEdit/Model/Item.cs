﻿using System.Collections.Generic;
using SOTFEdit.Infrastructure;
using SOTFEdit.Model.SaveData.Storage.Module;
using SOTFEdit.Model.Storage;

namespace SOTFEdit.Model;

public class Item
{
    private string? _normalizedLowercaseName;
    public int Id { get; init; }
    public string Name => TranslationManager.Get("items." + Id);
    public string Type { get; init; }
    public FoodSpoilModuleDefinition? FoodSpoilModuleDefinition { get; init; }
    public SourceActorModuleDefinition? SourceActorModuleDefinition { get; init; }
    public bool IsInventoryItem { get; init; } = true;
    public bool IsEquippableArmor { get; init; } = false;
    public bool IsWearableCloth { get; init; } = false;
    public DefaultMinMax? Durability { get; init; }
    public StorageMax? StorageMax { get; init; }

    public string NormalizedLowercaseName
    {
        get { return _normalizedLowercaseName ??= TranslationHelper.Normalize(Name).ToLower(); }
    }

    private bool Equals(Item other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((Item)obj);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public bool HasModules()
    {
        return FoodSpoilModuleDefinition != null;
    }
}

public record ItemModule(int ModuleId);

// ReSharper disable once ClassNeverInstantiated.Global
public record FoodSpoilModuleDefinition(int DefaultVariant, List<int> Variants) : ItemModule(3)
{
    public FoodSpoilStorageModule BuildNewModuleWithDefaults()
    {
        return new FoodSpoilStorageModule(ModuleId, DefaultVariant);
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
public record SourceActorModuleDefinition(int DefaultSourceActorType) : ItemModule(1)
{
    public SourceActorStorageModule BuildNewModuleWithDefaults()
    {
        return new SourceActorStorageModule(DefaultSourceActorType);
    }
}