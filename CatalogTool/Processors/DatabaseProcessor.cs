using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CatalogTool
{
    class DatabaseProcessor
    {
        private readonly DataAccess DataAccess = new DataAccess();

        private readonly MainWindow window;

        public DatabaseProcessor(MainWindow window)
        {
            this.window = window;
            InitForm();
        }

        public void InitForm()
        {
            var catalogs = DataAccess.GetCatalogCounts().ToDictionary(x => x.Catalog, x => $"{x.Catalog} (записів: {x.Count})");

            InitCombobox(this.window.DatabaseCatalogsCbx, catalogs.Values.ToArray(), false);
        }

        public void RemoveCatalog()
        {
            var catalog = this.window.DatabaseCatalogsCbx.SelectedValue?.ToString();
            if (string.IsNullOrEmpty(catalog))
                return;
            catalog = catalog.Split(new[] { " (записів: " }, StringSplitOptions.RemoveEmptyEntries).First();

            DataAccess.RemoveCatalog(catalog);
            MessageBox.Show("Видалено!");
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
    }
}
