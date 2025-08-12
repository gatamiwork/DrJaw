using DrJaw.Models;
using DrJaw.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DrJaw.Views.User
{
    public partial class AddItem : Window
    {
        private List<MSSQLType> Types;
        private List<MSSQLStone> Stones;
        private List<MSSQLManufacturer> Manufacturers;
        private CancellationTokenSource? _articulCts;
        private int _articulId = 0;

        public AddItem()
        {
            InitializeComponent();
            Loaded += AddItem_Loaded;
            InputValidators.AttachNumericValidation(TextBoxWeight);
            InputValidators.AttachNumericValidation(TextBoxSize);
            InputValidators.AttachNumericValidation(TextBoxPrice);

            WireValidationEvents();
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // отмена/освобождение CTS
            _articulCts?.Cancel();
            _articulCts?.Dispose();
            _articulCts = null;
            // сбросить картинку и привязки
            if (ImagePreview != null)
            {
                ImagePreview.Source = null;                  // убираем сильную ссылку на BitmapImage
                ImagePreview.ClearValue(Image.SourceProperty);
            }

            ComboBoxType.ItemsSource = null;
            ComboBoxMetal.ItemsSource = null;
            ComboBoxStone.ItemsSource = null;
            ComboBoxManufacturer.ItemsSource = null;

            DataContext = null;
        }
        private void WireValidationEvents()
        {
            TextBoxArticul.TextChanged += AnyInputChanged;
            TextBoxWeight.TextChanged += AnyInputChanged;
            TextBoxSize.TextChanged += AnyInputChanged;
            TextBoxPrice.TextChanged += AnyInputChanged;
            ComboBoxType.SelectionChanged += AnyInputChanged;
            ComboBoxMetal.SelectionChanged += AnyInputChanged;
            ComboBoxStone.SelectionChanged += AnyInputChanged;
            ComboBoxManufacturer.SelectionChanged += AnyInputChanged;
            AnyInputChanged(null, EventArgs.Empty);
        }
        private void AnyInputChanged(object? s, EventArgs e)
        {
            if (ButtonAdd != null) ButtonAdd.IsEnabled = IsInputValid();
        }
        private async void AddItem_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var tTypes = Storage.Repo.LoadTypes();
                var tMetals = Storage.Repo.LoadMetals();
                var tStones = Storage.Repo.LoadStones();
                var tMfrs = Storage.Repo.LoadManufacturers();

                await Task.WhenAll(tTypes, tMetals, tStones, tMfrs);

                Types = tTypes.Result;
                var metals = tMetals.Result;
                Stones = tStones.Result;
                Manufacturers = tMfrs.Result;

                ComboBoxType.ItemsSource = Types;
                ComboBoxMetal.ItemsSource = metals;
                ComboBoxStone.ItemsSource = Stones;
                ComboBoxManufacturer.ItemsSource = Manufacturers;

                if (Storage.CurrentMetal != null)
                    ComboBoxMetal.SelectedValue = Storage.CurrentMetal.Id;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }
        private void ButtonAddImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите изображение",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var imagePath = dialog.FileName;

                    // Загружаем изображение в ImagePreview
                    var bitmap = new BitmapImage();
                    using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze(); // чтобы можно было использовать в другом потоке, если надо
                    }

                    ImagePreview.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке изображения:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private async void TextBoxArticul_TextChanged(object sender, TextChangedEventArgs e)
        {
            _articulCts?.Cancel();
            _articulCts?.Dispose();
            _articulCts = new CancellationTokenSource();
            var token = _articulCts.Token;

            try
            {
                await Task.Delay(300, token);

                string input = TextBoxArticul.Text.Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    ComboBoxType.IsEnabled = true;
                    ComboBoxMetal.IsEnabled = true;
                    _articulId = 0;

                    ImagePreview.Source = null;
                    return;
                }

                var list = await Storage.Repo.LoadArticulByName(input);
                if (list.Count == 0)
                {
                    ComboBoxType.IsEnabled = true;
                    ComboBoxMetal.IsEnabled = true;
                    _articulId = 0;

                    ImagePreview.Source = null;
                    return;
                }

                var match = list[0];

                _articulId = match.Id;
                ComboBoxType.SelectedValue = match.TypeId;
                ComboBoxType.IsEnabled = false;

                ComboBoxMetal.SelectedValue = match.MetalId;
                ComboBoxMetal.IsEnabled = false;

                if (match.ImageData is not null)
                    ImagePreview.Source = Utils.ImageHelper.BytesToBitmapImage(match.ImageData);

            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при автозаполнении артикула: " + ex.Message);
            }
        }
        private static bool TryParseDecimal(string? s, out decimal v)
        {
            v = 0m;
            if (string.IsNullOrWhiteSpace(s)) return false;

            // уберём пробелы-разделители тысяч
            s = s.Trim().Replace(" ", "");

            // 1) пробуем текущую культуру
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out v))
                return true;

            // 2) пробуем инвариантную (точка как разделитель)
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v))
                return true;

            // 3) пробуем альтернативный разделитель (запятая/точка)
            var curSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            var altSep = curSep == "," ? "." : ",";
            var swapped = s.Replace(altSep, curSep);
            return decimal.TryParse(swapped, NumberStyles.Number, CultureInfo.CurrentCulture, out v);
        }
        private bool IsInputValid()
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(TextBoxArticul.Text) ||
                ComboBoxType.SelectedItem == null ||
                ComboBoxMetal.SelectedItem == null ||
                ComboBoxStone.SelectedItem == null ||
                ComboBoxManufacturer.SelectedItem == null)
                return false;

            // Проверка числовых значений
            if (!decimal.TryParse(TextBoxWeight.Text.Replace('.', ','), out _) ||
                !decimal.TryParse(TextBoxPrice.Text.Replace('.', ','), out _))
                return false;

            if (!TryParseDecimal(TextBoxWeight.Text, out var weight)) return false;
            if (!TryParseDecimal(TextBoxSize.Text, out var size)) return false;
            if (!TryParseDecimal(TextBoxPrice.Text, out var price)) return false;

            return true;
        }
        private async void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInputValid())
            {
                MessageBox.Show("Пожалуйста, заполните все поля корректно.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!TryParseDecimal(TextBoxWeight.Text, out var weight) ||
                !TryParseDecimal(TextBoxSize.Text, out var size) ||
                !TryParseDecimal(TextBoxPrice.Text, out var price))
            {
                MessageBox.Show("Некорректные числовые значения.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                System.Drawing.Image? finalImage = null;

                if (ImagePreview.Source is BitmapImage bmp)
                {
                    finalImage = ImageHelper.BitmapImageToDrawingImage(bmp);
                }

                bool success = await Storage.Repo.CreateItem(
                    articulName: TextBoxArticul.Text,
                    typeId: (int)ComboBoxType.SelectedValue!,
                    metalId: (int)ComboBoxMetal.SelectedValue!,
                    weight: weight,
                    size: size,
                    stoneId: (int)ComboBoxStone.SelectedValue!,
                    manufacturerId: (int)ComboBoxManufacturer.SelectedValue!,
                    price: price,
                    comment: TextBoxComment.Text,
                    image: finalImage,
                    articulId: _articulId
                );

                if (success)
                {
                    DrJaw.Utils.EventBus.Publish("ItemsChanged");
                    Close();
                }
                else
                    MessageBox.Show("Не удалось добавить товар.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении товара: " + ex.Message);
            }
        }
    }
}
