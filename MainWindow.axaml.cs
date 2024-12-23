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
            AddNewTab();

            this.KeyDown += OnKeyDown;
        }

        // Event für "Neuen Tab hinzufügen"
        private void OnAddTabClick(object? sender, RoutedEventArgs e)
        {
            AddNewTab();
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
                Background = new SolidColorBrush(Color.FromRgb(123,35,39)),
                Foreground = new SolidColorBrush(Color.FromRgb(0,0,0)),
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
                }
            };

            TabView.Items.Add(tabItem);
            TabView.SelectedItem = tabItem;
            _isSaved = false;
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
            VornameField.Text = exam.Vorname;
            DatumField.SelectedDate = exam.Datum;

            TabView.Items.Clear();
            _tabCounter = 0;

            foreach (var tab in exam.Tabs)
            {
                _tabCounter++;
                var tabItem = new TabItem
                {
                    Header = $"{tab.Aufgabennummer} - {tab.Überschrift}",
                    Content = CreateTabContent(tab.Aufgabennummer, tab.Überschrift, tab.Inhalt)
                };
                TabView.Items.Add(tabItem);
            }

            _currentFilePath = filePath;
            _isSaved = true;
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
            };

            // Event für dynamische Tab-Umbenennung
            aufgabenNummerTextBox.KeyUp += (s, e) => UpdateTabHeader(TabView.SelectedItem as TabItem, aufgabenNummerTextBox, ueberschriftTextBox);
            ueberschriftTextBox.KeyUp += (s, e) => UpdateTabHeader(TabView.SelectedItem as TabItem, aufgabenNummerTextBox, ueberschriftTextBox);

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

            tab.Header = string.IsNullOrWhiteSpace(ueberschriftText)
                ? aufgabeText
                : $"{aufgabeText} - {ueberschriftText}";
        }


        //
        // Backups
        //

        private const int BackupInterval = 20000; // 20 Sekunden in Millisekunden
        

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

        private const int MaxBackupFiles = 5; // Maximal 5 Backups
        private readonly string BackupFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "PLG_Exam_Backup");

        

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

                // Speicher-Dialog
                var saveDialog = new SaveFileDialog
                {
                    DefaultExtension = "pdf",
                    Filters = { new FileDialogFilter { Name = "PDF files", Extensions = { "pdf" } } }
                };
                var filePath = await saveDialog.ShowAsync(this);
                if (string.IsNullOrEmpty(filePath)) return;

                // PDF-Dokument erstellen
                using var document = new PdfDocument();

                // Erste Seite: Name, Vorname, Datum
                var firstPage = document.AddPage();
                var gfx = XGraphics.FromPdfPage(firstPage);
                var font = new XFont("Arial", 14, XFontStyleEx.Bold);
                gfx.DrawString("PLG Exam", font, XBrushes.Black, new XRect(0, 0, firstPage.Width, 50), XStringFormats.TopCenter);

                gfx.DrawString($"Name: {_currentExam.Name}", font, XBrushes.Black, new XRect(50, 100, firstPage.Width, firstPage.Height), XStringFormats.TopLeft);
                gfx.DrawString($"Vorname: {_currentExam.Vorname}", font, XBrushes.Black, new XRect(50, 130, firstPage.Width, firstPage.Height), XStringFormats.TopLeft);
                gfx.DrawString($"Datum: {_currentExam.Datum?.ToShortDateString() ?? "N/A"}", font, XBrushes.Black, new XRect(50, 160, firstPage.Width, firstPage.Height), XStringFormats.TopLeft);

                // Weitere Seiten: Aufgaben
                foreach (var tab in _currentExam.Tabs)
                {
                    var page = document.AddPage();
                    var pageGfx = XGraphics.FromPdfPage(page);

                    pageGfx.DrawString($"Aufgabe {tab.Aufgabennummer}", font, XBrushes.Black, new XRect(50, 50, page.Width, page.Height), XStringFormats.TopLeft);
                    pageGfx.DrawString($"Überschrift: {tab.Überschrift}", font, XBrushes.Black, new XRect(50, 100, page.Width, page.Height), XStringFormats.TopLeft);

                    var descriptionFont = new XFont("Arial", 12, XFontStyleEx.Regular);
                    var descriptionRect = new XRect(50, 150, page.Width - 100, page.Height - 200);
                    pageGfx.DrawString(tab.Inhalt, descriptionFont, XBrushes.Black, descriptionRect, XStringFormats.TopLeft);
                }

                // PDF speichern
                document.Save(filePath);
                await MessageBox.Show(this, "PDF erfolgreich gespeichert!", "Erfolg", MessageBoxButton.Ok);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim PDF-Export: {ex.Message}");
                await MessageBox.Show(this, "Fehler beim PDF-Export.", "Fehler", MessageBoxButton.Ok);
            }
        }


    }
}
