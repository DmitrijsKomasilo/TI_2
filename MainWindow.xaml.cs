using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Lab_2
{
    public partial class MainWindow : Window
    {
        private const int RegisterSize = 33;
        private static readonly int[] Taps = { 32, 12 };

        private byte[] _sourceBytes;
        private byte[] _resultBytes;
        private bool[] _keyStream;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ReadFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите любой файл",
                Filter = "Все файлы|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                _sourceBytes = File.ReadAllBytes(dlg.FileName);
                string fullBits = BytesToBitString(_sourceBytes);
                SourceBitsBox.Text = $"Первые 66 и последние 66 бит:\n{FormatBitPreview(fullBits)}";

                KeyBitsBox.Clear();
                ResultBitsBox.Clear();
                _keyStream = null;
                _resultBytes = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при чтении файла:\n" + ex.Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateKeyButton_Click(object sender, RoutedEventArgs e)
        {
            string raw = SeedTextBox.Text;
            var bits = raw.Where(c => c == '0' || c == '1').ToArray();
            if (bits.Length < RegisterSize)
            {
                MessageBox.Show($"Нужно не менее {RegisterSize} бит в поле seed.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool[] state = bits.Take(RegisterSize)
                                .Select(c => c == '1')
                                .ToArray();

            if (_sourceBytes == null)
            {
                MessageBox.Show("Сначала нужно прочитать файл.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int totalBits = _sourceBytes.Length * 8;
            _keyStream = new bool[totalBits];

            for (int i = 0; i < totalBits; i++)
            {
                bool outBit = state[RegisterSize - 1];
                _keyStream[i] = outBit;

                bool fb = Taps.Aggregate(false, (acc, tap) => acc ^ state[tap]);

                for (int j = RegisterSize - 1; j >= 1; j--)
                    state[j] = state[j - 1];
                state[0] = fb;
            }

            string keyBits = string.Concat(_keyStream.Select(b => b ? '1' : '0'));
            KeyBitsBox.Text = $"Первые 66 и последние 66 бит ключа:\n{FormatBitPreview(keyBits)}";
            ResultBitsBox.Clear();
            _resultBytes = null;
        }

        private void EncryptButton_Click(object sender, RoutedEventArgs e)
        {
            if (_keyStream == null)
            {
                MessageBox.Show("Сначала сгенерируйте ключ.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int nBytes = _sourceBytes.Length;
            _resultBytes = new byte[nBytes];

            for (int i = 0; i < nBytes; i++)
            {
                byte plain = _sourceBytes[i];
                byte cipher = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    bool pb = (plain & (1 << (7 - bit))) != 0;
                    bool kb = _keyStream[i * 8 + bit];
                    bool cb = pb ^ kb;
                    if (cb)
                        cipher |= (byte)(1 << (7 - bit));
                }
                _resultBytes[i] = cipher;
            }

            string resultBits = BytesToBitString(_resultBytes);
            ResultBitsBox.Text = $"Первые 66 и последние 66 бит результата:\n{FormatBitPreview(resultBits)}";
        }

        private void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            EncryptButton_Click(sender, e);
        }

        private void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_resultBytes == null)
            {
                MessageBox.Show("Нет данных для сохранения. Сначала шифруйте или дешифруйте.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title = "Сохранить результат",
                Filter = "Все файлы|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                File.WriteAllBytes(dlg.FileName, _resultBytes);
                MessageBox.Show("Успешно сохранено.",
                                "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении:\n" + ex.Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SeedTextBox.Clear();
            SourceBitsBox.Clear();
            KeyBitsBox.Clear();
            ResultBitsBox.Clear();
            _sourceBytes = null;
            _keyStream = null;
            _resultBytes = null;
        }

        private static string BytesToBitString(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 8);
            foreach (byte b in data)
                for (int bit = 7; bit >= 0; bit--)
                    sb.Append((b & (1 << bit)) != 0 ? '1' : '0');
            return sb.ToString();
        }

        private static string FormatBitPreview(string bits)
        {
            const int previewLen = 66;
            if (bits.Length <= 2 * previewLen)
                return bits;

            string firstPart = bits.Substring(0, previewLen);
            string lastPart = bits.Substring(bits.Length - previewLen, previewLen);
            return $"{firstPart}...{lastPart}";
        }
    }
}
