using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Windows.Media.Animation;

namespace ClanHelper
{
    // Класс для сохранения настроек
    public class AppSettings
    {
        public int ThemeIndex { get; set; } = 0;
        public bool AnimationsEnabled { get; set; } = true;
        public Dictionary<string, string> SavedFolders { get; set; } = new Dictionary<string, string>();
    }

    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private List<TextBlock> particles;
        private Random random = new Random();
        private Storyboard pulseStoryboard;
        private bool isTitleAnimating = true;
        
        private Dictionary<string, string> savedFolders = new Dictionary<string, string>();
        private AppSettings appSettings = new AppSettings();
        private bool isInitializing = true; // Защита от багов при запуске

        private class AppTheme
        {
            public string Name { get; set; }
            public string Bg { get; set; }
            public string Title { get; set; }
            public string Card { get; set; }
            public string Accent { get; set; }
            public string Text { get; set; }
            public string ParticleSymbol { get; set; }
            public string ParticleColor { get; set; }
        }

        private List<AppTheme> themes;
        private int currentThemeIndex = 0;

        public MainWindow()
        {
            InitializeTextAnimation();
            InitializeComponent();
            InitializeThemes();
            InitializeParticles();
            LoadSettings();
            ApplyTheme(themes[currentThemeIndex]); 
            StartTitleAnimation();        
            isInitializing = false;
        }

        // --- СОХРАНЕНИЕ И ЗАГРУЗКА НАСТРОЕК ---
        private string GetConfigPath()
        {
            string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string appFolder = Path.Combine(docsPath, "CHelper");
            if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "config.json");
        }

        private void LoadSettings()
        {
            try
            {
                string path = GetConfigPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    appSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { appSettings = new AppSettings(); }

            currentThemeIndex = appSettings.ThemeIndex;
            ParticlesToggle.IsChecked = appSettings.AnimationsEnabled;
            savedFolders = appSettings.SavedFolders ?? new Dictionary<string, string>();
        }

        private void SaveSettings()
        {
            if (isInitializing) return; // Не сохраняем во время загрузки окна

            try
            {
                appSettings.ThemeIndex = currentThemeIndex;
                appSettings.AnimationsEnabled = ParticlesToggle.IsChecked ?? true;
                appSettings.SavedFolders = savedFolders;
                string json = JsonSerializer.Serialize(appSettings);
                File.WriteAllText(GetConfigPath(), json);
            }
            catch { }
        }

        // --- ТЕМЫ И ПЛАВНАЯ АНИМАЦИЯ ЦВЕТА ---
        private void InitializeThemes()
        {
            themes = new List<AppTheme>
            {
                new AppTheme { Name = "Default Dark Blue", Bg = "#0B132B", Title = "#162450", Card = "#1C2541", Accent = "#3A506B", Text = "#FFFFFF", ParticleSymbol = "❄", ParticleColor = "#E0FFFF" },
                new AppTheme { Name = "Orange", Bg = "#2B1A12", Title = "#462414", Card = "#3A2318", Accent = "#FD9B23", Text = "#FDF8EB", ParticleSymbol = "✦", ParticleColor = "#E28250" },
                new AppTheme { Name = "Midnight Purple", Bg = "#0b0614", Title = "#110822", Card = "#160b2e", Accent = "#a855f7", Text = "#ede9fe", ParticleSymbol = "★", ParticleColor = "#7c3aed" },
                new AppTheme { Name = "Ice Gray", Bg = "#0f1115", Title = "#21242b", Card = "#1c1f26", Accent = "#6b7280", Text = "#f9fafb", ParticleSymbol = "❄", ParticleColor = "#9ca3af" },
                new AppTheme { Name = "Red Alert", Bg = "#1a0404", Title = "#330404", Card = "#2a0808", Accent = "#ef4444", Text = "#fee2e2", ParticleSymbol = "⚠", ParticleColor = "#dc2626" },
                new AppTheme { Name = "Purple Neon", Bg = "#12001a", Title = "#230431", Card = "#1f002b", Accent = "#c77dff", Text = "#f3e8ff", ParticleSymbol = "✨", ParticleColor = "#9d4edd" },
                new AppTheme { Name = "Forest", Bg = "#0f1f17", Title = "#13271D", Card = "#172f25", Accent = "#22c55e", Text = "#ecfdf5", ParticleSymbol = "🍃", ParticleColor = "#4ade80" },
                new AppTheme { Name = "Solar Light", Bg = "#faf08c", Title = "#c1ae43", Card = "#f2d86f", Accent = "#ca8a04", Text = "#422006", ParticleSymbol = "☀", ParticleColor = "#eab308" }
            };
        }

        // --- ТЕМЫ И БЕЗОПАСНАЯ АНИМАЦИЯ ---
        private async void Theme_Click(object sender, RoutedEventArgs e)
        {
            currentThemeIndex++;
            if (currentThemeIndex >= themes.Count) currentThemeIndex = 0;

            // Проверяем, включены ли анимации в настройках
            if (ParticlesToggle.IsChecked == true)
            {
                // Анимация ВКЛЮЧЕНА: Плавно гасим прозрачность окна
                DoubleAnimation fadeOut = new DoubleAnimation(1.0, 0.4, TimeSpan.FromSeconds(0.15));
                this.BeginAnimation(Window.OpacityProperty, fadeOut);
                
                await Task.Delay(150); // Ждем пока окно потемнеет

                ApplyTheme(themes[currentThemeIndex]);
                SaveSettings();

                // Плавно возвращаем окну 100% яркость
                DoubleAnimation fadeIn = new DoubleAnimation(0.4, 1.0, TimeSpan.FromSeconds(0.15));
                this.BeginAnimation(Window.OpacityProperty, fadeIn);
            }
            else
            {
                // Анимация ВЫКЛЮЧЕНА: Мгновенная смена темы без эффектов
                ApplyTheme(themes[currentThemeIndex]);
                SaveSettings();
            }
        }

        private void ApplyTheme(AppTheme theme)
        {
            // Безопасное присваивание цветов (никаких конфликтов с WPF)
            this.Resources["BgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Bg));
            this.Resources["Titel"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Title));
            this.Resources["CardBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Card));
            this.Resources["AccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Accent));
            this.Resources["TextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Text));

            SolidColorBrush particleBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.ParticleColor));
            foreach (var particle in particles)
            {
                particle.Text = theme.ParticleSymbol;
                particle.Foreground = particleBrush;
            }
        }

        // --- АНИМАЦИИ И ЧАСТИЦЫ ---
        private void InitializeParticles()
        {
            particles = new List<TextBlock>();
            int particleCount = 40;

            for (int i = 0; i < particleCount; i++)
            {
                TextBlock particle = new TextBlock
                {
                    Text = "❄",
                    FontSize = random.Next(10, 24),
                    Opacity = random.NextDouble() * 0.7 + 0.3
                };

                Canvas.SetLeft(particle, random.Next(0, 650)); 
                Canvas.SetTop(particle, random.Next(-400, 450));

                ParticleCanvas.Children.Add(particle);
                particles.Add(particle);
            }

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(30);
            timer.Tick += Timer_Tick;
        }

        private void InitializeTextAnimation()
        {
            DoubleAnimation pulseAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.3,
                Duration = TimeSpan.FromSeconds(1.2),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            pulseStoryboard = new Storyboard();
            pulseStoryboard.Children.Add(pulseAnimation);
            Storyboard.SetTarget(pulseAnimation, SettingsTitle);
            Storyboard.SetTargetProperty(pulseAnimation, new PropertyPath(UIElement.OpacityProperty));
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var particle in particles)
            {
                double currentTop = Canvas.GetTop(particle);
                double speed = particle.FontSize / 8.0; 
                currentTop += speed;

                if (currentTop > this.ActualHeight)
                {
                    currentTop = -30;
                    Canvas.SetLeft(particle, random.Next(0, 650));
                }

                Canvas.SetTop(particle, currentTop);
            }
        }

        private void Particles_Checked(object sender, RoutedEventArgs e) 
        {
            timer?.Start();
            isTitleAnimating = true;
            pulseStoryboard?.Begin(SettingsTitle, true); 
            SaveSettings();
        }

        private void Particles_Unchecked(object sender, RoutedEventArgs e) 
        {
            timer?.Stop();
            isTitleAnimating = false;
            pulseStoryboard?.Stop(SettingsTitle); 
            if (SettingsTitle != null) SettingsTitle.Opacity = 1.0;
            SaveSettings();
        }

        // --- УМНОЕ ЗАГРУЖЕНИЕ, ПАПКИ И ЗАПУСК ---
        private async Task DownloadFileAsync(string directUrl, string saveAsFileName)
        {
            // УМНАЯ ПРОВЕРКА: Если файл уже скачан
            if (savedFolders.ContainsKey(saveAsFileName))
            {
                string existingPath = Path.Combine(savedFolders[saveAsFileName], saveAsFileName);
                if (File.Exists(existingPath))
                {
                    var result = MessageBox.Show(
                        $"Файл {saveAsFileName} уже существует в папке:\n{savedFolders[saveAsFileName]}\n\nСкачать его заново? (Нажмите «Нет», чтобы просто запустить)", 
                        "Файл уже скачан", 
                        MessageBoxButton.YesNoCancel, 
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Cancel) return;
                    if (result == MessageBoxResult.No)
                    {
                        LaunchSavedFile(saveAsFileName);
                        return;
                    }
                }
            }

            var dialog = new OpenFolderDialog { Title = $"Выберите папку для сохранения {saveAsFileName}" };
            
            if (dialog.ShowDialog() == true)
            {
                string folderPath = dialog.FolderName;
                string fullFilePath = Path.Combine(folderPath, saveAsFileName);

                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    ProgressPanel.Visibility = Visibility.Visible;
                    ProgressText.Text = $"Загрузка {saveAsFileName}... 0%";
                    DownloadProgressBar.Value = 0;

                    using (HttpClient client = new HttpClient())
                    using (var response = await client.GetAsync(directUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        var totalBytes = response.Content.Headers.ContentLength;

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var buffer = new byte[8192];
                            var isMoreToRead = true;
                            long totalRead = 0;

                            do
                            {
                                var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                                if (read == 0) isMoreToRead = false;
                                else
                                {
                                    await fileStream.WriteAsync(buffer, 0, read);
                                    totalRead += read;
                                    if (totalBytes.HasValue)
                                    {
                                        double percentage = (double)totalRead / totalBytes.Value * 100;
                                        DownloadProgressBar.Value = percentage;
                                        ProgressText.Text = $"Загрузка {saveAsFileName}... {Math.Round(percentage)}%";
                                    }
                                }
                            }
                            while (isMoreToRead);
                        }
                    }
                    
                    savedFolders[saveAsFileName] = folderPath;
                    SaveSettings(); // Сохраняем путь в конфиг
                    
                    await Task.Delay(300); 
                    MessageBox.Show($"Файл {saveAsFileName} успешно загружен!", "Готово!", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Невозможно загрузить файл. Проверьте ссылку.\nОшибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                    ProgressPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void OpenSavedFolder(string fileName)
        {
            if (savedFolders.ContainsKey(fileName))
            {
                string fullFilePath = Path.Combine(savedFolders[fileName], fileName);
                if (File.Exists(fullFilePath))
                    Process.Start("explorer.exe", $"/select,\"{fullFilePath}\"");
                else
                    MessageBox.Show("Файл не найден! Возможно, он был удален.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else MessageBox.Show("Вы еще не загружали этот файл!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void LaunchSavedFile(string fileName)
        {
            if (savedFolders.ContainsKey(fileName))
            {
                string fullFilePath = Path.Combine(savedFolders[fileName], fileName);
                if (File.Exists(fullFilePath))
                {
                    try { Process.Start(new ProcessStartInfo { FileName = fullFilePath, UseShellExecute = true }); }
                    catch (Exception ex) { MessageBox.Show("Не удалось запустить файл: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
                else MessageBox.Show("Файл не найден! Возможно, он был удален.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else MessageBox.Show("Сначала нужно загрузить этот файл!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void OpenWebSite(string url)
        {
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
        }

        // --- КНОПКИ САЙТОВ ---
        private void Everything_Click(object sender, RoutedEventArgs e) => OpenWebSite("https://www.voidtools.com/");
        private void SystemInformer_Click(object sender, RoutedEventArgs e) => OpenWebSite("https://systeminformer.sourceforge.io/");
        private void EverythingCheker_Click(object sender, RoutedEventArgs e) => OpenWebSite("https://github.com/SSDDAA-AFK/Everything_cheker/releases/latest");
        private void SystemInformerCheker_Click(object sender, RoutedEventArgs e) => OpenWebSite("https://github.com/SSDDAA-AFK/SustemInformer_Cheker/releases/latest");

        // --- КНОПКИ ЗАГРУЗОК ---
        private async void EverythingDL_Click(object sender, RoutedEventArgs e) => await DownloadFileAsync("https://www.voidtools.com/Everything-1.4.1.1032.x64-Setup.exe", "Everything.exe");
        private async void SystemInformerDL_Click(object sender, RoutedEventArgs e) => await DownloadFileAsync("https://deac-riga.dl.sourceforge.net/project/systeminformer/systeminformer-3.2.25011-release-setup.exe?viasf=1", "SystemInformer.exe");
        private async void EverythingChekerDL_Click(object sender, RoutedEventArgs e) => await DownloadFileAsync("https://github.com/SSDDAA-AFK/Everything_cheker/releases/download/v1.1/Everything_cheker.exe", "EverythingCheker.exe");
        private async void SystemInformerChekerDL_Click(object sender, RoutedEventArgs e) => await DownloadFileAsync("https://github.com/SSDDAA-AFK/SustemInformer_Cheker/releases/download/v1.1/SystemInformerChecker.exe", "SystemInformerChecker.exe");

        // --- КНОПКИ ПАПОК ---
        private void EverythingFolder_Click(object sender, RoutedEventArgs e) => OpenSavedFolder("Everything.exe");
        private void SystemInformerFolder_Click(object sender, RoutedEventArgs e) => OpenSavedFolder("SystemInformer.exe");
        private void EverythingChekerFolder_Click(object sender, RoutedEventArgs e) => OpenSavedFolder("EverythingCheker.exe");
        private void SystemInformerChekerFolder_Click(object sender, RoutedEventArgs e) => OpenSavedFolder("SystemInformerChecker.exe");

        // --- КНОПКИ ЗАПУСКА ---
        private void EverythingLaunch_Click(object sender, RoutedEventArgs e) => LaunchSavedFile("Everything.exe");
        private void SystemInformerLaunch_Click(object sender, RoutedEventArgs e) => LaunchSavedFile("SystemInformer.exe");
        private void EverythingChekerLaunch_Click(object sender, RoutedEventArgs e) => LaunchSavedFile("EverythingCheker.exe");
        private void SystemInformerChekerLaunch_Click(object sender, RoutedEventArgs e) => LaunchSavedFile("SystemInformerChecker.exe");

        // --- ИНТЕРФЕЙС И КАСТОМНЫЙ ЗАГОЛОВОК ---
        private void Settings_Click(object sender, RoutedEventArgs e) 
        {
            DimOverlay.Visibility = Visibility.Visible;
            SettingsPanel.Visibility = Visibility.Visible;
        }
        private void CloseSettings_Click(object sender, RoutedEventArgs e) 
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
            DimOverlay.Visibility = Visibility.Collapsed;
        }
        private void DimOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
            DimOverlay.Visibility = Visibility.Collapsed;
        }

        private void header_MouseEnter(object sender, MouseEventArgs e)
        {
            Border border = sender as Border;
            if (border.Name == "close") border.Background = Brushes.Red;
        }
        private void header_MouseLeave(object sender, MouseEventArgs e)
        {
            Border border = sender as Border;
            border.SetResourceReference(Border.BackgroundProperty, "Titel");
        }
        private void header_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Border border = sender as Border;
                if (border.Name == "close") border.Background = Brushes.LightPink;
            }
        }
        private void header_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Border border = sender as Border;
                if (border.Name == "close") this.Close();
            }
        }

        private async void StartTitleAnimation()
        {
            string fullText = "Clan Helper";
            
            while (true)
            {
                if (!isTitleAnimating)
                {
                    TitleText.Text = fullText;
                    await Task.Delay(1000);
                    continue;
                }

                TitleText.Text = "";
                
                for (int i = 0; i <= fullText.Length; i++)
                {
                    if (!isTitleAnimating) break;
                    TitleText.Text = fullText.Substring(0, i);
                    await Task.Delay(150); 
                }

                if (!isTitleAnimating) continue;
                await Task.Delay(4000);

                for (int i = fullText.Length; i >= 0; i--)
                {
                    if (!isTitleAnimating) break;
                    TitleText.Text = fullText.Substring(0, i);
                    await Task.Delay(80); 
                }

                if (!isTitleAnimating) continue;
                await Task.Delay(500);
            }
        }

        private void Header_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}