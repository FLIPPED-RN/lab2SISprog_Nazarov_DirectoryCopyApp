using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace lab2SISprog_Nazarov_DirectoryCopyApp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private string GetRelativePath(string basePath, string fullPath)
        {
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                basePath += Path.DirectorySeparatorChar;
            }

            Uri baseUri = new Uri(basePath);
            Uri fullUri = new Uri(fullPath);

            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);
            return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        private void BrowseSource_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SourceDirectoryTextBox.Text = dialog.SelectedPath;
            }
        }

        private void BrowseTarget_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TargetDirectoryTextBox.Text = dialog.SelectedPath;
            }
        }

        private async void StartCopy_Click(object sender, RoutedEventArgs e)
        {
            string sourceDir = SourceDirectoryTextBox.Text;
            string targetDir = TargetDirectoryTextBox.Text;
            int threadCount = int.Parse(ThreadCountTextBox.Text);

            if (!Directory.Exists(sourceDir))
            {
                MessageBox.Show("Исходная директория не существует");
                return;
            }

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string[] files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            CopyProgressBar.Maximum = files.Length;
            CopyProgressBar.Value = 0;

            await CopyFilesAsync(files, targetDir, threadCount);

            MessageBox.Show("Копирование завершено");
        }

        private async Task CopyFilesAsync(string[] files, string targetDir, int threadCount)
        {
            var tasks = new Task[threadCount];
            int filesPerThread = files.Length / threadCount;

            for (int i = 0; i < threadCount; i++)
            {
                int startIndex = i * filesPerThread;
                int endIndex = (i == threadCount - 1) ? files.Length : startIndex + filesPerThread;
                tasks[i] = Task.Run(() => CopyFiles(files, startIndex, endIndex, targetDir));
            }

            await Task.WhenAll(tasks);
        }

        private void CopyFiles(string[] files, int startIndex, int endIndex, string targetDir)
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                try
                {
                    string sourceDir = string.Empty;
                    Dispatcher.Invoke(() => sourceDir = SourceDirectoryTextBox.Text);

                    string relativePath = GetRelativePath(sourceDir, Path.GetDirectoryName(files[i]));
                    string targetPath = Path.Combine(targetDir, relativePath, Path.GetFileName(files[i]));

                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    File.Copy(files[i], targetPath, true);

                    Dispatcher.Invoke(() => CopyProgressBar.Value++);
                }
                catch (IOException ex)
                {
                    Dispatcher.Invoke(()=>MessageBox.Show($"Ошибка при копировании файла {files[i]}: {ex.Message}"));
                }
                catch (UnauthorizedAccessException ex)
                {
                    Dispatcher.Invoke(()=>MessageBox.Show($"Нет доступа к файлу {files[i]}: {ex.Message}"));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(()=>MessageBox.Show($"Неизвестная ошибка при копировании файла {files[i]}: {ex.Message}"));
                }
            }
        }
    }
}
