using System;
using System.Collections.Generic;

namespace CatalogTool
{
    public class ReportForm
    {
        private float _columnTrackNamePercent = 90;
        private float _columnPerformerPercent = 90;
        private float _columnComposerPercent = 90;
        private float _threasholdPercent = 70;

        public List<CatalogsListElement> CatalogsList { get; set; }

        public string ReportCheckParameter { get; set; }

        public string ColumnTrackName { get; set; }
        public string ColumnPerformer { get; set; }
        public string ColumnComposer { get; set; }

        public float ColumnTrackNamePercent { get => _columnTrackNamePercent; set { _columnTrackNamePercent = Math.Max(0, Math.Min(100, value)); } }
        public float ColumnPerformerPercent { get => _columnPerformerPercent; set { _columnPerformerPercent = Math.Max(0, Math.Min(100, value)); } }
        public float ColumnComposerPercent { get => _columnComposerPercent; set { _columnComposerPercent = Math.Max(0, Math.Min(100, value)); } }
        public double ThreasholdPercent => GetThreashold();

        private double GetThreashold()
        {
            short value = Convert.ToInt16(Math.Round(_columnTrackNamePercent * _columnPerformerPercent * _columnComposerPercent * 0.000096));
            return Math.Max(0d, Math.Min(100d, value));
        }

        public string ColumnPercent { get; set; }
        public string CatalogColumn { get; set; }

        public string ReportPath { get; set; }
    }
}
