using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Fonts;

using System.Globalization;
using MigraDoc.DocumentObjectModel;
using System.Net.NetworkInformation;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;


namespace PLG_Exam
{
    public partial class MainWindow : Window
    {
        private int _tabCounter = 0;
        private bool _isSaved = true; 
        private string? _currentFilePath;
        public static Exam _currentExam = new();
        public static MainWindow _instance;

        public MainWindow()
        {
            InitializeComponent();
            InitializeBackupTimer();
            //AddNewTab();
            _instance = this;

            this.KeyDown += OnKeyDown;
            GlobalFontSettings.FontResolver = new CustomFontResolver();

            RenewColors();
            this.Closing += MainWindow_Closing;

            if(DatumField.SelectedDate.HasValue == false){
                DatumField.SelectedDate = DateTime.Now;
            }

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += (_, __) => {
                LblClock.Content = DateTime.Now.ToString("HH:mm:ss");
            };
            timer.Start();
            
        }

        private void OnDarkModeClick(object? sender, RoutedEventArgs e)
        {
            if (Application.Current is App app)
            {
                app.ToggleTheme();
                RenewColors();
            }

            
        }

        private void RenewColors(){
            if(Application.Current.ActualThemeVariant == ThemeVariant.Dark){
                BrdMid.Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(35,35,39)); // #232327
                //BrdTop.Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(35,35,39)); // #232327
                BrdTitle.Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(35,35,39)); // #232327
                BrdName.Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(35,35,39)); // #232327
            } else {
                BrdMid.Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(230,230,230)); // #232327
                //BrdTop.Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(230,230,230)); // #232327
                BrdTitle.Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(230,230,230)); // #232327
                BrdName.Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(230,230,230)); // #232327
            }

            if(Application.Current.ActualThemeVariant == ThemeVariant.Dark){
                BtnTheme.Content="🌞";
            } else {
                BtnTheme.Content="🌙";
            }
        }

        // Event für "Neuen Tab hinzufügen"
        private void OnAddTabClick(object? sender, RoutedEventArgs e)
        {
            AddNewTab();
        }

        private bool isFormVisible = true;
        private void OnToggleClick(object? sender, RoutedEventArgs e)
        {
            isFormVisible = !isFormVisible;

            BrdTitle.IsVisible = isFormVisible;
            BrdName.IsVisible = isFormVisible;
        }

        private void OnInfoClick(object? sender, RoutedEventArgs e)
        {
            new InfoWindow(CountTotalWords()).Show();
        }

        public int CountWords(string s)
        {
            s = s.Trim();
            if (s == "")
                return 0;
            return s.Split(new char[] { ' ', '.', '?', '!', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        public string CountTotalWords(){
            _ = GetCurrentExamDataAsJson();
            string toReturn = "";
            int totalWords = 0;
            foreach (var tab in _currentExam.Tabs)
            {
                totalWords += CountWords(tab.Inhalt);
                toReturn += "Aufgabe " + tab.Aufgabennummer + ": " + CountWords(tab.Inhalt) + "\n";
            }
            
            return $"Anzahl Wörter (insg.): {totalWords}\n" + toReturn;
        }

        private async void OnKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control))
            {
                switch (e.Key)
                {
                    case Avalonia.Input.Key.S:
                        if (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift))
                        {
                            // Strg + Shift + S: Speichern Unter
                            OnSaveAsClick(sender, e);
                        }
                        else
                        {
                            // Strg + S: Speichern
                            OnSaveClick(sender, e);
                        }
                        break;

                    case Avalonia.Input.Key.O:
                        // Strg + O: Öffnen
                        OnOpenClick(sender, e);
                        break;

                    case Avalonia.Input.Key.T:
                        // Strg + T: Neuer Tab
                        OnAddTabClick(sender, e);
                        break;

                    case Avalonia.Input.Key.N:
                        // Strg + N: Neu
                        OnNewClick(sender, e);
                        break;

                    case Avalonia.Input.Key.R:
                        // Strg + R: Abgeben (coming soon)
                        OnSubmitClick(sender, e);
                        break;
                }
            }
        }

        private async void OnSubmitClick(object sender, RoutedEventArgs e)
        {
            ExportOptions exportOptions = new ExportOptions();
            exportOptions.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            await exportOptions.ShowDialog(this);
            
        }


        private void AddNewTab()
        {
            _tabCounter++;

            var closeButton = new Button
            {
                Content = "×",
                FontSize = 14,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Width = 20,
                Height = 20,
                Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(123,35,39)),
                Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(0,0,0)),
                Padding = new Thickness(0),
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            ToolTip.SetTip(closeButton, "Tab schließen");

            var headerStackPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal
            };

            var tabHeader = new TextBlock
            {
                Text = $"{_tabCounter} - Neu",
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            headerStackPanel.Children.Add(tabHeader);
            headerStackPanel.Children.Add(closeButton);

            var tabItem = new TabItem
            {
                Header = headerStackPanel,
                Content = CreateTabContent()
            };

            // Schließen-Event hinzufügen
            closeButton.Click += async (sender, e) =>
            {
                var result = await MessageBox.Show(this, "Diese Aktion schließt die Aufgabe unwiderruflich. Möchten Sie fortfahren?", 
                                                "Tab schließen", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    TabView.Items.Remove(tabItem);
                    _isSaved = false;

                    LblFilename.Content = (!_currentFilePath.IsValueNullOrEmpty() ? Path.GetFileName(_currentFilePath) : "Ungespeichert ") + " *";
                }
            };

            TabView.Items.Add(tabItem);
            TabView.SelectedItem = tabItem;
            _isSaved = false;

            LblFilename.Content = (!_currentFilePath.IsValueNullOrEmpty() ? Path.GetFileName(_currentFilePath) : "Ungespeichert ") + " *";
        }


        // Tab entfernen mit Sicherheitsabfrage
        private async void OnRemoveTabClick(object? sender, RoutedEventArgs e)
        {
            if (TabView.SelectedItem is TabItem selectedTab)
            {
                var result = await MessageBox.Show(this, "Dieser Tab wird unwiderruflich gelöscht. Möchten Sie fortfahren?", 
                                                   "Tab löschen", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    TabView.Items.Remove(selectedTab);
                    _isSaved = false;
                    LblFilename.Content = (!_currentFilePath.IsValueNullOrEmpty() ? Path.GetFileName(_currentFilePath) : "Ungespeichert ") + " *";
                }
            }
        }

        // Speichern
        private async void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                await SaveAs();
            }
            else
            {
                SaveToFile(_currentFilePath);
            }
        }

        private async void OnSaveAsClick(object? sender, RoutedEventArgs e)
        {
            await SaveAs();
        }

        private async Task<bool> SaveAs()
        {
            var saveDialog = new SaveFileDialog
            {
                DefaultExtension = "exam",
                Filters = { new FileDialogFilter { Name = "Exam files", Extensions = { "exam" } } }
            };
            var filePath = await saveDialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(filePath))
            {
                SaveToFile(filePath);
                return true;
            } else {
                return false;
            }
        }

        private void SaveToFile(string filePath)
        {
            // Update Exam-Daten
            _currentExam.Name = NameField.Text;
            _currentExam.Title = TitleField.Text;
            _currentExam.Vorname = VornameField.Text;
            _currentExam.Datum = DatumField.SelectedDate?.UtcDateTime;
            _currentExam.Tabs = TabView.Items.OfType<TabItem>()
                .Select(tab =>
                {
                    var grid = tab.Content as Grid;
                    if (grid == null) return null;

                    var aufgabennummer = (grid.Children[0] as Grid)?.Children[0] as TextBox;
                    var ueberschrift = (grid.Children[0] as Grid)?.Children[1] as TextBox;
                    var beschreibung = grid.Children[1] as TextBox;

                    return new ExamTab
                    {
                        Aufgabennummer = aufgabennummer?.Text ?? "",
                        Überschrift = ueberschrift?.Text ?? "",
                        Inhalt = beschreibung?.Text ?? ""
                    };
                })
                .Where(tab => tab != null)
                .ToList();

            var json = JsonSerializer.Serialize(_currentExam, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);

            _currentFilePath = filePath;
            _isSaved = true;

            LblFilename.Content = !_currentFilePath.IsValueNullOrEmpty() ? Path.GetFileName(_currentFilePath) : "Ungespeichert ";
        }

        // Öffnen
        private async void OnOpenClick(object? sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Filters = { new FileDialogFilter { Name = "Exam files", Extensions = { "exam" } } }
            };
            var result = await openDialog.ShowAsync(this);

            if (result != null && result.Length > 0)
            {
                if (!_isSaved)
                {
                    var result2 = await MessageBox.Show(this, "Möchten Sie die aktuellen Änderungen speichern?", 
                                                    "Nicht gespeicherte Änderungen", MessageBoxButton.YesNoCancel);
                    if (result2 == MessageBoxResult.Cancel) return;
                    if (result2 == MessageBoxResult.Yes) {
                        if((await SaveAs()) != true){
                            return;
                        }
                    }
                }
                LoadFromFile(result[0]);
            }
        }

        private void LoadFromFile(string filePath)
        {
            var fileContent = File.ReadAllText(filePath);
            var exam = JsonSerializer.Deserialize<Exam>(fileContent);

            if (exam == null) return;

            // Daten wiederherstellen
            _currentExam = exam;
            NameField.Text = exam.Name;
            TitleField.Text = exam.Title;
            VornameField.Text = exam.Vorname;
            DatumField.SelectedDate = exam.Datum;

            TabView.Items.Clear();
            _tabCounter = 0;

            foreach (var tab in exam.Tabs)
            {
                _tabCounter++;
                var tabItem = new TabItem
                {
                    
                    Content = CreateTabContent(tab.Aufgabennummer, tab.Überschrift, tab.Inhalt)
                };
                tabItem.Header = ReturnTabHeaderContent($"{tab.Aufgabennummer} - {tab.Überschrift}", tabItem);
                TabView.Items.Add(tabItem);
            }

            _currentFilePath = filePath;
            _isSaved = true;

            LblFilename.Content = !_currentFilePath.IsValueNullOrEmpty() ? Path.GetFileName(_currentFilePath) : "Ungespeichert *";

            SaveToFile(filePath);
        }

        private StackPanel ReturnTabHeaderContent(string name, TabItem curr_tab){
            var closeButton = new Button
            {
                Content = "×",
                FontSize = 14,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Width = 20,
                Height = 20,
                Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(123,35,39)),
                Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255,255,255)),
                Padding = new Thickness(0),
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            ToolTip.SetTip(closeButton, "Tab schließen");

            var headerStackPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal
            };

            var tabHeader = new TextBlock
            {
                Text = name,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            headerStackPanel.Children.Add(tabHeader);
            headerStackPanel.Children.Add(closeButton);

            // Schließen-Event hinzufügen
            closeButton.Click += async (sender, e) =>
            {
                var result = await MessageBox.Show(this, "Diese Aktion schließt die Aufgabe unwiderruflich. Möchten Sie fortfahren?", 
                                                "Tab schließen", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    TabView.Items.Remove(curr_tab);
                    _isSaved = false;
                    LblFilename.Content = (!_currentFilePath.IsValueNullOrEmpty() ? Path.GetFileName(_currentFilePath) : "Ungespeichert ") + " *";
                }
            };

            return headerStackPanel;
        }

        private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            if (!_isSaved)
            {
                MessageBoxResult result = await MessageBox.Show(this, "Möchten Sie die aktuellen Änderungen speichern?", 
                                                   "Nicht gespeicherte Änderungen", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Cancel) {
                    e.Cancel = true;
                    return;
                }
                if (result == MessageBoxResult.Yes) {
                    //SaveToFile(_currentFilePath);
                    OnSaveClick(null,null);
                    if(_isSaved == false){
                        e.Cancel = true;
                        return;
                    }
                }
            }
            e.Cancel = false;
            Environment.Exit(0);
        }

        // Neu
        private async void OnNewClick(object? sender, RoutedEventArgs e)
        {
            if (!_isSaved)
            {
                var result = await MessageBox.Show(this, "Möchten Sie die aktuellen Änderungen speichern?", 
                                                   "Nicht gespeicherte Änderungen", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Cancel) return;
                if (result == MessageBoxResult.Yes) {
                    if((await SaveAs()) != true){
                        return;
                    }
                }
            }

            _currentExam = new Exam();
            NameField.Text = "";
            VornameField.Text = "";
            DatumField.SelectedDate = null;
            TabView.Items.Clear();
            _tabCounter = 0;
            _currentFilePath = null;
            _isSaved = true;

            LblFilename.Content = !_currentFilePath.IsValueNullOrEmpty() ? Path.GetFileName(_currentFilePath) : "Ungespeichert *";
        }

        private Grid CreateTabContent(string? aufgabennummer = null, string? ueberschrift = null, string? beschreibung = null)
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(33) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var aufgabenNummerTextBox = new TextBox
            {
                Name = "Aufgabennummer",
                Watermark = "Aufgabennummer",
                Margin = new Thickness(0,10,0,0),
                Text = aufgabennummer ?? ""
            };

            var ueberschriftTextBox = new TextBox
            {
                Name = "Überschrift",
                Watermark = "Überschrift (optional)",
                Margin = new Thickness(10,10,0,0),
                Text = ueberschrift ?? ""
            };

            var beschreibungTextBox = new TextBox
            {
                Name = "Beschreibung",
                AcceptsReturn = true,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                Watermark = "Beschreibung",
                Text = beschreibung ?? "",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                LineHeight = 21,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Event für dynamische Tab-Umbenennung
            aufgabenNummerTextBox.TextChanged += (s, e) => UpdateTabHeader(TabView.SelectedItem as TabItem, aufgabenNummerTextBox, ueberschriftTextBox);
            ueberschriftTextBox.TextChanged += (s, e) => UpdateTabHeader(TabView.SelectedItem as TabItem, aufgabenNummerTextBox, ueberschriftTextBox);

            beschreibungTextBox.TextChanged += (s, e) =>
            {
                _isSaved = false;
                LblFilename.Content = (!_currentFilePath.IsValueNullOrEmpty() ? Path.GetFileName(_currentFilePath) : "Ungespeichert ") + " *";
            };

            headerGrid.Children.Add(aufgabenNummerTextBox);
            headerGrid.Children.Add(ueberschriftTextBox);
            Grid.SetColumn(aufgabenNummerTextBox, 0);
            Grid.SetColumn(ueberschriftTextBox, 1);

            grid.Children.Add(headerGrid);
            grid.Children.Add(beschreibungTextBox);
            Grid.SetRow(headerGrid, 0);
            Grid.SetRow(beschreibungTextBox, 1);

            return grid;
        }

        private void UpdateTabHeader(TabItem? tab, TextBox aufgabennummer, TextBox ueberschrift)
        {
            if (tab == null) return;

            var aufgabeText = string.IsNullOrWhiteSpace(aufgabennummer.Text) ? "Neu" : aufgabennummer.Text;
            var ueberschriftText = string.IsNullOrWhiteSpace(ueberschrift.Text) ? "" : ueberschrift.Text;

            tab.Header = ReturnTabHeaderContent(string.IsNullOrWhiteSpace(ueberschriftText)
                ? aufgabeText
                : $"{aufgabeText} - {ueberschriftText}", tab);


            _isSaved = false;
            LblFilename.Content = (!_currentFilePath.IsValueNullOrEmpty() ? Path.GetFileName(_currentFilePath) : "Ungespeichert ") + " *";
        }


        //
        // Backups
        //

        private const int BackupInterval = 10000; // 20 Sekunden in Millisekunden
        

        private DispatcherTimer _backupTimer;

        private void InitializeBackupTimer()
        {
            _backupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(20)
            };
            _backupTimer.Tick += (sender, e) => CreateBackup();
            _backupTimer.Start();
        }

        private const int MaxBackupFiles = 25; // Maximal 5 Backups
        private readonly string BackupFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "PLG-Development", "PLG-Exam", "backup");

        

        // Backup erstellen
        private void CreateBackup()
        {
            try
            {
                // Ordner erstellen, falls nicht vorhanden
                if (!Directory.Exists(BackupFolderPath))
                {
                    Directory.CreateDirectory(BackupFolderPath);
                }

                // Erstelle Dateinamen mit Zeitstempel
                var timestamp = DateTime.Now.ToString("yyMMdd_HHmmss");
                var backupFileName = Path.Combine(BackupFolderPath, $"{timestamp}_backup.exam");

                // Aktuelle Daten in JSON-Format speichern
                var jsonData = GetCurrentExamDataAsJson();
                File.WriteAllText(backupFileName, jsonData);

                // Alte Backups löschen, wenn mehr als MaxBackupFiles vorhanden sind
                CleanupOldBackups();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Backup konnte nicht erstellt werden: {ex.Message}");
            }
        }

        // Löscht alte Backups, wenn die maximale Anzahl überschritten wird
        private void CleanupOldBackups()
        {
            var backupFiles = Directory.GetFiles(BackupFolderPath, "*_backup.exam")
                .OrderBy(File.GetCreationTime)
                .ToList();

            while (backupFiles.Count > MaxBackupFiles)
            {
                var oldestFile = backupFiles.First();
                File.Delete(oldestFile);
                backupFiles.RemoveAt(0);
            }
        }

        // Löscht alle Backups unwiderruflich (mit Überschreiben)
        private void DeleteAllBackups()
        {
            try
            {
                if (Directory.Exists(BackupFolderPath))
                {
                    foreach (var file in Directory.GetFiles(BackupFolderPath, "*_backup.exam"))
                    {
                        OverwriteAndDeleteFile(file);
                    }
                    Directory.Delete(BackupFolderPath, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Backups konnten nicht gelöscht werden: {ex.Message}");
            }
        }

        // Überschreibt eine Datei mit Zufallsdaten und löscht sie anschließend
        private void OverwriteAndDeleteFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return;

                var fileLength = new FileInfo(filePath).Length;

                // Datei mit Zufallsdaten überschreiben
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    var randomData = new byte[fileLength];
                    new Random().NextBytes(randomData);
                    stream.Write(randomData, 0, randomData.Length);
                }

                // Datei löschen
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Datei konnte nicht überschrieben und gelöscht werden: {ex.Message}");
            }
        }

        // Holt die aktuelle Exam-Daten als JSON
        private string GetCurrentExamDataAsJson()
        {
            _currentExam.Title = TitleField.Text;
            _currentExam.Name = NameField.Text;
            _currentExam.Vorname = VornameField.Text;
            _currentExam.Datum = DatumField.SelectedDate?.UtcDateTime;

            _currentExam.Tabs = TabView.Items.OfType<TabItem>()
                .Select(tab =>
                {
                    var grid = tab.Content as Grid;
                    if (grid == null) return null;

                    var aufgabennummer = (grid.Children[0] as Grid)?.Children[0] as TextBox;
                    var ueberschrift = (grid.Children[0] as Grid)?.Children[1] as TextBox;
                    var beschreibung = grid.Children[1] as TextBox;

                    return new ExamTab
                    {
                        Aufgabennummer = aufgabennummer?.Text ?? "",
                        Überschrift = ueberschrift?.Text ?? "",
                        Inhalt = beschreibung?.Text ?? ""
                    };
                })
                .Where(tab => tab != null)
                .ToList();

            return System.Text.Json.JsonSerializer.Serialize(_currentExam, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }



        public void ExportToPdf(int margin, double lineSpacing, bool hasCorrectionLines, string textPosition){
            _ = GetCurrentExamDataAsJson();
            PFDExporter exp = new PFDExporter();
            exp.ExportToPdf(_currentExam, margin, lineSpacing, hasCorrectionLines, textPosition);
            //exp.ExportAllCombinations(_currentExam);
        }

    }

    


}
