using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Adescom_CTM_Billings
{
    public static class Utils
    {
        public static string RemoveInvalidCharactersFromFilename(string inputFilename)
        {
            List<char> _invalidFileNameChars = new List<char> {
                '\x0022', '\x003C', '\x003E', '\x007C',
                '\x0000', '\x0001', '\x0002', '\x0003',
                '\x0004', '\x0005', '\x0006', '\x0007',
                '\x0008', '\x0009', '\x000A', '\x000B',
                '\x000C', '\x000D', '\x000E', '\x000F',
                '\x0010', '\x0011', '\x0012', '\x0013',
                '\x0014', '\x0015', '\x0016', '\x0017',
                '\x0018', '\x0019', '\x001A', '\x001B',
                '\x001C', '\x001D', '\x001E', '\x001F',
                '\x003A', '\x002A', '\x003F', '\x005C',
                '\x002F'
            };

            Regex regex = new Regex(string.Format("[{0}]", Regex.Escape(new string(_invalidFileNameChars.ToArray()))));
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
