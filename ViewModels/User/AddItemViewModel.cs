using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DrJaw.Models;
using DrJaw.Services;
using DrJaw.Services.Data;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels;

public sealed class AddItemViewModel : ViewModelBase
{
    private readonly IMssqlRepository _repo;
    private readonly IReferenceDataService _refData;
    private readonly IUserSessionService _session;
    private MSSQLMetal? _selectedMetal;
    private MSSQLType? _selectedType;
    private MSSQLStone? _selectedStone;
    private MSSQLManufacturer? _selectedManufacturer;
    private MSSQLArticul? _articulMatch;
    private CancellationTokenSource? _articulCts;
    private int? _pendingTypeId;


    public ReadOnlyObservableCollection<MSSQLMetal> Metals => _refData.Metals;
    public ObservableCollection<MSSQLType> Types { get; } = new();
    public ObservableCollection<MSSQLStone> Stones { get; } = new();
    public ObservableCollection<MSSQLManufacturer> Manufacturers { get; } = new();
    public decimal? WeightDec => TryParseInvariant(Weight);
    public decimal? SizeDec => TryParseInvariant(Size);
    public decimal? PriceDec => TryParseInvariant(Price);
    public bool IsArticulLocked => _articulMatch != null;
    public bool IsArticulEditable => !IsArticulLocked;
    public bool IsTypeMetalEnabled => !IsArticulLocked;
    public bool CanEditImage => !IsArticulLocked;


    public AsyncRelayCommand SaveCommand { get; }
    public ICommand PickImageCommand { get; }
    public ICommand ClearImageCommand { get; }
    public ICommand CancelCommand { get; }
    public event EventHandler<bool>? CloseRequested;
    public event EventHandler<string>? ErrorOccurred;

    public AddItemViewModel(IMssqlRepository repo, MSSQLMetal? metalFromPanel, IReferenceDataService refData, IUserSessionService session)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _refData = refData ?? throw new ArgumentNullException(nameof(refData));
        _session = session ?? throw new ArgumentNullException(nameof(session));

        // 1) Сначала команды
        PickImageCommand = new RelayCommand(_ => PickImage(), _ => CanEditImage);
        ClearImageCommand = new RelayCommand(_ => ClearImage(), _ => CanEditImage);
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync(), _ => CanSave());
        CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, false));

        // 2) Теперь стартовое значение для комбобокса
        SelectedMetal = metalFromPanel ?? _refData.Metals.FirstOrDefault();
        _ = LoadTypesAsync();
        _ = LoadStonesAsync();
        _ = LoadManufacturersAsync();
        // 3) На всякий — пересчитать доступность
        SaveCommand.RaiseCanExecuteChanged();
    }

    public MSSQLMetal? SelectedMetal
    {
        get => _selectedMetal;
        set
        {
            if (Set(ref _selectedMetal, value))
                SaveCommand?.RaiseCanExecuteChanged(); // ← на будущее с ?.
        }
    }
    public MSSQLType? SelectedType
    {
        get => _selectedType;
        set { if (Set(ref _selectedType, value)) SaveCommand?.RaiseCanExecuteChanged(); }
    }
    public MSSQLStone? SelectedStone
    {
        get => _selectedStone;
        set { if (Set(ref _selectedStone, value)) SaveCommand?.RaiseCanExecuteChanged(); }
    }
    public MSSQLManufacturer? SelectedManufacturer
    {
        get => _selectedManufacturer;
        set { if (Set(ref _selectedManufacturer, value)) SaveCommand?.RaiseCanExecuteChanged(); }
    }
    private string _weight = "";
    public string Weight
    {
        get => _weight;
        set
        {
            if (Set(ref _weight, value))
                SaveCommand?.RaiseCanExecuteChanged();
        }
    }
    private string _size = "";
    public string Size
    {
        get => _size;
        set
        {
            if (Set(ref _size, value))
                SaveCommand?.RaiseCanExecuteChanged();
        }
    }
    private string _price = "";
    public string Price
    {
        get => _price;
        set
        {
            if (Set(ref _price, value))
                SaveCommand?.RaiseCanExecuteChanged();
        }
    }
    private string _articul = "";
    public string Articul
    {
        get => _articul;
        set
        {
            var norm = (value ?? string.Empty).Trim();
            if (Set(ref _articul, norm))
            {
                SaveCommand?.RaiseCanExecuteChanged();
                ScheduleArticulLookup(); // ← запускаем проверку
            }
        }
    }
    private const int MaxCommentLength = 2000;
    private string _comment = "";
    public string Comment
    {
        get => _comment;
        set
        {
            var v = (value ?? string.Empty)
                    .Replace("\r\n", "\n")            // унифицируем переносы
                    .Replace("\r", "\n");             // на всякий
            if (v.Length > MaxCommentLength)
                v = v[..MaxCommentLength];           // режем по лимиту

            if (Set(ref _comment, v))
                SaveCommand?.RaiseCanExecuteChanged();
        }
    }
    private byte[]? _imageBytes;
    public byte[]? ImageBytes
    {
        get => _imageBytes;
        private set => Set(ref _imageBytes, value);
    }
    private BitmapSource? _previewImage;
    public BitmapSource? PreviewImage
    {
        get => _previewImage;
        private set
        {
            if (Set(ref _previewImage, value))
                SaveCommand?.RaiseCanExecuteChanged(); // если от картинки зависит доступность кнопки
        }
    }

    private bool CanSave()
    {
        // справочники
        if (SelectedMetal == null) return false;
        if (SelectedType == null) return false;
        if (SelectedManufacturer == null) return false;
        if (SelectedStone == null) return false;

        // артикул
        if (string.IsNullOrWhiteSpace(Articul)) return false;

        // числа
        if (!WeightDec.HasValue || WeightDec.Value <= 0m) return false;
        if (!SizeDec.HasValue || SizeDec.Value <= 0m) return false;
        if (!PriceDec.HasValue || PriceDec.Value < 0m) return false;

        // картинка обязательна (разрешим либо по PreviewImage, либо по байтам)
        bool hasImage = PreviewImage != null || (ImageBytes is { Length: > 0 });
        if (!hasImage) return false;

        // Comment — необязателен
        return true;
    }
    private async Task SaveAsync()
    {
        try
        {
            // страхуемся на магазин
            var martId = _session.CurrentMart?.Id
                ?? throw new InvalidOperationException("Не выбран магазин в сессии.");

            // округляем под scale=2 в БД
            decimal weight2 = Math.Round(WeightDec!.Value, 2, MidpointRounding.AwayFromZero);
            decimal price2 = Math.Round(PriceDec!.Value, 2, MidpointRounding.AwayFromZero);

            // 1) гарантируем артикул
            int articulId;
            if (_articulMatch != null)
            {
                articulId = _articulMatch.Id; // найден раньше, Type/Metal/Image не трогаем
            }
            else
            {
                articulId = (await _repo.EnsureArticulWithImageAsync(
                    name: Articul,
                    typeId: SelectedType!.Id,
                    metalId: SelectedMetal!.Id,
                    imageBytes: ImageBytes!)).articulId;
            }

            // 2) создаём товар
            var dto = new ItemCreate
            {
                ArticulId = articulId,
                Weight = weight2,
                Size = Size,
                MartId = martId,
                ManufacturerId = SelectedManufacturer!.Id,
                Price = price2,
                StoneId = SelectedStone!.Id,
                Comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim()
            };

            var newItemId = await _repo.AddItemAsync(dto);
            // при желании: лог/уведомление об успехе

            CloseRequested?.Invoke(this, true);
        }
        catch (SqlException ex)
        {
            // ошибки БД (уникальные ключи, FK и т.д.)
            ErrorOccurred?.Invoke(this, $"Ошибка базы данных:\n{ex.Message}");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Не удалось сохранить позицию:\n{ex.Message}");
        }
    }
    private async Task LoadTypesAsync()
    {
        try
        {
            var list = await _repo.GetTypesAsync();
            Types.Clear();
            foreach (var t in list.OrderBy(t => t.Name))
                Types.Add(t);

            if (_pendingTypeId.HasValue)
            {
                var t = Types.FirstOrDefault(x => x.Id == _pendingTypeId.Value);
                if (t != null) SelectedType = t;
                _pendingTypeId = null;
            }

            if (SelectedType == null && Types.Count > 0)
                SelectedType = Types[0];
        }
        catch (Exception ex)
        {
            // по желанию: всплывающее окно/лог — сейчас просто игнор
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            SaveCommand?.RaiseCanExecuteChanged();
        }
    }
    private async Task LoadStonesAsync()
    {
        try
        {
            var list = await _repo.GetStonesAsync();
            Stones.Clear();
            foreach (var s in list.OrderBy(s => s.Name))
                Stones.Add(s);

            if (SelectedStone == null && Stones.Count > 0)
                SelectedStone = Stones[0];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            // при желании: показать окно ошибки
        }
        finally
        {
            SaveCommand?.RaiseCanExecuteChanged();
        }
    }
    private async Task LoadManufacturersAsync()
    {
        try
        {
            var list = await _repo.GetManufacturersAsync();
            Manufacturers.Clear();
            foreach (var m in list.OrderBy(m => m.Name))
                Manufacturers.Add(m);

            if (SelectedManufacturer == null && Manufacturers.Count > 0)
                SelectedManufacturer = Manufacturers[0];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            SaveCommand?.RaiseCanExecuteChanged();
        }
    }
    private static decimal? TryParseInvariant(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Replace(',', '.'); // на всякий
        return decimal.TryParse(
                   s,
                   NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                   CultureInfo.InvariantCulture,
                   out var v)
               ? v
               : (decimal?)null;
    }
    public void SetImageBytes(byte[]? bytes)
    {
        ImageBytes = bytes;
        PreviewImage = bytes is { Length: > 0 } ? ToBitmapImage(bytes) : null;
    }
    public void ClearImage()
    {
        ImageBytes = null;
        PreviewImage = null;
    }
    private static BitmapImage ToBitmapImage(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad; // читаем полностью в память
        bmp.StreamSource = ms;
        bmp.EndInit();
        bmp.Freeze(); // чтобы можно было использовать из любого потока
        return bmp;
    }
    private void PickImage()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Выберите изображение",
            Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.webp|Все файлы|*.*",
            CheckFileExists = true
        };
        if (dlg.ShowDialog() == true)
            SetImageBytes(File.ReadAllBytes(dlg.FileName));
    }
    private async void ScheduleArticulLookup()
    {
        _articulCts?.Cancel();
        _articulCts = new CancellationTokenSource();
        var token = _articulCts.Token;

        try
        {
            await Task.Delay(250, token); // лёгкий дебаунс
            if (token.IsCancellationRequested) return;
            await CheckArticulAsync(Articul, token);
        }
        catch (TaskCanceledException) { }
    }
    private async Task CheckArticulAsync(string? name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _articulMatch = null;
            OnArticulLockChanged();
            return;
        }

        var found = await _repo.GetArticulByNameAsync(name.Trim());
        if (ct.IsCancellationRequested) return;

        if (found != null)
        {
            _articulMatch = found;

            // Металл — сразу по Id
            var metal = _refData.Metals.FirstOrDefault(m => m.Id == found.MetalId);
            if (metal != null) SelectedMetal = metal;

            // Тип — либо сразу, либо отложенно до загрузки типов
            var typeNow = Types.FirstOrDefault(t => t.Id == found.TypeId);
            if (typeNow != null) SelectedType = typeNow;
            else _pendingTypeId = found.TypeId;

            // Картинка
            if (found.Image is { Length: > 0 })
                SetImageBytes(found.Image);
        }
        else
        {
            _articulMatch = null;
            // ничего не сбрасываем — оставляем выбор пользователя
        }

        OnArticulLockChanged();
    }
    private void OnArticulLockChanged()
    {
        OnPropertyChanged(nameof(IsArticulLocked));
        OnPropertyChanged(nameof(IsTypeMetalEnabled));
        OnPropertyChanged(nameof(CanEditImage));

        (PickImageCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ClearImageCommand as RelayCommand)?.RaiseCanExecuteChanged();
        SaveCommand?.RaiseCanExecuteChanged();

        // НОВОЕ: если разблокировалось (совпадения нет) — очищаем картинку
        if (!IsArticulLocked)
        {
            if (PreviewImage != null || (ImageBytes is { Length: > 0 }))
                ClearImage();
        }
    }
}
