using UnityEngine;

namespace RimworldCloudSave;

public class Dialog_SyncFolders : Window
{
    private readonly VirtualTreeItem<string> ExcludeItemsRoot;
    private readonly VirtualTreeItem<string> SyncItemsRoot;
    
    private readonly FolderViewer SyncFolderViewer;
    private readonly FolderViewer ExcludeFolderViewer;
    
    public Dialog_SyncFolders(VirtualTreeItem<string> excludeItemsRoot, VirtualTreeItem<string> syncItemsRoot)
    {
        this.ExcludeItemsRoot = excludeItemsRoot;
        this.SyncItemsRoot = syncItemsRoot;

        this.SyncFolderViewer = new FolderViewer(this.SyncItemsRoot, item => OnItemMove(this.SyncItemsRoot, this.ExcludeItemsRoot, item));
        this.ExcludeFolderViewer = new FolderViewer(this.ExcludeItemsRoot, item => OnItemMove(this.ExcludeItemsRoot, this.SyncItemsRoot, item));
    }

    private static void OnItemMove(VirtualTreeItem<string> source, VirtualTreeItem<string> destination, VirtualTreeItem<string> itemToMove)
    {
        Stack<string> pathToRoot = new Stack<string>();
        var current = itemToMove;
        while (current.Parent is not null)
        {
            pathToRoot.Push(current.Id);
            current = current.Parent;
        }

        var newItem = destination.BuildTreeFromNodesPath(pathToRoot);
        itemToMove.CopySubTree(newItem);
        source.RemoveChild(itemToMove);
    }

    public override void DoWindowContents(Rect inRect)
    {
        var leftHalf = new Rect(inRect.x, inRect.y, inRect.width / 2, inRect.height);
        var rightHalf = new Rect(inRect.x + inRect.width / 2, inRect.y, inRect.width / 2, inRect.height);
        
        this.SyncFolderViewer.Draw(rightHalf);
        this.ExcludeFolderViewer.Draw(leftHalf);
    }
}