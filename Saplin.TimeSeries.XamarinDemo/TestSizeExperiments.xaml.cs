using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Saplin.TimeSeries.XamarinDemo
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class TestSizeExperiments : ContentPage
    {
        private int counter = -1;

        public TestSizeExperiments()
        {
            InitializeComponent();
            
            Device.StartTimer(TimeSpan.FromMilliseconds(500), () => { Button_Clicked(null, null); return false; });
        }

        string graph = @" 6.99 ┤╮····╭╮···············╭╮········
 6.29 ┤│╭───╯╰────╮╭╮╭─╮╭────╯│╭╮╭──╮╭─
 5.59 ┤╰╯         ╰╯╰╯ ╰╯     ╰╯╰╯  ╰╯ 
 4.89 ┤                                
 4.20 ┤                                
 3.50 ┤                                
 2.80 ┤                                
 2.10 ┤                                
 1.40 ┤                                
 0.70 ┤                                
-0.00 ┤                                ";

        void Button_Clicked(System.Object sender, System.EventArgs e)
        {
            counter++;

            switch (counter)
            {
                case 0: test.Text = "1";  break;
                case 1: test.Text = "12"; break;
                case 2: test.Text = "123"; break;
                case 3: test.Text = "123\n1"; break;
                case 4: test.Text = "123\n12"; break;
                case 5: test.Text = "123\n1234"; break;
                case 6: test.Text = "─╮ \n │ \n ╰─"; break;
                case 7: test.Text = "12.2 ─╮ \n      │ \n      ╰─"; break;
                case 8: test.Text = "12.2\n ─╮ \n  │ \n  ╰─"; break;
                case 9: test.Text = graph; break;
                default: test.Text = "11";  counter = 0; break;
            }

            Device.StartTimer(TimeSpan.FromMilliseconds(100), () => { ShowParams(); return false; });
        }

        void ShowParams()
        {
            var lines = test.Text.Split('\n');
            width.Text = "W: " + test.Width.ToString("0.0") + "; Per char: " + (test.Width / lines.Select(l => l.Length).Max()).ToString("0.0");
            height.Text = "H: " + test.Height.ToString("0.0") + "; Per line: " + (test.Height / lines.Length).ToString("0.0");

            log.Text += width.Text + " - " + height.Text + "\n";
        }
    }
}
