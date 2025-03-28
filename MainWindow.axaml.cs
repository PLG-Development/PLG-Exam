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

        public MainWindow()
        {
            InitializeComponent();
            InitializeBackupTimer();
            //AddNewTab();

            this.KeyDown += OnKeyDown;
            GlobalFontSettings.FontResolver = new CustomFontResolver();

            RenewColors();
            this.Closing += MainWindow_Closing;

            if(DatumField.SelectedDate.HasValue == false){
                DatumField.SelectedDate = DateTime.Now;
            }
            
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
           ExportToPdf();
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



        private async void ExportToPdf()
        {
            try
            {

                _ = GetCurrentExamDataAsJson();

                if(_currentExam.Name.IsValueNullOrEmpty() || _currentExam.Vorname.IsValueNullOrEmpty() || _currentExam.Title.IsValueNullOrEmpty() || _currentExam.Datum == null){
                    await MessageBox.Show(this, "Bitte füllen Sie mindestens die Felder für Name, Vorname, Titel und Datum aus", "Fehler", MessageBoxButton.Ok);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    DefaultExtension = "pdf",
                    Filters = { new FileDialogFilter { Name = "PDF files", Extensions = { "pdf" } } }
                };
                var filePath = await saveDialog.ShowAsync(this);
                if (string.IsNullOrEmpty(filePath)) return;

                var font = new XFont("Cantarell", 14, XFontStyleEx.Bold);
                var fontB = new XFont("Cantarell", 14, XFontStyleEx.Bold);
                var fontsmall = new XFont("Cantarell", 10, XFontStyleEx.Regular);
                var descriptionFont = new XFont("Cantarell", 12, XFontStyleEx.Regular);

                using var document = new PdfDocument();

                document.Info.Title = _currentExam.Title;
                document.Info.Author = _currentExam.Vorname + " " + _currentExam.Name;
                document.Info.Subject = "PLG Exam Submission";
                document.Info.Keywords = "Exam, PLG, Report, PDF";
                SetPdfLanguage(document);


                var firstPage = document.AddPage();
                var gfx = XGraphics.FromPdfPage(firstPage);
                gfx.DrawString(_currentExam.Title, fontB, XBrushes.Black, new XRect(0, 40, firstPage.Width, 50), XStringFormats.TopCenter);

                gfx.DrawString($"Name: {_currentExam.Name}", fontB, XBrushes.Black, new XRect(50, 100, firstPage.Width, firstPage.Height), XStringFormats.TopLeft);
                gfx.DrawString($"Vorname: {_currentExam.Vorname}", fontB, XBrushes.Black, new XRect(50, 130, firstPage.Width, firstPage.Height), XStringFormats.TopLeft);
                gfx.DrawString($"Datum: {_currentExam.Datum?.ToShortDateString() ?? "N/A"}", fontB, XBrushes.Black, new XRect(50, 160, firstPage.Width, firstPage.Height), XStringFormats.TopLeft);

                foreach (var tab in _currentExam.Tabs)
                {
                    DrawTaskWithPageBreak(document, tab, descriptionFont, font, fontsmall);
                }


                var endpage = document.AddPage();
                var endpageGfx = XGraphics.FromPdfPage(endpage);

                // Beschreibung am unteren Rand platzieren
                var bottomDescriptionRect = new XRect(
                    50,                                  // X-Position (horizontaler Abstand)
                    endpage.Height - 100,                // Y-Position (Seitenhöhe - Textbereichshöhe - Margin)
                    endpage.Width - 100,                 // Breite des Bereichs
                    100                                  // Höhe des Bereichs
                );

                var endformatter = new XTextFormatter(endpageGfx)
                {
                    Alignment = XParagraphAlignment.Left
                };

                // Text zeichnen
                endformatter.DrawString(
                    "Erstellt mit PLG Exam - powered by PLG Development\nhttps://github.com/PLG-Development/PLG-Exam\n(c) 2025 - PLG Development",
                    fontsmall,
                    XBrushes.Black,
                    bottomDescriptionRect
                );        

                 // Beschreibung am unteren Rand platzieren
                var informationRect = new XRect(
                    50,                                  // X-Position (horizontaler Abstand)
                    50,                // Y-Position (Seitenhöhe - Textbereichshöhe - Margin)
                    endpage.Width - 100,                 // Breite des Bereichs
                    300                                  // Höhe des Bereichs
                );

                string outinfo = $"Dieses Dokument wurde automatisch erstellt und enthält alle Bestandteile Ihrer Prüfung. Bitte prüfen Sie alle Inhalte vor der Abgabe und speichern das Dokument an einem sicheren Ort. Nutzen Sie zur Abgabe bitte einen durch die Lehrkraft gestellten USB-Stick.";
                outinfo += $"\n\nErstelldatum: {DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.CreateSpecificCulture("de-DE"))}";
                outinfo += $"\nErstellzeit: {DateTime.Now.ToString("HH:mm:ss", CultureInfo.CreateSpecificCulture("de-DE"))} Uhr";
                outinfo += $"\nGerätename: {Environment.MachineName}";
                outinfo += $"\nBenutzername: {Environment.UserName}";
                outinfo += $"\nBetriebssystem: {Environment.OSVersion.VersionString}";
                outinfo += $"\nIP-Adresse: {System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)}";
                outinfo += $"\nInternetverbindung: {(isInternetAvailable() ? "8.8.8.8 ist erreichbar" : "8.8.8.8 ist nicht erreichbar")}";

                endformatter.DrawString(
                    outinfo,
                    fontsmall,
                    XBrushes.Black,
                    informationRect
                );

                var page = $"Seite {document.PageCount}";
                endpageGfx.DrawString(page, fontsmall, XBrushes.Gray, new XRect(50, gfx.PageSize.Height - 50 + 15, gfx.PageSize.Width - 50 * 2, 50), XStringFormats.TopRight);
    

                endpageGfx.Dispose();
                gfx.Dispose();
                //AddPageNumbers(document, fontsmall, 35);
                document.Save(filePath);
                await MessageBox.Show(this, "PDF erfolgreich gespeichert!", "Erfolg", MessageBoxButton.Ok);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim PDF-Export: {ex.Message}");
                await MessageBox.Show(this, "Fehler beim PDF-Export.", "Fehler", MessageBoxButton.Ok);
            }
        }

        private bool isInternetAvailable()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 2000); // 3000 ms timeout
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private List<(string Text, XFont Font, XBrush Brush)> ParseFormattedText(string text, XFont regularFont, XFont boldFont, XFont underlineFont)
        {
            var result = new List<(string Text, XFont Font, XBrush Brush)>();
            var currentFont = regularFont;
            var currentBrush = XBrushes.Black;

            var buffer = new System.Text.StringBuilder();
            bool isEscaped = false;
            bool isBold = false;
            bool isUnderline = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (isEscaped)
                {
                    buffer.Append(c);
                    isEscaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    isEscaped = true;
                    continue;
                }

                if (c == '*' && i + 1 < text.Length && text[i + 1] == '*')
                {
                    if (isBold)
                    {
                        result.Add((buffer.ToString(), currentFont, currentBrush));
                        buffer.Clear();
                        currentFont = isUnderline ? underlineFont : regularFont;
                        isBold = false;
                    }
                    else
                    {
                        result.Add((buffer.ToString(), currentFont, currentBrush));
                        buffer.Clear();
                        currentFont = boldFont;
                        isBold = true;
                    }
                    i++; // Skip the second '*'
                    continue;
                }

                if (c == '_')
                {
                    if (isUnderline)
                    {
                        result.Add((buffer.ToString(), currentFont, currentBrush));
                        buffer.Clear();
                        currentFont = isBold ? boldFont : regularFont;
                        isUnderline = false;
                    }
                    else
                    {
                        result.Add((buffer.ToString(), currentFont, currentBrush));
                        buffer.Clear();
                        currentFont = underlineFont;
                        isUnderline = true;
                    }
                    continue;
                }

                buffer.Append(c);
            }

            if (buffer.Length > 0)
            {
                result.Add((buffer.ToString(), currentFont, currentBrush));
            }

            return result;
        }

        private void DrawFormattedText(XGraphics gfx, string text, XFont regularFont, XFont boldFont, XFont underlineFont, XRect rect)
        {
            var parsedText = ParseFormattedText(text, regularFont, boldFont, underlineFont);
            double x = rect.X;
            double y = rect.Y;

            foreach (var (Text, Font, Brush) in parsedText)
            {
                var size = gfx.MeasureString(Text, Font);
                gfx.DrawString(Text, Font, Brush, new XRect(x, y, rect.Width, rect.Height), XStringFormats.TopLeft);
                x += size.Width;
            }
        }

        private void DrawTaskWithPageBreak(PdfDocument document, ExamTab tab, XFont font, XFont headerFont, XFont smallFont)
        {
            const double margin = 50; // Seitenränder
            const double corr_margin = 300; // Seitenrand Korrektur
            const double lineHeight = 21; // Höhe einer Textzeile
            const double headerHeight = 30; // Platz für die Kopfzeile pro Seite
            const double footerHeight = 20; // Platz für die Fußzeile
            const double usableHeight = 842 - margin * 2 - headerHeight - footerHeight; // Höhe des nutzbaren Bereichs

            var lines = SplitTextIntoLines(tab.Inhalt, document.Pages[0].Width - margin - corr_margin, font);

            double currentHeight = 0;
            PdfPage page = null;
            XGraphics gfx = null;

            foreach (var line in lines)
            {
                if (page == null || currentHeight + lineHeight > usableHeight)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    currentHeight = 0;

                    DrawName(gfx, tab, smallFont, margin, headerHeight, document.PageCount);
                    DrawHeader(gfx, tab, headerFont, margin, headerHeight);
                }

                var rect = new XRect(corr_margin, margin + headerHeight * headerline_count + currentHeight, page.Width - margin, lineHeight);
                DrawFormattedText(gfx, line, font, new XFont(font.Name, font.Size, XFontStyleEx.Bold), new XFont(font.Name, font.Size, XFontStyleEx.Underline), rect);
                currentHeight += lineHeight;
            }

            gfx?.Dispose();
        }

        int headerline_count = 0;
        private void DrawHeader(XGraphics gfx, ExamTab tab, XFont font, double margin, double headerHeight)
        {
            var maxWidth = gfx.PageSize.Width - margin * 2; // verfügbare Breite
            var lines = SplitTextIntoLines($"Aufgabe {tab.Aufgabennummer}: {tab.Überschrift}", maxWidth, font);

            double currentY = margin;
            headerline_count=0;
            foreach (var line in lines)
            {
                gfx.DrawString(line, font, XBrushes.Gray, new XRect(margin, currentY, maxWidth, headerHeight), XStringFormats.TopLeft);
                currentY += font.Height; 

                headerline_count++;
            }

            
        }

        private void AddPageNumbers(PdfDocument document, XFont font, double margin)
        {
            
            int totalPages = document.PageCount;
            for (int i = 1; i < totalPages; i++) // Beginnt ab der zweiten Seite (Index 1)
            {
                PdfPage page = document.Pages[i];
                XGraphics gfx = XGraphics.FromPdfPage(page);
                string text = $"Seite {i + 1} von {totalPages}";
                double yPosition = page.Height - margin;
                gfx.DrawString(text, font, XBrushes.Gray, new XRect(margin, yPosition, page.Width - 2 * margin, 20), XStringFormats.Center);
                gfx.Dispose();
                
            }
        }

        private void DrawName(XGraphics gfx, ExamTab tab, XFont font, double margin, double headerHeight, int page_num)
        {
            var headerText = $"{_currentExam.Name}, {_currentExam.Vorname}";
            gfx.DrawString(headerText, font, XBrushes.Gray, new XRect(margin, margin-15, gfx.PageSize.Width - margin * 2, headerHeight), XStringFormats.TopLeft);
    
            var headerText2 = _currentExam.Datum.Value.ToString("dd.MM.yyyy", CultureInfo.CreateSpecificCulture("de-DE"));
            gfx.DrawString(headerText2, font, XBrushes.Gray, new XRect(margin, margin-15, gfx.PageSize.Width - margin * 2, headerHeight), XStringFormats.TopRight);
            
            var page = $"Seite {page_num}";
            gfx.DrawString(page, font, XBrushes.Gray, new XRect(margin, gfx.PageSize.Height - margin + 15, gfx.PageSize.Width - margin * 2, headerHeight), XStringFormats.TopRight);
    
        }

        // Methode zum Aufteilen des Textes in Zeilen
        private List<string> SplitTextIntoLines(string text, double maxWidth, XFont font)
        {
            var lines = new List<string>();

            // Text anhand von \n aufteilen, einschließlich leerer Zeilen
            var manualLines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            using (var gfx = XGraphics.CreateMeasureContext(new XSize(maxWidth, 842), XGraphicsUnit.Point, XPageDirection.Downwards))
            {
                foreach (var manualLine in manualLines)
                {
                    // Falls die Zeile leer ist (z. B. durch mehrere \n), direkt hinzufügen
                    if (string.IsNullOrWhiteSpace(manualLine))
                    {
                        lines.Add(""); // Leere Zeile hinzufügen
                        continue;
                    }

                    // Falls eine Zeile zu lang ist, weiter aufteilen
                    var words = manualLine.Split(' ');
                    var currentLine = "";

                    foreach (var word in words)
                    {
                        var testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
                        var size = gfx.MeasureString(testLine, font);

                        if (size.Width > maxWidth)
                        {
                            lines.Add(currentLine);
                            currentLine = word;
                        }
                        else
                        {
                            currentLine = testLine;
                        }
                    }

                    // Letzte Zeile hinzufügen
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        lines.Add(currentLine);
                    }

                    
                }
                gfx.Dispose();
            }

            return lines;
        }

        

        private void SetPdfLanguage(PdfSharp.Pdf.PdfDocument document, string language = "de")
        {
            var catalog = document.Internals.Catalog;

            if (catalog.Elements.ContainsKey("/Lang"))
            {
                catalog.Elements["/Lang"] = new PdfSharp.Pdf.PdfString(language);
            }
            else
            {
                catalog.Elements.Add("/Lang", new PdfSharp.Pdf.PdfString(language));
            }
        }

    }

    


}
