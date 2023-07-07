using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FastExplorer.bk_tree;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FastExplorer;

public partial class MainWindow
{

    public const bool Debug = false;

    private const string Path = "C:/Users/noahk";
    private static volatile List<string> _paths = new();

    private static volatile BKTree _tree = new();
    private static long _totalPaths;

    private static string _prevQuery = "";
    private static int _maxLength;

    private static readonly List<Task> Tasks = new();
    public static readonly List<Task> SubTasks = new();

    public static int DebugCount;
    private static bool _ready = false;
    
    public static long Progress = 0;

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
        {
            GenCache();
        }
        
        SaveCache();

        _ready = true;


        Console.WriteLine("Ready!");

        while (true)
        {
            var query = "";

            // get query
            Dispatcher.Invoke(() => query = TextBox.Text);

            if (query != _prevQuery)
            {
                // query the tree                
                var results = _tree.Query(new BKTreeNode(query), 1000);

                Console.WriteLine(DistanceMetric.CalculateContainsPartial("findme.txt", query.ToLower()));

                // sort the results
                var sortedResults = new List<string>();

                foreach (var result in results)
                {
                    var diff = result.Value - sortedResults.Count;

                    if (diff < 0)
                    {
                        sortedResults[result.Value] = result.Key.Data;
                        continue;
                    }

                    for (var i = 0; i <= diff; i++) sortedResults.Add("");
                    sortedResults[result.Value] = result.Key.Data;
                }

                // put the results in a string
                var resultStringBuilder = new StringBuilder();

                foreach (var node in sortedResults)
                {
                    if (node is null or "") continue;
                    resultStringBuilder.Append(node).Append('\n');
                }

                var resultString = resultStringBuilder.ToString();

                // display the results
                Dispatcher.Invoke(() => TextBlock.Text = resultString);
                _prevQuery = query;
            }

            Thread.Sleep(10);
        }
    }


    private static void GenCache()
    {
        Console.WriteLine("Searching...");

        DirectorySearch(Path);
        
        WaitForTasks(Tasks, 2000);

        _totalPaths = _paths.Count;
        
        Console.WriteLine("Done Searching!");
        Console.WriteLine("Total Paths: " + _totalPaths);
        Console.WriteLine("Max File Name Length: " + _maxLength);
        Console.WriteLine("Creating BK Tree...");

        int threadSize = 256;


        int totalThreads = (int)Math.Floor(((float)_paths.Count / threadSize));
        int remainderThreads = (int)((float)_paths.Count % threadSize);
        
        
        string[,] splitPaths = new string[totalThreads + 1, threadSize];
        
        // main threads
        for (int i1 = 0; i1 < totalThreads; i1++)
            for (int i2 = 0; i2 < threadSize; i2++)
                splitPaths[i1,i2] = _paths[(i1 * threadSize) + i2];
        
        // final, remainder, thread
        for (int i2 = 0; i2 < remainderThreads; i2++)
            splitPaths[totalThreads, i2] = _paths[(totalThreads * threadSize) + i2];
        
        Console.WriteLine(splitPaths.GetLength(0));
        Console.WriteLine(splitPaths.Length);
        
        
        for (int i = 0; i < totalThreads; i++)
            Tasks.Add(Task.Run(() => LoadTree(Enumerable.Range(0, threadSize).Select(x => splitPaths[i, x]).ToArray())));
        
        WaitForTasks(SubTasks, 100);
        WaitForTasks(Tasks, 2000);

        Console.WriteLine("Done Creating BK Tree!");

    }

    private static void LoadCache()
    {
        var cacheStream = File.OpenRead("C:/ProgramData/FastExplorer/cache.json");

        var jsonBytes = new Span<byte>(new byte[cacheStream.Length]);

        cacheStream.Read(jsonBytes);

        Console.WriteLine(jsonBytes.Length);


        var jsonObject = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(jsonBytes),
            new JsonSerializerSettings
            {
                MaxDepth = int.MaxValue
            });

        _tree.GetRoot().FromJson(jsonObject);


        cacheStream.Dispose();


        Console.WriteLine("Done Loading BK Tree!");

    }

    private static void SaveCache()
    {
        Progress = 0;
        
        Console.WriteLine("Saving BK Tree...");

        var treeJson = _tree.GetRoot().ToJson();
        var cacheStream = File.OpenWrite("C:/ProgramData/FastExplorer/cache.json");

        cacheStream.SetLength(0);
        cacheStream.Flush();

        JsonWriter cacheJson = new JsonTextWriter(new StreamWriter(cacheStream));
        treeJson.WriteTo(cacheJson);

        cacheJson.Close();
        cacheStream.Close();

        Console.WriteLine("Done Saving BK Tree!");
    }

    private static void LoadTree(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (path == null) continue;
            
            DebugCount++;

            if (DebugCount >= 4096)
            {
                DebugCount = 0;
                Console.WriteLine("Creating BK Tree (4096ly update) | " + Progress + " / " + _totalPaths + " | " +
                                      Math.Round((float)Progress / _totalPaths * 100) + "%");
            }
            
            _tree.Add(new BKTreeNode(path.Replace(@"\\", "/").Replace(@"\", "/")));
            

            Progress++;
        }
    }

    private static void DirectorySearch(string dir)
    {
        DebugCount++;

        if (DebugCount >= 4096)
        {
            DebugCount = 0;
            Console.WriteLine("Exploring Filesystem | Found Paths: " + _paths.Count);
        }
        
        try
        {
            foreach (var f in Directory.GetFiles(dir))
            {
                _paths.Add(System.IO.Path.GetFullPath(f));

                if (System.IO.Path.GetFileName(f).Length > _maxLength)
                    _maxLength = System.IO.Path.GetFileName(f).Length;
            }

            foreach (var d in Directory.GetDirectories(dir))
            {
                _paths.Add(System.IO.Path.GetFullPath(d));

                Tasks.Add(
                    Task.Run(() => DirectorySearch(d))
                        );
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public static int WaitForTasks(List<Task> tasks, int delay)
    {
        int iterations = 0;
        while (true)
        {
            Task[] tasksCopy = new Task[tasks.Count << 1];
            tasks.CopyTo(tasksCopy);
            
            bool allCompleted = tasksCopy.Where(task => task != null).All(task => task.IsCompleted);

            if (allCompleted)
                break;
            
            Thread.Sleep(delay);
            iterations++;
        }
        
        tasks.Clear();

        return iterations * delay;
    }
    
    public static int WaitForTask(Task task, int delay)
    {
        int iterations = 0;
        while (true)
        {
            if (task.IsCompleted)
                break;
            
            Thread.Sleep(delay);
            iterations++;
        }

        return iterations * delay;
    }

    private void Root_OnLoaded(object sender, RoutedEventArgs e)
    {
        var thread = new Thread(BackgroundMain);
        thread.Start();
        
        var dtc = new Thread(DeadTaskCleaner);
        dtc.Start();
    }

    private static void DeadTaskCleaner()
    {
        while (true)
        {
            Task[] tasksCopy = new Task[Tasks.Count << 1];
            Tasks.CopyTo(tasksCopy);
            
            foreach (var task in tasksCopy)
            {
                try
                {
                    if (task.IsCompleted) Tasks.Remove(task);
                } catch {}
            }

            tasksCopy = new Task[SubTasks.Count << 1];
            SubTasks.CopyTo(tasksCopy);
            
            foreach (var task in tasksCopy)
            {
                try
                {
                    if (task.IsCompleted) Tasks.Remove(task);
                } catch {}
            }
            
            Thread.Sleep(2000);
            
        }
    }

    public static bool IsReady()
    {
        return _ready;
    }

    public static long GetTotalPaths()
    {
        return _totalPaths;
    }
}