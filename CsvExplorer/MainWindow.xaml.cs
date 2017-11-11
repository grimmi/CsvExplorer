﻿using Microsoft.Win32;
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

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenFile()
        {
            var dialog = new OpenFileDialog();
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                LoadIntoDataGrid(dialog.FileName);
            }
        }

        private void LoadIntoDataGrid(string file)
        {
            var data = new DataTable();
            using (var reader = new StreamReader(file))
            {
                var headerLine = reader.ReadLine();
                var headerNames = headerLine.Split(';');
                foreach(var header in headerNames)
                {
                    data.Columns.Add(new DataColumn(header, typeof(string)));
                }

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

                foreach(var column in probeData)
                {
                    var dataType = new DataTypeGuesser().GuessType(column.ToArray());
                    MessageBox.Show(dataType.ToString());
                }

                var contentLine = "";
                var rows = new List<string[]>();
                while((contentLine = reader.ReadLine()) != null)
                {
                    var contentParts = contentLine.Split(';');
                    data.Rows.Add(contentParts);
                }

                CsvData = data.DefaultView;
            }
        }
    }
}
