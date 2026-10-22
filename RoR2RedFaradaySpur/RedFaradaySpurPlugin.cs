using BepInEx;
using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RiskOfOptions;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.Items;
using System.IO;
using Path = System.IO.Path; // RoR2.Path is also a thing
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using System.Globalization;
using System;

namespace RoR2RedFaradaySpur;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency("com.rune580.riskofoptions")]
[BepInDependency("com.bepis.r2api.language")]
[BepInDependency("com.Gorakh.ItemQualities", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("droppod.lookingglass", BepInDependency.DependencyFlags.SoftDependency)]
public class RedFaradaySpurPlugin : BaseUnityPlugin
{
    public const string PluginGUID = PluginAuthor + "." + PluginName;
    public const string PluginAuthor = "SSM24";
    public const string PluginName = "RedFaradaySpur";
    public const string PluginVersion = "1.0.0";

    internal static RedFaradaySpurPlugin Instance;
    internal bool ItemQualitiesLoaded = false;
    internal bool LookingGlassLoaded = false;

    public static ConfigEntry<bool> AdjustChargeSpeed;
    public static ConfigEntry<float> ChargeSpeedMult;
    public static ConfigEntry<bool> AdjustDamage;
    public static ConfigEntry<float> BaseDamage;
    public static ConfigEntry<float> StackDamage;

    public void Awake()
    {
        Instance = this;
        Log.Init(Logger);

        ItemQualitiesLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.Gorakh.ItemQualities");
        LookingGlassLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("droppod.lookingglass");

        AdjustChargeSpeed = Config.Bind("Stats", "Adjust Charge Speed", true,
            "Whether to change the speed at which Faraday Spur charges");
        ChargeSpeedMult = Config.Bind("Stats", "Charge Speed Multiplier", 2f,
            "The rate at which Faraday Spur should charge");
        AdjustDamage = Config.Bind("Stats", "Adjust Explosion Damage", true,
            "Whether to adjust the explosion damage of Faraday Spur");
        BaseDamage = Config.Bind("Stats", "Base Explosion Damage", 10f,
            "The damage the explosion should do on the first stack. Vanilla default is 4");
        StackDamage = Config.Bind("Stats", "Explosion Damage Per Stack", 7f,
            "The damage the explosion should do for every stack beyond the first. Vanilla default is 2.8");

        AdjustChargeSpeed.SettingChanged += UpdateDesc;
        ChargeSpeedMult.SettingChanged += UpdateDesc;
        AdjustDamage.SettingChanged += UpdateDesc;
        BaseDamage.SettingChanged += UpdateDesc;
        StackDamage.SettingChanged += UpdateDesc;
        Language.onCurrentLanguageChanged += UpdateDesc;

        ModSettingsManager.AddOption(new CheckBoxOption(AdjustChargeSpeed));
        StepSliderConfig chargeSpeedMultConfig = new()
        {
            min = 0f, max = 5f, increment = 0.05f, FormatString = "{0:P0}",
            checkIfDisabled = () => !AdjustChargeSpeed.Value
        };
        ModSettingsManager.AddOption(new StepSliderOption(ChargeSpeedMult, chargeSpeedMultConfig));
        ModSettingsManager.AddOption(new CheckBoxOption(AdjustDamage));
        StepSliderConfig baseDamageConfig = new()
        {
            min = 0f, max = 40f, increment = 0.2f, FormatString = "{0:P0}",
            description = "The damage the explosion should do on the first stack.\n\nVanilla default is 400%, mod default is 1000%",
            checkIfDisabled = () => !AdjustDamage.Value
        };
        ModSettingsManager.AddOption(new StepSliderOption(BaseDamage, baseDamageConfig));
        StepSliderConfig stackDamageConfig = new()
        {
            min = 0f, max = 40f, increment = 0.2f, FormatString = "{0:P0}",
            description = "The damage the explosion should do for every stack beyond the first.\n\nVanilla default is 280%, mod default is 700%",
            checkIfDisabled = () => !AdjustDamage.Value
        };
        ModSettingsManager.AddOption(new StepSliderOption(StackDamage, stackDamageConfig));

        if (GetSpriteFromFile("", "icon.png", out var sprite))
        {
            ModSettingsManager.SetModIcon(sprite);
        }

        On.RoR2.ItemCatalog.SetItemDefs += On_ItemCatalog_SetItemDefs;
        IL.RoR2.Items.JumpDamageStrikeBodyBehavior.UpdateCharge += IL_JumpDamageStrikeBodyBehavior_UpdateCharge;
        IL.RoR2.Items.JumpDamageStrikeBodyBehavior.DischargeEffects += IL_JumpDamageStrikeBodyBehavior_DischargeEffects;

        if (LookingGlassLoaded)
        {
            ItemCatalog.availability.CallWhenAvailable(LookingGlassInterop.UpdateStats);
        }
    }

    private static readonly string[] qualityTokens = ["", "_UNCOMMON", "_RARE", "_EPIC", "_LEGENDARY"];
    private void UpdateDesc(object sender, EventArgs e) => UpdateDesc();
    private void UpdateDesc()
    {
        int baseDamage = (int) (AdjustDamage.Value ? BaseDamage.Value * 100f : 400f);
        int stackDamage = (int) (AdjustDamage.Value ? StackDamage.Value * 100f : 280f);

        foreach (string quality in qualityTokens)
        {
            string baseToken = $"ITEM_JUMPDAMAGESTRIKE{quality}_DESC";
            string fmtToken = $"ITEM_JUMPDAMAGESTRIKE{quality}_DESC_FMT";

            string formattedStr = Language.GetStringFormatted(fmtToken, baseDamage, stackDamage);
            if (formattedStr != fmtToken) // make sure the formatted string actually exists before we clobber the base version
            {
                Language.currentLanguage.SetStringByToken(baseToken, formattedStr);
            }
        }

        if (LookingGlassLoaded && ItemCatalog.availability.available)
        {
            LookingGlassInterop.UpdateStats();
        }
    }

    // shoutouts to https://github.com/prodzpod/WhiteGuillotine/blob/main/WhiteGuillotine/Main.cs
    private void On_ItemCatalog_SetItemDefs(On.RoR2.ItemCatalog.orig_SetItemDefs orig, ItemDef[] newItemDefs)
    {
        DLC3Content.Items.JumpDamageStrike._itemTierDef = ItemTierCatalog.GetItemTierDef(ItemTier.Tier3);
        if (GetSpriteFromFile("icons", "texJumpDamageStrike_new.png", out Sprite sprite))
        {
            #pragma warning disable CS0618 // Type or member is obsolete
            DLC3Content.Items.JumpDamageStrike.pickupIconSprite = sprite;
            #pragma warning restore CS0618 // Type or member is obsolete
        }

        if (ItemQualitiesLoaded)
        {
            ItemQualitiesInterop.HandleQualityFaradaySpur();
        }

        orig(newItemDefs);
    }

    private void IL_JumpDamageStrikeBodyBehavior_UpdateCharge(ILContext il)
    {
        ILCursor cursor = new(il);
        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallOrCallvirt<CharacterBody>("get_moveSpeed")))
        {
            cursor.EmitDelegate(ModifyMoveSpeed);
        }
        else
        {
            Log.Error("Failed to match IL in JumpDamageStrikeBodyBehavior.UpdateCharge");
        }
    }

    private float ModifyMoveSpeed(float moveSpeed)
    {
        if (AdjustChargeSpeed.Value)
        {
            moveSpeed *= ChargeSpeedMult.Value;
        }
        return moveSpeed;
    }

    private void IL_JumpDamageStrikeBodyBehavior_DischargeEffects(ILContext il)
    {
        ILCursor cursor = new(il);
        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(4f)))
        {
            cursor.EmitDelegate(ModifyBaseDamage);
        }
        else
        {
            Log.Error("Failed to match base damage IL in JumpDamageStrikeBodyBehavior.DischargeEffects");
        }
        cursor.Index = 0;
        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(2.8f)))
        {
            cursor.EmitDelegate(ModifyStackDamage);
        }
        else
        {
            Log.Error("Failed to match stack damage IL in JumpDamageStrikeBodyBehavior.DischargeEffects");
        }
    }

    private float ModifyBaseDamage(float baseDamage)
    {
        if (AdjustDamage.Value)
        {
            return BaseDamage.Value;
        }
        return baseDamage;
    }

    private float ModifyStackDamage(float stackDamage)
    {
        if (AdjustDamage.Value)
        {
            return StackDamage.Value;
        }
        return stackDamage;
    }

    // loads a png and creates a sprite object out of it because i am not installing all of unity just for texture replacements
    // if someone wants to make + give me an assetbundle or whatever for these textures hmu
    internal static bool GetSpriteFromFile(string folder, string filename, out Sprite sprite)
    {
        sprite = null;
        // largely taken from https://github.com/Vl4dimyr/CaptainShotgunModes/blob/fdf828e/RiskOfOptionsMod.cs#L36-L48
        try
        {
            using Stream stream = File.OpenRead(Path.Combine(Path.GetDirectoryName(Instance.Info.Location), folder, filename));
            Texture2D texture = new Texture2D(0, 0);
            byte[] imgData = new byte[stream.Length];

            stream.Read(imgData, 0, (int)stream.Length);

            if (ImageConversion.LoadImage(texture, imgData))
            {
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
                return true;
            }
            return false;
        }
        catch (FileNotFoundException)
        {
            Log.Error($"file {Path.Combine(folder, filename)} not found");
            return false;
        }
    }
}
