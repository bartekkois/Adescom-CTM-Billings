using System.Collections.Generic;

namespace Adescom_CTM_Billings.Models
{
    public static class Extensions
    {
        public static IEnumerable<(int, T)> Enumerate<T>(this IEnumerable<T> input, int start = 0)
        {
            int i = start;

            foreach (var t in input)
                yield return (i++, t);
        }

        public static string ToMinutesAndSeconds(this int time)
        {
            return (time / 60).ToString() + ":" +(time % 60).ToString("00");
        }
    }
}
