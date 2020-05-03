using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Saplin.TimeSeries
{

    public class AsciiTimeSeries
    {
        /// <summary>
        /// Total height (outter) of the chart in lines
        /// </summary>
        public int HeigthLines { get; set; } = 10;

        /// <summary>
        /// Total width (outter) of the chart in characters/columns (PlotAreaWidth + YAxisAndLabelsWidth), unused columns are filled with EmptyChar
        /// 0 - width is determined by the number of data point in the chart, the chart can grow infinitely.
        /// If the width is > 0 (it's constrained) and number of data points is gtreater than plt can fit, only tail of the collection will be displayed.
        /// </summary>
        public int WidthCharacters { get; set; } = 0;

        /// <summary>
        /// If grpah width is contrained (WidthCharecters is greater than 0) by default only tail of the series if displayed.
        /// To scale the series and fit the complete data set into the chart then set this property to false
        /// </summary>
        public bool ShowOnlyTail { get; set; } = true;

        /// <summary>
        /// The number of columns in plot area. It is determined as the difference of WidthCharacters-YAxisAndLabelsWidth (if the width is limited)
        /// or grows with Series/SeriesModifiable number of items
        /// </summary>
        public int PlotAreaWidth { get; private set; }

        /// <summary>
        /// PlotAreaWidth+1. The number of data points that are visible/can fit in the plot area is greater by 1 than the number of columns.
        /// If WidthCharacters is set this shows how many points can fit, otherwise how many data points there're now
        /// </summary>
        public int DataPoints => PlotAreaWidth + 1;

        public char EmptyChar { get; set; } = ' ';

        public char AbovePointChar { get; set; } = ' ';

        public char BelowPointChar { get; set; } = ' ';

        public string LabelFormat { get; set; } = "0.00";

        /// <summary>
        /// 0 - don't fix the total width of Y axis and label, otherwise either trim it (if there's no enough space) or pad left with spaces
        /// </summary>
        public int YAxisAndLabelsWidth { get; set; } = 0;

        /// <summary>
        /// How many spaces between label text and axis line
        /// </summary>
        public int YLabelRightPadding { get; set; } = 1;

        public double? Min { get; set; }

        public double? Max { get; set; }

        double? prevMin, prevMax;
        int? prevPlotWidth, prevHeight, prevCount;

        IEnumerable<double> series;

        /// <summary>
        /// Collection fot which to build/render the graph. In multi-threaded environment if the series is changed in other threads while the graph
        /// is being built you better use IList (and avoid collection changed exception in foreach inside the component).
        /// There's also optimizations for growing IList collection when only diff is rerendered (when possible, e.g min/max stay same)
        /// </summary>
        public IEnumerable<double> Series
        {
            get => series;
            set => series = value;
        }

        string[] yAxis;

        char[,] plot;

        int labelsAndAxisLength = 0;

        IList<double> prevSeriesModifiable = null;

        bool builtUsingShrink = false;

        void BuildGraph()
        {
            IEnumerable<double> series = Series;
            IList<double> seriesModifiable = Series as IList<double>;
            var count = series != null ? series.Count() : seriesModifiable.Count;

            if (count < 2) return;

            if (HeigthLines <= 1) throw new InvalidOperationException("TimeSeries.HeigthLines must be greater than 1, now it isn't");

            if (Min.HasValue && Max.HasValue && Min >= Max) throw new InvalidOperationException("TimeSeries.Max must be greater than TimeSeries.Min");

            var min = Min.HasValue ? Min.Value : series.Min();
            var max = Max.HasValue ? Max.Value : series.Max();

            if (min > max) { max = min; min = max; } // swap min/max in one of the points is fixed, another calculated and they happen to be at odds

            if (min == max) { min -= 1; ; max += 1; } // straight horizontal line, min max will be autocalculated to the same value

            var range = Math.Abs(max - min);
            var bucket = range / (HeigthLines - 1);
            var yAxisChanged = false;

            if (yAxis == null || prevMin != min || prevMax != max || prevHeight != HeigthLines || labelsAndAxisLength == 0)
            {
                BuildYAxis(min, max, bucket, HeigthLines, out labelsAndAxisLength);
                prevMin = min;
                prevMax = max;
                prevHeight = HeigthLines;
                yAxisChanged = true;
            }

            PlotAreaWidth = count;
            var startAtCollectionIndex = 0;
            var i = 0;
            var col = 0;

            if (WidthCharacters > 0)
            {
                PlotAreaWidth = WidthCharacters - labelsAndAxisLength;

                if (PlotAreaWidth < 1) // Hide yAxis, show only plot
                {
                    labelsAndAxisLength = 0;
                    PlotAreaWidth = WidthCharacters;
                }

                if (count > PlotAreaWidth + 2)
                {
                    if (!ShowOnlyTail)
                    {
                        seriesModifiable = null;
                        count = PlotAreaWidth + 1;// more data points fit by 1
                        series = ShrinkCollection(series, count);
                    }
                    else startAtCollectionIndex = count - PlotAreaWidth - 2;
                }
            }

            if (
                plot == null || prevPlotWidth != PlotAreaWidth ||
                seriesModifiable == null ||
                seriesModifiable != prevSeriesModifiable || yAxisChanged) // build the plot from scratch
            {
                BuildPlot(HeigthLines, PlotAreaWidth); //y,x = row, column
                prevPlotWidth = PlotAreaWidth;
            }
            else if (seriesModifiable != null && prevCount.HasValue)
            {
                var diff = count - prevCount.Value;

                if (startAtCollectionIndex > 0) // there're more data points than columns, shift existing plot and render only the diff, works only with seriesModifiable/IList
                {
                    ShiftColumnsLeft(plot, diff);
                    startAtCollectionIndex = count - 2 - diff;
                    col = PlotAreaWidth - diff;
                }
                else if (prevSeriesModifiable == seriesModifiable && prevCount < count) // still growing chart, add only the added columns
                {
                    startAtCollectionIndex = count - 2 - diff;
                    col = prevCount.Value - 2;
                }
            }

            prevCount = count;
            prevSeriesModifiable = seriesModifiable;

            if (seriesModifiable == null)
            {
                var enumarator = series.GetEnumerator();
                enumarator.MoveNext();

                while (i < count - 2)
                {
                    if (i >= startAtCollectionIndex)
                    {
                        var currVal = enumarator.Current;
                        enumarator.MoveNext();
                        var nextVal = enumarator.Current;

                        ConnectDots(max, bucket, col, currVal, nextVal);

                        col++;
                        i++;
                    }
                    else
                    {
                        enumarator.MoveNext();
                        i++;
                    }
                }
            }
            else
            {
                for (i = startAtCollectionIndex; i < count - 2; i++)
                {
                    var currVal = seriesModifiable[i];
                    var nextVal = seriesModifiable[i + 1];

                    ConnectDots(max, bucket, col, currVal, nextVal);

                    col++;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ConnectDots(double max, double bucket, int col, double currVal, double nextVal)
        {
            var curRow = (int)Math.Round((max - currVal) / bucket, MidpointRounding.AwayFromZero);
            var nextRow = (int)Math.Round((max - nextVal) / bucket, MidpointRounding.AwayFromZero);
            var diff = nextRow - curRow;

            if (curRow == nextRow && curRow >= 0 && curRow < plotRows)
            {
                plot[curRow, col] = '─';
            }
            else if (curRow < nextRow)
            {
                if (curRow >= 0 && curRow < plotRows) plot[curRow, col] = '╮';
                if (nextRow >= 0 && nextRow < plotRows) plot[nextRow, col] = '╰';
            }
            else if (curRow > nextRow)
            {
                if (curRow >= 0 && curRow < plotRows) plot[curRow, col] = '╯';
                if (nextRow >= 0 && nextRow < plotRows) plot[nextRow, col] = '╭';
            }

            var correction = curRow < 0 ? 0 : 1;

            if (diff > 1)
            {
                for (var k = Math.Max(curRow,0) + correction; k < Math.Min(nextRow, plotRows); k++)
                    plot[k, col] = '│';
            }
            else if (diff < -1)
            {
                for (var k = Math.Min(curRow, plotRows) - 1; k > Math.Max(nextRow,-1); k--)
                    plot[k, col] = '│';
            }

            correction = nextRow < 0 && curRow < 0 ? 0 : 1;

            for (var k = Math.Max(Math.Max(curRow, nextRow),0) + correction; k < HeigthLines; k++)
                plot[k, col] = BelowPointChar;

            for (var k = Math.Min(Math.Min(curRow, nextRow), plotRows) - 1; k >= 0; k--)
                plot[k, col] = AbovePointChar;
        }

        double[] GetYAxisTicks(double max, double bucket, int rows)
        {
            var result = new double[rows];

            for (var i = 0; i < rows; i++)
            {
                result[i] = max - i * bucket;
            }

            return result;
        }

        void BuildYAxis(double min, double max, double bucket, int rows, out int labelAndAxisMaxLength)
        {
            var yAxisTicks = GetYAxisTicks(max, bucket, rows);

            if (yAxis == null || prevHeight != HeigthLines)
                yAxis = new string[HeigthLines];

            var maxLabel = Math.Max(min.ToString(LabelFormat).Length, max.ToString(LabelFormat).Length);
            var padRight = String.Empty.PadLeft(YLabelRightPadding);

            if (YAxisAndLabelsWidth > 0)
            {
                labelAndAxisMaxLength = YAxisAndLabelsWidth;
                if (maxLabel > labelAndAxisMaxLength - YLabelRightPadding - 1) padRight = "";
            }
            else
            {
                labelAndAxisMaxLength = maxLabel + YLabelRightPadding + 1;
            }

            for (int i = 0; i < yAxis.Length; i++)
            {
                var s = yAxisTicks[i].ToString(LabelFormat);

                if (s.Length <= labelAndAxisMaxLength - 1) // enough room for label and axis, maybe for right pad
                {
                    s += padRight + "┤";
                }
                else if (labelAndAxisMaxLength == 1) // enough room for axis only
                {
                    s = "┤";
                }
                else // enough axis and some text from label;
                {
                    s = s.Substring(0, labelAndAxisMaxLength - 2) + "…";
                    s += "┤";
                }

                yAxis[i] = s.PadLeft(labelAndAxisMaxLength);
            }
        }

        int plotRows, plotCols;  // actual dimensions of the plot

        const int plotGrowByCols = 100;

        void BuildPlot(int rows, int cols)
        {
            plotRows = rows;
            plotCols = cols;
            //if plot matrix happens to be bigger than needed, keep it and stor only the bound
            if (plot == null || plot.GetUpperBound(0) + 1 < rows || plot.GetUpperBound(1) + 1 < cols)
                plot = new char[rows, (int)Math.Ceiling(cols/(double)plotGrowByCols)*plotGrowByCols];

            for (var i = 0; i < rows; i++)
                for (var k = 0; k < cols; k++)
                {
                    plot[i, k] = EmptyChar;
                }
        }

        void ShiftColumnsLeft(char[,] plot, int n)
        {
            for (var i = 0; i < plotRows; i++)
            {
                for (var k = 0; k < plotCols - n; k++)
                {
                    plot[i, k] = plot[i, k + n];
                }
                for (var k = plotCols - n; k < plotCols; k++)
                {
                    plot[i, k] = EmptyChar;
                }
            }
        }

        string CombineToString(string[] yAxis, char[,] plot, int rows, int cols)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < rows; i++)
            {
                if (yAxis != null) sb.Append(yAxis[i]);
                for (var k = 0; k < cols; k++)
                    sb.Append(plot[i, k]); //y,x
                if (i < rows - 1) sb.AppendLine();
            }

            return sb.ToString();
        }

        string[] CombineToLines(string[] yAxis, char[,] plot, int rows, int cols)
        {
            var lines = new string[rows];
            var sb = new StringBuilder();

            for (int i = 0; i < rows; i++)
            {
                if (yAxis != null) sb.Append(yAxis[i]);
                for (var k = 0; k < cols; k++)
                    sb.Append(plot[i, k]); //y,x

                lines[i] = sb.ToString();
                sb.Clear();
            }

            return lines;
        }

        public string RenderToString()
        {
            if (Series == null) return null;

            Debug.Write("Building graph as string...");

            var sw = new Stopwatch();
            sw.Start();

            BuildGraph();

            var s = CombineToString(labelsAndAxisLength != 0 ? yAxis : null, plot, plotRows, plotCols);

            sw.Stop();
            Debug.WriteLine(" Done, time(ms): " + sw.ElapsedMilliseconds);

            return s;
        }

        public string[] RenderToLines()
        {
            if (Series == null) return null;

            Debug.Write("Building graph as lines...");

            var sw = new Stopwatch();
            sw.Start();

            BuildGraph();

            var lines = CombineToLines(labelsAndAxisLength != 0 ? yAxis : null, plot, plotRows, plotCols);

            sw.Stop();
            Debug.WriteLine(" Done, time(ms): " + sw.ElapsedMilliseconds);

            return lines;
        }

        public static string MergeTwoGraphs(AsciiTimeSeries first, AsciiTimeSeries second, string split, int padRight = 0)
        {
            var lines1 = first.RenderToLines();
            var lines2 = second.RenderToLines();

            if (lines1 == null || lines2 == null) return null;

            if (lines1.Length != lines2.Length)
                throw new InvalidOperationException("frist and second must have same number of elements");

            var sb = new StringBuilder();

            for (int i = 0; i < first.yAxis.Length; i++)
            {
                sb.Append(lines1[i]);
                if (lines1[i].Length < padRight) sb.Append(' ', padRight - lines1[i].Length);

                sb.Append(split);

                sb.Append(lines2[i]);
                if (lines2[i].Length < padRight) sb.Append(' ', padRight - lines2[i].Length);

                if (i < lines1.Length - 1) sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Shrink the collectiong to a smaller size and recalculate numbers so it could fit into a smaller plot
        /// </summary>
        /// <param name="source">Source collection</param>
        /// <param name="toN">The number of elements in the resulting collection</param>
        /// <returns>New collection if toN is greater than the number of elements in source. Otherwise the original collection is returned</returns>
        public static IEnumerable<double> ShrinkCollection(IEnumerable<double> source, int toN)
        {
            var sourceCount = source.Count();
            var destCount = toN;

            var ratio = (double)sourceCount / destCount;

            if (ratio <= 1) return source;


            var dest = new double[destCount];


            int bin = 0;
            int counter = 0;
            double accum = 0;
            int prevIndex = 0;
            int curIndex;

            foreach (var val in source)
            {
                curIndex = (int)Math.Floor((double)counter / ratio);

                if (prevIndex != curIndex)
                {
                    dest[prevIndex] = accum / bin;
                    bin = 0;
                    accum = 0;
                    prevIndex = curIndex;
                }

                accum += val;
                bin++;

                counter++;
            }

            dest[destCount - 1] = accum / bin;

            //dest[0] = source.First();
            //dest[destCount - 1] = source.Last();                  

            return dest;
        }
    }
}