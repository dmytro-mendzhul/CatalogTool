using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using LinqToExcel;

namespace CatalogTool
{
    class CatalogProcessor
    {
        private readonly DataAccess DataAccess = new DataAccess();

        private readonly MainWindow window;

        private ExcelQueryFactory excel = null;
        private string worksheet = null;

        private Dictionary<string, string> columnDataExamles;

        public string CatalogFile { get; set; }

        public CatalogProcessor(MainWindow window)
        {
            this.window = window;
        }

        public void Initialize()
        {
            this.window.Logger.Info("on CatalogProcessor.Initialize()");

            try
            {
                Log("завантаження...");
                window.Dispatcher.Invoke(() => {
                    this.window.CatalogPathTxt.Text = CatalogFile;
                });

                this.window.Logger.Info("on CatalogProcessor.Initialize() open excel");
                excel = new ExcelQueryFactory(CatalogFile);

                worksheet = excel.GetWorksheetNames().FirstOrDefault();
                var columns = excel.GetColumnNames(worksheet).ToArray();
                this.window.Logger.Info($"on CatalogProcessor.Initialize() columns:{columns?.Length}");

                var firstRow = excel.Worksheet(worksheet).First();
                columnDataExamles = columns.ToDictionary(c => c, c => firstRow[c].Value.ToString());
                this.window.Logger.Info($"on CatalogProcessor.Initialize() columnDataExamles:{columnDataExamles?.Count}");

                window.Dispatcher.Invoke(() => {
                    InitCombobox(window.CatalogColumnTrackNameCbx, columns);
                    InitCombobox(window.CatalogColumnPerformerCbx, columns);
                    InitCombobox(window.CatalogColumnComposerCbx, columns);
                    InitCombobox(window.CatalogColumnSynchronisationCbx, columns);
                    InitCombobox(window.CatalogColumnMechanicalCbx, columns);
                    InitCombobox(window.CatalogColumnPerformanceCbx, columns);
                });


                string fileName = CatalogFile.Split(new[] { '/', '\\' }).Last();
                fileName = fileName.Replace(".xls", string.Empty);
                fileName = fileName.Replace(".xlsx", string.Empty);
                fileName = fileName.Replace(".XLS", string.Empty);
                fileName = fileName.Replace(".XLSX", string.Empty);

                window.Dispatcher.Invoke(() => {
                    this.window.CatalogNameTxt.Text = fileName;
                });
                Log("");
            }
            catch (Exception ex)
            {
                this.window.Logger.Error($"on CatalogProcessor.Initialize() failed", ex);
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

        public void ImportCatalog()
        {
            try
            {
                var count = excel.Worksheet(worksheet).Count();
                Log($"кількість записів: {count}");

                var catalogName = window.FormData.Catalog.CatalogName;

                var catalog = new Catalog
                {
                    Name = catalogName,
                };

                DataAccess.AddCatalog(catalog);

                var tracks = LoadCatalogRowsFromExcel(count);

                DataAccess.AddTracks(tracks, catalog);

                Log($"Завантажено: {count}");
            }
            catch (Exception ex)
            {
                this.window.Logger.Error($"on CatalogProcessor.Initialize() failed", ex);
                throw;
            }
        }

        public void Log(string text)
        {
            this.window.CatalogLogTxt.Dispatcher.Invoke(
                new UpdateTextCallback(this.UpdateLog),
                args: new object[] { text }
            );
        }

        private delegate void UpdateTextCallback(string message);

        private void UpdateLog(string message)
        {
            this.window.CatalogLogTxt.Text = message;
        }

        private IEnumerable<CatalogTrack> LoadCatalogRowsFromExcel(int count)
        {
            var columnTrackName = window.FormData.Catalog.ColumnTrackName;
            var columnPerformer = window.FormData.Catalog.ColumnPerformer;
            var columnComposer = window.FormData.Catalog.ColumnComposer;
            var columnSynchronisation = window.FormData.Catalog.ColumnSynchronisation;
            var columnMechanical = window.FormData.Catalog.ColumnMechanical;
            var columnPerformance = window.FormData.Catalog.ColumnPerformance;
            var i = 0;

            foreach (var row in excel.Worksheet(worksheet))
            {
                var catalog = new CatalogTrack();
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
                if (!string.IsNullOrEmpty(columnSynchronisation))
                {
                    catalog.Synchronisation = ToPercent(row[columnSynchronisation]);
                }
                if (!string.IsNullOrEmpty(columnMechanical))
                {
                    catalog.Mechanical = ToPercent(row[columnMechanical]);
                }
                if (!string.IsNullOrEmpty(columnPerformance))
                {
                    catalog.Performance = ToPercent(row[columnPerformance]);
                }
                yield return catalog;
                Log($"Завантажено: {++i} з {count}");
            }
        }

        private double ToPercent(string value)
        {
            try
            {
                var percent = Regex.Match(value, @"\d+[\.|,]?\d*").Value.Replace(',', '.');
                var format = new NumberFormatInfo();
                // Set the 'splitter' for thousands
                format.NumberGroupSeparator = ".";
                return double.Parse(percent);
            }
            catch
            {
                return 0;
            }
        }
    }
}
