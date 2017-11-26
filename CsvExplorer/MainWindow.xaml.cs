using Microsoft.Win32;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Windows.Media;

namespace CsvExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>    
    [AddINotifyPropertyChangedInterface]
    public partial class MainWindow : Window
    {
        public ICommand OpenFileCommand => new RelayCommand((o) => OpenFile());
        public ICommand ClearFilterCommand => new RelayCommand((o) =>
        {
            foreach (var list in FilterMap.Values)
            {
                list.Clear();
            }
            LoadData();
        });

        public ICommand HideColumnCommand => new RelayCommand((o) => HideColumn(SelectedColumnIndex));

        public ICommand ShowColumnsCommand => new RelayCommand((o) => { HiddenColumns.Clear(); LoadData(); });

        public ICommand ReloadDocumentCommand => new RelayCommand((o) => { HiddenColumns.Clear(); FilterMap.Clear(); LoadData(); });

        public DataView CsvData { get; set; }
        private DataTypeGuesser Guesser { get; set; }

        private DataTable dataTable;

        private Dictionary<int, List<Filter>> FilterMap { get; } = new Dictionary<int, List<Filter>>();
        private List<int> HiddenColumns { get; } = new List<int>();

        private static Func<string[], int, bool> defaultFilter = (vs, c) => true;

        private string currentFile = "";
        public string CurrentFile
        {
            get { return currentFile; }
            set { currentFile = value; LoadData(); }
        }

        public Point RightClickPosition { get; private set; }
        public int SelectedColumnIndex { get; set; } = 0;

        public string SelectedColumn
        {
            get;
            set;
        } = "TheColumn";

        public int SelectedRowIndex { get; set; } = 0;
        private DataGridRow SelectedRow { get; set; }

        public MainWindow()
        {
            SetupGuesser();
            InitializeComponent();
        }

        private void SetupGuesser()
        {
            Guesser = new DataTypeGuesser();
            Guesser.AddGuesser(new EmailGuesser());
            Guesser.AddGuesser(new IpAddressGuesser());
            Guesser.AddGuesser(new IntegerGuesser());
            Guesser.AddGuesser(new FloatGuesser());
        }

        private void OpenFile()
        {
            var dialog = new OpenFileDialog();
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                CurrentFile = dialog.FileName;
            }
        }

        private void AddFilter(Filter filter, int column)
        {
            FilterMap[column].Add(filter);
            LoadData();
        }

        private void HideColumn(int idx)
        {
            if (idx == -1) return;
            HiddenColumns.Add(idx);
            LoadData();
        }
        
        private void LoadData()
        {
            dataTable = new DataTable();
            using (var reader = new StreamReader(CurrentFile))
            {
                var headerLine = reader.ReadLine();
                var headerNames = headerLine.Split(';');

                var probeLines = new List<string[]>();
                for(int i = 0; i < 10; i++)
                {
                    probeLines.Add(reader.ReadLine().Split(';'));
                }

                var probeData = new List<List<string>>();
                foreach(var probeLine in probeLines)
                {
                    for(int i = 0; i < probeLine.Length; i++)
                    {
                        if(i >= probeData.Count)
                        {
                            probeData.Add(new List<string>());
                        }
                        probeData[i].Add(probeLine[i]);
                    }
                }

                var guessedDataTypes = probeData.Select(column => Guesser.GuessType(column.ToArray())).ToList();

                for(int i = 0; i < headerNames.Length; i++)
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
                foreach(var probeLine in probeLines)
                {
                    var valueLine = probeLine.Where((v, idx) => !HiddenColumns.Contains(idx)).ToArray();
                    if (CheckFilterList(valueLine))
                    {
                        dataTable.Rows.Add(valueLine);
                    }
                }
                while((contentLine = reader.ReadLine()) != null)
                {
                    var contentParts = contentLine.Split(';').Where((v, idx) => !HiddenColumns.Contains(idx)).ToArray();
                    if (CheckFilterList(contentParts))
                    {
                        dataTable.Rows.Add(contentParts);
                    }
                }

                CsvData = dataTable.DefaultView;
            }
        }

        private bool CheckFilterList(string[] line)
        {
            var visible = true;

            for(int i = 0; i < line.Length; i++)
            {
                var value = line[i];
                foreach(var filter in FilterMap[i])
                {
                    visible &= filter.Matches(value);
                }
            }

            return visible;
        }

        private void DataGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.RightButton == MouseButtonState.Pressed)
            {
                RightClickPosition = e.GetPosition(csvData);

                var hitResult = VisualTreeHelper.HitTest(csvData, RightClickPosition);
                var hit = hitResult.VisualHit;

                if ((hit as FrameworkElement).Parent is DataGridCell cell)
                {
                    SelectedColumnIndex = cell.Column.DisplayIndex;
                    SelectedColumn = cell.Column.Header?.ToString() ?? "";

                    var parent = VisualTreeHelper.GetParent(cell);
                    while(parent != null && !(parent is DataGridRow))
                    {
                        parent = VisualTreeHelper.GetParent(parent);
                    }

                    if(parent is DataGridRow row)
                    {
                        SelectedRowIndex = csvData.ItemContainerGenerator.IndexFromContainer(row);
                        SelectedRow = row;
                    }

                }
                else
                {
                    SelectedColumnIndex = -1;
                    SelectedRowIndex = -1;
                }
            }
        }

        private void FilterBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var tBox = sender as TextBox;
            if (SelectedColumnIndex > -1)
            {
                AddFilter(new TextFilter(tBox.Text.ToLower()), SelectedColumnIndex);
            }

            tBox.Text = string.Empty;
        }

        private void HideColumnClicked(object sender, EventArgs e) => HideColumnCommand.Execute(null);

        private void CopyRowClicked(object sender, EventArgs e)
        {
            if (SelectedRowIndex == -1) return;

            var values = new List<string>();

            for(int i = 0; i < csvData.Columns.Count; i++)
            {
                var content = csvData.Columns[i].GetCellContent(SelectedRow);
                if(content is TextBlock block)
                {
                    values.Add(block.Text);
                }
            }

            Clipboard.SetText(string.Join(";", values));
        }

        private void CopyColumnClicked(object sender, EventArgs e)
        {
            if (SelectedColumnIndex == 1) return;
            var column = dataTable.Columns[SelectedColumnIndex];

            var values = new List<string>();
            foreach (DataRow row in dataTable.Rows)
            {
                values.Add(row[column]?.ToString());
            }

            //var column = csvData.Columns[SelectedColumnIndex];

            //var values = new List<string>();

            //foreach(var item in csvData.Items)
            //{
            //    var cell = column.GetCellContent(item);

            //    if(cell is TextBlock block)
            //    {
            //        values.Add(block.Text);
            //    }
            //}

            Clipboard.SetText(string.Join(Environment.NewLine, values));
        }
    }
}
