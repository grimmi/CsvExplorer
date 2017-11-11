using System.Collections.Generic;
using System.Linq;

namespace CsvExplorer
{
    public class FloatGuesser : IDataTypeGuesser
    {
        public (double Chance, DataType DataType) Guess(IEnumerable<string> values)
        {
            var validFloats = 0;
            
            //TODO: check if the numbers use '.' or ',' as decimal separator

            foreach(var value in values)
            {
                if (double.TryParse(value, out var d)) validFloats++;
            }

            return ((double)validFloats / values.Count(), DataType.Float);
        }
    }
}
