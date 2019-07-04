using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolfReleaser.General
{
    public static class Extensions
    {
        public static string GetSplitPart(this string @this, int index)
        {
            return @this
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .ElementAt(index);
        }

        public static bool Equalish(this string @this, string other)
        {
            return string.Equals(
                @this?.Trim(),
                other?.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        public static int AddRange<T>(
            this HashSet<T> @this,
            IEnumerable<T> items)
        {
            int count = 0;

            foreach (var item in items)
            {
                if (@this.Add(item))
                    count++;
            }

            return count;
        }
    }
}
