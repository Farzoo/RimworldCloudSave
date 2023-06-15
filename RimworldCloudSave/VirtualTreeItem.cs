using System.Text;

namespace RimworldCloudSave;

public class VirtualTreeItem<T> where T : IEquatable<T>, IComparable<T>
{
    public T Id;
    
    public readonly VirtualTreeItem<T>? Parent;
    
    private readonly HashSet<T> _childrenIds;
    
    private readonly HashSet<VirtualTreeItem<T>> _children;
    
    public IReadOnlyCollection<VirtualTreeItem<T>> Children => this._children;

    public VirtualTreeItem(T id) : this(id, default){}

    private VirtualTreeItem(T id, VirtualTreeItem<T>? parent)
    {
        this.Id = id;
        this.Parent = parent;
        this._childrenIds = new HashSet<T>();
        this._children = new HashSet<VirtualTreeItem<T>>();
    }

    public VirtualTreeItem<T> CreateAndAddChild(T id)
    {
        var child = new VirtualTreeItem<T>(id, this);
        
        if(this._childrenIds.Add(id) && this._children.Add(child))
        {
            return child;
        }
        
        this._children.TryGetValue(child, out var existingChild);
        
        return existingChild;
    }
    
    public VirtualTreeItem<T>? RemoveChild(VirtualTreeItem<T> child)
    {
        return this._childrenIds.Remove(child.Id) && this._children.Remove(child) ? child : null;
    }

    public VirtualTreeItem<T>? FindChild(Func<VirtualTreeItem<T>, bool> foundPredicate, Func<VirtualTreeItem<T>, bool> searchPredicate)
    {
        var stack = new Stack<VirtualTreeItem<T>>();
        stack.Push(this);
        
        while (stack.Count > 0)
        {
            var node = stack.Pop();

            if (foundPredicate(node))
            {
                return node;
            }

            foreach (var child in node._children)
            {
                if(searchPredicate(child)) stack.Push(child);
            }
        }
        
        return null;

    }
    
    public string PrettyFormatting()
    {
        var sb = new StringBuilder();
        this.PrettyFormatting(sb, "", false);
        return sb.ToString();
    }

    private void PrettyFormatting(StringBuilder sb, string prefix, bool isTail)
    {
        sb.Append(prefix + (isTail ? "└── " : "├── ") + this.Id + "\n");
        var childPrefix = prefix + (isTail ? "    " : "│   ");
        var lastChild = this._children.LastOrDefault();
        foreach (var child in this._children)
        {
            child.PrettyFormatting(sb, childPrefix, child == lastChild);
        }
    }
}