using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CsvExplorer
{
    public class EmailGuesser : IDataTypeGuesser
    {
        public (double Chance, DataType DataType) Guess(IEnumerable<string> values)
        {
            var tlds = File.ReadAllLines("./GuessData/tldlist.txt");

            var chance = (double)values.Count(v => v.Contains("@") && v.Contains(".") && tlds.Any(tld => v.ToUpper().EndsWith(tld))) / values.Count();

            return (Chance: chance, DataType: DataType.Email);
        }
    }

    public interface IDataTypeGuesser
    {
        (double Chance, DataType DataType) Guess(IEnumerable<string> values);
    }
}
