namespace RimworldCloudSave;

public static class VirtualTreeItemExtensions
{
    
    public static IEnumerable<VirtualTreeItem<T>> GetSubTree<T>(this VirtualTreeItem<T> root) where T : IEquatable<T>, IComparable<T>
    {
        var stack = new Stack<VirtualTreeItem<T>>();
        stack.Push(root);

        List<VirtualTreeItem<T>> result = new List<VirtualTreeItem<T>>();
        
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            result.Add(node);
            foreach (var child in node.Children)
            {
                stack.Push(child);
            }
        }
        
        return result;
    }
    
    public static void CopySubTree<T>(this VirtualTreeItem<T> root, VirtualTreeItem<T> destination) where T : IEquatable<T>, IComparable<T>
    {
        var stack = new Stack<VirtualTreeItem<T>>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if(node != root)  destination.CreateAndAddChild(node.Id);
            foreach (var child in node.Children)
            {
                stack.Push(child);
            }
        }
    }
    
    public static VirtualTreeItem<T>? FindFromNodesPath<T>(this VirtualTreeItem<T> root, IEnumerable<T> ids) where T : IEquatable<T>, IComparable<T>
    {
        var current = root;

        foreach (var id in ids)
        {
            current = current.Children.FirstOrDefault(child => child.Id.Equals(id));

            if (current is default(VirtualTreeItem<T>)) return default(VirtualTreeItem<T>);
        }

        return current;
    }
    
    public static VirtualTreeItem<T>? FindFromNodesPath<T>(this VirtualTreeItem<T> root, IEnumerable<T> ids, Func<VirtualTreeItem<T>?, bool> foundPredicate) where T : IEquatable<T>, IComparable<T>
    {
        var current = root;

        foreach (var id in ids)
        {
            current = current.Children.FirstOrDefault(child => child.Id.Equals(id));
            
            if (foundPredicate(current)) return current;

            if (current is default(VirtualTreeItem<T>)) return default(VirtualTreeItem<T>);
        }

        return current;
    }
    
    
    public static VirtualTreeItem<T> BuildTreeFromNodesPath<T>(this VirtualTreeItem<T> root, IEnumerable<T> ids) where T : IEquatable<T>, IComparable<T>
    {
        VirtualTreeItem<T> current = root;

        foreach (var id in ids)
        {
            var child = current!.Children.FirstOrDefault(child => child.Id.Equals(id));

            if (child is default(VirtualTreeItem<T>))
            {
                child = current.CreateAndAddChild(id);
            }

            current = child;
        }

        return current;
    }
    
    public static VirtualTreeItem<T> ConditionalBuildTreeFromNodesPath<T>(this VirtualTreeItem<T> root, IEnumerable<T> ids, Func<VirtualTreeItem<T>, bool> PrehaltPredicate) where T : IEquatable<T>, IComparable<T>
    {
        VirtualTreeItem<T> current = root;

        foreach (var id in ids)
        {
            if (PrehaltPredicate(current)) return current;
            
            var child = current!.Children.FirstOrDefault(child => child.Id.Equals(id));


            if (child is default(VirtualTreeItem<T>))
            {
                child = current.CreateAndAddChild(id);
            }

            current = child;
        }

        return current;
    }
    
    public static VirtualTreeItem<T>? Find<T>(this VirtualTreeItem<T> root, Func<VirtualTreeItem<T>, bool> foundPredicate, Func<VirtualTreeItem<T>, bool> searchPredicate) where T : IEquatable<T>, IComparable<T>
    {
        var stack = new Stack<VirtualTreeItem<T>>();
        stack.Push(root);
        
        while (stack.Count > 0)
        {
            var node = stack.Pop();

            if (foundPredicate(node))
            {
                return node;
            }

            foreach (var child in node.Children)
            {
                if(searchPredicate(child)) stack.Push(child);
            }
        }
        
        return null;

    }
}