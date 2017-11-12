using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace CsvExplorer
{
    public class IpAddressGuesser : IDataTypeGuesser
    {
        public (double Chance, DataType DataType) Guess(IEnumerable<string> values)
        {
            var validIps = 0;

            foreach(var value in values)
            {
                if (value.Count(c => c == '.') == 3 && IPAddress.TryParse(value, out var adr)) validIps++;
            }

            return ((double)validIps / values.Count(), DataType.IpAddress);
        }
    }
}
