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
using Microsoft.Win32;
using WolfReleaser.General;
using WolfReleaser.Objects;
using WolfReleaser.Parsers;

namespace WolfReleaser
{
    public enum UIState
    {
        NeedMap = 0,
        ReadyToScan,
        ReadyToPack,
        Packed,
        FatalError = int.MaxValue
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _outpath = null;
        private Map _map = null;

        public Map CurrentMap
        {
            get => _map;
            private set
            {
                if (_map != value)
                {
                    _map = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ContainingFolderButtonEnabled));
                }
            }
        }

        public string OutFilePath
        {
            get => _outpath;
            private set
            {
                if (_outpath != value)
                {
                    _outpath = value;
                    OnPropertyChanged();
                }
            }
        }

        public MapFileCollection AllFiles { get; private set; }

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
                    OnPropertyChanged(nameof(StateReadyToScan));
                    OnPropertyChanged(nameof(StateReadyToPack));
                    OnPropertyChanged(nameof(StatePacked));
                    OnPropertyChanged(nameof(StateFatalError));
                    OnPropertyChanged(nameof(SelectResetButtonText));
                }
            }
        }

        public ObservableCollection<LogEntry> LogLines { get; }

        public bool StateNeedMap => _uistate == UIState.NeedMap;
        public bool StateReadyToScan => _uistate == UIState.ReadyToScan;
        public bool StateReadyToPack => _uistate == UIState.ReadyToPack;
        public bool StatePacked => _uistate == UIState.Packed;
        public bool StateFatalError => _uistate == UIState.FatalError;

        public string SelectResetButtonText => this.StateNeedMap ? "Browse" : "Reset";
        public bool ContainingFolderButtonEnabled
            => (_map != null && Directory.Exists(_map.ETMain));

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
            Log.Info($"Report issues at github.com/sibel1us/Pack3r, and include " +
                "the log contents with 'All'-loglevel.");
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
            {
                this.RadioInfo.IsChecked = true;
            }

            var help = new[]
            {
                "Select the .map file you wish to analyze, and select the output " +
                "folder to write packed pk3 to.",
                "You can choose to include the .map source file, but you do not need to.",
                "Note that files that are unneeded in the release are NOT included. You will " +
                "have to include them yourself. This includes (but might not be limited to): " +
                "editorimages, lightimages, misc_models",
                //"You can also choose a different release name for your map, so you don't have to " +
                //"keep renaming mapscripts and such to different beta versions.",
                "You have everything in their normal locations, meaning the .map file " +
                "is in etmain/maps, and that other required files (mapscripts and such) are " +
                "in their correct folders. Put log-level to 'All' to see information about " +
                "possible missing files.",
                "The program needs to have write access to %appdata% and the folder where " +
                "the release pk3 is packed to.",
                "",
            };

            foreach (var h in help)
                Log.Info(h);

            Log.Info("");
        }

        private void MainWnd_Closing(object sender, CancelEventArgs e)
        {
            Log.Info("Cleaning up temporary files...");
            FileUtil.DeleteTempData();
        }

        private void SelectReset_Click(object sender, RoutedEventArgs _)
        {
            if (StateNeedMap)
            {
                var openFileDialog = new OpenFileDialog
                {
                    CheckFileExists = true,
                    Filter = "Map Source Files (*.map)|*.map"
                };

                if (openFileDialog.ShowDialog() != true)
                    return;

                var path = openFileDialog.FileName;

                try
                {
                    var parser = new MapParser(path);
                    if (parser.Parse() is Map map)
                    {
                        this.CurrentMap = map;
                        this.UIState = UIState.ReadyToScan;
                    }
                }
                catch (Exception e)
                {
                    Log.Fatal($"Failed to read map: {e.Message}");
                    Log.Debug(e.ToString());
                    return;
                }
            }
            else
            {
                this.UIState = UIState.NeedMap;
                this.CurrentMap = null;
                this.AllFiles = null;
                this.OutFilePath = null;
                Log.Debug("Selected map reset.");
            }
        }

        private void OpenContainingFolder_Click(object sender, RoutedEventArgs _)
        {
            try
            {
                Process.Start(CurrentMap.ETMain);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to open folder '{CurrentMap.ETMain}'");
                Log.Debug(e.ToString());
            }
        }

        private void ButtonScanFiles_Click(object sender, RoutedEventArgs _)
        {
            try
            {
                var shaders = ShaderParser.ReadShaders(
                    System.IO.Path.Combine(CurrentMap.ETMain, "scripts")).ToList();

                if (shaders.Count == 0)
                    throw new Exception("Found no shaders");

                this.AllFiles = new MapFileCollection(this.CurrentMap.FullPath);
                this.UIState = UIState.ReadyToPack;
                Log.Info("Finished analyzing files");
            }
            catch (Exception e)
            {
                this.AllFiles = null;
                Log.Error($"Error when analyzing files: {e.Message}");
                Log.Debug(e.ToString());
            }
        }

        private void ButtonSelectOutFolder_Click(object sender, RoutedEventArgs _)
        {
            try
            {
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    var result = dialog.ShowDialog();

                    if (result != System.Windows.Forms.DialogResult.OK)
                        return;

                    var pk3 = Path.Combine(
                        dialog.SelectedPath,
                        Path.ChangeExtension(CurrentMap.Name, "pk3"));

                    if (!Directory.Exists(dialog.SelectedPath))
                        throw new DirectoryNotFoundException("Directory does not exist " +
                            $"({dialog.SelectedPath})");

                    if (File.Exists(pk3))
                        throw new Exception($"File already exists: {pk3}");

                    this.OutFilePath = pk3;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error when selecting output folder: {e.Message}");
                Log.Debug(e.ToString());
            }
        }

        private void ButtonPack_Click(object sender, RoutedEventArgs _)
        {
            try
            {
                var stockPk3s = new[]
                {
                    Path.Combine(CurrentMap.ETMain, "pak0.pk3"),
                    Path.Combine(CurrentMap.ETMain, "pak1.pk3"),
                    Path.Combine(CurrentMap.ETMain, "pak2.pk3")
                };

                var existingFiles = Pk3Reader.GetFiles(stockPk3s);

                Pk3Packer.PackPk3(
                    this.OutFilePath,
                    this.AllFiles,
                    existingFiles);

                Log.Fatal($"Succesfully packed to {OutFilePath}");
            }
            catch (Exception e)
            {
                Log.Error($"Error when packing pk3: {e.Message}");
                Log.Debug(e.ToString());
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
