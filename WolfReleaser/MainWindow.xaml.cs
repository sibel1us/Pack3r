using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
using WolfReleaser.General;
using WolfReleaser.Objects;
using WolfReleaser.Parsers;

namespace WolfReleaser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private LogLevel _loglevel = LogLevel.Fatal;
        public LogLevel LogLevel
        {
            get => _loglevel;
            set
            {
                if (value != _loglevel)
                {
                    _loglevel = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogLevel)));
                }
            }
        }

        public ObservableCollection<LogEntry> LogLines { get; }

        public MainWindow()
        {
            this.LogLines = new ObservableCollection<LogEntry>();

            InitializeComponent();
            this.DataContext = this;

            this.InitConsole();

            var fc = new MapFileCollection(@"C:\Temp\ET\map\ET\etmain\maps\inertia.map");

            var allFilez = fc.GetFiles();

            var sw = Stopwatch.StartNew();

            foreach (var f in allFilez)
            {
                Console.WriteLine($"{f.target.PadRight(40)} in '{f.source}'");
            }
            //var mapParser = new MapParser(@"C:\Temp\ET\map\ET\etmain\maps\railgun_final.map");
            //var map = mapParser.Parse();
            //var shaders = ShaderParser.ReadShaders(@"C:\Temp\ET\map\ET\etmain\scripts").ToList();
            //var requiredFiles = ShaderParser.GetRequiredFiles(map, shaders);
            sw.Stop();
        }


        private void InitConsole()
        {
            Log.OutInfo = (msg) => LogLines.Add(new LogEntry
            {
                Message = msg,
                Level = LogLevel.Info
            });
            Log.OutWarn = (msg) => LogLines.Add(new LogEntry
            {
                Message = msg,
                Level = LogLevel.Warn
            });
            Log.OutError = (msg) => LogLines.Add(new LogEntry
            {
                Message = msg,
                Level = LogLevel.Error
            });
            Log.OutFatal = (msg) => LogLines.Add(new LogEntry
            {
                Message = msg,
                Level = LogLevel.Fatal
            });
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            this.LogLines.Clear();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            switch ((sender as RadioButton).Content)
            {
                case "Debug (All)":
                    this.LogLevel = LogLevel.Debug;
                    break;
                case "Info":
                    this.LogLevel = LogLevel.Info;
                    break;
                case "Warning":
                    this.LogLevel = LogLevel.Warn;
                    break;
                case "Error":
                    this.LogLevel = LogLevel.Error;
                    break;
                case "Fatal":
                    this.LogLevel = LogLevel.Fatal;
                    break;
                default:
                case "None":
                    this.LogLevel = LogLevel.None;
                    break;
            }
        }
    }

    public class LogColorConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            switch (value)
            {
                case LogLevel.Info:
                    return Brushes.WhiteSmoke;
                case LogLevel.Warn:
                    return Brushes.Goldenrod;
                case LogLevel.Error:
                    return Brushes.Red;
                case LogLevel.Fatal:
                    return Brushes.Magenta;
                default:
                case LogLevel.Debug:
                    return Brushes.LightGreen;
            }
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LogVisibleConverter : IMultiValueConverter
    {
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            var level = (int)(values[0] as LogEntry).Level;
            var maxlevel = (int)values[1];
            return level >= maxlevel ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
