using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace DrJaw.Models
{
    public class MSSQLMetal
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class MSSQLType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class MSSQLCartTotals
    {
        public string UserName { get; set; }
        public string Metal { get; set; }
        public decimal ItemCount { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalPrice { get; set; }
    }
    public class MSSQLReturnCartItem
    {
        public int Id { get; set; }
        public string Articul { get; set; }
        public decimal Weight { get; set; }
        public decimal Size { get; set; }
        public string Stones { get; set; }
        public decimal Price { get; set; }
        public decimal Bonus { get; set; }
        public decimal TotalPrice { get; set; }
        public int CartId { get; set; }
    }
    public class MSSQLArticul
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int TypeId { get; set; }
        public int MetalId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class MSSQLArticulByName
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int TypeId { get; set; }
        public int MetalId { get; set; }
        public byte[]? ImageData { get; set; }
    }
    public class MSSQLArticulImage
    {
        public int Id { get; set; }
        public int ArticulId { get; set; }
        public byte[] Image { get; set; }
        public DateTime UploadedAt { get; set; }
    }
    public class MSSQLMart
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class MSSQLManufacturer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class MSSQLLomTotals
    {
        public int MartId { get; set; }
        public decimal StartWeight { get; set; }
        public decimal StartPrice { get; set; }
        public decimal CurrentWeight { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal EndWeight { get; set; }
        public decimal EndPrice { get; set; }
    }
    public class MSSQLStone
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class MSSQLUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public bool Display { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class MSSQLStatus
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class MSSQLPaymentType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class MSSQLLom
    {
        public int Id { get; set; }
        public int MartId { get; set; }
        public decimal Weight { get; set; }
        public decimal? PricePerGram { get; set; }
        public bool Receiving { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class MSSQLLomItem
    {
        public int Id { get; set; }
        public string Mart { get; set; } = "";
        public decimal Weight { get; set; }
        public decimal? PricePerGram { get; set; }
        public bool Receiving { get; set; } // не bool, т.к. из БД приходит int (0/1)
        public string? Type { get; set; }  // Для вывода "Покупка", "Отгрузка", "Заказ"
        public decimal Price { get; set; } // Вычисляется вручную
        public string UserName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int? CartId { get; set; } // может быть null
    }
    public class MSSQLCart
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MartId { get; set; }
        public int PaymentTypeId { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public decimal Bonus { get; set; }
        public decimal TotalSum { get; set; }
        public int? LomId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class MSSQLItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private string _type = "";
        public string Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(nameof(Type)); }
        }

        private string _metal = "";
        public string Metal
        {
            get => _metal;
            set { _metal = value; OnPropertyChanged(nameof(Metal)); }
        }

        private string _articul = "";
        public string Articul
        {
            get => _articul;
            set { _articul = value; OnPropertyChanged(nameof(Articul)); }
        }

        private decimal _weight;
        public decimal Weight
        {
            get => _weight;
            set { _weight = value; OnPropertyChanged(nameof(Weight)); }
        }

        private int _itemCount;
        public int ItemCount
        {
            get => _itemCount;
            set { _itemCount = value; OnPropertyChanged(nameof(ItemCount)); }
        }

        private decimal _size;
        public decimal Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(nameof(Size)); }
        }

        private string _stones = "";
        public string Stones
        {
            get => _stones;
            set { _stones = value; OnPropertyChanged(nameof(Stones)); }
        }

        private string _comment = "";
        public string Comment
        {
            get => _comment;
            set { _comment = value; OnPropertyChanged(nameof(Comment)); }
        }

        private decimal _price;
        public decimal Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(nameof(Price)); }
        }

        private string _manufacturer = "";
        public string Manufacturer
        {
            get => _manufacturer;
            set { _manufacturer = value; OnPropertyChanged(nameof(Manufacturer)); }
        }

        private int _mid;
        public int mid
        {
            get => _mid;
            set { _mid = value; OnPropertyChanged(nameof(mid)); }
        }
    }
    public class MSSQLCartItem
    {
        public byte[]? ImageData { get; set; }           // ai.Image AS ImageData
        public string Articul { get; set; } = "";        // a.Name AS Articul
        public decimal Weight { get; set; }              // i.Weight AS Weight
        public string Size { get; set; } = "";           // i.Size AS Size
        public string Manufacturer { get; set; } = "";   // m.Name AS Manufacturer
        public string Stone { get; set; } = "";          // s.Name AS Stone
        public int ItemBonus { get; set; }               // ci.Bonus AS ItemBonus
        public string Comment { get; set; } = "";        // i.Comment AS Comment
        public string CiStatus { get; set; } = "";       // st.Name AS CiStatus
    }
    public class MSSQLReadyToSold : INotifyPropertyChanged
    {
        public int Id { get; set; }

        public byte[]? ImageData { get; set; }

        public string Articul { get; set; } = "";

        public decimal Weight { get; set; }

        public string Size { get; set; } = "";

        private decimal _price;
        public decimal Price
        {
            get => _price;
            set
            {
                if (_price != value)
                {
                    _price = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        private int _bonus = 0;
        public int Bonus
        {
            get => _bonus;
            set
            {
                if (_bonus != value)
                {
                    _bonus = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public decimal TotalPrice => Math.Round(Price * (1 - Bonus / 100m), 2);

        public BitmapImage? Image => ImageData != null
            ? DrJaw.Utils.ImageHelper.BytesToBitmapImage(ImageData)
            : null;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class MSSQLTransferItem
    {
        public int Id { get; set; }
        public string Articul { get; set; } = "";
        public string Metal { get; set; } = "";
        public decimal Weight { get; set; }
        public string Size { get; set; } = "";
        public string Stone { get; set; } = "";
        public string InMartName { get; set; } = "";
    }
}