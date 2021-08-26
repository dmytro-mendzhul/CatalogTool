namespace CatalogTool
{
    public class MainForm
    {
        public CatalogForm Catalog { get; set; } = new CatalogForm();

        public ReportForm Report { get; set; } = new ReportForm();

        public DatabaseForm Database { get; set; } = new DatabaseForm();
    }
}
