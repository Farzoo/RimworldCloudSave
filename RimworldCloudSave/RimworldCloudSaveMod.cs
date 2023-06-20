using System.IO;
using UnityEngine;

namespace RimworldCloudSave;

[StaticConstructorOnStartup]
public class RimworldCloudSaveMod : Mod
{

    public static RimworldCloudSaveMod? Instance { get; private set; }

    public ICloudStorageService CloudService { get; }
    public FileSystemWatcher FileWatcher { get; }
    public RimCloudSaveSettings Settings { get; }
    
    public RimworldCloudSaveMod(ModContentPack content) : base(content)
    {
        Log.Message($"[RimworldCloudSaveMod] Runtime version: {Application.platform} {Application.unityVersion} {Application.version}");
        
        this.Settings = this.GetSettings<RimCloudSaveSettings>();
        
        CloudService = new SteamRemoteStorageService();
        
        String path = Path.Combine(GenFilePaths.SaveDataFolderPath, this.SettingsCategory());
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        FileWatcher = new FileSystemWatcher(path);
        FileWatcher.EnableRaisingEvents = true;

        List<string> excludedFilesPaths = new List<string>();
        List<string> excludedFoldersPaths = new List<string>();

        VirtualTreeItem<string> excludedItemsRoot = new VirtualTreeItem<string>("");

        var steamSyncService = new SteamSyncService(FileWatcher, CloudService, excludedFilesPaths, excludedFoldersPaths, excludedItemsRoot);
        
        RimworldCloudSaveMod.Instance = this;
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listingStandard = new Listing_Standard();
        listingStandard.Begin(inRect);

        this.Settings.CloudServiceName = listingStandard.TextEntryLabeled(nameof(this.Settings.CloudServiceName), this.Settings.CloudServiceName, 2);
            
        listingStandard.End();
        base.DoSettingsWindowContents(inRect);
    }

    public sealed override string SettingsCategory() => "RimCloudSave";
}