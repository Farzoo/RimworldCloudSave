using System.IO;
using HarmonyLib;
using UnityEngine;

namespace RimworldCloudSave;

[StaticConstructorOnStartup]
public class RimworldCloudSaveMod : Mod
{
    
    public static ICloudStorageService CloudService;
    public static FileSystemWatcher FileWatcher;

    static RimworldCloudSaveMod()
    {
        try
        {
            // Log runtime version
            Log.Message($"[RimworldCloudSaveMod] Runtime version: {Application.platform} {Application.unityVersion} {Application.version}");
            
            CloudService = new SteamRemoteStorageService();
            CloudService.DeleteFileAsync("Test.txt").Wait();

            FileWatcher = new FileSystemWatcher(GenFilePaths.SaveDataFolderPath);
            FileWatcher.EnableRaisingEvents = true;

            List<string> excludedFilesPaths = new List<string>();
            List<string> excludedFoldersPaths = new List<string>();

            excludedFoldersPaths.Add(Path.Combine(FileWatcher.Path, "Config", "Config0"));

            VirtualTreeItem<string> excludedItemsRoot = new VirtualTreeItem<string>("");

            var steamSyncService = new SteamSyncService(FileWatcher, CloudService, excludedFilesPaths, excludedFoldersPaths, excludedItemsRoot);

            /*FileWatcher.Changed += (sender, args) =>
            {
                Log.Message($"Changed file {args.FullPath}");
            };*/

            // Inject a new button in the main menu

            var harmony = new Harmony("RimworldCloudSave");
            harmony.PatchAll();
        } 
        catch (Exception e)
        {
            Log.Error($"[RimworldCloudSaveMod] failed to load {e}");
        }
            

        /*FileWatcher.Changed += (sender, args) =>
        {
            string fileName = Path.GetFileName(args.FullPath);
            string cloudFileName = $"save_{fileName}";
            // byte[] fileData = File.ReadAllBytes(args.FullPath);
            
            Log.Message($"Changed file {fileName}");
        };
        
        FileWatcher.Created += (sender, args) =>
        {
            string fileName = Path.GetFileName(args.FullPath);
            string cloudFileName = $"save_{fileName}";
            // byte[] fileData = File.ReadAllBytes(args.FullPath);
            
            Log.Message($"Created file {fileName}");
        };

        FileWatcher.Deleted += (sender, args) =>
        {
            string fileName = Path.GetFileName(args.FullPath);
            // string cloudFileName = $"save_{fileName}";
            Log.Message($"Deleted file {fileName}");
        };
        
        FileWatcher.Renamed += (sender, args) =>
        {
            string oldFileName = Path.GetFileName(args.OldFullPath);
            string newFileName = Path.GetFileName(args.FullPath);
            //string oldCloudFileName = $"save_{oldFileName}";
            // string newCloudFileName = $"save_{newFileName}";
            Log.Message($"Renamed file {oldFileName} to {newFileName}");
        };
        
        VirtualTreeItem<string> root = new VirtualTreeItem<string>("Rimworld by Ludeon Studios");
        
        string save = root.Id + "/Saves/Test.rws";
        string save2 = root.Id + "/Saves/Test2.rws";
        
        string config = root.Id + "/Config/Config.xml";
        string modsConfig = root.Id + "/Config/ModsConfig.xml";
        
        string mods = root.Id + "/Mods/Mod1/Mod1.dll";
        
        Queue<string> savePath = new Queue<string>(save.Split('/'));
        Queue<string> save2Path = new Queue<string>(save2.Split('/'));
        Queue<string> configPath = new Queue<string>(config.Split('/'));
        Queue<string> modsConfigPath = new Queue<string>(modsConfig.Split('/'));
        Queue<string> modsPath = new Queue<string>(mods.Split('/'));

        root.BuildTreeFromNodesPath(savePath);
        root.BuildTreeFromNodesPath(save2Path);
        root.BuildTreeFromNodesPath(configPath);
        root.BuildTreeFromNodesPath(modsConfigPath);
        root.BuildTreeFromNodesPath(modsPath);*/
        
        


    }


    public RimworldCloudSaveMod(ModContentPack content) : base(content)
    {

    }
}