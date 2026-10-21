using BepInEx.Logging;
using ItemQualities;
using ItemQualities.ContentManagement;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace RoR2RedFaradaySpur;

internal static class ItemQualitiesInterop
{
    private static bool GetSpriteFromFile(string filename, out Sprite sprite) 
        => RedFaradaySpurPlugin.GetSpriteFromFile(filename, out sprite);

    internal static void HandleQualityFaradaySpur()
    {
        SetQualityTiers();
        SetQualityIcons();
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void SetQualityTiers()
    {
        ItemQualityGroup group = ItemQualitiesContent.ItemQualityGroups.JumpDamageStrike;
        ItemTierDef tier3 = ItemTierCatalog.GetItemTierDef(ItemTier.Tier3);
        group.GetItemDef(QualityTier.Uncommon)._itemTierDef = tier3;
        group.GetItemDef(QualityTier.Rare)._itemTierDef = tier3;
        group.GetItemDef(QualityTier.Epic)._itemTierDef = tier3;
        group.GetItemDef(QualityTier.Legendary)._itemTierDef = tier3;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void SetQualityIcons()
    {
        ItemQualityGroup group = ItemQualitiesContent.ItemQualityGroups.JumpDamageStrike;
        // bluh
        #pragma warning disable CS0618 // Type or member is obsolete
        if (GetSpriteFromFile("texJumpDamageStrikeUncommon_new.png", out var uncommonSprite))
        {
            group.GetItemDef(QualityTier.Uncommon).pickupIconSprite = uncommonSprite;
        }
        if (GetSpriteFromFile("texJumpDamageStrikeRare_new.png", out var rareSprite))
        {
            group.GetItemDef(QualityTier.Rare).pickupIconSprite = rareSprite;
        }
        if (GetSpriteFromFile("texJumpDamageStrikeEpic_new.png", out var epicSprite))
        {
            group.GetItemDef(QualityTier.Epic).pickupIconSprite = epicSprite;
        }
        if (GetSpriteFromFile("texJumpDamageStrikeLegendary_new.png", out var legendarySprite))
        {
            group.GetItemDef(QualityTier.Legendary).pickupIconSprite = legendarySprite;
        }
        #pragma warning restore CS0618 // Type or member is obsolete
    }
}
