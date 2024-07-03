using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ImageTableDesigner
{
    public class GridModel
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public List<double> ColumnDefinitions { get; set; } = new List<double>();
        public List<double> RowDefinitions { get; set; } = new List<double>();
        public List<CellModel> Cells { get; set; } = new List<CellModel>();
        public GridData Separators { get; set; } = new GridData();
    }

    public class SeparatorModel
    {
        public bool IsHorizontal { get; set; }
        public double Position { get; set; }
    }

    public class CellModel
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public int RowSpan { get; set; } = 1;
        public int ColumnSpan { get; set; } = 1;
        public bool IsMerged { get; set; } = false;
    }

    public class GridData
    {
        public List<double> RowSeparators { get; set; } = new List<double>();
        public List<double> ColumnSeparators { get; set; } = new List<double>();
        public List<CellModel> MergedCells { get; set; } = new List<CellModel>();
    }

    public class ThumbData
    {
        public string Direction { get; set; }
        public UIElement Target { get; set; }
    }
}
