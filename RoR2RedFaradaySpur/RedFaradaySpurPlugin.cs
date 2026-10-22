using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using System.IO;
using UnityEngine;

namespace RoR2RedFaradaySpur;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency("com.Gorakh.ItemQualities", BepInDependency.DependencyFlags.SoftDependency)]
public class RedFaradaySpurPlugin : BaseUnityPlugin
{
    internal static RedFaradaySpurPlugin Instance;

    public const string PluginGUID = PluginAuthor + "." + PluginName;
    public const string PluginAuthor = "SSM24";
    public const string PluginName = "RedFaradaySpur";
    public const string PluginVersion = "1.0.0";

    internal bool ItemQualitiesLoaded = false;

    public void Awake()
    {
        Instance = this;
        Log.Init(Logger);

        ItemQualitiesLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.Gorakh.ItemQualities");

        On.RoR2.ItemCatalog.SetItemDefs += On_ItemCatalog_SetItemDefs;
    }

    // shoutouts to https://github.com/prodzpod/WhiteGuillotine/blob/main/WhiteGuillotine/Main.cs
    private void On_ItemCatalog_SetItemDefs(On.RoR2.ItemCatalog.orig_SetItemDefs orig, ItemDef[] newItemDefs)
    {
        DLC3Content.Items.JumpDamageStrike._itemTierDef = ItemTierCatalog.GetItemTierDef(ItemTier.Tier3);
        if (GetSpriteFromFile("texJumpDamageStrike_new.png", out Sprite sprite))
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

    // loads a png and creates a sprite object out of it because i am not installing all of unity just for texture replacements
    // if someone wants to make + give me an assetbundle or whatever for these textures hmu
    internal static bool GetSpriteFromFile(string filename, out Sprite sprite)
    {
        sprite = null;
        // largely taken from https://github.com/Vl4dimyr/CaptainShotgunModes/blob/fdf828e/RiskOfOptionsMod.cs#L36-L48
        try
        {
            using Stream stream = File.OpenRead(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Instance.Info.Location), "icons", filename));
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
            Log.Error($"file {filename} not found");
            return false;
        }
    }
}
