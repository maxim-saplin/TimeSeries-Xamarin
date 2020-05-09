21KB .NET Standard library (compared to at least 8MB with any SkiaSharp based chart control).

A visual element built purely on top of Xamarin.Forms controls (no platform renderers or platform-specific code) that displays ASCII time series chart via multi-line monospaced Label wrapped in a Grid. The control can resize to adapt to it's container. It was tested on Android, iOS, macOS and WPF though should work fine across Xamarin.Froms supported platforms.


# Usage
1. Add [Nuget](https://www.nuget.org/packages/Saplin.TimeSeries.Xamarin/) package or reference to `Saplin.TimeSeries.Xamarin.dll` library (check [Releases](https://github.com/maxim-saplin/TimeSeries-Xamarin/releases)) to your Xamarin.Forms UI project

2. Add XAML reference to control's namespace in a view file:
```
<ContentPage 
    xmlns:ctrl="clr-namespace:Saplin.TimeSeries.Xamarin;assembly=Saplin.TimeSeries.Xamarin" ...
```

3. Add control to XAML, adjust the looks:
```
<ctrl:TimeSeries x:Name="timeSeries" BackgroundColor="#FFBBBBBB" FontSize="10" Margin="10, 5, 10, 10"
                            HeigthLines="0" VerticalOptions="FillAndExpand"/>
```
- in this case `HeightLines` is set to `0` so that the chart could grow vertically together with it's container.

4. Set `Series` property in code behind (or via Binding).
```
timeSeries.Series = GenerateRandom(counter);
```

Check the demo project in this repo.

# Monospaced Fonts
It is important to use a monospaced font (set via `FontFamily` property) that has all the ASCII box chars and misc chars (e.g. set in `AbovePointChar` property) to have the control properly displayed. Default font family works fine on iOS, macOS and WPF. Default Android's `monospace` font is NOT OK. You can use [Source Code Pro](https://github.com/maxim-saplin/TimeSeries-Xamarin/blob/master/Saplin.TimeSeries.XamarinDemo/SourceCodePro-Regular.ttf) font and import it like [that](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/text/fonts#use-a-custom-font) (iOS, Android) or [that](http://saplin.blogspot.com/2018/12/xamarinforms-custom-fonts-with-android.html) (macOS, WPF).

![UI](https://github.com/maxim-saplin/TimeSeries-Xamarin/blob/master/TimeSeries-Xamarin.gif?raw=true)
