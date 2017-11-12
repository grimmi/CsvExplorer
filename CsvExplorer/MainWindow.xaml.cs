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

        public DataView CsvData { get; set; }
        private DataTypeGuesser Guesser { get; set; }

        private static Func<string[], int, bool> defaultFilter = (vs, c) => true;
        private Func<string[], int, bool> currentFilter = defaultFilter;
        public Func<string[], int, bool> CurrentFilter
        {
            get { return currentFilter; }
            set { currentFilter = value; LoadData(); }
        }

        private string currentFile = "";
        private string CurrentFile
        {
            get { return currentFile; }
            set { currentFile = value; LoadData(); }
        }

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
                }

                var contentLine = "";
                var rows = new List<string[]>();
                foreach(var probeLine in probeLines)
                {
                    if (CurrentFilter(probeLine, 1))
                    {
                        data.Rows.Add(probeLine);
                    }
                }
                while((contentLine = reader.ReadLine()) != null)
                {
                    var contentParts = contentLine.Split(';');
                    if (CurrentFilter(contentParts, 2))
                    {
                        data.Rows.Add(contentParts);
                    }
                }

                CsvData = data.DefaultView;
            }
        }

        private void DataGridFilterChanged(object sender, TextChangedEventArgs e)
        {
            var tBox = sender as TextBox;
            if(tBox.Text.Length > 2)
            {
                CurrentFilter = (vs, c) => vs[1].ToLower().Contains(tBox.Text.ToLower());
            }
            else
            {
                CurrentFilter = defaultFilter;
            }
        }
    }
}
