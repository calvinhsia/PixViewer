﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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

namespace PixViewer
{
    /*
<Window x:Class="PixViewer.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:PixViewer"
        mc:Ignorable="d"
        Title="MainWindow" Height="450" Width="1200">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="40"/>
        </Grid.RowDefinitions>
        <DockPanel Grid.Row="0" Name="dp"/>
        <StackPanel Grid.Row="1" Orientation="Horizontal">
            <TextBox Text="{Binding CurPicIndex }" Width="50"/>
            <TextBox Text="{Binding Filename}" Width="300"/>
            <TextBox Text="{Binding Dtime}" Width="150"/>
            <Label Content="{Binding Notes}" Width="700"/>
            <Button Name="btngoBack" Content="&lt;" Width="20" Click="btngo_Click"/>
            <Button Name="btngoForward" Content=">" Width="20" Click="btngo_Click" />
        </StackPanel>
    </Grid>
</Window>


Watch out for embedded quote marks in notes.
BROWSE FOR ATC("lila",notes)>0
COPY TO t.txt csv

    todo: add WholeWord, RegEx query

     */
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public string Filename { get; set; }
        public DateTime Dtime { get; set; }
        public string Notes { get; set; }
        public string CurPicIndex { get; set; }


        public bool IncludePix { get; set; } = true;
        public bool IncludeMovies { get; set; } = true;
        public DateTime DtFrom { get; set; } = DateTime.Parse("01/01/2000");
        public DateTime DtTo { get; set; } = DateTime.Parse("01/01/2020");

        string rootPix = @"c:\users\calvinh\OneDrive\Pictures\OldPictures\";
        string targFolder = @"C:\Users\calvinh\OneDrive\QueryResults";
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        List<PicData> lstAllPicData = new List<PicData>();

        List<PicData> lstFilteredPicData = new List<PicData>();
        int curNdx;
        private CancellationTokenSource ctsPublish;

        public MainWindow()
        {
            InitializeComponent();
            this.Title = "PixViewer";
            this.WindowState = WindowState.Maximized;
            this.DataContext = this;
            this.Loaded += (o, e) =>
            {
                this.cboQueryString.Initialize(nameof(Properties.Settings.Default.QueryString));
                var fnCSV = Environment.ExpandEnvironmentVariables(@"c:\pictures\t.txt");
                var firstline = true;
                var dictFields = new Dictionary<string, int>(); //fldname->index
                using (var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(fnCSV))// handles quoted values in csv
                {
                    parser.Delimiters = new[] { "," };
                    while (!parser.EndOfData)
                    {
                        var flds = parser.ReadFields();
                        if (firstline)
                        {
                            foreach (var item in flds)
                            {
                                dictFields[item] = dictFields.Count;
                            }
                            firstline = false;
                        }
                        else
                        {
                            var data = new PicData()
                            {
                                fullname = flds[dictFields["fullname"]].ToLower(),
                                dtime = DateTime.Parse(flds[dictFields["dtime"]]),
                                notes = flds[dictFields["notes"]],
                                rotate = int.Parse(flds[dictFields["rotate"]])
                            };
                            {
                                lstAllPicData.Add(data);
                            }
                        }
                    }
                }
                this.curNdx = -1;
                this.btnQuery.RaiseEvent(new RoutedEventArgs() { RoutedEvent = Button.ClickEvent, Source = this });
            };
        }
        class PicData
        {
            public string fullname;
            public DateTime dtime;
            public string notes;
            public int rotate;
            public bool IsPicture => fullname.EndsWith("jpg");
            public override string ToString()
            {
                return $"{dtime} {notes} {fullname}";
            }
        }
        void ShowPic(PicData fileData)
        {
            var picFile = System.IO.Path.Combine(rootPix, fileData.fullname);
            if (File.Exists(picFile))
            {
                try
                {
                    if (fileData.IsPicture)
                    {
                        var im = new BitmapImage(new Uri(picFile));
                        this.dp.Children.Clear();
                        this.dp.Children.Add(new Image() { Source = im });
                    }
                    else
                    {
                        var w = new WebBrowser();
                        w.Navigate(picFile);
                        this.dp.Children.Clear();
                        this.dp.Children.Add(w);
                    }
                    this.CurPicIndex = $"{curNdx}/{lstFilteredPicData.Count}";
                    this.Filename = fileData.fullname;
                    this.Dtime = fileData.dtime;
                    this.Notes = fileData.notes;
                    RaisePropChanged(nameof(CurPicIndex));
                    RaisePropChanged(nameof(Dtime));
                    RaisePropChanged(nameof(Filename));
                    RaisePropChanged(nameof(Notes));
                }
                catch (Exception ex)
                {
                    this.ShowError(ex);
                }
            }
        }

        private void ShowError(Exception ex)
        {
            var w = new Window();
            w.Content = ex.ToString();
            w.ShowDialog();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            switch (e.Key)
            {
                case Key.Left:
                    this.btnNavBack.RaiseEvent(new RoutedEventArgs() { RoutedEvent = Button.ClickEvent, Source = this.btnNavBack });
                    e.Handled = true;
                    break;
                case Key.Right:
                    this.btnNavForward.RaiseEvent(new RoutedEventArgs() { RoutedEvent = Button.ClickEvent, Source = this });
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        private void btnNav_Click(object sender, RoutedEventArgs e)
        {
            var incr = 1;
            if (sender is Button btn && btn.Name == nameof(this.btnNavBack))
            {
                incr = -1;
            }
            if (this.curNdx + incr >= 0 && this.curNdx + incr < this.lstFilteredPicData.Count)
            {
                this.curNdx += incr;
                ShowPic(this.lstFilteredPicData[this.curNdx]);
            }
        }

        private void btnQuery_Click(object sender, RoutedEventArgs e)
        {
            lstFilteredPicData = lstAllPicData
                .Where(t => t.dtime >= DtFrom && t.dtime <= DtTo)
                .Where(t =>
            {
                if (t.notes.IndexOf(cboQueryString.Text, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
                if (t.IsPicture)
                {
                    return IncludePix;
                }
                return IncludeMovies;
            }).ToList();
            curNdx = -1;
            btnNavForward.RaiseEvent(new RoutedEventArgs() { RoutedEvent = Button.ClickEvent, Source = this });
        }

        private async void btnPublish_Click(object sender, RoutedEventArgs e)
        {
            this.progBar.Value = 0;
            this.progBar.Visibility = Visibility.Visible;
            this.btnCancel.Visibility = Visibility.Visible;
            this.btnPublish.IsEnabled = false;
            this.ctsPublish = new CancellationTokenSource();
            /*
            await Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    _ = this.progBar.Dispatcher.BeginInvoke(new Action(() =>
                     {
                         this.progBar.Value = 10 * i;

                     }));
                    if (this.ctsPublish.IsCancellationRequested)
                    {
                        break;
                    }
                }
            });
            /*/
            int ndx = 0;
            this.progBar.Maximum = lstFilteredPicData.Count;

            await Task.Run(() =>
            {
                foreach (var data in lstFilteredPicData)
                {
                    var targFilename = System.IO.Path.Combine(targFolder, System.IO.Path.GetFileName(data.fullname));
                    if (!File.Exists(targFilename))
                    {
                        var picFile = System.IO.Path.Combine(rootPix, data.fullname);
                        File.Copy(picFile, targFilename);
                    }
                    else
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                    if (this.ctsPublish.IsCancellationRequested)
                    {
                        break;
                    }
                    ndx++;
                    _ = this.progBar.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.progBar.Value = ndx;

                    }));
                }
            });
            //*/
            this.progBar.Visibility = Visibility.Hidden;
            this.btnCancel.Visibility = Visibility.Hidden;
            this.btnPublish.IsEnabled = true;

        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.ctsPublish.Cancel();
            this.btnCancel.Visibility = Visibility.Hidden;
        }
    }
}
