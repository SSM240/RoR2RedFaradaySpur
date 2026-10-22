using LookingGlass.ItemStatsNameSpace;
using RoR2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RoR2RedFaradaySpur;

internal static class LookingGlassInterop
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void UpdateStats()
    {
        // change the LookingGlass stats display to use the new values
        ItemStatsDef spurStats = ItemDefinitions.allItemDefinitions[(int)DLC3Content.Items.JumpDamageStrike.itemIndex];
        spurStats.calculateValuesFlat = (stackCount) =>
        {
            float baseDamage = RedFaradaySpurPlugin.AdjustDamage.Value ? RedFaradaySpurPlugin.BaseDamage.Value : 4f;
            float stackDamage = RedFaradaySpurPlugin.AdjustDamage.Value ? RedFaradaySpurPlugin.StackDamage.Value : 2.8f;
            List<float> values = new();
            values.Add(baseDamage + stackDamage * (stackCount - 1));
            values.Add(24.8f + stackCount * 7.5f);
            values.Add(1f);
            return values;
        };
    }
}
