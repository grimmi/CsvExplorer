using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace CsvExplorer
{
    public class DataLoadedEventArgs
    {
        public DataTable LoadedData { get; set; }
    }

    public class DataLoader
    {
        public event EventHandler<DataLoadedEventArgs> DataLoaded;

        private string currentFile;
        public string CurrentFile { get { return currentFile; } set { currentFile = value; LoadData(); } }
        private DataTypeGuesser typeGuesser;

        private Dictionary<int, List<Filter>> FilterMap { get; } = new Dictionary<int, List<Filter>>();
        private List<int> HiddenColumns { get; } = new List<int>();

        public DataLoader(string sourceFile = null)
        {
            CurrentFile = sourceFile;
            SetupGuesser();
        }

        private void SetupGuesser()
        {
            typeGuesser = new DataTypeGuesser();
            typeGuesser.AddGuesser(new EmailGuesser());
            typeGuesser.AddGuesser(new IpAddressGuesser());
            typeGuesser.AddGuesser(new IntegerGuesser());
            typeGuesser.AddGuesser(new FloatGuesser());
        }

        public void ClearFilters()
        {
            foreach(var list in FilterMap.Values)
            {
                list.Clear();
            }
            LoadData();
        }

        public void AddFilter(Filter filter, int column)
        {
            FilterMap[column].Add(filter);
            LoadData();
        }

        public void ClearHiddenColumns(bool reload = true)
        {
            HiddenColumns.Clear();
            if(reload) LoadData();
        }

        public void HideColumn(int index)
        {
            if (index == -1) return;
            HiddenColumns.Add(index);
            LoadData();
        }

        public void LoadData(string sourceFile = null)
        {
            if (!string.IsNullOrEmpty(sourceFile)) CurrentFile = sourceFile;

            if (string.IsNullOrEmpty(CurrentFile)) return;

            var dataTable = new DataTable();
            using (var reader = new StreamReader(CurrentFile))
            {
                var headerLine = reader.ReadLine();
                var headerNames = headerLine.Split(';');

                var probeLines = new List<string[]>();
                for (int i = 0; i < 10; i++)
                {
                    probeLines.Add(reader.ReadLine().Split(';'));
                }

                var probeData = new List<List<string>>();
                foreach (var probeLine in probeLines)
                {
                    for (int i = 0; i < probeLine.Length; i++)
                    {
                        if (i >= probeData.Count)
                        {
                            probeData.Add(new List<string>());
                        }
                        probeData[i].Add(probeLine[i]);
                    }
                }

                var guessedDataTypes = probeData.Select(column => typeGuesser.GuessType(column.ToArray())).ToList();

                for (int i = 0; i < headerNames.Length; i++)
                {
                    if (HiddenColumns.Contains(i)) continue;

                    var header = $"{headerNames[i]} ({guessedDataTypes[i]})";
                    dataTable.Columns.Add(new DataColumn(header, typeof(string)));
                    if (!FilterMap.ContainsKey(i))
                    {
                        FilterMap[i] = new List<Filter>();
                    }
                }

                var contentLine = "";
                var rows = new List<string[]>();
                foreach (var probeLine in probeLines)
                {
                    var valueLine = probeLine.Where((v, idx) => !HiddenColumns.Contains(idx)).ToArray();
                    if (CheckFilterList(valueLine))
                    {
                        dataTable.Rows.Add(valueLine);
                    }
                }
                while ((contentLine = reader.ReadLine()) != null)
                {
                    var contentParts = contentLine.Split(';').Where((v, idx) => !HiddenColumns.Contains(idx)).ToArray();
                    if (CheckFilterList(contentParts))
                    {
                        dataTable.Rows.Add(contentParts);
                    }
                }
            }

            DataLoaded?.Invoke(this, new DataLoadedEventArgs { LoadedData = dataTable });
        }

        private bool CheckFilterList(string[] line)
        {
            var visible = true;

            for (int i = 0; i < line.Length; i++)
            {
                var value = line[i];
                foreach (var filter in FilterMap[i])
                {
                    visible &= filter.Matches(value);
                }
            }

            return visible;
        }
    }
}
