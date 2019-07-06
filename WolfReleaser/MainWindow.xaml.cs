using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    public enum UIState
    {
        NeedMap = 0,
        MissingFiles,
        ReadyToPack,
        Packed,
        FatalError = int.MaxValue
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private bool forceClose = false;

        #region UI Bind Properties

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
                    OnPropertyChanged();
                }
            }
        }

        private UIState _uistate = UIState.NeedMap;
        public UIState UIState
        {
            get => _uistate;
            set
            {
                if (value != _uistate)
                {
                    _uistate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StateNeedMap));
                    OnPropertyChanged(nameof(StateMissingFiles));
                    OnPropertyChanged(nameof(StateReadyToPack));
                    OnPropertyChanged(nameof(StatePacked));
                    OnPropertyChanged(nameof(StateFatalError));
                }
            }
        }

        public ObservableCollection<LogEntry> LogLines { get; }

        public bool StateNeedMap => _uistate == UIState.NeedMap;
        public bool StateMissingFiles => _uistate == UIState.MissingFiles;
        public bool StateReadyToPack => _uistate == UIState.ReadyToPack;
        public bool StatePacked => _uistate == UIState.Packed;
        public bool StateFatalError => _uistate == UIState.FatalError;

        #endregion UI Bind Properties

        public MainWindow()
        {
            this.LogLines = new ObservableCollection<LogEntry>();
            this.LogLines.CollectionChanged +=
                delegate (object s, NotifyCollectionChangedEventArgs _)
                {
                    this.ConsoleScrollViewer.ScrollToEnd();
                };

            InitializeComponent();
            this.DataContext = this;

            this.InitConsole();

            var ass = Assembly.GetExecutingAssembly().GetName().Version;
            Log.Info($"Pack3r Version {ass}");
            Log.Info($"Report issues at github.com/sibel1us/Pack3r");
            Log.Info($"");
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
            Log.OutDebug = (msg) => LogLines.Add(new LogEntry
            {
                Message = msg,
                Level = LogLevel.Debug
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

            this.ConsoleScrollViewer?.ScrollToEnd();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.LogLevel == LogLevel.None)
                return;

            var sb = new StringBuilder();
            var lvc = new LogVisibleConverter();

            foreach (var log in this.LogLines)
            {
                var val = lvc.Convert(
                    new object[] { log, LogLevel },
                    null,
                    null,
                    null);

                if ((Visibility)val == Visibility.Visible)
                {
                    sb.Append($"{log.Level}:".PadRight(7));
                    sb.AppendLine(log.Message);
                }
            }

            Clipboard.SetText(sb.ToString());
        }

        protected void OnPropertyChanged(
            [CallerMemberName]string propertyName = null)
        {
            if (propertyName != null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            if ((int)this.LogLevel > (int)LogLevel.Info)
                this.LogLevel = LogLevel.Info;

            var help = new[]
            {
                "Select the .map file you wish to analyze, and select the output " +
                "folder to write packed pk3 to.",
                "You can choose to include the .map source file, but you do not need to.",
                "Note that files that are unneeded in the release are NOT included. You will " +
                "have to include them yourself. This includes (but might not be limited to): " +
                "editorimages, lightimages, misc_models",
                "You can also choose a different release name for your map, so you don't have to " +
                "keep renaming mapscripts and such to different beta versions.",
                "",
                "Note: this tool makes multiple assumptions about your folder structure:",
                "- You have everything in their normal locations, meaning the .map file " +
                "is in etmain/maps, and that other required files (mapscripts and such) are " +
                "in their correct folders. Put log-level to 'All' to see information about " +
                "possible missing files.",
                "- The program needs to have write access to %appdata% and the folder where " +
                "the release pk3 is packed to.",
                "",
            };

            foreach (var h in help)
                Log.Info(h);

            Log.Info("");
        }

        private void MainWnd_Closing(object sender, CancelEventArgs e)
        {
            if (!forceClose)
            {
                Log.Info("Cleaning up temporary files...");
                FileUtil.DeleteTempData();
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
