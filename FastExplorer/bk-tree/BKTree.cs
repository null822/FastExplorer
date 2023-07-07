using System.Collections.Generic;


/*
 * This class is an implementation of a Burkhard-Keller tree.
 * The BK-Tree is a tree structure used to quickly find close matches to
 * any defined object.
 *
 * The BK-Tree was first described in the paper:
 * "Some Approaches to Best-Match File Searching" by W. A. Burkhard and R. M. Keller
 * It is available in the ACM archives.
 *
 * Another good explanation can be found here:
 * http://blog.notdot.net/2007/4/Damn-Cool-Algorithms-Part-1-BK-Trees
 *
 * Searching the tree yields O(logn), which is a huge upgrade over brute force.
 *
 * The original author of this code in Java is Josh Clemm
 * (The preceding comment block is his with a handful of edits)
 * http://code.google.com/p/java-bk-tree
 *
 * Ported to C# with generic tree nodes + three example distance metrics
 * by Mike Karlesky.
 */

namespace FastExplorer.bk_tree;

public class BKTree
{
    private readonly Dictionary<BKTreeNode, int> _matches = new();
    private volatile BKTreeNode _root = new("");

    public void Add(BKTreeNode node)
    {
        if (_root != null)
            _root.Add(node);
        else
            _root = node;
    }

    /**
     * This method will find all the close matching Nodes within
     * a certain threshold.  For instance, to search for similar
     * strings, threshold set to 1 will return all the strings that
     * are off by 1 edit distance.
     * @param searchNode
     * @param threshold
     * @return
     */
    public Dictionary<BKTreeNode, int> Query(BKTreeNode searchNode, int threshold)
    {
        var matches = new Dictionary<BKTreeNode, int>();

        _root.Query(searchNode, threshold, matches);

        return CopyMatches(matches);
    }

    /**
     * Attempts to find the closest match to the search node.
     * @param node
     * @return The edit distance of the best match
     */
    public int FindBestDistance(BKTreeNode node)
    {
        BKTreeNode bestNode;
        return _root.FindBestMatch(node, int.MaxValue, out bestNode);
    }

    /**
     * Attempts to find the closest match to the search node.
     * @param node
     * @return A match that is within the best edit distance of the search node.
     */
    public BKTreeNode FindBestNode(BKTreeNode node)
    {
        _root.FindBestMatch(node, int.MaxValue, out var bestNode);
        return (BKTreeNode)bestNode;
    }

    /**
     * Attempts to find the closest match to the search node.
     * @param node
     * @return A match that is within the best edit distance of the search node.
     */
    public Dictionary<BKTreeNode, int> FindBestNodeWithDistance(BKTreeNode node)
    {
        var distance = _root.FindBestMatch(node, int.MaxValue, out var bestNode);
        _matches.Clear();
        _matches.Add(bestNode, distance);
        return _matches;
    }

    private Dictionary<BKTreeNode, int> CopyMatches(Dictionary<BKTreeNode, int> source)
    {
        _matches.Clear();

        foreach (var pair in source) _matches.Add((BKTreeNode)pair.Key, pair.Value);

        return _matches;
    }


    public BKTreeNode GetRoot()
    {
        return _root;
    }
}
