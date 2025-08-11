using DrJaw.Models;
using DrJaw.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        }
        private async void AddItem_Loaded(object sender, RoutedEventArgs e)
        {
            Types = await Storage.Repo.LoadTypes();
            ComboBoxType.ItemsSource = Types;

            Storage.Metals = await Storage.Repo.LoadMetals();
            ComboBoxMetal.ItemsSource = Storage.Metals;

            Stones = await Storage.Repo.LoadStones();
            ComboBoxStone.ItemsSource = Stones;

            Manufacturers = await Storage.Repo.LoadManufacturers();
            ComboBoxManufacturer.ItemsSource = Manufacturers;


        }
        private void ButtonAddImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите изображение",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp",
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



        private bool IsInputValid()
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(TextBoxArticul.Text) ||
                ComboBoxType.SelectedItem == null ||
                ComboBoxMetal.SelectedItem == null ||
                string.IsNullOrWhiteSpace(TextBoxWeight.Text) ||
                string.IsNullOrWhiteSpace(TextBoxSize.Text) ||
                string.IsNullOrWhiteSpace(TextBoxPrice.Text) ||
                ComboBoxStone.SelectedItem == null ||
                ComboBoxManufacturer.SelectedItem == null)
                return false;

            // Проверка числовых значений
            if (!decimal.TryParse(TextBoxWeight.Text.Replace('.', ','), out _) ||
                !decimal.TryParse(TextBoxPrice.Text.Replace('.', ','), out _))
                return false;

            return true;
        }
        private async void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInputValid())
            {
                MessageBox.Show("Пожалуйста, заполните все поля корректно.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                System.Drawing.Image? finalImage = null;

                if (ImagePreview.Source is BitmapImage bmp)
                {
                    finalImage = ImageHelper.BitmapImageToDrawingImage(bmp);
                    // Если нужно — можешь получить байты:
                    // var bytes = ImageHelper.ImageToBytes(finalImage);
                }

                bool success = await Storage.Repo.CreateItem(
                    articulName: TextBoxArticul.Text,
                    typeId: (int)ComboBoxType.SelectedValue!,
                    metalId: (int)ComboBoxMetal.SelectedValue!,
                    weight: TextBoxWeight.Text,
                    size: TextBoxSize.Text,
                    stoneId: (int)ComboBoxStone.SelectedValue!,
                    manufacturerId: (int)ComboBoxManufacturer.SelectedValue!,
                    price: TextBoxPrice.Text,
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
