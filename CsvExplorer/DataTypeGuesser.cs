using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace CsvExplorer
{
    public enum DataType
    {
        Text,
        Number,
        Email,
        IpAddress,
        Date,
        Url
    }

    public class DataTypeGuesser
    {
        public DataType GuessType(string[] values)
        {
            var threshold = 0.8;

            var emailGuess = GuessEmail(values);

            if(emailGuess >= threshold)
            {
                return DataType.Email;
            }

            return DataType.Text;
        }

        private double GuessEmail(string[] values)
        {
            var tlds = File.ReadAllLines("./GuessData/tldlist.txt");

            return (double)values.Count(v => v.Contains("@") && v.Contains(".") && tlds.Any(tld => v.ToUpper().EndsWith(tld))) / values.Count();
        }
    }
}
