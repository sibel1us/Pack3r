using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using WolfReleaser.Parsers;

namespace WolfReleaser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var sw = Stopwatch.StartNew();
            var map = MapParser.ParseMap(@"C:\Temp\ET\map\ET\etmain\maps\railgun_final.map");
            var shaders = ShaderParser.ReadShaders(@"C:\Temp\ET\map\ET\etmain\scripts").ToList();
            var requiredFiles = ShaderParser.GetRequiredFiles(map, shaders);
            sw.Stop();
        }
    }
}
