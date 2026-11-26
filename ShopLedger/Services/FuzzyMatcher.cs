namespace ShopLedger.Services;

/// <summary>Finds the closest customer/product name for a spoken name (exact, prefix, contains, then small typo distance).</summary>
public static class FuzzyMatcher
{
    public static T? Best<T>(IEnumerable<T> items, Func<T, string> name, string query) where T : class
    {
        var q = Norm(query);
        if (q.Length == 0) return null;

        T? best = null;
        var bestScore = int.MaxValue;
        foreach (var item in items)
        {
            var n = Norm(name(item));
            if (n.Length == 0) continue;
            int score;
            if (n == q) score = 0;
            else if (n.Length >= 2 && q.Length >= 2 && (n.StartsWith(q) || q.StartsWith(n))) score = 1;
            else if (n.Length >= 3 && q.Length >= 3 && (n.Contains(q) || q.Contains(n))) score = 2;
            else
            {
                var d = Levenshtein(n, q);
                score = d <= Math.Max(1, Math.Min(n.Length, q.Length) / 4) ? 3 + d : int.MaxValue;
            }
            if (score < bestScore) { bestScore = score; best = item; }
        }
        return best;
    }

    private static string Norm(string s) =>
        new string(s.ToLowerInvariant().Where(ch => char.IsLetterOrDigit(ch) || ch == ' ').ToArray()).Trim();

    private static int Levenshtein(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
            for (var j = 1; j <= b.Length; j++)
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                                   d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
        return d[a.Length, b.Length];
    }
}
