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
        public ICommand ClearFilterCommand => new RelayCommand((o) => Loader.ClearFilters());

        public ICommand HideColumnCommand => new RelayCommand((o) => Loader.HideColumn(SelectedColumnIndex));

        public ICommand ShowColumnsCommand => new RelayCommand((o) => { Loader.ClearHiddenColumns(); });

        public ICommand ReloadDocumentCommand => new RelayCommand((o) => { Loader.ClearHiddenColumns(false); Loader.ClearFilters(); });

        public DataView CsvData { get; set; }

        private DataTable dataTable;
        
        private static Func<string[], int, bool> defaultFilter = (vs, c) => true;

        private string currentFile = "";
        public string CurrentFile
        {
            get { return currentFile; }
            set { currentFile = value; }
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

        private DataLoader Loader { get; } = new DataLoader();

        public MainWindow()
        {
            InitializeComponent();
            Loader.DataLoaded += LoadData;
        }

        private void OpenFile()
        {
            var dialog = new OpenFileDialog();
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                CurrentFile = dialog.FileName;
                Loader.CurrentFile = CurrentFile;
            }
        }

        private void LoadData(object sender, DataLoadedEventArgs loadedData)
        {
            CsvData = loadedData.LoadedData.DefaultView;
        }

        private void DataGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                RightClickPosition = e.GetPosition(csvData);

                var hitResult = VisualTreeHelper.HitTest(csvData, RightClickPosition);
                var hit = hitResult.VisualHit;

                if ((hit as FrameworkElement).Parent is DataGridCell cell)
                {
                    SelectedColumnIndex = cell.Column.DisplayIndex;
                    SelectedColumn = cell.Column.Header?.ToString() ?? "";

                    var parent = VisualTreeHelper.GetParent(cell);
                    while (parent != null && !(parent is DataGridRow))
                    {
                        parent = VisualTreeHelper.GetParent(parent);
                    }

                    if (parent is DataGridRow row)
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
                Loader.AddFilter(new TextFilter(tBox.Text.ToLower()), SelectedColumnIndex);
            }

            tBox.Text = string.Empty;
        }

        private void HideColumnClicked(object sender, EventArgs e) => HideColumnCommand.Execute(null);

        private void CopyRowClicked(object sender, EventArgs e)
        {
            if (SelectedRowIndex == -1) return;

            var values = new List<string>();

            for (int i = 0; i < csvData.Columns.Count; i++)
            {
                var content = csvData.Columns[i].GetCellContent(SelectedRow);
                if (content is TextBlock block)
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

            Clipboard.SetText(string.Join(Environment.NewLine, values));
        }
    }
}
