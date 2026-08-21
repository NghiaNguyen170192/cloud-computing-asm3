namespace NetCore.Donation.Migration.Extensions;

public static class Sort
{
    public static IEnumerable<TNode> TopologicalSort<TNode>(this IEnumerable<TNode> nodes, Func<TNode, IEnumerable<Type>> dependencySelector)
    {
        var elements = nodes.ToDictionary(node => node, node => new HashSet<Type>(dependencySelector(node)));
        var independentNodes = new Queue<TNode>(elements.Where(x => x.Value.Count == 0).Select(x => x.Key));

        while (independentNodes.Count > 0)
        {
            var node = independentNodes.Dequeue();
            yield return node;
            elements.Remove(node);

            foreach (var kvp in elements)
            {
                kvp.Value.Remove(node.GetType());
                if (kvp.Value.Count == 0)
                {
                    independentNodes.Enqueue(kvp.Key);
                }
            }
        }

        if (elements.Count > 0)
        {
            throw new ArgumentException($"Circular dependency detected among: {string.Join(", ", elements.Keys.Select(x => x.GetType().Name))}");
        }
    }
}