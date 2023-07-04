using System;
using System.Linq;
using System.Windows;
using FastExplorer;

namespace BKTree
{
    public static class DistanceMetric
    {
        /*
         * Lee Distance
         * http://en.wikipedia.org/wiki/Lee_distance
         */
        public static int calculateLeeDistance(byte[] source, byte[] target)
        {
            if (source.Length != target.Length)
            {
                throw new Exception("Lee distance string comparisons must be of equal length.");
            }

            // Iterate both arrays simultaneously, summing absolute value of difference at each position
            return source
                .Zip(target, (v1, v2) => new { v1, v2 })
                .Sum(m => Math.Abs(m.v1 - m.v2));
        }

        /*
         * Hamming distance
         * http://en.wikipedia.org/wiki/Hamming_distance
         */
        public static int calculateHammingDistance(byte[] source, byte[] target)
        {
            if (source.Length != target.Length)
            {
                throw new Exception("Hamming distance string comparisons must be of equal length.");
            }

            // Iterate both arrays simultaneously, summing count of bit differences of each byte
            return source
                .Zip(target, (v1, v2) => new { v1, v2 })
                .Sum(m =>
                // Wegner algorithm
                {
                    int d = 0;
                    int v = m.v1 ^ m.v2; // XOR values to find all dissimilar bits

                    // Count number of set bits
                    while (v > 0)
                    {
                        ++d;
                        v &= (v - 1);
                    }

                    return d;
                });
        }

        /*
         * Levenshtein distance
         * http://en.wikipedia.org/wiki/Levenshtein_distance
         *
         * The original author of this method in Java is Josh Clemm
         * http://code.google.com/p/java-bk-tree
         *
         */
        public static int calculateLevenshteinDistance(string source, string target)
        {
            
            int[,] distance; // distance matrix
            int n; // length of first string
            int m; // length of second string
            int i; // iterates through first string
            int j; // iterates through second string
            char s_i; // ith character of first string
            char t_j; // jth character of second string
            int cost; // cost

            // Step 1
            n = source.Length;
            m = target.Length;
            if (n == 0)
                return m;
            if (m == 0)
                return n;
            distance = new int[n+1,m+1];

            // Step 2
            for (i = 0; i <= n; i++)
                distance[i,0] = i;
            for (j = 0; j <= m; j++)
                distance[0,j] = j;

            // Step 3
            for (i = 1; i <= n; i++)
            {
                s_i = source[i-1];

                // Step 4
                for (j = 1; j <= m; j++)
                {
                    t_j = target[j-1];

                    // Step 5
                    if (s_i == t_j)
                        cost = 0;
                    else
                        cost = 1;

                    // Step 6
                    distance[i,j] = 
                        Math.Min(
                            Math.Min(distance[i-1,j]+1, distance[i,j-1]+1),
                            distance[i-1,j-1] + cost );
                }
            }

            // Step 7
            return distance[n,m];
        }
        
        /*
         * Damerau-Levenshtein distance
         * https://www.csharpstar.com/csharp-string-distance-algorithm/
         *
         */
        public static int CalculateDamerauLevenshteinDistance(string source, string target)
        {
            var bounds = new { Height = source.Length + 1, Width = target.Length + 1 };

            int[,] matrix = new int[bounds.Height, bounds.Width];

            for (int height = 0; height < bounds.Height; height++) { matrix[height, 0] = height; };
            for (int width = 0; width < bounds.Width; width++) { matrix[0, width] = width; };

            for (int height = 1; height < bounds.Height; height++)
            {
                for (int width = 1; width < bounds.Width; width++)
                {
                    int cost = (source[height - 1] == target[width - 1]) ? 0 : 1;
                    int insertion = matrix[height, width - 1] + 1;
                    int deletion = matrix[height - 1, width] + 1;
                    int substitution = matrix[height - 1, width - 1] + cost;

                    int distance = Math.Min(insertion, Math.Min(deletion, substitution));

                    if (height > 1 && width > 1 && source[height - 1] == target[width - 2] && source[height - 2] == target[width - 1])
                    {
                        distance = Math.Min(distance, matrix[height - 2, width - 2] + cost);
                    }

                    matrix[height, width] = distance;
                }
            }

            return matrix[bounds.Height - 1, bounds.Width - 1];
        }
        
        public static int CalculateMatchingCharacters(string source, string target)
        {
            int c = 0, j = 0;
     
            for (int i = 0; i < source.Length; i++)
            {
                if (target.IndexOf(source[i]) >= 0)
                {
                    c += 1;
                }
            }
            
            return c;
        }

        public static int CalculateContainsPartial(string source, string target)
        {
            if (!target.Contains('.') && source.Contains('.'))
            {
                int dotIndex = source.IndexOf('.');

                if (dotIndex != 0)
                {
                    source = source.Substring(0, dotIndex);
                }

            }
            
            
            
            int sourceLength = source.Length;
            int maxLength = MainWindow.maxLength;
            
            int index = 0;
            int length = 0;

            int[] partialContents = new int[sourceLength+1];

            for (int l = 1; l <= sourceLength; l++)
            {
                for (int i = 0; i < sourceLength - l; i++)
                {
                    string part = source.Substring(i, l);

                    if (!target.Contains(part))
                    {
                        partialContents[l]++;
                    }
                }
            }
            
            int distance = 0;
            int longestFit = 0;

            for (int l = 1; l <= sourceLength; l++)
            {
                if (partialContents[l] != 0)
                {
                    distance += partialContents[l];
                    
                    longestFit = l;
                    //break;
                }
            }

            //score++;
            
            bool isFindMe = source == "findme.txt" || source == "findme";
            
            if (isFindMe)
                Console.WriteLine("FINDME ->|=============================================================================================== ");

            
            if ((distance <= 5 || isFindMe) && MainWindow.ready)
                Console.WriteLine("Distance: " + distance + ", Longest Fit: " + longestFit + ", Name: " + source + ", Query: " + target);
            
            return distance;
        }
    }
}