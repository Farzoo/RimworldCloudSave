using UnityEngine;

namespace RimworldCloudSave;

public class FolderViewer
{
    private Vector2 _scrollPosition;

    private VirtualTreeItem<string> CurrentFolder { get; set; }
    
    private Action<VirtualTreeItem<string>> OnItemMove { get; set; }

    public FolderViewer(VirtualTreeItem<string> rootFolder, Action<VirtualTreeItem<string>> onItemMove)
    {
        this.CurrentFolder = rootFolder;
        this.OnItemMove = onItemMove;
    }

    public void Draw(Rect rect)
    {
        var outRect = new Rect(rect.x, rect.y, rect.width - 16f, rect.height);
        var viewRect = new Rect(0f, 0f, rect.width - 16f, (CurrentFolder.Children.Count + 1) * 22f);

        Widgets.BeginScrollView(outRect, ref _scrollPosition, viewRect);
        
        var itemRect = new Rect(0f, 0f, viewRect.width, 22f);
        if(this.CurrentFolder.Parent is not null && Widgets.ButtonText(itemRect, ".."))
        {
            this.CurrentFolder = this.CurrentFolder.Parent;
        }
        
        var y = 22f;
        var moveButtonWidth = 50f; // Define the width of the move button here

        foreach (VirtualTreeItem<string> child in this.CurrentFolder.Children)
        {
            itemRect = new Rect(0f, y, viewRect.width - moveButtonWidth, 22f);
            
            if (Widgets.ButtonText(itemRect, child.Id))
            {
                this.CurrentFolder = child;
                break;
            }

            // Draw the move button aligned to the right
            var moveButtonRect = new Rect(viewRect.width - moveButtonWidth, y, moveButtonWidth, 22f);
            if (Widgets.ButtonText(moveButtonRect, "Move"))
            {
                this.OnItemMove(child);
                break;
            }
            
            y += 22f;
        }
        
        Widgets.EndScrollView();
    }
}