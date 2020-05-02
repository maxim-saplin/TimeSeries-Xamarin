using Xamarin.Forms.Platform.WPF;
using Xamarin.Forms;

namespace Saplin.TimeSeries.XamarinDemo.WPF
{
    public partial class MainWindow : FormsApplicationPage
    {
        public MainWindow()
        {
            InitializeComponent();
            Forms.Init();
            var app = new Saplin.TimeSeries.XamarinDemo.App();
            LoadApplication(app);
        }
    }
}
