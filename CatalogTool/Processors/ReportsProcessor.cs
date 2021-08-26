using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LinqToExcel;

namespace CatalogTool
{
    class ReportsProcessor
    {
        private readonly DataAccess DataAccess = new DataAccess();

        private readonly MainWindow window;

        private ExcelQueryFactory excel = null;
        private string worksheet = null;

        private Dictionary<string, string> columnDataExamles;

        public string ReportFile { get; set; }

        private string[] Columns { get; set; }

        public ReportsProcessor(MainWindow window)
        {
            this.window = window;
            InitForm();
        }

        public void InitForm()
        {
            var catalogs = DataAccess.GetCatalogsList().Select(x => new CatalogsListElement { Name = x }).ToList();
            this.window.FormData.Report.CatalogsList = catalogs;

            this.window.ReportCheckParameters.Items.Clear();
            this.window.ReportCheckParameters.Items.Add("Synchronisation");
            this.window.ReportCheckParameters.Items.Add("Performance");
            this.window.ReportCheckParameters.Items.Add("Mechanical");
        }

        public string[] GetCatalogsList()
        {
            return DataAccess.GetCatalogsList();
        }

        public void Initialize()
        {
            this.window.Logger.Info("on CatalogProcessor.Initialize()");
            try
            {
                window.Dispatcher.Invoke(() => {
                    this.window.ReportLogTxt.Text = "loading...";
                    this.window.ReportPathTxt.Text = ReportFile;
                });

                excel = new ExcelQueryFactory(ReportFile);

                worksheet = excel.GetWorksheetNames().FirstOrDefault();
                Columns = excel.GetColumnNames(worksheet).ToArray();

                var firstRow = excel.Worksheet(worksheet).First();
                columnDataExamles = Columns.ToDictionary(c => c, c => firstRow[c].Value.ToString());

                window.Dispatcher.Invoke(() => {
                    InitCombobox(window.ReportPercentColumnCbx, Columns);
                    InitCombobox(window.ReportCatalogColumnCbx, Columns);
                    InitCombobox(window.ReportColumnTrackNameCbx, Columns);
                    InitCombobox(window.ReportColumnPerformerCbx, Columns);
                    InitCombobox(window.ReportColumnComposerCbx, Columns);
                });

                string fileName = ReportFile.Split(new[] { '/', '\\' }).Last();
                fileName = fileName.Replace(".xls", string.Empty);
                fileName = fileName.Replace(".xlsx", string.Empty);
                fileName = fileName.Replace(".XLS", string.Empty);
                fileName = fileName.Replace(".XLSX", string.Empty);

                window.Dispatcher.Invoke(() => {
                    this.window.ReportPathTxt.Text = ReportFile;
                    this.window.ReportLogTxt.Text = string.Empty;
                    this.window.ReportCheckParameters.SelectedIndex = 0;
                });
            }
            catch (Exception ex)
            {
                this.window.Logger.Error("CatalogProcessor.Initialize() failed", ex);
                throw;
            }
        }

        private void InitCombobox(ComboBox comboBox, string[] options, bool emptyOption = true)
        {
            comboBox.Items.Clear();
            if (emptyOption)
                comboBox.Items.Add(string.Empty);
            foreach (var option in options)
            {
                comboBox.Items.Add(option);
            }
        }

        public void ShowColumnExample(ComboBox columnElement, TextBox exampleElement)
        {
            var column = columnElement.SelectedValue.ToString();
            columnDataExamles.TryGetValue(column, out var value);
            exampleElement.Text = value ?? string.Empty;
        }

        public void UpdateReport(Parameter parameter)
        {
            Log($"завантажується...");

            var file = this.window.FormData.Report.ReportPath;
            var date = DateTime.Now;
            var newFile = file + $"{date.Year}.{date.Month}.{date.Day}.{date.Hour}.{date.Minute}.{date.Second}.xlsx";
            File.Copy(file, newFile);


            var catalogs = DataAccess.GetCatalogs().ToDictionary(x => x.Name, x => x);

            var tracks = LoadRowsFromExcel().ToArray();
            var count = tracks.Length;

            var catalogNames = this.window.FormData.Report.CatalogsList.Where(x => x.IsChecked).Select(x => x.Name).ToArray();
            if (catalogNames.Length == 0)
            {
                MessageBox.Show("Choose catalog");
                return;
            }

            var threashold = this.window.FormData.Report.ThreasholdPercent * 0.01;
            foreach (var catalogName in catalogNames)
            {
                UpdateByCatalog(parameter, catalogs, tracks, count, catalogName, threashold);
            }

            Log("збереження...");

            Microsoft.Office.Interop.Excel.Application xlApp = null;
            Microsoft.Office.Interop.Excel.Workbook xlWorkBook = null;
            Microsoft.Office.Interop.Excel.Worksheet xlWorkSheet = null;


            try
            {
                xlApp = new Microsoft.Office.Interop.Excel.Application();

                if (xlApp == null)
                {
                    MessageBox.Show("Excel is not properly installed!!");
                    return;
                }

                xlApp.Visible = false;
                xlWorkBook = xlApp.Workbooks.Open(newFile);//, 0, true, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                xlWorkSheet = (Microsoft.Office.Interop.Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

                var percentColumnIndex = Array.IndexOf(Columns, this.window.FormData.Report.ColumnPercent) + 1;
                var catalogColumnIndex = Array.IndexOf(Columns, this.window.FormData.Report.CatalogColumn) + 1;

                for (var i = 0; i < count; i++)
                {
                    if (percentColumnIndex > 0)
                        xlWorkSheet.Cells[i + 2, percentColumnIndex] = tracks[i].Percent;
                    if (catalogColumnIndex > 0)
                        xlWorkSheet.Cells[i + 2, catalogColumnIndex] = tracks[i].Catalog;
                    Log($"збережено {i+1} з {count}");
                }

                xlWorkBook.Save();
            }
            finally
            {
                xlWorkBook.Close();
                xlApp.Quit();

                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(xlWorkSheet);
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(xlWorkBook);
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(xlApp);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private string[] ToWords(string value) => value?
            .Split(null)
            .SelectMany(s => s.Split('.', ',', '-', '\'', '"'))
            .Where(w => w != null)
            .ToArray()
            ?? new string[0];

        private void UpdateByCatalog(Parameter parameter, Dictionary<string, Catalog> catalogs, ReportTrack[] tracks, int count, string catalogName, double threashold)
        {
            catalogs.TryGetValue(catalogName, out var catalog);

            for (var i = 0; i < count; i++)
            {
                var score = 0d;
                var percent = 0d;

                var track = tracks[i];
                var wordsTrackName = ToWords(track.TrackName);
                var wordsPerformer = ToWords(track.Performer);
                var wordsComposer = ToWords(track.Composer);

                foreach (var catalogTrack in DataAccess.FindCatalogTracks(catalogName, wordsTrackName, wordsPerformer, wordsComposer, threashold))
                {
                    var iscore = CompareTrack(catalogTrack, track, catalog);
                    if (iscore > score)
                    {
                        score = iscore;
                        switch (parameter)
                        {
                            case Parameter.Synchronisation:
                                percent = catalogTrack.Synchronisation;
                                break;
                            case Parameter.Performance:
                                percent = catalogTrack.Performance;
                                break;
                            case Parameter.Mechanical:
                                percent = catalogTrack.Mechanical;
                                break;
                        }
                    }
                }

                if (percent > 0)
                {
                    var p = tracks[i].Percent;
                    if (string.IsNullOrEmpty(tracks[i].Percent))
                    {
                        tracks[i].Percent = percent.ToString();
                    }
                    else
                    {
                        tracks[i].Percent += ", " + percent.ToString();
                    }
                    if (string.IsNullOrEmpty(tracks[i].Catalog))
                    {
                        tracks[i].Catalog = catalog.Name;
                    }
                    else
                    {
                        tracks[i].Catalog += ", " + catalog.Name;
                    }
                }
                else if (string.IsNullOrEmpty(tracks[i].Percent))
                {
                    tracks[i].Percent = "0";
                }

                Log($"оброблено {i+1} з {count} згідно каталогу {catalog.Name}");
            }
        }

        public void Log(string text)
        {
            this.window.ReportLogTxt.Dispatcher.Invoke(
                new UpdateTextCallback(this.UpdateLog),
                args: new object[] { text }
            );
        }

        private delegate void UpdateTextCallback(string message);

        private void UpdateLog(string message)
        {
            this.window.ReportLogTxt.Text = message;
        }

        private double CompareTrack(Track catalogTrack, Track track, Catalog catalog)
        {
            return CompareTrackValue(catalogTrack.TrackName, track.TrackName, this.window.FormData.Report.ColumnTrackNamePercent)
                + CompareTrackValue(catalogTrack.Performer, track.Performer, this.window.FormData.Report.ColumnPerformerPercent)
                + CompareTrackValue(catalogTrack.Composer, track.Composer, this.window.FormData.Report.ColumnComposerPercent);
        }

        private double CompareTrackValue(string catalogValue, string value, double minPercent)
        {
            if (string.IsNullOrEmpty(catalogValue) || string.IsNullOrEmpty(value))
                return 0;

            var s = StringsSimilarity.Compute(catalogValue, value);

            return s * 100 < minPercent ? 0 : s;
        }

        private IEnumerable<ReportTrack> LoadRowsFromExcel()
        {
            var columnTrackName = window.FormData.Report.ColumnTrackName;
            var columnPerformer = window.FormData.Report.ColumnPerformer;
            var columnComposer = window.FormData.Report.ColumnComposer;
            var columnPercent = window.FormData.Report.ColumnPercent;

            foreach (var row in excel.Worksheet(worksheet))
            {
                var catalog = new ReportTrack {  };
                if (!string.IsNullOrEmpty(columnTrackName))
                {
                    catalog.TrackName = row[columnTrackName];
                }
                if (!string.IsNullOrEmpty(columnPerformer))
                {
                    catalog.Performer = row[columnPerformer];
                }
                if (!string.IsNullOrEmpty(columnComposer))
                {
                    catalog.Composer = row[columnComposer];
                }
                if (!string.IsNullOrEmpty(columnPercent))
                {
                    catalog.Percent = row[columnPercent];
                }
                yield return catalog;
            }
        }
    }

    enum Parameter
    {
        Synchronisation,
        Performance,
        Mechanical
    }

    class ReportTrack : Track
    {
        public string Percent { get; set; }

        public string Catalog { get; set; }
    }

    interface IComparer<T>
    {
        double Dist(T x, T y);

        double Length(T x);
    }

    class CharComparer : IComparer<char>
    {
        public double Dist(char x, char y)
        {
            return x == y ? 0 : string.Equals(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase) ? 0.9 : 1;
        }

        public double Length(char x)
        {
            return 1;
        }
    }

    class LevenshteinComparer<T> : IComparer<T[]>
    {
        private readonly IComparer<T> singleComparer;

        public LevenshteinComparer(IComparer<T> singleComparer) => this.singleComparer = singleComparer;

        public double Length(T[] x)
        {
            return x.Sum(a => singleComparer.Length(a));
        }

        public double Dist(T[] x, T[] y)
        {
            int n = x.Length;
            int m = y.Length;

            if (x.Length == 0 || y.Length == 0)
            {
                return Length(x) + Length(y);
            }

            var d = new double[n + 1, m + 1];

            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 1; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    var cost = singleComparer.Dist(y[j - 1], x[i - 1]);

                    var min1 = d[i - 1, j] + cost;
                    var min2 = d[i, j - 1] + cost;

                    var min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }
            return d[n, m];
        }
    }

    class SimpleStringComparer : IComparer<string>
    {
        private readonly IComparer<char[]> charsComparer = new LevenshteinComparer<char>(new CharComparer());

        public double Dist(string x, string y) => charsComparer.Dist(x.ToArray(), y.ToArray());

        public double Length(string x) => x.Length;//charsComparer.Length(x.ToArray());
    }

    class IndexedString
    {
        public string Value { get; set; }

        public int Index { get; set; }
    }

    class IndexedStringsComparer : IComparer<IndexedString>
    {
        private readonly IComparer<string> simpleStringComparer;

        private readonly int MaxIndex;

        public IndexedStringsComparer(int maxIndex, IComparer<string> simpleStringComparer)
        {
            this.simpleStringComparer = simpleStringComparer ?? new SimpleStringComparer();
            MaxIndex = maxIndex;
            //length = Math.Sqrt(MaxIndex);
        }

        public double Dist(IndexedString x, IndexedString y)
        {
            var d = simpleStringComparer.Dist(x.Value, y.Value);
            var shift = /*length **/ Math.Abs(x.Index - y.Index);// / (MaxIndex + 1);
            return (d + shift);
        }

        public double Length(IndexedString x) => simpleStringComparer.Length(x.Value);
    }

    class SentenceStringComparer : IComparer<string>
    {
        private readonly IComparer<string> simpleComparer = new SimpleStringComparer();

        public double Dist(string x, string y)
        {
            var xWords = ToIndexedWords(x);
            var yWords = ToIndexedWords(y);

            var d = simpleComparer.Dist(string.Join(" ", xWords.Select(a => a.Value)), string.Join(" ", yWords.Select(a => a.Value)));

            var sc = new IndexedStringsComparer(Math.Max(xWords.Length, yWords.Length), simpleComparer);
            var lc = new LevenshteinComparer<IndexedString>(sc);
            return lc.Dist(xWords, yWords) + d;
        }

        public double Length(string x)
        {
            var xWords = ToIndexedWords(x);
            var sc = new IndexedStringsComparer(xWords.Length, simpleComparer);
            return xWords.Sum(a => sc.Length(a)) + x.Length + 1;
        }

        private static IndexedString[] ToIndexedWords(string x)
        {
            return x.Split(null)
                .Select((s, i) => new IndexedString { Value = s, Index = i })
                .OrderBy(a => a.Value)
                .ToArray();
        }
    }

    static class StringsSimilarity
    {
        public static double Compute(string x, string y)
        {
            if (string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y))
                return 0d;

            var comparer = new SentenceStringComparer();

            var length = comparer.Length(x) + comparer.Length(y);
            var dist = comparer.Dist(x, y);

            var same = length - 2 * dist;

            if (length <= 0 || same <= 0)
                return 0;

            return Math.Sqrt(same / length);
        }
    }
}
