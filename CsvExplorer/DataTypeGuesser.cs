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
        Integer,
        Float,
        Email,
        IpAddress,
        Date,
        Url
    }

    public class DataTypeGuesser
    {
        private IList<IDataTypeGuesser> Guessers { get; } = new List<IDataTypeGuesser>();

        public void AddGuesser(IDataTypeGuesser guesser)
        {
            Guessers.Add(guesser);
        }

        public DataType GuessType(string[] values)
        {
            var threshold = 0.8;

            var guesses = Guessers.Select(g => g.Guess(values));

            var bestChance = guesses.Max(g => g.Chance);

            if (bestChance < threshold) return DataType.Text;

            var bestTypes = guesses.Where(g => g.Chance == bestChance).Select(g => g.DataType);

            return bestTypes.First();
        }
    }
}
