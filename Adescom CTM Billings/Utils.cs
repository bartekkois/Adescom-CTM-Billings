using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Adescom_CTM_Billings
{
    public static class Utils
    {
        public static string RemoveInvalidCharactersFromFilename(string inputFilename)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex regex = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return regex.Replace(inputFilename, "");
        }

        public static IEnumerable<string> RangifyLiteralValues(IList<string> literalValues)
        {
            List<long> longValues = new List<long>();
            foreach (string value in literalValues.OrderBy(s => s))
            {
                if (long.TryParse(value, out long currentNumber))
                    longValues.Add(currentNumber);
                else
                    return literalValues.AsEnumerable();
            }

            return RangifyLongValues(longValues);
        }

        public static IEnumerable<string> RangifyLongValues(IList<long> longValues)
        {
            for (int i = 0; i < longValues.Count;)
            {
                var start = longValues[i];
                long size = 1;
                while (++i < longValues.Count && longValues[i] == start + size)
                    size++;

                if (size == 1)
                    yield return start.ToString();
                else if (size == 2)
                {
                    yield return start.ToString();
                    yield return (start + 1).ToString();
                }
                else if (size > 2)
                    yield return start + "-" + (start + size - 1);
            }
        }
    }
}
