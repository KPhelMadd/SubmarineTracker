using System.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using SubmarineTracker.Data;

namespace SubmarineTracker;

public static class Utils
{
    public static string ToStr(SeString content) => content.ToString();
    public static string ToStr(Lumina.Text.SeString content) => content.ToDalamudString().ToString();
    public static string ToTime(TimeSpan time) => $"{(int)time.TotalHours:#00}:{time:mm}:{time:ss}";

    public static string UpperCaseStr(Lumina.Text.SeString s, sbyte article = 0)
    {
        if (article == 1)
            return s.ToDalamudString().ToString();

        var sb = new StringBuilder(s.ToDalamudString().ToString());
        var lastSpace = true;
        for (var i = 0; i < sb.Length; ++i)
        {
            if (sb[i] == ' ')
            {
                lastSpace = true;
            }
            else if (lastSpace)
            {
                lastSpace = false;
                sb[i]     = char.ToUpperInvariant(sb[i]);
            }
        }

        return sb.ToString();
    }

    public static string MapToShort(int key) => MapToShort((uint)key);
    public static string MapToShort(uint key)
    {
        return key switch
        {
            1 => "Deep-sea",
            2 => "Sea of Ash",
            3 => "Sea of Jade",
            4 => "Sirensong",
            _ => ""
        };
    }

    public static string MapToThreeLetter(int key, bool resolveToMap = false) => MapToThreeLetter((uint) key, resolveToMap);
    public static string MapToThreeLetter(uint key, bool resolveToMap = false)
    {
        if (resolveToMap)
            key = Voyage.SectorToMap(key);

        return key switch
        {
            1 => "DSS",
            2 => "SOA",
            3 => "SOJ",
            4 => "SSS",
            5 => "TLS",
            _ => ""
        };
    }

    public static string NumToLetter(uint num, bool findStart = false)
    {
        if (findStart)
            num -= Voyage.FindVoyageStartPoint(num);

        var index = (int)(num - 1);  // 0 indexed

        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        var value = "";

        if (index >= letters.Length)
            value += letters[(index / letters.Length) - 1];

        value += letters[index % letters.Length];

        return value;
    }

    public static string FormattedRouteBuild(string name, Build.RouteBuild build)
    {
        var route = "No Route";
        if (build.Sectors.Any())
        {
            var startPoint = Voyage.FindVoyageStartPoint(build.Sectors.First());
            route = $"{MapToThreeLetter(build.Map + 1)}: {string.Join(" -> ", build.Sectors.Select(p => NumToLetter(p - startPoint)))}";;
        }

        return $"{name.Replace("%", "%%")} (R: {build.Rank} B: {build.GetSubmarineBuild.FullIdentifier()})" +
               $"\n{route}";
    }

    public static SeString SuccessMessage(string success)
    {
        return new SeStringBuilder()
               .AddUiForeground("[Submarine Tracker] ", 540)
               .AddUiForeground($"{success}", 43)
               .BuiltString;
    }

    public static SeString ErrorMessage(string error)
    {
        return new SeStringBuilder()
               .AddUiForeground("[Submarine Tracker] ", 540)
               .AddUiForeground($"{error}", 17)
               .BuiltString;
    }

    public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
    {
        if (!dict.TryGetValue(key, out TValue? val))
        {
            val = new TValue();
            dict.Add(key, val);
        }

        return val;
    }
    public static bool ContainsAllItems<T>(this IEnumerable<T> a, IEnumerable<T> b)
    {
        return !b.Except(a).Any();
    }

    public class ListComparer : IEqualityComparer<List<uint>>
    {
        public bool Equals(List<uint>? x, List<uint>? y)
        {
            if (x == null)
                return false;
            if (y == null)
                return false;

            return x.Count == y.Count && !x.Except(y).Any();
        }

        public int GetHashCode(List<uint> obj)
        {
            var hash = 19;
            foreach (var element in obj.OrderBy(x => x))
            {
                hash = (hash * 31) + element.GetHashCode();
            }

            return hash;
        }
    }
}

public static class StringExt
{
    public static string? Truncate(this string? value, int maxLength, string truncationSuffix = "...")
    {
        return value?.Length > maxLength
                   ? string.Concat(value.AsSpan(0, maxLength), truncationSuffix)
                   : value;
    }
}

public static class Extensions
{
    public static void Swap<T>(this List<T> list, int i, int j)
    {
        (list[i], list[j]) = (list[j], list[i]);
    }
}
