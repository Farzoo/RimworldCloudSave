using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MemoryCacheT;

namespace RimworldCloudSave;

public class SteamSyncService
{
    private FileSystemWatcher FileWatcher;
    private ICloudStorageService CloudService;
    
    // Leaves are the excluded files and folders
    private VirtualTreeItem<string> ExcludedItemsRoot;
    private VirtualTreeItem<string> WatchedNode;
    
    private VirtualTreeItem<string> RemoteItemsRoot;

    private Cache<string, FileSystemEventArgs> FileEventCache;
    private TimeSpan FileEventCacheExpiration => TimeSpan.FromMilliseconds(500);
    
    private Cache<string, RenamedEventArgs> FileRenamedEventCache;
    
    private string FolderName => Path.GetFileName(FileWatcher.Path);

    private string[] FolderPathSplitted;
    
    public SteamSyncService(FileSystemWatcher fileWatcher, ICloudStorageService cloudService, List<string> excludedFilesPaths, List<string> excludedFoldersPaths, VirtualTreeItem<string> excludedItemsRoot)
    {
        this.FileEventCache = new Cache<string, FileSystemEventArgs>(
            TimeSpan.FromTicks(this.FileEventCacheExpiration.Ticks / 10)
        );
        
        this.FileRenamedEventCache = new Cache<string, RenamedEventArgs>(
            TimeSpan.FromTicks(this.FileEventCacheExpiration.Ticks / 10)
        );

        this.FileWatcher = fileWatcher;
        this.CloudService = cloudService;
        this.FolderPathSplitted = fileWatcher.Path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        this.ExcludedItemsRoot = excludedItemsRoot;
        this.WatchedNode = this.ExcludedItemsRoot.CreateAndAddChild(Path.GetFileName(fileWatcher.Path));
        Log.Message($"Created WatchedNode for {Path.GetFileName(fileWatcher.Path)}");

        this.RemoteItemsRoot = new VirtualTreeItem<string>("");

        var remoteFiles = this.CloudService.ListFilesAsync().Result;
        foreach (var remoteFile in remoteFiles)
        {
            var nodes = this.ParseRemoteFilePath(remoteFile);
            this.RemoteItemsRoot.BuildTreeFromNodesPath(nodes);
        }
        
        Log.Message($"RemoteItemsRoot:\n{this.RemoteItemsRoot.PrettyFormatting()}");
        
        #region TreeBuilding
        
        /*List<string> invalidPaths = excludedFoldersPaths.Where(path => !Directory.Exists(path)).ToList();
        
        if (invalidPaths.Any())
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[RimworldCloudSave] Invalid excluded folders for {this.FolderName}:");
            foreach (var path in invalidPaths)
            {
                sb.AppendLine(path);
            }
            
            throw new ArgumentException(sb.ToString(), nameof(excludedFoldersPaths));
        }*/
        
        this.BuildFilterTreeFromExcludedPaths(excludedFoldersPaths);
        this.BuildFilterTreeFromExcludedPaths(excludedFilesPaths);
        #endregion
        
        this.FileWatcher.EnableRaisingEvents = true;
        this.FileWatcher.IncludeSubdirectories = true;
        
        static Func<TValue, ICacheItem<TValue>> CreateCacheItemFactory<TValue>(TimeSpan expiration, Action<TValue, DateTime> onExpire = default!, Action<TValue, DateTime> onRemoved = default!)
            => value => new SlidingExpirationCacheItem<TValue>(value, expiration){OnExpire = onExpire, OnRemove = onRemoved};
        
        this.FileWatcher.Changed += 
            (_, args) => this.OnFileEvent(args, CreateCacheItemFactory<FileSystemEventArgs>(this.FileEventCacheExpiration, this.OnFileChanged));
        
        this.FileWatcher.Created += 
            (_, args) => this.OnFileEvent(args, CreateCacheItemFactory<FileSystemEventArgs>(this.FileEventCacheExpiration, this.OnFileCreated));
        
        this.FileWatcher.Deleted += 
            (_, args) => this.OnFileEvent(args, CreateCacheItemFactory<FileSystemEventArgs>(this.FileEventCacheExpiration, this.OnFileDeleted));
        
        this.FileWatcher.Renamed += 
            (_, args) => this.OnFileRenamedEvent(args, CreateCacheItemFactory<RenamedEventArgs>(TimeSpan.FromTicks(this.FileEventCacheExpiration.Ticks * 10L), this.OnFileRenamed));
        

        Log.Message($"Created SteamSyncService for {fileWatcher.Path}");
        Log.Message($"Filter Tree for {fileWatcher.Path}: {this.ExcludedItemsRoot.PrettyFormatting()}");
    }

    private void OnFileEvent(FileSystemEventArgs e, Func<FileSystemEventArgs, ICacheItem<FileSystemEventArgs>> cacheItemFactory)
    {
        Log.Message($"Event {e.ChangeType} for {e.FullPath}");
        if (this.FileEventCache.TryRemoveAndAdd(e.FullPath, cacheItemFactory(e)))
        {
            lock(Log.Messages)
            {
                Log.Message($"TryRemoveAndAdd {e.ChangeType} event for {e.FullPath} to the cache");
            }
        }
    }
    
    private void OnFileRenamedEvent(RenamedEventArgs e, Func<RenamedEventArgs, ICacheItem<RenamedEventArgs>> cacheItemFactory)
    {
        Log.Message($"Event {e.ChangeType} for {e.FullPath}");
        if (this.FileRenamedEventCache.TryGetValue(e.OldFullPath, out RenamedEventArgs oldRenamedEventArgs))
        {
            this.FileRenamedEventCache.Remove(e.OldFullPath);
            e = new RenamedEventArgs(e.ChangeType, Path.GetDirectoryName(e.FullPath)!, e.Name, oldRenamedEventArgs.OldName);
        }
        
        this.FileRenamedEventCache.TryAdd(e.FullPath, cacheItemFactory(e));
        
    }
    
    private async void OnFileChanged(FileSystemEventArgs e, DateTime time)
    {
        if (Directory.Exists(e.FullPath)) return;
        Log.Message($"Changed {e.FullPath}");

        var nodes = this.ParseLocalFilePath(e.FullPath);
        
        VirtualTreeItem<string>? node = this.ExcludedItemsRoot.FindFromNodesPath(nodes, IsExcluded);
        
        if (node == default(VirtualTreeItem<string>))
        {
            string fileCloudName = string.Join(Path.AltDirectorySeparatorChar.ToString(), nodes);
            
            // File open stream
            using Stream fileStream = File.OpenRead(e.FullPath);

            await this.CloudService.WriteFileAsync(fileCloudName, fileStream);
            
            Log.Message($"Uploaded {e.FullPath}");
        }
    }

    private async void OnFileCreated(FileSystemEventArgs e, DateTime time)
    {
        if(Directory.Exists(e.FullPath)) return;

        var nodes = this.ParseLocalFilePath(e.FullPath);
        
        VirtualTreeItem<string>? node = this.ExcludedItemsRoot.FindFromNodesPath(nodes, IsExcluded);
        
        if (node == default(VirtualTreeItem<string>))
        {
            
            string fileCloudName = string.Join(Path.AltDirectorySeparatorChar.ToString(), nodes);
            
            using Stream fileStream = File.OpenRead(e.FullPath);
            
            var task = this.CloudService.WriteFileAsync(fileCloudName, fileStream);
            await task;

            if (task.IsCompletedSuccessfully)
            {
                this.RemoteItemsRoot.BuildTreeFromNodesPath(nodes);
            }
            
            Log.Message($"Uploaded {e.FullPath}");
        }
    }


    private async void OnFileDeleted(FileSystemEventArgs e, DateTime time)
    {
        IEnumerable<string> nodes = this.ParseLocalFilePath(e.FullPath);

        VirtualTreeItem<string>? node = this.ExcludedItemsRoot.FindFromNodesPath(nodes, IsExcluded);
        if (node != default(VirtualTreeItem<string>)) return;

        node = this.RemoteItemsRoot.FindFromNodesPath(nodes);
        if (node == default(VirtualTreeItem<string>)) return;

        var files = node.GetSubTree().ToList();

        Log.Message("Fichiers à supprimer :\n" + files.Select(file => file.Id).Aggregate((file1, file2) => file1 + "\n" + file2));
        List<Task> tasks = new List<Task>();
        Log.Message($"Count : {files.Count}");
        foreach (VirtualTreeItem<string>? file in files)
        {
            var fileCloudName = this.SerializeNodePath(file);
            Log.Message($"Deleting file {fileCloudName}");
            tasks.Add(this.CloudService.DeleteFileAsync(fileCloudName));
        }
        await Task.WhenAll(tasks);
        
        node.Parent?.RemoveChild(node);
    }

    private async void OnFileRenamed(RenamedEventArgs e, DateTime time)
    {
        IEnumerable<string> oldNodes = this.ParseLocalFilePath(e.OldFullPath);

        VirtualTreeItem<string>? node = this.ExcludedItemsRoot.FindFromNodesPath(oldNodes, IsExcluded);
        if (node == default(VirtualTreeItem<string>)) return;
        
        node = this.RemoteItemsRoot.FindFromNodesPath(oldNodes);
        if (node == default(VirtualTreeItem<string>)) return;
        
        
        var oldRemoteNames = node.GetSubTree().ToList().Select(this.SerializeNodePath).ToList();
        Log.Message("Fichiers à renommer :\n" + oldRemoteNames.Aggregate((file1, file2) => file1 + "\n" + file2));

        node.Id = e.Name;

        List<Task> tasks = new List<Task>();
        foreach (var oldRemoteName in oldRemoteNames)
        {
            var newRemoteName = this.SerializeNodePath(node);
            Log.Message($"Renaming file {oldRemoteName} to {newRemoteName}");
            tasks.Add(this.CloudService.RenameFileAsync(oldRemoteName, newRemoteName));
        }

        await Task.WhenAll(tasks);
    }

    private void BuildFilterTreeFromExcludedPaths(List<string> excludedPaths)
    {
        var sbInvalidFoldersPath = new StringBuilder();
        foreach (var path in excludedPaths)
        {
            var nodes = this.ParseLocalFilePath(path);

            bool isInvalid = false;
            int previousCount = this.WatchedNode.Children.Count;

            var fileNode = this.ExcludedItemsRoot.ConditionalBuildTreeFromNodesPath(nodes, node =>
            {
                if (!ReferenceEquals(this.WatchedNode, node) && node.Children.Count == 0 && previousCount == node.Parent!.Children.Count)
                {
                    Log.Message($"node {node.Id}, parent {node.Parent!.Id}");
                    Log.Message($"previous count: {previousCount} - current count: {node.Parent!.Children.Count}");
                    isInvalid = true;
                    return true;
                }
                previousCount = node.Children.Count;
                return false;
            });
            
            isInvalid = isInvalid || fileNode.Children.Count > 0;
            
            if (isInvalid)
            {
                sbInvalidFoldersPath.AppendLine(path);
            }
            Log.Message($"Serialized node {fileNode.Id} to {this.SerializeNodePath(fileNode)}");
        }

        Log.Message(this.WatchedNode.PrettyFormatting());
        
        if (sbInvalidFoldersPath.Length > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Invalid excluded items for {this.WatchedNode.Id}:");
            sb.Append(sbInvalidFoldersPath);

            sb.AppendLine($"These items are conflicting with other excluded items. e.g. if you exclude a folder, you can't exclude a file inside that folder or vice versa.");
            
            throw new ArgumentException(sb.ToString(), nameof(excludedPaths));
        }
    }

    private bool IsExcluded(VirtualTreeItem<string>? node) => node != this.WatchedNode && node is not null && node.Children.Count == 0;

    private IReadOnlyCollection<string> ParseLocalFilePath(string filePath)
    {
        string[] list = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        int i = 0;
        
        int length = Math.Min(this.FolderPathSplitted.Length, list.Length);
        
        while(i < length && this.FolderPathSplitted[i] == list[i])
        {
            i++;
        }

        i--;
        
        if(this.FolderPathSplitted[i] != list[i])
        {
            throw new ArgumentException($"The path {filePath} is not a child of {this.FileWatcher.Path}", nameof(filePath));
        }
        
        return new ArraySegment<string>(list, i, list.Length - i);
    }
    
    private IReadOnlyCollection<string> ParseRemoteFilePath(string filePath) => filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private string SerializeNodePath(VirtualTreeItem<string> node)
    {
        var list = new List<string>();

        VirtualTreeItem<string> currentNode = node;

        while (currentNode.Parent is not default(VirtualTreeItem<string>))
        {
            list.Add(currentNode.Id);
            currentNode = currentNode.Parent!;
        }
        
        StringBuilder sb = new StringBuilder();
        for(int i = list.Count - 1; i > 0; i--)
        {
            sb.Append(list[i]);
            sb.Append(Path.AltDirectorySeparatorChar);
        }
        sb.Append(list[0]);
        
        return sb.ToString();
    }

    private class RenamedEventArgsForHashSearch : RenamedEventArgs
    {
        public RenamedEventArgsForHashSearch(WatcherChangeTypes changeType, string directory, string name,
            string oldName) : base(changeType, directory, name, oldName)
        {
        }

        // override hashcode 
        public override int GetHashCode()
        {
            return this.FullPath.GetHashCode();
        }
    }
}