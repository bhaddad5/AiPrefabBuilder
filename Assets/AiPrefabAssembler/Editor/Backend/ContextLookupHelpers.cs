using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class ContextLookupHelpers
{
	//STRING MATCHING

	// ---------------- Levenshtein for titles ----------------
	private static int Levenshtein(string a, string b, bool ignoreCase)
	{
		if (a == null) a = string.Empty;
		if (b == null) b = string.Empty;

		int m = a.Length, n = b.Length;
		if (m == 0) return n;
		if (n == 0) return m;

		var prev = new int[n + 1];
		var curr = new int[n + 1];

		for (int j = 0; j <= n; j++) prev[j] = j;

		for (int i = 1; i <= m; i++)
		{
			curr[0] = i;
			char ca = a[i - 1];
			if (ignoreCase) ca = char.ToUpperInvariant(ca);

			for (int j = 1; j <= n; j++)
			{
				char cb = b[j - 1];
				if (ignoreCase) cb = char.ToUpperInvariant(cb);

				int cost = (ca == cb) ? 0 : 1;

				int del = prev[j] + 1;
				int ins = curr[j - 1] + 1;
				int sub = prev[j - 1] + cost;

				int best = del < ins ? del : ins;
				if (sub < best) best = sub;
				curr[j] = best;
			}

			var tmp = prev; prev = curr; curr = tmp;
		}

		return prev[n];
	}

	private static double NormalizedEditSim(string a, string b, bool ignoreCase)
	{
		if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 1.0;
		if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;
		int maxLen = Math.Max(a.Length, b.Length);
		int dist = Levenshtein(a, b, ignoreCase);
		return 1.0 - (double)dist / maxLen; // 0..1
	}


	private static double ContainmentBoost(string query, string field)
	{
		if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(field)) return 0.0;
		return field.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ? 0.05 : 0.0;
	}

	public static List<(string name, int id, double score)> TopMatches(
		string query,
		IEnumerable<(string name, int id)> candidates,
		double minScore = .2,
		double tokenThreshold = 0.84)  // how close tokens must be to count as a match
	{
		if (candidates == null) return new();

		return candidates
			.Select(c =>
			{
				double t = NormalizedEditSim(query, c.name ?? "", ignoreCase: true) + ContainmentBoost(query, c.name);
				if (t > 1) t = 1;
				return (c.name, c.id, t);
			})
			.Where(x => x.t >= minScore)
			.OrderByDescending(x => x.t)
			.Select(x => (x.name, x.id, x.t))
			.ToList();
	}



	//TAG MATCHING

	/// <summary>
	/// Returns up to topN objects with highest IDF-weighted Jaccard similarity to the queryTags.
	/// </summary>
	public static List<(string id, double score)> TopMatches(
		IEnumerable<string> queryTags,
		IEnumerable<(string id, List<string> tags)> objects)
	{
		if (queryTags == null) throw new ArgumentNullException(nameof(queryTags));
		if (objects == null) throw new ArgumentNullException(nameof(objects));

		// Normalize & de-duplicate query tags
		var q = NormalizeSet(queryTags);
		if (q.Count == 0) return new();

		// Materialize objects with normalized, de-duped tag sets
		var items = objects
			.Select(o => new { Obj = o, Tags = NormalizeSet(o.tags) })
			.ToArray();

		if (items.Length == 0) return new();

		// Document frequency for IDF (per object)
		var df = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var it in items)
		{
			foreach (var t in it.Tags)
			{
				df.TryGetValue(t, out var c);
				df[t] = c + 1;
			}
		}

		// Smooth IDF: log((N+1)/(df+1)) + 1  (keeps values > 0)
		int N = items.Length;
		double Idf(string t)
		{
			int dfi = df.TryGetValue(t, out var c) ? c : 0;
			return Math.Log((double)(N + 1) / (dfi + 1)) + 1.0;
		}

		// Precompute weights for all tags we’ll touch
		var weightCache = new Dictionary<string, double>(StringComparer.Ordinal);
		double W(string t) => weightCache.TryGetValue(t, out var w) ? w : (weightCache[t] = Idf(t));

		// Weighted Jaccard: sum_w(intersection) / sum_w(union)
		double Score(HashSet<string> a, HashSet<string> b)
		{
			if (a.Count == 0 && b.Count == 0) return 0;

			double inter = 0, uni = 0;

			// Union: iterate over both sets once
			foreach (var t in a) uni += W(t);
			foreach (var t in b) if (!a.Contains(t)) uni += W(t);

			// Intersection
			// (iterate over the smaller set for speed)
			var small = a.Count <= b.Count ? a : b;
			var large = ReferenceEquals(small, a) ? b : a;
			foreach (var t in small) if (large.Contains(t)) inter += W(t);

			return uni > 0 ? inter / uni : 0;
		}

		var results = items
			.Select(it => (it.Obj.id, score: Score(q, it.Tags)))
			.Where(x => x.score > 0)              // drop total non-matches; remove if you want everything
			.OrderByDescending(x => x.score)
			.ThenBy(x => x.id, StringComparer.Ordinal)
			.ToList();

		return results;
	}

	private static HashSet<string> NormalizeSet(IEnumerable<string> tags) =>
		new HashSet<string>(
			tags.Where(s => !string.IsNullOrWhiteSpace(s))
				.Select(Norm),
			StringComparer.Ordinal);

	private static string Norm(string s) => s.Trim().ToLowerInvariant();
}
