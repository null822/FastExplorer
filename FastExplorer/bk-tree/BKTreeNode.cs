using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace FastExplorer.bk_tree;

[Serializable]
public class BKTreeNode
{
    public volatile Dictionary<int, BKTreeNode> _children;
    public string Data { get; private set; } // String of symbols
    
    
    public BKTreeNode(string values)
    {
        Data = values;
        _children = new Dictionary<int, BKTreeNode>(1);
    }
    
    protected int CalculateDistance(BKTreeNode node)
    {
        string source = Path.GetFileName(Data).ToLower();
        string target = Path.GetFileName(node.Data).ToLower();
        
        int distance = DistanceMetric.CalculateContainsPartial(source, target);
        
        if (source == "findme.txt")
            Console.WriteLine("dist. to \"FindMe.txt\": " + distance);
        
        return distance;
    }

    private static string CharGen(char input, int count)
    {
        StringBuilder result = new StringBuilder();
        
        for (int i = 0; i < count; i++)
        {
            result.Append(input);
        }

        return result.ToString();
    }
    
    public Dictionary<int, BKTreeNode> GetChildNodes()
    {
        return _children;
    }
        
    public virtual void Add(BKTreeNode node)
    {
        int distance = CalculateDistance(node);

        if (_children.ContainsKey(distance))
        {
            _children[distance].Add(node);
        }
        else
        {
            _children.Add(distance, node);
        }
    }

    public virtual int FindBestMatch(BKTreeNode node, int bestDistance, out BKTreeNode bestNode)
    {
        int distanceAtNode = CalculateDistance(node);

        bestNode = node;

        if(distanceAtNode < bestDistance)
        {
            bestDistance = distanceAtNode;
            bestNode = this;
        }

        foreach (int distance in _children.Keys)
        {
            if (distance < distanceAtNode + bestDistance)
            {
                int possibleBest = _children[distance].FindBestMatch(node, bestDistance, out bestNode);
                if (possibleBest < bestDistance)
                {
                    bestDistance = possibleBest;
                }
            }
        }

        return bestDistance;
    }

    public virtual void Query(BKTreeNode node, int threshold, Dictionary<BKTreeNode, int> collected)
    {
        int distanceAtNode = CalculateDistance(node);

        if (distanceAtNode == threshold)
        {
            collected.Add(this, distanceAtNode);
            return;
        }

        if (distanceAtNode < threshold)
        {
            collected.Add(this, distanceAtNode);
        }

        for (int distance = (distanceAtNode - threshold); distance <= (threshold + distanceAtNode); distance++)
        {
            if (_children.ContainsKey(distance))
            {
                _children[distance].Query(node, threshold, collected);
            }
        }
    }
    
    public JObject ToJson()
    {
        JObject node = new JObject();

        foreach (var child in _children)
        {
            JObject jsonChildren = new JObject { { "key", child.Key }, { "children", child.Value.ToJson()} };
            
            node.Add(child.Value.Data, jsonChildren);
        }
        
        return node;
    }

    public void FromJson(JObject? tree)
    {
        if (tree == null) return;
        
        foreach (var child in tree)
        {
            BKTreeNode node = new BKTreeNode(child.Key);
            
            node.FromJson(child.Value["children"].Value<JObject>());
            
            _children.Add(child.Value["key"].Value<int>(), node);
        }
        
    }
    
}
