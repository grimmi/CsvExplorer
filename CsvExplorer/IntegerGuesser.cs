using System.Collections.Generic;
using System.Linq;

namespace CsvExplorer
{
    public class IntegerGuesser : IDataTypeGuesser
    {
        public (double Chance, DataType DataType) Guess(IEnumerable<string> values)
        {
            var validInts = 0;
            foreach(var value in values)
            {
                if (int.TryParse(value, out var tmp)) validInts++;
            }

            return ((double)validInts / values.Count(), DataType.Integer);
        }
    }
}
