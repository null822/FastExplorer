using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using BKTree;
using Path = System.IO.Path;

namespace FastExplorer;

public partial class MainWindow : Window
{

    private static List<string> _paths = new();

    private static BKTree<CharNode> _tree = new();
    private static long _totalPaths;

    private static string prevQuery = "";

    public static int maxLength;

    public static bool ready = false; //DEBUG

    public MainWindow()
    {
        InitializeComponent();
    }
    
    private static void DirectorySearch(string dir)
    {
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
                
                DirectorySearch(d);
            }
        }
        catch (Exception) {}

    }

    private void Root_OnLoaded(object sender, RoutedEventArgs e)
    {
        Thread thread = new Thread(BackgroundMain);
        thread.Start();


    }

    private void BackgroundMain()
    {

        DirectorySearch("C:/Users/noahk/Documents");

        _totalPaths = _paths.Count;

        
        Console.WriteLine("Done Searching!");
        Console.WriteLine("Total Paths: " + _totalPaths);
        Console.WriteLine("Max File Name Length: " + maxLength);
        
        long prog = 0;
        foreach (var path in _paths.ToArray())
        {
            _tree.add(new CharNode(path));
            
            prog++;
            Console.WriteLine(prog + " / " + _totalPaths);
        }
        
        ready = true;

        Console.WriteLine("Done Creating BK Tree!");
        
        while (true)
        {
            string query = "";

            // get query
            Dispatcher.Invoke(() => query = TextBox.Text);

            if (query != prevQuery)
            {
                
                Console.WriteLine("======================================================================================"); // DEBUG
                
                Dictionary<CharNode, int> results = _tree.query(new CharNode(query), 5000);

                List<string> sortedResults = new List<string>();

                foreach (var result in results)
                {
                    if (result.Value < 0)
                        Console.WriteLine("NEGATIVE INDEX! " + result.Value); // DEBUG
                    
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

                bool foundLowest = false; // DEBUG
                int lowestDistance = 0; // DEBUG
                string lowestPath = ""; // DEBUG
                
                StringBuilder resultStringBuilder = new StringBuilder();
                
                foreach (var node in sortedResults)
                {
                    if (node == null || node == "") continue;
                    resultStringBuilder.Append(node).Append('\n');

                    if (!foundLowest)
                    {
                        lowestDistance = sortedResults.IndexOf(node);
                        lowestPath = Path.GetFileName(node);
                        foundLowest = true;
                    }
                }
                
                Console.WriteLine("Closest to query: " + lowestPath + " @ " + lowestDistance);

                

                string resultString = resultStringBuilder.ToString();
                
                Dispatcher.Invoke(() => TextBlock.Text = resultString);

                prevQuery = query;
            }

            Thread.Sleep(10);

        }
    }
}

public class CharNode : BKTreeNode
{

    private char zero = Encoding.ASCII.GetString(new byte[] { 0x0 })[0];
    
    public ushort Id { get; private set; }
    public string Data { get; private set; } // String of symbols

    public CharNode(ushort id, string values)
    {
        if (id == 0)
        {
            throw new ArgumentException("0 is a reserved Id value");
        }

        Data = values;
        Id = id;
    }

    public CharNode(string values)
    {
        Data = values;
        Id = 0;
    }
    
    // The only required method of abstract class BKTreeNode
    protected override int calculateDistance(BKTreeNode node)
    {
        string source = Path.GetFileName(Data).ToLower();
        string target = Path.GetFileName(((CharNode)node).Data).ToLower();
        
        
        int diff = source.Length - target.Length;
        
        if (diff > 0)
            target += charGen(zero, diff);
        if (diff < 0)
            source += charGen(zero, -diff);
        
        // return DistanceMetric.calculateLeeDistance(
        //     Encoding.ASCII.GetBytes(source), Encoding.ASCII.GetBytes(target));
        //
        
        int distance = DistanceMetric.CalculateContainsPartial(source, target);
        
        if (source == "findme.txt")
            Console.WriteLine("dist. to \"FindMe.txt\": " + distance);
        
        return distance;
    }

    private string charGen(char input, int count)
    {
        StringBuilder result = new StringBuilder();
        
        for (int i = 0; i < count; i++)
        {
            result.Append(input);
        }

        return result.ToString();
    }
}