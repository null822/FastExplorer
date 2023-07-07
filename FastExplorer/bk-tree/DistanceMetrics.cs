using System;

namespace FastExplorer.bk_tree;

public static class DistanceMetric
{
    // Other Methods
    // /*
    //  * Lee Distance
    //  * http://en.wikipedia.org/wiki/Lee_distance
    //  */
    // public static int CalculateLeeDistance(byte[] source, byte[] target)
    // {
    //     if (source.Length != target.Length)
    //         throw new Exception("Lee distance string comparisons must be of equal length.");
    //
    //     // Iterate both arrays simultaneously, summing absolute value of difference at each position
    //     return source
    //         .Zip(target, (v1, v2) => new { v1, v2 })
    //         .Sum(m => Math.Abs(m.v1 - m.v2));
    // }
    //
    // /*
    //  * Hamming distance
    //  * http://en.wikipedia.org/wiki/Hamming_distance
    //  */
    // public static int CalculateHammingDistance(byte[] source, byte[] target)
    // {
    //     if (source.Length != target.Length)
    //         throw new Exception("Hamming distance string comparisons must be of equal length.");
    //
    //     // Iterate both arrays simultaneously, summing count of bit differences of each byte
    //     return source
    //         .Zip(target, (v1, v2) => new { v1, v2 })
    //         .Sum(m =>
    //             // Wegner algorithm
    //         {
    //             var d = 0;
    //             var v = m.v1 ^ m.v2; // XOR values to find all dissimilar bits
    //
    //             // Count number of set bits
    //             while (v > 0)
    //             {
    //                 ++d;
    //                 v &= v - 1;
    //             }
    //
    //             return d;
    //         });
    // }
    //
    // /*
    //  * Levenshtein distance
    //  * http://en.wikipedia.org/wiki/Levenshtein_distance
    //  *
    //  * The original author of this method in Java is Josh Clemm
    //  * http://code.google.com/p/java-bk-tree
    //  *
    //  */
    // public static int CalculateLevenshteinDistance(string source, string target)
    // {
    //     int i; // iterates through first string
    //     int j; // iterates through second string
    //
    //     // Step 1
    //     var n = source.Length; // length of first string
    //     var m = target.Length; // length of second string
    //     if (n == 0)
    //         return m;
    //     if (m == 0)
    //         return n;
    //     var distance = new int[n + 1, m + 1]; // distance matrix
    //
    //     // Step 2
    //     for (i = 0; i <= n; i++)
    //         distance[i, 0] = i;
    //     for (j = 0; j <= m; j++)
    //         distance[0, j] = j;
    //
    //     // Step 3
    //     for (i = 1; i <= n; i++)
    //     {
    //         var sl = source[i - 1]; // ith character of first string
    //
    //         // Step 4
    //         for (j = 1; j <= m; j++)
    //         {
    //             var tj = target[j - 1]; // jth character of second string
    //
    //             // Step 5
    //             var cost = // cost
    //                 sl == tj ? 0 : 1;
    //
    //             // Step 6
    //             distance[i, j] =
    //                 Math.Min(
    //                     Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
    //                     distance[i - 1, j - 1] + cost);
    //         }
    //     }
    //
    //     // Step 7
    //     return distance[n, m];
    // }
    //
    // /*
    //  * Damerau-Levenshtein distance
    //  * https://www.csharpstar.com/csharp-string-distance-algorithm/
    //  *
    //  */
    // public static int CalculateDamerauLevenshteinDistance(string source, string target)
    // {
    //     var bounds = new { Height = source.Length + 1, Width = target.Length + 1 };
    //
    //     var matrix = new int[bounds.Height, bounds.Width];
    //
    //     for (var height = 0; height < bounds.Height; height++) matrix[height, 0] = height;
    //     ;
    //     for (var width = 0; width < bounds.Width; width++) matrix[0, width] = width;
    //     ;
    //
    //     for (var height = 1; height < bounds.Height; height++)
    //     for (var width = 1; width < bounds.Width; width++)
    //     {
    //         var cost = source[height - 1] == target[width - 1] ? 0 : 1;
    //         var insertion = matrix[height, width - 1] + 1;
    //         var deletion = matrix[height - 1, width] + 1;
    //         var substitution = matrix[height - 1, width - 1] + cost;
    //
    //         var distance = Math.Min(insertion, Math.Min(deletion, substitution));
    //
    //         if (height > 1 && width > 1 && source[height - 1] == target[width - 2] &&
    //             source[height - 2] == target[width - 1])
    //             distance = Math.Min(distance, matrix[height - 2, width - 2] + cost);
    //
    //         matrix[height, width] = distance;
    //     }
    //
    //     return matrix[bounds.Height - 1, bounds.Width - 1];
    // }
    //
    // public static int CalculateMatchingCharacters(string source, string target)
    // {
    //     int c = 0, j = 0;
    //
    //     foreach (var character in source)
    //         if (target.Contains(character))
    //             c += 1;
    //
    //     return c;
    // }

    public static int CalculateContainsPartial(string source, string target)
    {
        // remove file type if it is not in the query
        if (!target.Contains('.') && source.Contains('.'))
        {
            var dotIndex = source.IndexOf('.');

            if (dotIndex != 0) source = source[..dotIndex];
        }
        
        var sourceLength = source.Length;
        var targetLength = target.Length;
        
        var longestFit = 0;

        // get longest match
        for (var l = 1; l <= targetLength; l++)
        {
            for (var i = 0; i < targetLength - l; i++)
            {
                var part = target.Substring(i, l);

                if (source.Contains(part))
                    longestFit = l;
            }
        }

        longestFit++;


        // calculate distance based off of the longest match and length of the query
        var distance = longestFit == 0 ? 100 : (int)(1f / longestFit * 100f);
        
        // debug
        if (MainWindow.IsReady() && MainWindow.Debug)
        {

            bool isFindMe = source is "findme.txt" or "findme";

            if (isFindMe)
                Console.WriteLine(
                    "FINDME ->|=============================================================================================== ");


            if ((distance <= 2 || isFindMe))
                Console.WriteLine("Distance: " + distance + ", Longest Fit: " + longestFit + ", Name: " + source +
                                  ", Query: " + target);
        }

        return distance;
    }
}
