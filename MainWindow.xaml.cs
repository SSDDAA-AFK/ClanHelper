using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace ClanHelper
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private List<TextBlock> particles;
        private Random random = new Random();
        
        // Словник для збереження шляхів до папок (щоб знати, яку папку відкривати)
        private Dictionary<string, string> savedFolders = new Dictionary<string, string>();

        private class AppTheme
        {
            public string Name { get; set; }
            public string Bg { get; set; }
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
            InitializeComponent();
            InitializeThemes();
            InitializeParticles();
            ApplyTheme(themes[0]);
        }

        private void InitializeThemes()
        {
            themes = new List<AppTheme>
            {
                new AppTheme { Name = "Default Dark Blue", Bg = "#0B132B", Card = "#1C2541", Accent = "#3A506B", Text = "#FFFFFF", ParticleSymbol = "❄", ParticleColor = "#E0FFFF" },
                new AppTheme { Name = "Orange", Bg = "#2B1A12", Card = "#3A2318", Accent = "#FD9B23", Text = "#FDF8EB", ParticleSymbol = "✦", ParticleColor = "#E28250" },
                new AppTheme { Name = "Midnight Purple", Bg = "#0b0614", Card = "#160b2e", Accent = "#a855f7", Text = "#ede9fe", ParticleSymbol = "★", ParticleColor = "#7c3aed" },
                new AppTheme { Name = "Ice Gray", Bg = "#0f1115", Card = "#1c1f26", Accent = "#6b7280", Text = "#f9fafb", ParticleSymbol = "❄", ParticleColor = "#9ca3af" },
                new AppTheme { Name = "Red Alert", Bg = "#1a0404", Card = "#2a0808", Accent = "#ef4444", Text = "#fee2e2", ParticleSymbol = "⚠", ParticleColor = "#dc2626" },
                new AppTheme { Name = "Purple Neon", Bg = "#12001a", Card = "#1f002b", Accent = "#c77dff", Text = "#f3e8ff", ParticleSymbol = "✨", ParticleColor = "#9d4edd" },
                new AppTheme { Name = "Forest", Bg = "#0f1f17", Card = "#172f25", Accent = "#22c55e", Text = "#ecfdf5", ParticleSymbol = "🍃", ParticleColor = "#4ade80" },
                new AppTheme { Name = "Solar Light", Bg = "#faf08c", Card = "#f2d86f", Accent = "#ca8a04", Text = "#422006", ParticleSymbol = "☀", ParticleColor = "#eab308" }
            };
        }

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
            timer.Start();
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

        private void Particles_Checked(object sender, RoutedEventArgs e) => timer?.Start();
        private void Particles_Unchecked(object sender, RoutedEventArgs e) => timer?.Stop();

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            currentThemeIndex++;
            if (currentThemeIndex >= themes.Count) currentThemeIndex = 0;
            ApplyTheme(themes[currentThemeIndex]);
        }

        private void ApplyTheme(AppTheme theme)
        {
            this.Resources["BgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Bg));
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

        private void OpenWebSite(string url)
        {
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
        }

        // --- УНІВЕРСАЛЬНА ФУНКЦІЯ ЗАВАНТАЖЕННЯ ---
        // --- УНІВЕРСАЛЬНА ФУНКЦІЯ ЗАВАНТАЖЕННЯ З ПРОГРЕС-БАРОМ ---
        private async Task DownloadFileAsync(string directUrl, string saveAsFileName)
        {
            var dialog = new OpenFolderDialog { Title = $"Выберите папку для сохранения {saveAsFileName}" };
            
            if (dialog.ShowDialog() == true)
            {
                string folderPath = dialog.FolderName;
                string fullFilePath = Path.Combine(folderPath, saveAsFileName);

                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    
                    // Показуємо прогрес-бар і скидаємо його на 0
                    ProgressPanel.Visibility = Visibility.Visible;
                    ProgressText.Text = $"Загрузка {saveAsFileName}... 0%";
                    DownloadProgressBar.Value = 0;

                    using (HttpClient client = new HttpClient())
                    // Робимо запит, але не вантажимо все одразу, а читаємо потік
                    using (var response = await client.GetAsync(directUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        
                        // Дізнаємося загальний розмір файлу
                        var totalBytes = response.Content.Headers.ContentLength;

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var buffer = new byte[8192];
                            var isMoreToRead = true;
                            long totalRead = 0;

                            do
                            {
                                // Читаємо "шматочок" файлу
                                var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                                if (read == 0)
                                {
                                    isMoreToRead = false;
                                }
                                else
                                {
                                    // Записуємо цей шматочок на диск
                                    await fileStream.WriteAsync(buffer, 0, read);
                                    totalRead += read;
                                    
                                    // Рахуємо відсотки і оновлюємо прогрес-бар
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
                    
                    // Невеличка пауза, щоб користувач побачив "100%", перш ніж вилізе віконце
                    await Task.Delay(300); 
                    
                    MessageBox.Show($"Файл {saveAsFileName} успешно загружен!", "Сделано!", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Невозможно загрузить файл. Проверьте ссылку.\nОшибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                    // Ховаємо прогрес-бар після завершення або помилки
                    ProgressPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        // --- УНІВЕРСАЛЬНА ФУНКЦІЯ ВІДКРИТТЯ ПАПКИ ---
        private void OpenSavedFolder(string fileName)
        {
            if (savedFolders.ContainsKey(fileName))
            {
                // Склеюємо шлях до папки і назву файлу, щоб отримати повний шлях
                string fullFilePath = Path.Combine(savedFolders[fileName], fileName);
                
                // Команда /select змушує провідник відкрити папку і одразу виділити потрібний файл
                Process.Start("explorer.exe", $"/select,\"{fullFilePath}\"");
            }
            else
            {
                MessageBox.Show("Вы еще не загружали этот файл, поэтому папка неизвестна!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // --- КНОПКИ САЙТІВ ---
        private void Everything_Click(object sender, RoutedEventArgs e) => OpenWebSite("https://www.voidtools.com/");
        private void SystemInformer_Click(object sender, RoutedEventArgs e) => OpenWebSite("https://systeminformer.sourceforge.io/");
        private void EverythingCheker_Click(object sender, RoutedEventArgs e) => OpenWebSite("https://github.com/SSDDAA-AFK/Everything_cheker/releases/latest");
        private void SystemInformerCheker_Click(object sender, RoutedEventArgs e) => OpenWebSite("https://github.com/SSDDAA-AFK/SustemInformer_Cheker/releases/latest");

        // --- КНОПКИ ЗАВАНТАЖЕННЯ ---
        // УВАГА: Замініть "ПРЯМЕ_ПОСИЛАННЯ_..." на справжні посилання на файли!
        private async void EverythingDL_Click(object sender, RoutedEventArgs e) 
            => await DownloadFileAsync("https://www.voidtools.com/Everything-1.4.1.1032.x64-Setup.exe", "Everything.exe");
            
        private async void SystemInformerDL_Click(object sender, RoutedEventArgs e) 
            => await DownloadFileAsync("https://deac-riga.dl.sourceforge.net/project/systeminformer/systeminformer-3.2.25011-release-setup.exe?viasf=1", "SystemInformer.exe");
            
        private async void EverythingChekerDL_Click(object sender, RoutedEventArgs e) 
            => await DownloadFileAsync("https://github.com/SSDDAA-AFK/Everything_cheker/releases/download/v1.1/Everything_cheker.exe", "EverythingCheker.exe");
            
        private async void SystemInformerChekerDL_Click(object sender, RoutedEventArgs e) 
            => await DownloadFileAsync("https://github.com/SSDDAA-AFK/SustemInformer_Cheker/releases/download/v1.1/SystemInformerChecker.exe", "SystemInformerChecker.exe");


        // --- КНОПКИ ВІДКРИТТЯ ПАПОК ---
        private void EverythingFolder_Click(object sender, RoutedEventArgs e) => OpenSavedFolder("Everything.exe");
        private void SystemInformerFolder_Click(object sender, RoutedEventArgs e) => OpenSavedFolder("SystemInformer.exe");
        private void EverythingChekerFolder_Click(object sender, RoutedEventArgs e) => OpenSavedFolder("EverythingCheker.exe");
        private void SystemInformerChekerFolder_Click(object sender, RoutedEventArgs e) => OpenSavedFolder("SystemInformerChecker.exe");

        // --- НАЛАШТУВАННЯ ---
        private void Settings_Click(object sender, RoutedEventArgs e) => SettingsPanel.Visibility = Visibility.Visible;
        private void CloseSettings_Click(object sender, RoutedEventArgs e) => SettingsPanel.Visibility = Visibility.Collapsed;
    }
}