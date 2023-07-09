using HarmonyLib;
using UnityEngine;

namespace RimworldCloudSave;

[HarmonyPatch]
public static class Patch_DoMainMenuControls
{
    
    private static VirtualTreeItem<string> testSyncFiles = new VirtualTreeItem<string>("Rimworld By Ludeon Studios");
    private static VirtualTreeItem<string> testExcludedFiles = new VirtualTreeItem<string>("Rimworld By Ludeon Studios");

    static Patch_DoMainMenuControls()
    {
        testSyncFiles.CreateAndAddChild("Config");
        testSyncFiles.CreateAndAddChild("Data");
        testSyncFiles.CreateAndAddChild("Mods").CreateAndAddChild("Core");

        var saves = testExcludedFiles.CreateAndAddChild("Saves");
        testExcludedFiles.CreateAndAddChild("Config").CreateAndAddChild("LocalConfig");
        testExcludedFiles.CreateAndAddChild("Data").CreateAndAddChild("LocalData");

        for (int i = 0; i < 50; i++)
        {
            saves.CreateAndAddChild($"save{i}.rws");
        }
    }


    [HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoMainMenuControls))]
    [HarmonyPostfix]
    public static void Postfix(Rect rect, bool anyMapFiles)
    {
        var buttonRect = new Rect(rect.x, rect.y-35-7, 200, 35); // set position and size of the button here

        var steamSyncService = RimworldCloudSaveMod.Instance!.SteamSyncService;
        
        if (Widgets.ButtonText(buttonRect, "RimCloudSave")) // set the label of the button here
        {
            Find.WindowStack.Add(new Dialog_SyncFolders(testExcludedFiles, testSyncFiles));
        }
    }
    
}