using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Xamarin.Forms;

namespace Saplin.TimeSeries.XamarinDemo
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        int counter = 20;
        Random rand = new Random();

        ObservableCollection<double> wave = new ObservableCollection<double>();

        public MainPage()
        {
            InitializeComponent();

            waveTimeSeries.Series = wave;
            timeSeries.Series = GenerateRandom(counter);

            collectionTypeButton.Text = showingRandom;
            switchAllTailButton.Text = showingTail;

            Device.StartTimer(TimeSpan.FromMilliseconds(250), () =>
            {
                var val = 5 * Math.Cos(Math.PI * wave.Count / 45) * (1 + 0.2 * rand.NextDouble()) + 1.85 * rand.NextDouble();
                wave.Add(val);
                waveValue.Text = val.ToString("0.00");
                return true;
            });
        }

        List<double> GenerateRandom(int n)
        {
            var list = new List<double>();

            for (int i = 0; i < n; i++) 
                list.Add(5 - rand.Next(0, 1000000) / (double)100000);

            return list;
        }

        List<double> GenerateParabola(int n)
        {
            var list = new List<double>();

            for (int i = 0; i < n; i++)
                list.Add((i-60)*(i-60)-20);

            return list;
        }

        const string showingRandom = "Showing Random";
        const string showingParabola = "Showing Parabola";
        const string showingTail = "Showing Tail";
        const string showingAll = "Showing All";

        void collectionTypeButton_Clicked(System.Object sender, System.EventArgs e)
        {
            counter = 0;
            if (collectionTypeButton.Text == showingRandom)
                collectionTypeButton.Text = showingParabola;
            else collectionTypeButton.Text = showingRandom;

            addDataButton_Clicked(null, null);
        }


        void addDataButton_Clicked(System.Object sender, System.EventArgs e)
        {
            counter+=20;


            IEnumerable<double> l = collectionTypeButton.Text == showingRandom ?
                GenerateRandom(counter) :
                GenerateParabola(counter);

            timeSeries.Series = l;
        }

        void switchAllTailButton_Clicked(System.Object sender, System.EventArgs e)
        {
            timeSeries.ShowOnlyTail = !timeSeries.ShowOnlyTail;
            if (timeSeries.ShowOnlyTail) switchAllTailButton.Text = showingTail;
                else switchAllTailButton.Text = showingAll;
        }

    }
}
