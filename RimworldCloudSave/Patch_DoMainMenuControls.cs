using HarmonyLib;
using UnityEngine;

namespace RimworldCloudSave;

[HarmonyPatch]
public class Patch_DoMainMenuControls
{
    
    [HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoMainMenuControls))]
    [HarmonyPostfix]
    public static void Postfix(Rect rect, bool anyMapFiles)
    {
        var buttonRect = new Rect(rect.x, rect.y-35-7, 200, 35); // set position and size of the button here

        if (Widgets.ButtonText(buttonRect, "RimCloudSave")) // set the label of the button here
        {
            var list = RimworldCloudSaveMod.CloudService.ListFilesAsync().Result;
            var list2 = list.Select(s => RimworldCloudSaveMod.CloudService.GetFileMetadataAsync(s)).ToList();
            // Print the names of all files in the cloud on a window
            // Combine the two lists
            var list3 = list.Zip(list2, (s, fileMetadata) => $"{s} {fileMetadata.Result.FileSize} - {fileMetadata.Result.LastModified}");
            var window = new Dialog_MessageBox(list3.Aggregate("", (s, s1) => s + "\n" + s1));
            Find.WindowStack.Add(window);
        }
    }
    
}