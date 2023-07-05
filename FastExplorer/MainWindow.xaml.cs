using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FastExplorer.bk_tree;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Path = System.IO.Path;

namespace FastExplorer;

public partial class MainWindow : Window
{

    private static volatile List<string> _paths = new();

    private static volatile BKTree _tree = new();
    private static long _totalPaths;

    private static string prevQuery = "";

    private static int maxLength;
    private static int stackSize = 1024;

    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void BackgroundMain()
    {
        Directory.CreateDirectory("C:/ProgramData/FastExplorer");

        if (File.Exists("C:/ProgramData/FastExplorer/cache.json"))
            LoadCache();
        else
            GenCache();
        
        
        Console.WriteLine("Ready!");
        
        while (true)
        {
            string query = "";

            // get query
            Dispatcher.Invoke(() => query = TextBox.Text);

            if (query != prevQuery)
            {
                
                // query the tree                
                Dictionary<BKTreeNode, int> results = _tree.query(new BKTreeNode(query), 50);
                
                // sort the results
                List<string> sortedResults = new List<string>();

                foreach (var result in results)
                {
                    int diff = result.Value - sortedResults.Count;

                    if (diff < 0)
                    {
                        sortedResults[result.Value] = result.Key.Data;
                        continue;
                    }

                    for (int i = 0; i <= diff; i++)
                    {
                        sortedResults.Add("");
                    }
                    sortedResults[result.Value] = result.Key.Data;
                }
                
                // put the results in a string
                StringBuilder resultStringBuilder = new StringBuilder();
                
                foreach (var node in sortedResults)
                {
                    if (node == null || node == "") continue;
                    resultStringBuilder.Append(node).Append('\n');
                }
                
                string resultString = resultStringBuilder.ToString();
                
                // display the results
                Dispatcher.Invoke(() => TextBlock.Text = resultString);
                prevQuery = query;
            }

            Thread.Sleep(10);

        }
    }
    
    
    private static void GenCache()
    {
        Console.WriteLine("Searching...");

        DirectorySearch("C:/Users/noahk/Documents", stackSize);

        _totalPaths = _paths.Count;
        
        Console.WriteLine("Done Searching!");
        Console.WriteLine("Total Paths: " + _totalPaths);
        Console.WriteLine("Max File Name Length: " + maxLength);
        Console.WriteLine("Creating BK Tree...");


        long prog = 0;
        int maxStack = 0;
        foreach (var path in _paths.ToArray())
        {
            maxStack--;
            if (maxStack <= 0)
            {
                Task task = Task.Run(() =>
                    _tree.add(new BKTreeNode(path.Replace("\\\\", "/").Replace("\\", "/")))
                );

                
                maxStack = stackSize;
            }
            else
            {
                _tree.add(new BKTreeNode(path.Replace("\\\\", "/").Replace("\\", "/")));
            }

            prog++;
            if (maxStack <= 0)
            {
                Console.WriteLine(prog + " / " + _totalPaths);
                maxStack = 0;
            }
        }
        
        Console.WriteLine("Done Creating BK Tree!");

        SaveCache();
    }
    
    private static void LoadCache()
    {
        FileStream cacheStream = File.OpenRead("C:/ProgramData/FastExplorer/cache.json");
        
        Span<byte> jsonBytes = new Span<byte>(new byte[cacheStream.Length]);
        
        cacheStream.Read(jsonBytes);
        
        Console.WriteLine(jsonBytes.Length);

        
        JObject? jsonObject = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(jsonBytes), new JsonSerializerSettings
        {
            MaxDepth = 1024
        });
        
        _tree.getRoot().FromJson(jsonObject);

        
        cacheStream.Dispose();
        
        
        Console.WriteLine("Done Loading BK Tree!");

        SaveCache();
    }

    private static void SaveCache()
    {
        Console.WriteLine("Saving BK Tree...");
        
        JObject treeJson = _tree.getRoot().ToJson();
        FileStream cacheStream = File.OpenWrite("C:/ProgramData/FastExplorer/cache.json");
        
        cacheStream.SetLength(0);
        cacheStream.Flush();
        
        JsonWriter cacheJson = new JsonTextWriter(new StreamWriter(cacheStream));
        treeJson.WriteTo(cacheJson);
        
        cacheJson.Close();
        cacheStream.Close();
        
        Console.WriteLine("Done Saving BK Tree!");
    }
    
    private static void DirectorySearch(string dir, int maxStack)
    {
        maxStack--;
        try
        {
            foreach (string f in Directory.GetFiles(dir))
            {
                _paths.Add(Path.GetFullPath(f));

                if (Path.GetFileName(f).Length > maxLength)
                    maxLength = Path.GetFileName(f).Length;
            }

            foreach (string d in Directory.GetDirectories(dir))
            {
                _paths.Add(Path.GetFullPath(d));

                if (maxStack <= 0) {
                    Task task = Task.Run(() => DirectorySearch(d, stackSize));
                    task.Start();
                } else
                    DirectorySearch(d, maxStack - 1);
            }
        }
        catch (Exception) {}

    }
    
    private void Root_OnLoaded(object sender, RoutedEventArgs e)
    {
        Thread thread = new Thread(BackgroundMain);
        thread.Start();


    }
}