using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using static FastExplorer.MainWindow;

namespace FastExplorer.bk_tree;

public class BKTreeNode
{
    private volatile Dictionary<int, BKTreeNode> _children;
    public string Data { get; }

    public BKTreeNode(string values)
    {
        Data = values;
        _children = new Dictionary<int, BKTreeNode>(1);
    }


    private int CalculateDistance(BKTreeNode node)
    {
        var source = Path.GetFileName(Data).ToLower();
        var target = Path.GetFileName(node.Data).ToLower();

        var distance = DistanceMetric.CalculateContainsPartial(source, target);

        return distance;
    }

    // private static string CharGen(char input, int count)
    // {
    //     var result = new StringBuilder();
    //
    //     for (var i = 0; i < count; i++) result.Append(input);
    //
    //     return result.ToString();
    // }
    //
    // public Dictionary<int, BKTreeNode> GetChildNodes()
    // {
    //     return _children;
    // }

    public void Add(BKTreeNode node)
    {
        var distance = CalculateDistance(node);
        
        if (_children.TryGetValue(distance, out var value))
            SubTasks.Add(Task.Run(() =>
                value.Add(node)
            ));
        else
        {
            SubTasks.Add(Task.Run(() => {
                try
                {
                    _children.Add(distance, node);
                }
                catch
                {
                    Add(node);
                }
            }));
        }
    }

    public virtual int FindBestMatch(BKTreeNode node, int bestDistance, out BKTreeNode bestNode)
    {
        var distanceAtNode = CalculateDistance(node);

        bestNode = node;

        if (distanceAtNode < bestDistance)
        {
            bestDistance = distanceAtNode;
            bestNode = this;
        }

        foreach (var distance in _children.Keys)
            if (distance < distanceAtNode + bestDistance)
            {
                var possibleBest = _children[distance].FindBestMatch(node, bestDistance, out bestNode);
                if (possibleBest < bestDistance) bestDistance = possibleBest;
            }

        return bestDistance;
    }

    public virtual void Query(BKTreeNode node, int threshold, Dictionary<BKTreeNode, int> collected)
    {
        var distanceAtNode = CalculateDistance(node);

        if (distanceAtNode == threshold)
        {
            collected.Add(this, distanceAtNode);
            return;
        }

        if (distanceAtNode < threshold) collected.Add(this, distanceAtNode);

        for (var distance = distanceAtNode - threshold; distance <= threshold + distanceAtNode; distance++)
            if (_children.TryGetValue(distance, out var value))
                value.Query(node, threshold, collected);
    }
    
    public JObject ToJson()
    {
        if (DebugCount >= 4096)
        {
            DebugCount = 0;
            Console.WriteLine("Creating BK Tree (4096ly update) | " + Progress + " / " + GetTotalPaths() + " | " +
                              Math.Round((float)Progress / GetTotalPaths() * 100) + "%");
        }
        
        Progress++;
        DebugCount++;
        
        var node = new JObject();

        foreach (var child in _children)
        {
            JObject? childJson = null;

            Task task = Task.Run(() =>
                childJson = child.Value.ToJson()
            );

            WaitForTask(task, 10);
            
            var jsonChildren = new JObject { { "key", child.Key }, { "children", childJson } };

            node.Add(child.Value.Data, jsonChildren);
        }
        
        return node;
    }

    public void FromJson(JObject? tree)
    {
        if (tree == null) return;

        foreach (var child in tree)
        {
            var node = new BKTreeNode(child.Key);

            Task task = Task.Run(() =>
                node.FromJson(child.Value["children"].Value<JObject>())
            );
            while (true)
            {
                if (task.Status != TaskStatus.Running) break;
                Thread.Sleep(10);
            }
            
            _children.Add(child.Value["key"].Value<int>(), node);
        }
    }
}
