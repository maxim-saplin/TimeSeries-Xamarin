using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace Saplin.TimeSeries.Xamarin
{
    public class TimeSeries : Grid
    {
        Label chartLabel;

        AsciiTimeSeries ascii = new AsciiTimeSeries();

        /// <summary>
        /// Visual control that renders underlying Series collection to a time series graph. The graph is displayed via text (ASCII boxes)
        /// and is essentially a Xamarin Forms Label control wrapped in a Grid layout. The controls takes care of bulding a multi-line text
        /// that will look like graph? assuming controls font is monospace and supports required characters
        /// (by default it is except Android, see FontFamily property description).
        /// The control can show cut of the head of the series (and show the tail) if the collection has more data points than fit into the plot area
        /// or it can interpolate and fit all points.
        /// </summary>
        public TimeSeries()
        {
            chartLabel = new Label();
            chartLabel.VerticalOptions = LayoutOptions.CenterAndExpand;
            chartLabel.HorizontalOptions = LayoutOptions.StartAndExpand;
            chartLabel.Padding = chartLabel.Margin = new Thickness(0,0,0,0);
            chartLabel.CharacterSpacing = 0;
            //chartLabel.LineBreakMode = LineBreakMode.NoWrap;

            RowSpacing = ColumnSpacing = 0;

            ColumnDefinitions.Add(new ColumnDefinition() { Width=GridLength.Star});
            RowDefinitions.Add(new RowDefinition() { Height = GridLength.Star });
            Children.Add(chartLabel, 0, 0);

            // Magic numbers, calculating width chars across platforms is a bit messy, empirically found some extra width to make chart claculated width fitting the container
            if (Device.RuntimePlatform == Device.iOS) { widthFix = 8; heightFix = 4; }
            else if (Device.RuntimePlatform == Device.macOS) widthFix = 4;
            else if (Device.RuntimePlatform == Device.Android) widthFix = 4;
        }

        double widthFix = 0;
        double heightFix = 0;

        /// <summary>
        /// Collection fot which to build/render the graph. In multi-threaded environment if the series is changed in other threads while the graph
        /// is being built you better use IList (and avoid collection changed exception in foreach inside the component).
        /// There's also optimizations for growing IList/ObservableCollection instances when only diff is rerendered (when possible, e.g min/max stay same)
        /// </summary>
        public IEnumerable<double> Series { get => (IEnumerable<double>)GetValue(SeriesProperty); set => SetValue(SeriesProperty, value); }

        bool observableCollectionSubscribed  = false;

        void ObservableCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Update();
        }

        public static readonly BindableProperty SeriesProperty = BindableProperty.Create(nameof(Series), typeof(IEnumerable<double>), typeof(TimeSeries), null,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var ts = ((TimeSeries)bindable);

                if (newValue != null)
                {
                    if (oldValue != newValue)
                    {
                        if (ts.observableCollectionSubscribed && ts.Series is INotifyCollectionChanged)
                        {
                            (ts.Series as INotifyCollectionChanged).CollectionChanged -= ts.ObservableCollectionChanged;
                            ts.observableCollectionSubscribed = false;
                        }

                        if (newValue is INotifyCollectionChanged)
                        {
                            (newValue as INotifyCollectionChanged).CollectionChanged += ts.ObservableCollectionChanged;
                            ts.observableCollectionSubscribed = true;
                        }
                        else ts.observableCollectionSubscribed = false;

                        ts.ascii.Series = (IEnumerable<double>)newValue;
                        ts.Update();
                    }
                }
            });

        /// <summary>
        /// The font must be monospace and support ASCII boxes to have the chart proprely displayed.
        /// Default font for iOS and macOS is Menlo, for WPF it is Consoloas. Although Android has 'monospace' as a default
        /// monospace typeface it is not OK - there's no Android typeface that fits ASCII boxing and you must embeded/use a custom font
        /// (e.g. Source Code Pro) via Xamarin's ExportFont attribute. You might want check TimeSeries demo project on GitHub.
        /// </summary>
        public string FontFamily { get => (string)GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }

        public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(TimeSeries),
            defaultValueCreator: (bindable) =>
            {
                var s = string.Empty;
                switch (Device.RuntimePlatform)
                {
                    case Device.iOS:
                    case Device.macOS: s = "Menlo"; break;
                    case Device.WPF: s = "Consolas"; break;
                    case Device.Android: s = "monospace"; break;
                }

                return s;
            },
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var ts = ((TimeSeries)bindable);

                if (newValue != null)
                {
                    ts.chartLabel.FontFamily = (string)newValue;
                    ts.characterMeasureNeeded = true;
                }
            });

        /// <summary>
        /// Values below 10 showed poor results on macOS (broken/jagged grpah). Android and WPF were fine
        /// </summary>
        public int FontSize { get => (int)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }

        public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(nameof(FontSize), typeof(int), typeof(TimeSeries), 15,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var ts = ((TimeSeries)bindable);

                if (newValue != null)
                {
                    ts.chartLabel.FontSize = (int)newValue;
                    ts.characterMeasureNeeded = true;
                }
            });


        public FontAttributes FontAttributes { get => (FontAttributes)GetValue(FontAttributesProperty); set => SetValue(FontAttributesProperty, value); }

        public static readonly BindableProperty FontAttributesProperty = BindableProperty.Create(nameof(FontAttributes), typeof(FontAttributes), typeof(TimeSeries), default(FontAttributes),
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var ts = ((TimeSeries)bindable);

                if (newValue != null)
                {
                    ts.chartLabel.FontAttributes = (FontAttributes)newValue;
                    ts.characterMeasureNeeded = true;
                }
            });


        public Color TextColor { get => (Color)GetValue(TextColorProperty); set => SetValue(TextColorProperty, value); }

        public static readonly BindableProperty TextColorProperty = BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(TimeSeries), default(Color),
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var ts = ((TimeSeries)bindable);

                if (newValue != null)
                {
                    ts.chartLabel.TextColor = (Color)newValue;
                }
            });

        /// <summary>
        /// Total height (outter) of the chart in lines. If set to 0 the number of lines will be autocalculated based on the height of the control
        /// </summary>
        public int HeigthLines { get => (int)GetValue(HeigthLinesProperty); set => SetValue(HeigthLinesProperty, value); }

        public static readonly BindableProperty HeigthLinesProperty = BindableProperty.Create(nameof(HeigthLines), typeof(int), typeof(TimeSeries), 10,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var ts = ((TimeSeries)bindable);

                if (newValue != null)
                {
                    ts.ascii.HeigthLines = Math.Max((int)newValue, 2);
                }
            });

        public double? Min { get => (double?)GetValue(MinProperty); set => SetValue(MinProperty, value); }

        public static readonly BindableProperty MinProperty = BindableProperty.Create(nameof(Min), typeof(double?), typeof(TimeSeries), null,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var ts = ((TimeSeries)bindable);

                if (newValue != null)
                {
                    ts.ascii.Min = (double?)newValue;
                }
            });

        public double? Max { get => (double?)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

        public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(double?), typeof(TimeSeries), null,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var ts = ((TimeSeries)bindable);

                if (newValue != null)
                {
                    ts.ascii.Max = (double?)newValue;
                }
            });


        public char EmptyChar { get => (char)GetValue(EmptyCharProperty); set => SetValue(EmptyCharProperty, value); }

        public static readonly BindableProperty EmptyCharProperty = BindableProperty.Create(nameof(EmptyChar), typeof(char), typeof(TimeSeries), ' ',
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var ts = ((TimeSeries)bindable);

                if (newValue != null)
                {
                    ts.ascii.EmptyChar = (char)newValue;
                }
            });

        public char AbovePointChar { get => (char)GetValue(AbovePointCharProperty); set => SetValue(AbovePointCharProperty, value); }

        public static readonly BindableProperty AbovePointCharProperty = BindableProperty.Create(nameof(AbovePointChar), typeof(char), typeof(TimeSeries), ' ',
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var ts = ((TimeSeries)bindable);

                if (newValue != null)
                {
                    ts.ascii.AbovePointChar = (char)newValue;
                }
            });

        public char BelowPointChar { get => (char)GetValue(BelowPointCharProperty); set => SetValue(BelowPointCharProperty, value); }

        public static readonly BindableProperty BelowPointCharProperty = BindableProperty.Create(nameof(BelowPointChar), typeof(char), typeof(TimeSeries), ' ',
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var ts = ((TimeSeries)bindable);

                if (newValue != null)
                {
                    ts.ascii.BelowPointChar = (char)newValue;
                }
            });

        /// <summary>
        /// How many data points can fit/be displayed in the plot area
        /// </summary>
        public int DataPointsInThePlot => ascii.DataPoints;

        /// <summary>
        /// By default only tail of the series if displayed.
        /// To scale the Series and shrink the fit data set into the chart set this property to false
        /// </summary>
        public bool ShowOnlyTail { get => (bool)GetValue(ShowOnlyTailProperty); set => SetValue(ShowOnlyTailProperty, value); }

        public static readonly BindableProperty ShowOnlyTailProperty = BindableProperty.Create(nameof(ShowOnlyTail), typeof(bool), typeof(TimeSeries), true,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var ts = ((TimeSeries)bindable);

                if (newValue != null)
                {
                    ts.ascii.ShowOnlyTail = (bool)newValue;
                    ts.Update();
                }
            });


        public string LabelFormat { get => (string)GetValue(LabelFormatProperty); set => SetValue(LabelFormatProperty, value); }

        public static readonly BindableProperty LabelFormatProperty = BindableProperty.Create(nameof(LabelFormat), typeof(string), typeof(TimeSeries), "0.00",
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var ts = ((TimeSeries)bindable);

                if (newValue != null)
                {
                    ts.ascii.LabelFormat = (string)newValue;
                }
            });

        /// <summary>
        /// 0 - don't fix the total width of Y axis and label, otherwise either trim it (if there's no enough space) or pad left with spaces
        /// </summary>
        public int YAxisAndLabelsWidth { get => (int)GetValue(YAxisAndLabelsWidthProperty); set => SetValue(YAxisAndLabelsWidthProperty, value); }

        public static readonly BindableProperty YAxisAndLabelsWidthProperty = BindableProperty.Create(nameof(YAxisAndLabelsWidth), typeof(int), typeof(TimeSeries), 0,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var ts = ((TimeSeries)bindable);

                if (newValue != null)
                {
                    ts.ascii.YAxisAndLabelsWidth = (int)newValue;
                }
            });

        /// <summary>
        /// How many spaces between label text and axis line
        /// </summary>
        public int YLabelRightPadding { get => (int)GetValue(YLabelRightPaddingProperty); set => SetValue(YLabelRightPaddingProperty, value); }

        public static readonly BindableProperty YLabelRightPaddingProperty = BindableProperty.Create(nameof(YLabelRightPadding), typeof(int), typeof(TimeSeries), 1,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var ts = ((TimeSeries)bindable);

                if (newValue != null)
                {
                    ts.ascii.YLabelRightPadding = (int)newValue;
                }
            });

        /// <summary>
        /// Bind any property in ViewModel and the control will be rerendered whenever property value is changed
        /// </summary>
        public object UpdateTrigger { get => GetValue(UpdateTriggerProperty); set => SetValue(UpdateTriggerProperty, value); }

        public static readonly BindableProperty UpdateTriggerProperty = BindableProperty.Create(nameof(UpdateTrigger), typeof(object), typeof(TimeSeries), null,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var ts = ((TimeSeries)bindable);

                ts.Update();
            });

        bool characterMeasureNeeded = true;

        double charWidth, lineHeight, charPadHorizontal, linePadVertical;

        int prevWidthCharacters = 0;

        int prevHeightLines = 0;

        protected override void OnSizeAllocated(double width, double height)
        {
             base.OnSizeAllocated(width, height);

            if (characterMeasureNeeded)
            {
                if (height < 0 || width < 0) return;

                characterMeasureNeeded = false;
                chartLabel.Text = "1";
                var s1 = chartLabel.Measure(width, height);
                chartLabel.Text = "12";
                var s2 = chartLabel.Measure(width, height);
                chartLabel.Text = "12\n12";
                var s22 = chartLabel.Measure(width, height);

                charWidth = s2.Request.Width - s1.Request.Width;
                charPadHorizontal = s1.Request.Width - charWidth;
                lineHeight = s22.Request.Height - s2.Request.Height;
                linePadVertical = s2.Request.Height - lineHeight;

                if (HeigthLines > 0) chartLabel.Text = new string('\n', HeigthLines);
            }

            var widthChars = Math.Max((int)((width - charPadHorizontal - widthFix) / charWidth), 0);
            var heightLines = Math.Max((int)((height - linePadVertical - heightFix) / lineHeight), 2);

            if (prevWidthCharacters != widthChars)
            {
                ascii.WidthCharacters = widthChars;
                prevWidthCharacters = widthChars;
                Update();
            }

            if (prevHeightLines != heightLines)
            {
                if (HeigthLines == 0)
                {
                    ascii.HeigthLines = heightLines;
                    prevHeightLines = heightLines;
                    Update();
                }
            }
        }

        /// <summary>
        /// Force control re-render (if visible)
        /// </summary>
        public void Update()
        {
            if (IsVisible) chartLabel.Text = ascii.RenderToString();
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == nameof(IsVisible) && IsVisible)
                chartLabel.Text = ascii.RenderToString();
        }
    }
}
