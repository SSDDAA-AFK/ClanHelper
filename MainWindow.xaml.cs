using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using System.ComponentModel;
using QRCoder;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using Color = System.Windows.Media.Color;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;

namespace ClanHelper
{
    // Класс для сохранения настроек
    public class AppSettings
    {
        public int ThemeIndex { get; set; } = 0;
        public bool AnimationsEnabled { get; set; } = true;
        public bool StartupSoundEnabled { get; set; } = true; // Нове налаштування звуку
        public Dictionary<string, string> SavedFolders { get; set; } = new Dictionary<string, string>();
    }

    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        public static extern bool BlockInput(bool fBlockIt);

        private DispatcherTimer? timer;
        private List<TextBlock> particles = new List<TextBlock>();
        private Random random = new Random();
        private Storyboard? pulseStoryboard;
        private bool isTitleAnimating = true;
        
        private Dictionary<string, string> savedFolders = new Dictionary<string, string>();
        private AppSettings appSettings = new AppSettings();
        private bool isInitializing = true; // Защита от багов при запуске

        private MediaPlayer? startupSoundPlayer; // Плеєр для стартового звуку

        private class AppTheme
        {
            public string Name { get; set; } = "";
            public string Bg { get; set; } = "";
            public string Title { get; set; } = "";
            public string Card { get; set; } = "";
            public string Accent { get; set; } = "";
            public string Text { get; set; } = "";
            public string ParticleSymbol { get; set; } = "";
            public string ParticleColor { get; set; } = "";
        }

        private List<AppTheme> themes = new List<AppTheme>();
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
            
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (appSettings.StartupSoundEnabled)
            {
                await PlayRandomStartupSoundAsync();
            }
        }

        private async Task PlayRandomStartupSoundAsync()
        {
            // Список посилань на звуки для запуску (заміни на свої)
            string[] soundUrls = new string[]
            {
                "https://github.com/SSDDAA-AFK/ClanHelper/releases/download/V1.1/startup.mp3",
                "https://github.com/SSDDAA-AFK/ClanHelper/releases/download/V1.1/startup.mp3",
                "https://github.com/SSDDAA-AFK/ClanHelper/releases/download/V1.1/startup2.mp3",
                "https://github.com/SSDDAA-AFK/ClanHelper/releases/download/V1.1/startup3.mp3",
                "https://github.com/SSDDAA-AFK/ClanHelper/releases/download/V1.1/startup4.mp3"
            };

            Random rnd = new Random();
            int selectedIndex = rnd.Next(soundUrls.Length);
            string soundUrl = soundUrls[selectedIndex];

            string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string soundsFolder = Path.Combine(docsPath, "CHelper", "sounds");
            if (!Directory.Exists(soundsFolder)) Directory.CreateDirectory(soundsFolder);

            string soundFileName = $"startup_sound_{selectedIndex}.mp3";
            string soundPath = Path.Combine(soundsFolder, soundFileName);

            // Видаляємо пошкоджений файл, якщо він надто малий
            if (File.Exists(soundPath))
            {
                try { if (new FileInfo(soundPath).Length < 1000) File.Delete(soundPath); } catch { }
            }

            if (!File.Exists(soundPath))
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                        var response = await client.GetAsync(soundUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var bytes = await response.Content.ReadAsByteArrayAsync();
                            await File.WriteAllBytesAsync(soundPath, bytes);
                        }
                        else return; // Якщо не вийшло скачати, просто виходимо
                    }
                }
                catch { return; } // Ігноруємо помилки мережі
            }

            // Відтворюємо звук
            try
            {
                startupSoundPlayer = new MediaPlayer();
                startupSoundPlayer.Open(new Uri(soundPath));
                startupSoundPlayer.Volume = 0.5; // Налаштуй гучність (0.0 - 1.0)
                
                // Знаходимо іконку за допомогою FindName, якщо вона є в XAML
                // (Нам треба додати x:Name="AppIcon" у XAML для іконки в хедері)
                var appIcon = this.FindName("AppIcon") as System.Windows.Controls.Image;
                Storyboard? pulseIcon = null;

                if (appIcon != null)
                {
                    appIcon.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                    ScaleTransform scaleTransform = new ScaleTransform(1, 1);
                    appIcon.RenderTransform = scaleTransform;

                    DoubleAnimation scaleXAnim = new DoubleAnimation(1.0, 1.2, TimeSpan.FromSeconds(0.5)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
                    DoubleAnimation scaleYAnim = new DoubleAnimation(1.0, 1.2, TimeSpan.FromSeconds(0.5)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };

                    pulseIcon = new Storyboard();
                    pulseIcon.Children.Add(scaleXAnim);
                    pulseIcon.Children.Add(scaleYAnim);
                    Storyboard.SetTarget(scaleXAnim, appIcon);
                    Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("RenderTransform.ScaleX"));
                    Storyboard.SetTarget(scaleYAnim, appIcon);
                    Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("RenderTransform.ScaleY"));

                    pulseIcon.Begin();
                }

                startupSoundPlayer.MediaEnded += (s, ev) => 
                {
                    pulseIcon?.Stop();
                    if (appIcon != null) appIcon.RenderTransform = new ScaleTransform(1, 1);
                };

                startupSoundPlayer.Play();
            }
            catch { }
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
            if (SoundToggle != null) SoundToggle.IsChecked = appSettings.StartupSoundEnabled;
            savedFolders = appSettings.SavedFolders ?? new Dictionary<string, string>();
        }

        private void SaveSettings()
        {
            if (isInitializing) return; // Не сохраняем во время загрузки окна

            try
            {
                appSettings.ThemeIndex = currentThemeIndex;
                appSettings.AnimationsEnabled = ParticlesToggle.IsChecked ?? true;
                if (SoundToggle != null) appSettings.StartupSoundEnabled = SoundToggle.IsChecked ?? true;
                appSettings.SavedFolders = savedFolders;
                string json = JsonSerializer.Serialize(appSettings);
                File.WriteAllText(GetConfigPath(), json);
            }
            catch { }
        }

        private void Sound_Checked(object sender, RoutedEventArgs e) => SaveSettings();
        private void Sound_Unchecked(object sender, RoutedEventArgs e) => SaveSettings();

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
            this.Resources["BgBrush"] = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.Bg));
            this.Resources["Titel"] = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.Title));
            this.Resources["CardBrush"] = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.Card));
            this.Resources["AccentBrush"] = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.Accent));
            this.Resources["TextBrush"] = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.Text));

            SolidColorBrush particleBrush = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.ParticleColor));
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
                    Opacity = random.NextDouble() * 0.7 + 0.3,
                    RenderTransform = new TranslateTransform() // Використовуємо трансформацію для оптимізації
                };

                Canvas.SetLeft(particle, random.Next(0, 650)); 
                Canvas.SetTop(particle, random.Next(-400, 450));

                ParticleCanvas.Children.Add(particle);
                particles.Add(particle);
            }

            // Використовуємо Rendering замість DispatcherTimer для плавнішої анімації
            CompositionTarget.Rendering += CompositionTarget_Rendering;
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

        private TimeSpan lastRenderTime = TimeSpan.Zero;

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            // Якщо анімації вимкнено в налаштуваннях - не рухаємо частинки
            if (ParticlesToggle.IsChecked != true) 
            {
                lastRenderTime = TimeSpan.Zero; // Скидаємо час при вимкненні
                return;
            }

            RenderingEventArgs renderArgs = (RenderingEventArgs)e;
            TimeSpan currentRenderTime = renderArgs.RenderingTime;

            // При першому кадрі або після паузи просто запам'ятовуємо час
            if (lastRenderTime == TimeSpan.Zero)
            {
                lastRenderTime = currentRenderTime;
                return;
            }

            double deltaTime = (currentRenderTime - lastRenderTime).TotalSeconds;
            lastRenderTime = currentRenderTime;

            // Захист від величезних стрибків (якщо вікно підвисло через завантаження чи наведення)
            // Обмежуємо "пропущену" паузу до 1/60 секунди
            if (deltaTime > 0.1) deltaTime = 0.016; 

            // Оскільки раніше швидкість була Particle.FontSize / 8.0 за кадр при 60 FPS
            // Тепер множимо це на 60 і на deltaTime, щоб швидкість була постійною за секунду
            double targetFps = 60.0; 

            foreach (var particle in particles)
            {
                var transform = (TranslateTransform)particle.RenderTransform;
                double speed = (particle.FontSize / 8.0) * targetFps * deltaTime; 
                transform.Y += speed;

                double absoluteTop = Canvas.GetTop(particle) + transform.Y;

                if (absoluteTop > this.ActualHeight)
                {
                    transform.Y = -30 - Canvas.GetTop(particle);
                    Canvas.SetLeft(particle, random.Next(0, 650));
                }
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Цей метод залишаємо порожнім для сумісності з іншим кодом (або видаляємо, якщо ніде не викликається)
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

        private bool isMiniMode = false;

        private void miniModeBtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                isMiniMode = !isMiniMode;
                if (isMiniMode)
                {
                    this.Width = 180;
                    this.Height = 180;
                    MainContentPanel.Visibility = Visibility.Collapsed;
                    MiniContentPanel.Visibility = Visibility.Visible;
                    TitleText.Visibility = Visibility.Collapsed;
                    EasterEggBtn.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.Width = 650;
                    this.Height = 400; // Повертаємо початкову висоту
                    MainContentPanel.Visibility = Visibility.Visible;
                    MiniContentPanel.Visibility = Visibility.Collapsed;
                    TitleText.Visibility = Visibility.Visible;
                    EasterEggBtn.Visibility = Visibility.Visible;
                }
            }
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
                    SpeedText.Text = "Подключение...";
                    DownloadProgressBar.Value = 0;

                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = Timeout.InfiniteTimeSpan;
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
                                
                                DateTime lastTime = DateTime.Now;
                                long lastRead = 0;

                                do
                                {
                                    var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                                    if (read == 0) isMoreToRead = false;
                                    else
                                    {
                                        await fileStream.WriteAsync(buffer, 0, read);
                                        totalRead += read;
                                        
                                        DateTime now = DateTime.Now;
                                        TimeSpan elapsed = now - lastTime;
                                        
                                        // Оновлюємо UI кожні півсекунди для плавності
                                        if (elapsed.TotalMilliseconds > 500)
                                        {
                                            double bytesPerSec = (totalRead - lastRead) / elapsed.TotalSeconds;
                                            double mbPerSec = bytesPerSec / 1048576.0;
                                            
                                            string etaText = "Неизвестно";
                                            if (totalBytes.HasValue && bytesPerSec > 0)
                                            {
                                                double secondsRemaining = (totalBytes.Value - totalRead) / bytesPerSec;
                                                etaText = TimeSpan.FromSeconds(secondsRemaining).ToString(@"mm\:ss");
                                            }

                                            SpeedText.Text = $"{mbPerSec:F1} МБ/с - Осталось: {etaText}";
                                            
                                            if (totalBytes.HasValue)
                                            {
                                                double percentage = (double)totalRead / totalBytes.Value * 100;
                                                DownloadProgressBar.Value = percentage;
                                                ProgressText.Text = $"Загрузка {saveAsFileName}... {Math.Round(percentage)}%";
                                            }

                                            lastTime = now;
                                            lastRead = totalRead;
                                        }
                                    }
                                }
                                while (isMoreToRead);
                            }
                        }
                    }
                    
                    savedFolders[saveAsFileName] = folderPath;
                    SaveSettings();
                    
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

        private void LaunchSavedFile(string fileName, Button launchButton = null)
        {
            if (savedFolders.ContainsKey(fileName))
            {
                string fullFilePath = Path.Combine(savedFolders[fileName], fileName);
                if (File.Exists(fullFilePath))
                {
                    try 
                    { 
                        if (launchButton != null && launchButton.Content.ToString().Contains("Запустить"))
                            launchButton.Content = "▶ Запуск";

                        Process.Start(new ProcessStartInfo { FileName = fullFilePath, UseShellExecute = true }); 
                    }
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
        private void ShellbagAnalyzer_Click(object sender, RoutedEventArgs e) => OpenWebSite("https://privazer.com/en/download-shellbag-analyzer-shellbag-cleaner.php");

        // --- КНОПКИ ЗАГРУЗОК ---
        private async void EverythingDL_Click(object sender, RoutedEventArgs e) => await DownloadFileAsync("https://www.voidtools.com/Everything-1.4.1.1032.x64-Setup.exe", "Everything.exe");
        private async void SystemInformerDL_Click(object sender, RoutedEventArgs e) => await DownloadFileAsync("https://deac-riga.dl.sourceforge.net/project/systeminformer/systeminformer-3.2.25011-release-setup.exe?viasf=1", "SystemInformer.exe");
        private async void EverythingChekerDL_Click(object sender, RoutedEventArgs e) => await DownloadFileAsync("https://github.com/SSDDAA-AFK/Everything_cheker/releases/download/v2.0/Everything_cheker.exe", "EverythingCheker.exe");
        private async void ShellbagAnalyzerDL_Click(object sender, RoutedEventArgs e) => await DownloadFileAsync("https://privazer.com/en/shellbag_analyzer_cleaner.exe", "ShellbagAnalyzer.exe");

        // --- КНОПКИ ПАПОК ---
        private void EverythingFolder_Click(object sender, RoutedEventArgs e) => OpenSavedFolder("Everything.exe");
        private void SystemInformerFolder_Click(object sender, RoutedEventArgs e) => OpenSavedFolder("SystemInformer.exe");
        private void EverythingChekerFolder_Click(object sender, RoutedEventArgs e) => OpenSavedFolder("EverythingCheker.exe");
        private void ShellbagAnalyzerFolder_Click(object sender, RoutedEventArgs e) => OpenSavedFolder("ShellbagAnalyzer.exe");

        // --- КНОПКИ ЗАПУСКА ---
        private void EverythingLaunch_Click(object sender, RoutedEventArgs e) => LaunchSavedFile("Everything.exe", sender as Button);
        private void SystemInformerLaunch_Click(object sender, RoutedEventArgs e) => LaunchSavedFile("SystemInformer.exe", sender as Button);
        private void EverythingChekerLaunch_Click(object sender, RoutedEventArgs e) => LaunchSavedFile("EverythingCheker.exe", sender as Button);
        private void ShellbagAnalyzerLaunch_Click(object sender, RoutedEventArgs e) => LaunchSavedFile("ShellbagAnalyzer.exe", sender as Button);

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

        private async void EasterEgg_Click(object sender, RoutedEventArgs e)
        {
            // Список посилань на відео
            string[] videoUrls = new string[]
            {
                "https://github.com/SSDDAA-AFK/ClanHelper/releases/download/V1.1/0225.mp4",
                "https://github.com/SSDDAA-AFK/ClanHelper/releases/download/V1.1/0225.1.mp4", // ЗАМІНІТЬ ЦІ ПОСИЛАННЯ НА СВОЇ
                "https://github.com/SSDDAA-AFK/ClanHelper/releases/download/V1.1/0225.2.mp4",
                "https://github.com/SSDDAA-AFK/ClanHelper/releases/download/V1.1/0225.3.mp4",
                "https://github.com/SSDDAA-AFK/ClanHelper/releases/download/V1.1/0225.4.mp4",
                "https://github.com/SSDDAA-AFK/ClanHelper/releases/download/V1.1/0225.5.mp4",
                "https://github.com/SSDDAA-AFK/ClanHelper/releases/download/V1.1/0225.6.mp4",
                "https://github.com/SSDDAA-AFK/ClanHelper/releases/download/V1.1/0225.7.mp4",
                "https://github.com/SSDDAA-AFK/ClanHelper/releases/download/V1.1/0225.8.mp4"
            };

            // Вибираємо випадкове відео
            Random rnd = new Random();
            int selectedIndex = rnd.Next(videoUrls.Length);
            string videoUrl = videoUrls[selectedIndex];

            string tempFolder = Path.GetTempPath();
            // Даємо файлу унікальне ім'я для кожного відео, щоб вони не перезаписували одне одного
            string videoPath = Path.Combine(tempFolder, $"fnaf_video_{selectedIndex}.mp4");

            // Видаляємо пошкоджений файл, якщо він надто малий
            if (File.Exists(videoPath))
            {
                try 
                { 
                    if (new FileInfo(videoPath).Length < 10000) 
                        File.Delete(videoPath); 
                } 
                catch { }
            }

            // Завантажуємо відео лише якщо його ще немає
            if (!File.Exists(videoPath))
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                        
                        using (var response = await client.GetAsync(videoUrl, HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();
                            using (var contentStream = await response.Content.ReadAsStreamAsync())
                            using (var fileStream = new FileStream(videoPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                            {
                                var buffer = new byte[8192];
                                var isMoreToRead = true;
                                do
                                {
                                    var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                                    if (read == 0) isMoreToRead = false;
                                    else await fileStream.WriteAsync(buffer, 0, read);
                                }
                                while (isMoreToRead);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    Mouse.OverrideCursor = null;
                    if (File.Exists(videoPath)) { try { File.Delete(videoPath); } catch { } } 
                    return;
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }

            // Рандомна затримка від 1 до 10 секунд (Ідея 2)
            await Task.Delay(rnd.Next(1000, 10000));

            // Запам'ятовуємо старий рівень гучності та стан Mute, ставимо на максимум
            float oldVolume = -1;
            bool oldMute = false;
            MMDevice defaultDevice = null;
            try
            {
                var enumerator = new MMDeviceEnumerator();
                defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                oldVolume = defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
                oldMute = defaultDevice.AudioEndpointVolume.Mute;
                
                defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = 1.0f; // 100% гучність
                defaultDevice.AudioEndpointVolume.Mute = false; // Знімаємо Mute
            }
            catch (Exception)
            {
                // Ігноруємо помилки, якщо аудіопристрій недоступний
            }

            bool isFinished = false;

            // Створення повноекранного вікна для відтворення
            Window videoWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                WindowState = WindowState.Maximized,
                Topmost = true,
                Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078D7")), // Синій фон BSoD
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
                Cursor = Cursors.None, // Ховаємо курсор
            };

            // Створюємо інтерфейс BSoD (Ідея 3)
            Grid bsodGrid = new Grid();
            
            // Основний контейнер з відступами як в реальному BSoD (відступ зліва близько 10-15%)
            StackPanel bsodContent = new StackPanel
            {
                Margin = new Thickness(SystemParameters.PrimaryScreenWidth * 0.12, SystemParameters.PrimaryScreenHeight * 0.15, SystemParameters.PrimaryScreenWidth * 0.12, 0),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // Величезний сумний смайлик
            bsodContent.Children.Add(new TextBlock
            {
                Text = ":(",
                Foreground = Brushes.White,
                FontSize = 130, // Специфічно великий розмір
                FontFamily = new FontFamily("Segoe UI"),
                Margin = new Thickness(0, 0, 0, 20)
            });

            // Основний текст проблеми
            bsodContent.Children.Add(new TextBlock
            {
                Text = "На вашем ПК возникла проблема, и его необходимо\nперезагрузить. Мы лишь собираем некоторые сведения об\nошибке, а затем будет выполнена автоматическая\nперезагрузка.",
                Foreground = Brushes.White,
                FontSize = 26,
                FontFamily = new FontFamily("Segoe UI"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 25),
                LineHeight = 35
            });

            // Відсоток завершення
            bsodContent.Children.Add(new TextBlock
            {
                Text = "15% завершено",
                Foreground = Brushes.White,
                FontSize = 26,
                FontFamily = new FontFamily("Segoe UI"),
                Margin = new Thickness(0, 0, 0, 40)
            });

            // Нижня панель для BSoD (Ідея 3)
            Grid bottomInfoGrid = new Grid();
            bottomInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Для QR коду (квадрат)
            bottomInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Для тексту

            // Генеруємо справжній QR-код для твого GitHub
            string githubUrl = "https://github.com/SSDDAA-AFK";
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(githubUrl, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrBitmap = qrCode.GetGraphic(20, System.Drawing.Color.White, System.Drawing.Color.FromArgb(255, 0, 120, 215), true); // Білий код, синій фон як у BSoD

            // Перетворюємо System.Drawing.Bitmap в WPF ImageSource
            BitmapSource qrImageSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                qrBitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            System.Windows.Controls.Image qrImage = new System.Windows.Controls.Image
            {
                Source = qrImageSource,
                Width = 90,
                Height = 90,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 5, 20, 0)
            };

            Grid.SetColumn(qrImage, 0);
            bottomInfoGrid.Children.Add(qrImage);

            // Текст праворуч від QR-коду
            StackPanel errorDetails = new StackPanel();
            
            errorDetails.Children.Add(new TextBlock
            {
                Text = "Дополнительные сведения об этой проблеме и возможных способах\nее решения см. на странице https://www.windows.com/stopcode",
                Foreground = Brushes.White,
                FontSize = 14,
                FontFamily = new FontFamily("Segoe UI"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            errorDetails.Children.Add(new TextBlock
            {
                Text = "При обращении в службу поддержки сообщите им этот код:\nКод остановки: CRITICAL_PROCESS_DIED",
                Foreground = Brushes.White,
                FontSize = 12,
                FontFamily = new FontFamily("Segoe UI"),
                TextWrapping = TextWrapping.Wrap
            });
            Grid.SetColumn(errorDetails, 1);
            bottomInfoGrid.Children.Add(errorDetails);

            bsodContent.Children.Add(bottomInfoGrid);
            bsodGrid.Children.Add(bsodContent);

            MediaElement media = new MediaElement
            {
                Source = new Uri(videoPath),
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Close,
                Stretch = Stretch.Fill,
                Visibility = Visibility.Collapsed // Спочатку ховаємо
            };

            // Контейнер, щоб тримати і BSoD і відео
            Grid mainGrid = new Grid();
            mainGrid.Children.Add(bsodGrid);
            mainGrid.Children.Add(media);

            videoWindow.Content = mainGrid;

            // "Блокуємо" натискання клавіатури та миші
            videoWindow.PreviewKeyDown += (s, args) => args.Handled = true;
            videoWindow.PreviewMouseLeftButtonDown += (s, args) => args.Handled = true;
            videoWindow.PreviewMouseRightButtonDown += (s, args) => args.Handled = true;

            // Ідея 5: "Безсмертя" - не даємо закрити вікно по Alt+F4 або хрестику, поки не дограє
            videoWindow.Closing += (s, args) =>
            {
                if (!isFinished)
                {
                    args.Cancel = true;
                }
            };
            
            // Якщо вікно втрачає фокус (наприклад через Win клавішу), пробуємо його повернути
            videoWindow.Deactivated += (s, args) =>
            {
                if (!isFinished)
                {
                    videoWindow.Topmost = true;
                    videoWindow.Activate();
                }
            };

            // Закриваємо вікно після відтворення
            media.MediaEnded += (s, args) =>
            {
                isFinished = true;
                BlockInput(false); // Розблоковуємо ввід
                videoWindow.Close();
            };

            // Про всяк випадок гарантовано знімаємо блокування при закритті вікна і повертаємо гучність
            videoWindow.Closed += (s, args) =>
            {
                BlockInput(false);
                isFinished = true;
                
                // Повертаємо звук як було
                if (defaultDevice != null && oldVolume >= 0)
                {
                    try
                    {
                        defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = oldVolume;
                        defaultDevice.AudioEndpointVolume.Mute = oldMute;
                    }
                    catch (Exception) { }
                }
            };

            // Логіка відтворення BSoD -> Відео
            videoWindow.Loaded += async (s, args) =>
            {
                BlockInput(true); // Блокуємо весь ввід (клавіатура і миша) одразу
                
                // Показуємо BSoD 2.5 секунди
                await Task.Delay(2500);
                
                // Перемикаємось на відео
                videoWindow.Background = Brushes.Black;
                bsodGrid.Visibility = Visibility.Collapsed;
                media.Visibility = Visibility.Visible;
                media.Play(); // Запускаємо відео
            };
            
            // ShowDialog гарантовано блокує головне вікно, поки відео не закриється
            videoWindow.ShowDialog(); 
        }
    }
}