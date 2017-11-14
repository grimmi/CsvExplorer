using Microsoft.Win32;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        public DataView CsvData { get; set; }
        private DataTypeGuesser Guesser { get; set; }

        private Dictionary<int, List<Filter>> FilterMap { get; } = new Dictionary<int, List<Filter>>();

        private static Func<string[], int, bool> defaultFilter = (vs, c) => true;

        private string currentFile = "";
        public string CurrentFile
        {
            get { return currentFile; }
            set { currentFile = value; LoadData(); }
        }

        public Point RightClickPosition { get; private set; }

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
        
        private void LoadData()
        {
            var data = new DataTable();
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

                var guessedDatatypes = new List<DataType>();

                foreach(var column in probeData)
                {
                    guessedDatatypes.Add(Guesser.GuessType(column.ToArray()));
                }

                for(int i = 0; i < headerNames.Length; i++)
                {
                    var header = $"{headerNames[i]} ({guessedDatatypes[i]})";
                    data.Columns.Add(new DataColumn(header, typeof(string)));
                    if (!FilterMap.ContainsKey(i))
                    {
                        FilterMap[i] = new List<Filter>();
                    }
                }

                var contentLine = "";
                var rows = new List<string[]>();
                foreach(var probeLine in probeLines)
                {
                    if (CheckFilterList(probeLine))
                    {
                        data.Rows.Add(probeLine);
                    }
                }
                while((contentLine = reader.ReadLine()) != null)
                {
                    var contentParts = contentLine.Split(';');
                    if (CheckFilterList(contentParts))
                    {
                        data.Rows.Add(contentParts);
                    }
                }

                CsvData = data.DefaultView;
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
            }
        }

        private void FilterBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var hitResult = VisualTreeHelper.HitTest(csvData, RightClickPosition);
            var hit = hitResult.VisualHit;

            var columnIndex = -1;

            if ((hit as FrameworkElement).Parent is DataGridCell cell)
            {
                columnIndex = cell.Column.DisplayIndex;
            }

            var tBox = sender as TextBox;
            if (columnIndex > -1)
            {
                AddFilter(new TextFilter(tBox.Text.ToLower()), columnIndex);
            }
        }
    }
}
