﻿using System;
using System.Linq;

namespace CatalogTool.MendzhulTextHelpers
{
    public static class SentenceComparer_DoubleLevenshtein
    {
        public static double Compute(string x, string y)
        {
            if (string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y))
                return 0d;

            var comparer = new SentenceStringComparer();

            var length = comparer.Length(x) + comparer.Length(y);
            var dist = comparer.Dist(x, y);

            var same = length - 2 * dist;

            if (length <= 0 || same <= 0)
                return 0;

            return Math.Sqrt(same / length);
        }
    }

    interface IComparer<T>
    {
        double Dist(T x, T y);

        double Length(T x);
    }

    class CharComparer : IComparer<char>
    {
        public double Dist(char x, char y)
        {
            return x == y ? 0 : string.Equals(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase) ? 0.9 : 1;
        }

        public double Length(char x)
        {
            return 1;
        }
    }

    class LevenshteinComparer<T> : IComparer<T[]>
    {
        private readonly IComparer<T> singleComparer;

        public LevenshteinComparer(IComparer<T> singleComparer) => this.singleComparer = singleComparer;

        public double Length(T[] x)
        {
            return x.Sum(a => singleComparer.Length(a));
        }

        public double Dist(T[] x, T[] y)
        {
            int n = x.Length;
            int m = y.Length;

            if (x.Length == 0 || y.Length == 0)
            {
                return Length(x) + Length(y);
            }

            var d = new double[n + 1, m + 1];

            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 1; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    var cost = singleComparer.Dist(y[j - 1], x[i - 1]);

                    var min1 = d[i - 1, j] + cost;
                    var min2 = d[i, j - 1] + cost;

                    var min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }
            return d[n, m];
        }
    }

    class SimpleStringComparer : IComparer<string>
    {
        private readonly IComparer<char[]> charsComparer = new LevenshteinComparer<char>(new CharComparer());

        public double Dist(string x, string y) => charsComparer.Dist(x.ToArray(), y.ToArray());

        public double Length(string x) => x.Length;//charsComparer.Length(x.ToArray());
    }

    class IndexedString
    {
        public string Value { get; set; }

        public int Index { get; set; }
    }

    class IndexedStringsComparer : IComparer<IndexedString>
    {
        private readonly IComparer<string> simpleStringComparer;

        private readonly int MaxIndex;

        public IndexedStringsComparer(int maxIndex, IComparer<string> simpleStringComparer)
        {
            this.simpleStringComparer = simpleStringComparer ?? new SimpleStringComparer();
            MaxIndex = maxIndex;
            //length = Math.Sqrt(MaxIndex);
        }

        public double Dist(IndexedString x, IndexedString y)
        {
            var d = simpleStringComparer.Dist(x.Value, y.Value);
            var shift = /*length **/ Math.Abs(x.Index - y.Index);// / (MaxIndex + 1);
            return (d + shift);
        }

        public double Length(IndexedString x) => simpleStringComparer.Length(x.Value);
    }

    class SentenceStringComparer : IComparer<string>
    {
        private readonly IComparer<string> simpleComparer = new SimpleStringComparer();

        public double Dist(string x, string y)
        {
            var xWords = ToIndexedWords(x);
            var yWords = ToIndexedWords(y);

            var d = simpleComparer.Dist(string.Join(" ", xWords.Select(a => a.Value)), string.Join(" ", yWords.Select(a => a.Value)));

            var sc = new IndexedStringsComparer(Math.Max(xWords.Length, yWords.Length), simpleComparer);
            var lc = new LevenshteinComparer<IndexedString>(sc);
            return lc.Dist(xWords, yWords) + d;
        }

        public double Length(string x)
        {
            var xWords = ToIndexedWords(x);
            var sc = new IndexedStringsComparer(xWords.Length, simpleComparer);
            return xWords.Sum(a => sc.Length(a)) + x.Length + 1;
        }

        private static IndexedString[] ToIndexedWords(string x)
        {
            return x.Split(null)
                .Select((s, i) => new IndexedString { Value = s, Index = i })
                .OrderBy(a => a.Value)
                .ToArray();
        }
    }
}
