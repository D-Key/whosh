using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WindowChrome.Demo.Styles.VS2012;

namespace WindowChrome.Demo
{
    public class CLog
    {
        private const int Max = 20;
        public ObservableCollection<string> Entries { get; private set; }
        public CLog() { Entries = new ObservableCollection<string>(); }
        public void Write(string type, string format, params object[] parameters)
        {
            var prefix = type + ": ";
            var message = prefix + string.Format(format, parameters);
            if(Entries.Count > 0 && Entries[0].StartsWith(prefix)) Entries.RemoveAt(0);
            while (Entries.Count > Max - 1) Entries.RemoveAt(Max - 1);
            Entries.Insert(0, message);
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CLog Log = new CLog();

        Border Box { get { return (Border)Template.FindName("PART_WindowContainer", this); } }
        double BL { get { return Box.PointToScreen(new Point(0,0)).X; } }
        double BT { get { return Box.PointToScreen(new Point(0,0)).Y; } }
        double BW { get { return Box.ActualWidth; } }
        double BH { get { return Box.ActualHeight; } }

        public MainWindow()
        {
            InitializeComponent();

            DataContext = Log;

            LocationChanged += MainWindow_LocationChanged;
            SizeChanged += MainWindow_SizeChanged;
            StateChanged += MainWindow_StateChanged;

            Hacks.OnAfterSnapping += Hacks_OnAfterSnapping;
        }

        void Hacks_OnAfterSnapping(Hacks.SnapSide snap)
        {
            Log.Write("Snapping Adjustment", "{0}", snap);
        }

        void MainWindow_StateChanged(object sender, EventArgs e)
        {
            Log.Write("StateChanged", "{0}", WindowState);
        }

        void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Log.Write("SizeChanged",
                "Window {0},{1} {2} x {3} | Box {4},{5} {6} x {7}",
                Left, Top, ActualWidth, ActualHeight,
                BL, BT, BW, BH);
        }

        void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            Log.Write("LocationChanged",
                "Window {0},{1} {2} x {3} | Box {4},{5} {6} x {7}",
                Left, Top, ActualWidth, ActualHeight,
                BL, BT, BW, BH);
        }
    }
}
