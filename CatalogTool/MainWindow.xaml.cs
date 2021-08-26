using log4net;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace CatalogTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CatalogProcessor CatalogProcessor { get; set; }

        private ReportsProcessor ReportsProcessor { get; set; }

        private DatabaseProcessor DatabaseProcessor { get; set; }

        public MainForm FormData { get; set; }

        public ILog Logger;

        public MainWindow()
        {
            InitializeComponent();
            LoadWindow();
        }

        private void LoadWindow()
        {
            this.FormData = new MainForm();
            this.DataContext = this.FormData;

            this.CatalogProcessor = new CatalogProcessor(this);
            this.ReportsProcessor = new ReportsProcessor(this);
            this.DatabaseProcessor = new DatabaseProcessor(this);

            Logger = LogManager.GetLogger("mainlog");
            Logger.Info("app start");
        }

        private void AddCatalogBtn_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug("on AddCatalogBtn_Click");
            var openDialog = new OpenFileDialog();
            openDialog.Multiselect = false;
            openDialog.InitialDirectory = Environment.CurrentDirectory;
            openDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
            var res = openDialog.ShowDialog();
            if (res == true)
            {
                try
                {
                    this.CatalogProcessor.CatalogFile = openDialog.FileName;
                    new Thread(this.CatalogProcessor.Initialize).Start();
                }
                catch (Exception ex)
                {
                    Logger.Error("AddCatalogBtn_Click failed", ex);
                    throw;
                }
            }
        }

        private void CatalogColumnTrackNameCbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CatalogProcessor?.ShowColumnExample(CatalogColumnTrackNameCbx, CatalogColumnTrackNameExampleTxt);
        }

        private void CatalogColumnPerformerCbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CatalogProcessor?.ShowColumnExample(CatalogColumnPerformerCbx, CatalogColumnPerformerExampleTxt);
        }

        private void CatalogColumnComposerCbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CatalogProcessor?.ShowColumnExample(CatalogColumnComposerCbx, CatalogColumnComposerExampleTxt);
        }

        private void CatalogColumnSynchronisationCbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CatalogProcessor?.ShowColumnExample(CatalogColumnSynchronisationCbx, CatalogColumnSynchronisationExampleTxt);
        }

        private void CatalogColumnMechanicalCbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CatalogProcessor?.ShowColumnExample(CatalogColumnMechanicalCbx, CatalogColumnMechanicalExampleTxt);
        }

        private void CatalogColumnPerformanceCbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CatalogProcessor?.ShowColumnExample(CatalogColumnPerformanceCbx, CatalogColumnPerformanceExampleTxt);
        }

        private async void LoadCatalogBtn_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug("on LoadCatalogBtn_Click");
            try
            {
                Task task = new Task(CatalogProcessor.ImportCatalog);
                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                Logger.Error("LoadCatalogBtn_Click failed");
                throw;
            }
            
            MessageBox.Show("Готово!");
        }

        private void AddReportBtn_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("on AddReportBtn_Click");
            try
            {
                var openDialog = new OpenFileDialog();
                openDialog.Multiselect = false;
                openDialog.InitialDirectory = Environment.CurrentDirectory;
                openDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
                var res = openDialog.ShowDialog();
                if (res == true)
                {
                    this.ReportsProcessor.ReportFile = openDialog.FileName;
                    new Thread(this.ReportsProcessor.Initialize).Start();
                    //this.CatalogProcessor.Initialize();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("AddReportBtn_Click failed", ex);
                throw;
            }
        }
        
        private void OnNavigateEmail(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private async void UpdateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug("on UpdateReportBtn_Click");
            try
            {
                Task task = Task.CompletedTask;
                switch (this.FormData.Report.ReportCheckParameter)
                {
                    case "Synchronisation":
                        task = new Task(() => ReportsProcessor?.UpdateReport(Parameter.Synchronisation));
                        break;
                    case "Performance":
                        task = new Task(() => ReportsProcessor?.UpdateReport(Parameter.Performance));
                        break;
                    case "Mechanical":
                        task = new Task(() => ReportsProcessor?.UpdateReport(Parameter.Mechanical));
                        break;
                }

                task.Start();
                await task;
            }
            catch (Exception ex)
            {
                Logger.Error("UpdateReportBtn_Click failed");
                throw;
            }

            MessageBox.Show("Готово!");
        }
        

        private void ReportColumnTrackNameCbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReportsProcessor?.ShowColumnExample(ReportColumnTrackNameCbx, ReportColumnTrackNameExampleTxt);
        }

        private void ReportColumnPerformerCbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReportsProcessor?.ShowColumnExample(ReportColumnPerformerCbx, ReportColumnPerformerExampleTxt);
        }

        private void ReportColumnComposerCbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReportsProcessor?.ShowColumnExample(ReportColumnComposerCbx, ReportColumnComposerExampleTxt);
        }


        private void ReportPercentColumnCbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReportsProcessor?.ShowColumnExample(ReportPercentColumnCbx, ReportPercentColumnExampleTxt);
        }

        private void ReportCatalogColumnCbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReportsProcessor?.ShowColumnExample(ReportCatalogColumnCbx, ReportCatalogColumnExampleTxt);
        }

        private void RemoveCatalogBtn_Click(object sender, RoutedEventArgs e)
        {
            Logger.Debug("on RemoveCatalogBtn_Click");
            try
            {
                DatabaseProcessor.RemoveCatalog();
            }
            catch (Exception ex)
            {
                Logger.Error("RemoveCatalogBtn_Click failed");
                throw;
            }

            MessageBox.Show("Готово!");
        }

        private void PercentUpdated(object sender, DependencyPropertyChangedEventArgs e)
        {
        }
    }
}
