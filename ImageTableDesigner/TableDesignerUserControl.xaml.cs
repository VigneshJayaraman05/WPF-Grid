using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ImageTableDesigner
{
    public partial class TableDesignerUserControl : UserControl
    {
        private Point startPoint;
        private Rectangle selectionRectangle;
        private Border resizeBorder;
        private bool isResizing = false;
        private bool isDragging = false;
        private string resizeDirection;
        private bool _columnSeperator;
        private bool _rowSeperator;
        private bool gridGenerator;
        private Grid selectedGrid;
        private Dictionary<Grid, GridData> gridDataMap = new Dictionary<Grid, GridData>();
        private List<Border> selectedCells = new List<Border>();
        private List<Border> gridBorders = new List<Border>();
        private Border activeBorder = null;
        private bool isSelecting = false;
        private bool isDeselecting = false;
        private Border initialCell = null;
        private bool isMouseDown = false;
        private int lastColumnIndex;
        private Border lastCell;

        public TableDesignerUserControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.Register("ImagePath", typeof(string), typeof(TableDesignerUserControl), new PropertyMetadata(string.Empty, OnImagePathChanged));

        public string ImagePath
        {
            get { return (string)GetValue(ImagePathProperty); }
            set { SetValue(ImagePathProperty, value); }
        }

        private static void OnImagePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as TableDesignerUserControl;
            if (control != null)
            {
                var uri = new Uri((string)e.NewValue, UriKind.RelativeOrAbsolute);
                control.imgDisplay.Source = new BitmapImage(uri);
            }
        }

        private void AddAdjustableBox_Click(object sender, RoutedEventArgs e)
        {
            gridGenerator = !gridGenerator;
            _columnSeperator = false;
            _rowSeperator = false;
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (gridGenerator)
            {
                startPoint = e.GetPosition(MainGrid);

                double imageWidth = imgDisplay.ActualWidth;
                double imageHeight = imgDisplay.ActualHeight;
                Point imagePosition = imgDisplay.TranslatePoint(new Point(0, 0), MainGrid);

                if (startPoint.X < imagePosition.X || startPoint.X > imagePosition.X + imageWidth ||
                    startPoint.Y < imagePosition.Y || startPoint.Y > imagePosition.Y + imageHeight)
                {
                    MessageBox.Show("Cannot draw outside the boundaries of the image.", "Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                selectionRectangle = new Rectangle
                {
                    Stroke = Brushes.Black,
                    StrokeDashArray = new DoubleCollection { 2 },
                    StrokeThickness = 1,
                    Width = 0,
                    Height = 0,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(startPoint.X, startPoint.Y, 0, 0)
                };

                MainGrid.Children.Add(selectionRectangle);
            }
            else
            {
                if (e.OriginalSource is Border border && gridBorders.Contains(border))
                {
                    activeBorder = border;
                    SelectGrid(activeBorder.Child as Grid);
                    return;
                }
                else if (e.OriginalSource is Grid grid && grid != MainGrid)
                {
                    SelectGrid(grid);
                    return;
                }
            }
        }

        private void MainGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (selectionRectangle == null || isResizing || isDragging)
                return;

            var currentPoint = e.GetPosition(MainGrid);

            double imageWidth = imgDisplay.ActualWidth;
            double imageHeight = imgDisplay.ActualHeight;
            Point imagePosition = imgDisplay.TranslatePoint(new Point(0, 0), MainGrid);

            currentPoint.X = Math.Max(imagePosition.X, Math.Min(currentPoint.X, imagePosition.X + imageWidth));
            currentPoint.Y = Math.Max(imagePosition.Y, Math.Min(currentPoint.Y, imagePosition.Y + imageHeight));

            var minX = Math.Min(startPoint.X, currentPoint.X);
            var minY = Math.Min(startPoint.Y, currentPoint.Y);

            var width = Math.Abs(currentPoint.X - startPoint.X);
            var height = Math.Abs(currentPoint.Y - startPoint.Y);

            selectionRectangle.Margin = new Thickness(minX, minY, 0, 0);
            selectionRectangle.Width = width;
            selectionRectangle.Height = height;
        }

        private void MainGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (selectionRectangle != null && !isResizing && !isDragging)
            {
                var currentPoint = e.GetPosition(MainGrid);

                double imageWidth = imgDisplay.ActualWidth;
                double imageHeight = imgDisplay.ActualHeight;
                Point imagePosition = imgDisplay.TranslatePoint(new Point(0, 0), MainGrid);

                currentPoint.X = Math.Max(imagePosition.X, Math.Min(currentPoint.X, imagePosition.X + imageWidth));
                currentPoint.Y = Math.Max(imagePosition.Y, Math.Min(currentPoint.Y, imagePosition.Y + imageHeight));

                var minX = Math.Min(startPoint.X, currentPoint.X);
                var minY = Math.Min(startPoint.Y, currentPoint.Y);

                var width = Math.Abs(currentPoint.X - startPoint.X);
                var height = Math.Abs(currentPoint.Y - startPoint.Y);

                MainGrid.Children.Remove(selectionRectangle);

                var border = new Border
                {
                    Width = width,
                    Height = height,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(minX, minY, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };

                var grid = new Grid
                {
                    Background = Brushes.LightBlue,
                    Opacity = 0.3,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                AddResizeThumbs(grid);

                grid.MouseLeftButtonDown += Grid_MouseLeftButtonDown;
                border.Child = grid;
                MainGrid.Children.Add(border);
                grid.Margin = new Thickness(0);
                SelectGrid(grid);
                selectionRectangle = null;

                gridBorders.Add(border);
                activeBorder = border;
                gridDataMap[grid] = new GridData(); 
            }
        }

        private void SelectGrid(Grid grid)
        {
            if (grid == null)
                return;

            selectedGrid = grid;

            selectedGrid.Background = Brushes.LightBlue;
            selectedGrid.Opacity = 0.5;

            foreach (var border in gridBorders)
            {
                if (border.Child != selectedGrid)
                {
                    (border.Child as Grid).Background = Brushes.Gray;
                    (border.Child as Grid).Opacity = 0.3;
                }
            }
        }

        private void AddResizeThumbs(Grid container)
        {
            AddResizeThumb(container, Cursors.SizeWE, HorizontalAlignment.Right, VerticalAlignment.Stretch, "Right", 0, 0);
            AddResizeThumb(container, Cursors.SizeNS, HorizontalAlignment.Stretch, VerticalAlignment.Bottom, "Bottom", 0, 0);
            AddResizeThumb(container, Cursors.SizeWE, HorizontalAlignment.Left, VerticalAlignment.Stretch, "Left", 0, 0);
            AddResizeThumb(container, Cursors.SizeNS, HorizontalAlignment.Stretch, VerticalAlignment.Top, "Top", 0, 0);
            AddDraggingThumb(container, 0, 0);
        }

        private void AddResizeThumb(Grid container, Cursor cursor, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, string direction, double marginLeft, double marginTop)
        {
            var thumb = new Thumb
            {
                Width = 10,
                Height = 10,
                Opacity = 1,
                Background = Brushes.White,
                HorizontalAlignment = horizontalAlignment,
                VerticalAlignment = verticalAlignment,
                Cursor = cursor,
                Tag = direction,
                Margin = new Thickness(marginLeft, marginTop, 0, 0)
            };

            thumb.DragStarted += (s, e) =>
            {
                resizeBorder = (Border)container.Parent;
                activeBorder = resizeBorder;
                isResizing = true;
                resizeDirection = (string)thumb.Tag;
                MainGrid.Children.Remove(activeBorder);
                MainGrid.Children.Add(activeBorder);
            };

            thumb.DragDelta += Thumb_DragDelta;
            thumb.DragCompleted += (s, e) =>
            {
                isResizing = false;
                resizeDirection = null;
            };

            container.Children.Add(thumb);
        }

        private void AddDraggingThumb(Grid container, double marginLeft, double marginTop)
        {
            var thumb = new Thumb
            {
                Width = 10,
                Height = 10,
                Opacity = 1,
                Background = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = Cursors.SizeAll,
                Margin = new Thickness(marginLeft, marginTop, 0, 0)
            };

            thumb.DragStarted += (s, e) =>
            {
                resizeBorder = (Border)container.Parent;
                activeBorder = resizeBorder;
                isDragging = true;
                MainGrid.Children.Remove(activeBorder);
                MainGrid.Children.Add(activeBorder);
            };

            thumb.DragDelta += Thumb_DragMove;
            thumb.DragCompleted += (s, e) =>
            {
                isDragging = false;
            };

            container.Children.Add(thumb);
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (resizeBorder == null || !isResizing) return;

            double newWidth, newHeight, newLeft, newTop;

            double imageWidth = imgDisplay.ActualWidth;
            double imageHeight = imgDisplay.ActualHeight;
            Point imagePosition = imgDisplay.TranslatePoint(new Point(0, 0), MainGrid);

            double minWidth = 10;
            double minHeight = 10;

            switch (resizeDirection)
            {
                case "Right":
                    newWidth = resizeBorder.Width + e.HorizontalChange / 2;
                    if (newWidth >= minWidth && resizeBorder.Margin.Left + newWidth <= imagePosition.X + imageWidth)
                        resizeBorder.Width = newWidth;
                    break;

                case "Bottom":
                    newHeight = resizeBorder.Height + e.VerticalChange / 2; 
                    if (newHeight >= minHeight && resizeBorder.Margin.Top + newHeight <= imagePosition.Y + imageHeight)
                        resizeBorder.Height = newHeight;
                    break;

                case "Left":
                    newWidth = resizeBorder.Width - e.HorizontalChange / 2; 
                    newLeft = resizeBorder.Margin.Left + e.HorizontalChange / 2;
                    if (newWidth >= minWidth && newLeft >= imagePosition.X)
                    {
                        resizeBorder.Width = newWidth;
                        resizeBorder.Margin = new Thickness(newLeft, resizeBorder.Margin.Top, 0, 0);
                    }
                    break;

                case "Top":
                    newHeight = resizeBorder.Height - e.VerticalChange / 2; 
                    newTop = resizeBorder.Margin.Top + e.VerticalChange / 2;
                    if (newHeight >= minHeight && newTop >= imagePosition.Y)
                    {
                        resizeBorder.Height = newHeight;
                        resizeBorder.Margin = new Thickness(resizeBorder.Margin.Left, newTop, 0, 0);
                    }
                    break;
            }

            // Ensure the last cell and last column remain static
            if (resizeBorder.Child is Grid grid)
            {
                foreach (var child in grid.Children.OfType<Border>())
                {
                    if (IsLastCellOrLastColumn(child))
                    {
                        child.Width = double.NaN; 
                        child.Height = double.NaN; 
                    }
                }
            }
        }


        private void Thumb_DragMove(object sender, DragDeltaEventArgs e)
        {
            if (resizeBorder == null || !isDragging)
                return;

            double newLeft = resizeBorder.Margin.Left + e.HorizontalChange;
            double newTop = resizeBorder.Margin.Top + e.VerticalChange;

            double imageWidth = imgDisplay.ActualWidth;
            double imageHeight = imgDisplay.ActualHeight;
            Point imagePosition = imgDisplay.TranslatePoint(new Point(0, 0), MainGrid);

            if (newLeft >= imagePosition.X && newTop >= imagePosition.Y &&
                newLeft + resizeBorder.Width <= imagePosition.X + imageWidth &&
                newTop + resizeBorder.Height <= imagePosition.Y + imageHeight)
            {
                resizeBorder.Margin = new Thickness(newLeft, newTop, 0, 0);
            }
        }

        private void InitializeLastCellAndColumn(Grid grid)
        {
            int rowCount = grid.RowDefinitions.Count;
            int columnCount = grid.ColumnDefinitions.Count;
            lastColumnIndex = columnCount - 1;

            lastCell = grid.Children
                .OfType<Border>()
                .FirstOrDefault(b => Grid.GetRow(b) == rowCount - 1 && Grid.GetColumn(b) == columnCount - 1);
        }

        private bool IsLastCellOrLastColumn(Border cell)
        {
            int row = Grid.GetRow(cell);
            int column = Grid.GetColumn(cell);
            return cell == lastCell || column == lastColumnIndex;
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPosition = e.GetPosition(selectedGrid);

            if (_rowSeperator && !isDragging)
            {
                if (!gridDataMap[selectedGrid].RowSeparators.Contains(clickPosition.Y))
                {
                    gridDataMap[selectedGrid].RowSeparators.Add(clickPosition.Y);
                    gridDataMap[selectedGrid].RowSeparators.Sort();
                }
                UpdateGrid();


                UpdateThumbArrangement();
            }
            else if (_columnSeperator && !isDragging)
            {
                if (!gridDataMap[selectedGrid].ColumnSeparators.Contains(clickPosition.X))
                {
                    gridDataMap[selectedGrid].ColumnSeparators.Add(clickPosition.X);
                    gridDataMap[selectedGrid].ColumnSeparators.Sort();
                }
                UpdateGrid();


                UpdateThumbArrangement();
            }

        }

        private void UpdateThumbArrangement()
        {
            int rowCount = gridDataMap[selectedGrid].RowSeparators.Count + 1;
            int columnCount = gridDataMap[selectedGrid].ColumnSeparators.Count + 1;
            IEnumerable<Thumb> thumbs = selectedGrid.Children.OfType<Thumb>();
            foreach (Thumb thumb in thumbs)
            {
                Grid.SetRowSpan(thumb, rowCount);
                Grid.SetColumnSpan(thumb, columnCount);
            }
        }

        private void UpdateGrid()
        {
            if (selectedGrid == null)
                return;

            selectedGrid.Children.Clear();
            selectedGrid.RowDefinitions.Clear();
            selectedGrid.ColumnDefinitions.Clear();

            double previousY = 0;
            foreach (var separator in gridDataMap[selectedGrid].RowSeparators)
            {
                selectedGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(separator - previousY, GridUnitType.Pixel) });
                previousY = separator;
            }
            selectedGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(selectedGrid.ActualHeight - previousY, GridUnitType.Pixel) });

            double previousX = 0;
            foreach (var separator in gridDataMap[selectedGrid].ColumnSeparators)
            {
                selectedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(separator - previousX, GridUnitType.Pixel) });
                previousX = separator;
            }
            selectedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(selectedGrid.ActualWidth - previousX, GridUnitType.Pixel) });

            for (int row = 0; row < selectedGrid.RowDefinitions.Count; row++)
            {
                for (int column = 0; column < selectedGrid.ColumnDefinitions.Count; column++)
                {
                    AddGridCell(selectedGrid, row, column);
                }
            }

           
            AddResizeThumbs(selectedGrid);
        }

        private void AdjustGridCells()
        {
            if (selectedGrid == null)
                return;

            foreach (var cell in selectedGrid.Children.OfType<Border>())
            {
                var row = Grid.GetRow(cell);
                var column = Grid.GetColumn(cell);

                cell.Width = selectedGrid.ColumnDefinitions[column].Width.Value;
                cell.Height = selectedGrid.RowDefinitions[row].Height.Value;
            }
        }

        private void AddGridCell(Grid grid, int row, int column)
        {
            var cellBorder = new Border
            {
                BorderBrush = Brushes.Blue,
                BorderThickness = new Thickness(0.5),
                Background = Brushes.Transparent
            };

            Grid.SetRow(cellBorder, row);
            Grid.SetColumn(cellBorder, column);
            cellBorder.MouseLeftButtonDown += CellBorder_MouseLeftButtonDown;
            cellBorder.MouseMove += CellBorder_MouseMove;
            cellBorder.MouseLeftButtonUp += CellBorder_MouseLeftButtonUp;

            AddCellResizeThumbs(cellBorder); // Add this line to add thumbs to each cell

            grid.Children.Add(cellBorder);
        }


        private void AddCellResizeThumbs(Border cell)
        {
            AddCellResizeThumb(cell, Cursors.SizeWE, HorizontalAlignment.Right, VerticalAlignment.Stretch, "Right");
            AddCellResizeThumb(cell, Cursors.SizeNS, HorizontalAlignment.Stretch, VerticalAlignment.Bottom, "Bottom");
        }

        private void AddCellResizeThumb(Border cell, Cursor cursor, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, string direction)
        {
            var thumb = new Thumb
            {
                Width = 5,
                Height = 5,
                Background = Brushes.Black,
                HorizontalAlignment = horizontalAlignment,
                VerticalAlignment = verticalAlignment,
                Cursor = cursor,
                Tag = direction
            };

            thumb.DragDelta += (s, e) => CellThumb_DragDelta(s, e, cell, direction);
            if (cell.Child is Grid grid)
            {
                grid.Children.Add(thumb);
            }
            else
            {
                var cgrid = new Grid();
                if (cell.Child != null)
                {
                    cgrid.Children.Add(cell.Child);
                }
                cgrid.Children.Add(thumb);
                cell.Child = cgrid;
            }
        }

        private void CellThumb_DragDelta(object sender, DragDeltaEventArgs e, Border cell, string direction)
        {
            var parentGrid = cell.Parent as Grid;
            if (parentGrid == null) return;

            int row = Grid.GetRow(cell);
            int column = Grid.GetColumn(cell);

            double newSize;
            double minSize = 10; // minimum size to prevent cells from becoming too small

            switch (direction)
            {
                case "Right":
                    newSize = parentGrid.ColumnDefinitions[column].Width.Value + e.HorizontalChange;
                    double remainingWidth = parentGrid.ActualWidth - parentGrid.ColumnDefinitions.Take(column).Sum(cd => cd.Width.Value);
                    if (newSize > minSize && newSize <= remainingWidth)
                    {
                        parentGrid.ColumnDefinitions[column].Width = new GridLength(newSize);
                    }
                    break;

                case "Bottom":
                    newSize = parentGrid.RowDefinitions[row].Height.Value + e.VerticalChange;
                    double remainingHeight = parentGrid.ActualHeight - parentGrid.RowDefinitions.Take(row).Sum(rd => rd.Height.Value);
                    if (newSize > minSize && newSize <= remainingHeight)
                    {
                        parentGrid.RowDefinitions[row].Height = new GridLength(newSize);
                    }
                    break;
            }
        }


        private void AdjustGridCells(Grid grid)
        {
            double spacing = 5;

            for (int i = 0; i < grid.RowDefinitions.Count; i++)
            {
                double newHeight = grid.RowDefinitions[i].Height.Value - spacing;
                if (newHeight > 0)
                {
                    grid.RowDefinitions[i].Height = new GridLength(newHeight);
                }
            }

            for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
            {
                double newWidth = grid.ColumnDefinitions[i].Width.Value - spacing;
                if (newWidth > 0)
                {
                    grid.ColumnDefinitions[i].Width = new GridLength(newWidth);
                }
            }
        }
        private void CellBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var clickedCell = sender as Border;
            if (clickedCell == null) return;

            isMouseDown = true;
            initialCell = clickedCell;

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (selectedCells.Contains(clickedCell))
                {
                    DeselectCell(clickedCell);
                }
                else
                {
                    SelectCell(clickedCell);
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                if (initialCell == null)
                {
                    initialCell = clickedCell;
                    SelectCell(clickedCell);
                }
                else
                {
                    SelectRange(initialCell, clickedCell);
                }
            }
            else
            {
                ClearSelection();
                SelectCell(clickedCell);
                initialCell = clickedCell;
            }

            isSelecting = true;
        }

        private void CellBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isMouseDown || !isSelecting || initialCell == null) return;

            var hoveredCell = sender as Border;
            if (hoveredCell == null) return;

            SelectRange(initialCell, hoveredCell);
        }

        private void CellBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = false;
            isSelecting = false;
            initialCell = null;
        }

        private void SelectCell(Border cell)
        {
            if (cell != null && !selectedCells.Contains(cell))
            {
                selectedCells.Add(cell);
                cell.Background = Brushes.LightGreen;
            }
        }

        private void DeselectCell(Border cell)
        {
            if (cell != null && selectedCells.Contains(cell))
            {
                selectedCells.Remove(cell);
                cell.Background = Brushes.Transparent;
            }
        }

        private void ClearSelection()
        {
            foreach (var cell in selectedCells.ToList())
            {
                DeselectCell(cell);
            }
        }

        private void SelectRange(Border startCell, Border endCell)
        {
            var startRow = Grid.GetRow(startCell);
            var startColumn = Grid.GetColumn(startCell);
            var endRow = Grid.GetRow(endCell);
            var endColumn = Grid.GetColumn(endCell);

            ClearSelection();

            for (int row = Math.Min(startRow, endRow); row <= Math.Max(startRow, endRow); row++)
            {
                for (int column = Math.Min(startColumn, endColumn); column <= Math.Max(startColumn, endColumn); column++)
                {
                    var cell = selectedGrid.Children
                        .OfType<Border>()
                        .FirstOrDefault(b => Grid.GetRow(b) == row && Grid.GetColumn(b) == column);
                    if (cell != null)
                    {
                        SelectCell(cell);
                    }
                }
            }
        }

        private void MergeCellsButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCells.Count < 2)
            {
                MessageBox.Show("Select at least two cells to merge.");
                return;
            }

            var minRow = selectedCells.Min(c => Grid.GetRow(c));
            var maxRow = selectedCells.Max(c => Grid.GetRow(c));
            var minColumn = selectedCells.Min(c => Grid.GetColumn(c));
            var maxColumn = selectedCells.Max(c => Grid.GetColumn(c));

            // Use the first selected cell as the base for the merged cell
            var mergedCell = selectedCells.First();
            Grid.SetRow(mergedCell, minRow);
            Grid.SetColumn(mergedCell, minColumn);
            Grid.SetRowSpan(mergedCell, maxRow - minRow + 1);
            Grid.SetColumnSpan(mergedCell, maxColumn - minColumn + 1);

            foreach (var cell in selectedCells)
            {
                if (cell != mergedCell)
                {
                    selectedGrid.Children.Remove(cell);
                }
            }

            selectedCells.Clear();
            mergedCell.Background = Brushes.LightGreen;
            mergedCell.BorderBrush = Brushes.Blue;
            mergedCell.BorderThickness = new Thickness(0.5);

            gridDataMap[selectedGrid].MergedCells.Add(new CellModel
            {
                Row = minRow,
                Column = minColumn,
                RowSpan = maxRow - minRow + 1,
                ColumnSpan = maxColumn - minColumn + 1,
                IsMerged = true
            });
        }


        private void UnMergeCellsButton_Click(object sender, RoutedEventArgs e)
        {
            var mergedCells = selectedGrid.Children.OfType<Border>().Where(c => Grid.GetRowSpan(c) > 1 || Grid.GetColumnSpan(c) > 1).ToList();
            foreach (var cell in mergedCells)
            {
                var row = Grid.GetRow(cell);
                var column = Grid.GetColumn(cell);
                var rowSpan = Grid.GetRowSpan(cell);
                var columnSpan = Grid.GetColumnSpan(cell);

                for (int r = row; r < row + rowSpan; r++)
                {
                    for (int c = column; c < column + columnSpan; c++)
                    {
                        if (r == row && c == column)
                        {
                            continue;
                        }

                        var newCell = new Border
                        {
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(0.5),
                            Background = Brushes.Transparent
                        };

                        Grid.SetRow(newCell, r);
                        Grid.SetColumn(newCell, c);
                        newCell.MouseLeftButtonDown += CellBorder_MouseLeftButtonDown;
                        selectedGrid.Children.Add(newCell);
                    }
                }

                Grid.SetRowSpan(cell, 1);
                Grid.SetColumnSpan(cell, 1);
                cell.Background = Brushes.Transparent;
            }
        }
        private void btnColumnSeperator_Click(object sender, RoutedEventArgs e)
        {
            gridGenerator = false;
            _columnSeperator = !_columnSeperator;
            _rowSeperator = false;
        }

        private void btnRowSeperator_Click(object sender, RoutedEventArgs e)
        {
            gridGenerator = false;
            _rowSeperator = !_rowSeperator;
            _columnSeperator = false;
        }



        private void SaveGridButton_Click(object sender, RoutedEventArgs e)
        {
            SaveGridState("gridState.json");
        }

        private void LoadGridButton_Click(object sender, RoutedEventArgs e)
        {
            LoadGridState("gridState.json");
        }
        public void SaveGridState(string filePath)
        {
            var grids = MainGrid.Children.OfType<Border>()
                .Where(b => b.Child is Grid)
                .Select(b => new GridModel
                {
                    Width = b.Width,
                    Height = b.Height,
                    Left = b.Margin.Left,
                    Top = b.Margin.Top,
                    ColumnDefinitions = ((Grid)b.Child).ColumnDefinitions.Select(cd => cd.Width.Value).ToList(),
                    RowDefinitions = ((Grid)b.Child).RowDefinitions.Select(rd => rd.Height.Value).ToList(),
                    Cells = ((Grid)b.Child).Children.OfType<Border>().Select(c => new CellModel
                    {
                        Row = Grid.GetRow(c),
                        Column = Grid.GetColumn(c),
                        RowSpan = Grid.GetRowSpan(c),
                        ColumnSpan = Grid.GetColumnSpan(c),
                    }).ToList(),
                    Separators = new GridData
                    {
                        RowSeparators = gridDataMap[(Grid)b.Child].RowSeparators,
                        ColumnSeparators = gridDataMap[(Grid)b.Child].ColumnSeparators
                    }
                })
                .ToList();

            var json = JsonConvert.SerializeObject(grids, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
            MessageBox.Show("Saved Successfully");
        }

        public void LoadGridState(string filePath)
        {
            if (!File.Exists(filePath)) return;

            var json = File.ReadAllText(filePath);
            var grids = JsonConvert.DeserializeObject<List<GridModel>>(json);

            gridBorders.Clear();
            gridDataMap.Clear();

            foreach (var gridModel in grids)
            {
                var border = new Border
                {
                    Width = gridModel.Width,
                    Height = gridModel.Height,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(gridModel.Left, gridModel.Top, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };

                var grid = new Grid
                {
                    Background = Brushes.LightBlue,
                    Opacity = 0.3,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                foreach (var columnDefinition in gridModel.ColumnDefinitions)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(columnDefinition) });
                }

                foreach (var rowDefinition in gridModel.RowDefinitions)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(rowDefinition) });
                }

                foreach (var cell in gridModel.Cells)
                {
                    var cellBorder = new Border
                    {
                        BorderBrush = Brushes.Blue,
                        BorderThickness = new Thickness(0.5),
                        Background = Brushes.Transparent,
                    };

                    Grid.SetRow(cellBorder, cell.Row);
                    Grid.SetColumn(cellBorder, cell.Column);

                    if (cell.RowSpan > 1)
                    {
                        Grid.SetRowSpan(cellBorder, cell.RowSpan);
                    }
                    if (cell.ColumnSpan > 1)
                    {
                        Grid.SetColumnSpan(cellBorder, cell.ColumnSpan);
                    }

                    cellBorder.MouseLeftButtonDown += CellBorder_MouseLeftButtonDown;
                    cellBorder.MouseMove += CellBorder_MouseMove;
                    cellBorder.MouseLeftButtonUp += CellBorder_MouseLeftButtonUp;

                    AddCellResizeThumbs(cellBorder);

                    grid.Children.Add(cellBorder);
                }

                gridDataMap[grid] = new GridData
                {
                    RowSeparators = gridModel.Separators.RowSeparators,
                    ColumnSeparators = gridModel.Separators.ColumnSeparators
                };

                AddResizeThumbs(grid);

                grid.MouseLeftButtonDown += Grid_MouseLeftButtonDown;
                border.Child = grid;
                MainGrid.Children.Add(border);
                grid.Margin = new Thickness(0);

                gridBorders.Add(border);
                SelectGrid(grid);
            }
        }

    }

}