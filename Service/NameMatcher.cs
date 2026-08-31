using System;
using System.Collections.Generic;
using System.Linq;
using TradeWeb.API.Models;

namespace TradeWeb.API.Service
{
    public static class NameMatcher
    {
        // ── PUBLIC ENTRY POINT ───────────────────────────────────────────────────
        public static NameMatchResult Match(string name1, string name2)
        {
            if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2))
                return new NameMatchResult { Status = "Fail", MatchStatus = "INVALID INPUT" };

            string n1 = name1.Trim().ToLower();
            string n2 = name2.Trim().ToLower();

            double tokenSort = TokenSortRatio(n1, n2);
            double tokenSet = TokenSetRatio(n1, n2);
            double jaroWinkler = JaroWinklerSimilarity(n1, n2) * 100;

            // Weighted average (same weights as the Python version)
            double finalScore = (tokenSort * 0.4 + tokenSet * 0.3 + jaroWinkler * 0.3) / 100;

            return new NameMatchResult
            {
                TokenSortRatio = Math.Round(tokenSort, 2),
                TokenSetRatio = Math.Round(tokenSet, 2),
                JaroWinkler = Math.Round(jaroWinkler, 2),
                FinalScore = Math.Round(finalScore, 4),
                MatchStatus = GetMatchStatus(finalScore),
                Status = "Success"
            };
        }

        private static string GetMatchStatus(double score)
        {
            if (score >= 0.85) return "MATCH";
            if (score >= 0.70) return "POSSIBLE MATCH";
            return "NO MATCH";
        }

        // 1. TOKEN SORT RATIO — sorts words alphabetically before comparing.
        //    Handles: "Sharma Rahul" vs "Rahul Sharma" → 100
        private static double TokenSortRatio(string s1, string s2)
        {
            string sorted1 = SortTokens(s1);
            string sorted2 = SortTokens(s2);
            return SequenceRatio(sorted1, sorted2) * 100;
        }

        private static string SortTokens(string s)
        {
            var tokens = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Array.Sort(tokens, StringComparer.Ordinal);
            return string.Join(" ", tokens);
        }

        // 2. TOKEN SET RATIO — builds intersection + remainders, compares best combo.
        //    Handles: "Rahul Kumar Sharma" vs "Rahul Sharma" → high score
        private static double TokenSetRatio(string s1, string s2)
        {
            var set1 = new HashSet<string>(s1.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            var set2 = new HashSet<string>(s2.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            var inter = new SortedSet<string>(set1.Intersect(set2));

            var diff1 = new SortedSet<string>(set1.Except(inter));
            var diff2 = new SortedSet<string>(set2.Except(inter));

            string tInter = string.Join(" ", inter);
            string t1 = (tInter + " " + string.Join(" ", diff1)).Trim();
            string t2 = (tInter + " " + string.Join(" ", diff2)).Trim();

            double r1 = SequenceRatio(tInter, t1) * 100;
            double r2 = SequenceRatio(tInter, t2) * 100;
            double r3 = SequenceRatio(t1, t2) * 100;

            return Math.Max(r1, Math.Max(r2, r3));
        }

        // 3. JARO-WINKLER SIMILARITY — bonus when the prefix matches.
        //    Handles: "Rahul" vs "Rahool" → ~0.94
        public static double JaroWinklerSimilarity(string s1, string s2)
        {
            if (s1 == s2) return 1.0;
            if (s1.Length == 0 || s2.Length == 0) return 0.0;

            int matchDistance = Math.Max(s1.Length, s2.Length) / 2 - 1;
            if (matchDistance < 0) matchDistance = 0;

            bool[] s1Matches = new bool[s1.Length];
            bool[] s2Matches = new bool[s2.Length];

            int matches = 0, transpositions = 0;

            for (int i = 0; i < s1.Length; i++)
            {
                int start = Math.Max(0, i - matchDistance);
                int end = Math.Min(i + matchDistance + 1, s2.Length);

                for (int j = start; j < end; j++)
                {
                    if (s2Matches[j] || s1[i] != s2[j]) continue;
                    s1Matches[i] = true;
                    s2Matches[j] = true;
                    matches++;
                    break;
                }
            }

            if (matches == 0) return 0.0;

            int k = 0;
            for (int i = 0; i < s1.Length; i++)
            {
                if (!s1Matches[i]) continue;
                while (!s2Matches[k]) k++;
                if (s1[i] != s2[k]) transpositions++;
                k++;
            }

            double jaro = (
                (double)matches / s1.Length +
                (double)matches / s2.Length +
                (double)(matches - transpositions / 2.0) / matches
            ) / 3.0;

            int prefix = 0;
            for (int i = 0; i < Math.Min(4, Math.Min(s1.Length, s2.Length)); i++)
            {
                if (s1[i] == s2[i]) prefix++;
                else break;
            }

            return jaro + prefix * 0.1 * (1 - jaro);
        }

        // HELPER: Sequence Ratio — 2 × LCS / total chars
        private static double SequenceRatio(string s1, string s2)
        {
            if (s1.Length == 0 && s2.Length == 0) return 1.0;
            if (s1.Length == 0 || s2.Length == 0) return 0.0;

            int[,] dp = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 1; i <= s1.Length; i++)
                for (int j = 1; j <= s2.Length; j++)
                    if (s1[i - 1] == s2[j - 1])
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    else
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);

            return 2.0 * dp[s1.Length, s2.Length] / (s1.Length + s2.Length);
        }
    }
}
