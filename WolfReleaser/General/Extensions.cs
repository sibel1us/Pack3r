using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolfReleaser.General
{
    public static class Extensions
    {
        public static bool HasValue(this string @this) => !string.IsNullOrEmpty(@this);
        public static bool NotEmpty(this string @this) => !string.IsNullOrWhiteSpace(@this);
        public static bool IsComment(this string @this) => @this.StartsWith("//");

        public static IEnumerable<string> GetSplit(this string @this)
        {
            return @this
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.NotEmpty());
        }

        public static string GetSplitPart(this string @this, int index)
        {
            return GetSplit(@this).ElementAt(index);
        }

        public static string TrimQuotes(this string @this)
        {
            bool starts = @this[0] == '"';
            bool ends = @this[@this.Length - 1] == '"';

            return @this.Substring(
                starts ? 1 : 0,
                @this.Length - (starts ? ends ? 2 : 1 : 0));
        }

        public static bool Equalish(this string @this, string other)
        {
            return string.Equals(
                @this?.Trim(),
                other?.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        public static IEnumerable<(string, int)> Clean(
            this IEnumerable<string> @this)
        {
            return @this.Where(s => s.NotEmpty()).Select((s, i) => (s.Trim(), i));
        }

        public static IEnumerable<string> SkipComments(
            this IEnumerable<string> @this)
        {
            return @this.Where(s => !s.IsComment());
        }

        public static IEnumerable<(string, int)> SkipComments(
            this IEnumerable<(string, int)> @this)
        {
            return @this.Where(s => !s.Item1.IsComment());
        }

        public static HashSet<T> AddRange<T>(
            this HashSet<T> @this,
            IEnumerable<T> items)
        {
            @this.UnionWith(items);
            return @this;
        }

        public static bool ContainsOne<T>(this IEnumerable<T> @this, T item)
        {
            int itemCount = 0;

            foreach (var collectionItem in @this)
            {
                if (item.Equals(collectionItem))
                {
                    itemCount++;

                    if (itemCount > 1)
                    {
                        return false;
                    }
                }
            }

            return (itemCount == 1)
                ? true
                : throw new Exception("Collection does not contain the item");
        }
    }
}
