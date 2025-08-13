using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DrJaw.Models
{
    // MainWindow - UserPanel
    public class DGMSSQLItem : INotifyPropertyChanged
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
    // UserPanel - AddItem
    public class MSSQLArticulByName
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int TypeId { get; set; }
        public int MetalId { get; set; }
        public byte[]? ImageData { get; set; }
    }
    // UserPanel - Return
    public class DGMSSQLReturnCartItem
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
    // UserPanel - TransferIn
    public class DGMSSQLTransferItem
    {
        public int Id { get; set; }
        public string Articul { get; set; } = "";
        public string Metal { get; set; } = "";
        public decimal Weight { get; set; }
        public string Size { get; set; } = "";
        public string Stone { get; set; } = "";
        public string InMartName { get; set; } = "";
    }
    // UserPanel - Cart
    public class DGMSSQLReadyToSold : INotifyPropertyChanged
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
    // MainMenu - Lom
    public class DGMSSQLLomItem
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
    // MainMenu - Orders
    public sealed class DGMSSQLOrders
    {
        public int CartId { get; set; }
        public int Id => CartId;
        public int UserId { get; set; }
        public int MartId { get; set; }
        public string PaymentType { get; set; } = "";
        public decimal? Bonus { get; set; }
        public decimal? TotalPrice { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public int? LomId { get; set; }
        public string Status { get; set; } = "";
    }
    public class DGMSSQLOrderItem
    {
        public byte[]? ImageData { get; set; }           // ai.Image AS ImageData
        public string Articul { get; set; } = "";        // a.Name AS Articul
        public decimal Weight { get; set; }              // i.Weight AS Weight
        public string Size { get; set; } = "";           // i.Size AS Size
        public string Manufacturer { get; set; } = "";   // m.Name AS Manufacturer
        public string Stone { get; set; } = "";          // s.Name AS Stone
        public decimal? ItemBonus { get; set; }          // ci.Bonus AS ItemBonus (DECIMAL)
        public string Comment { get; set; } = "";        // i.Comment AS Comment
        public string CiStatus { get; set; } = "";       // st.Name AS CiStatus
    }
    public sealed class MSSQLOrderTotals
    {
        public int UserId { get; set; }
        public int MartId { get; set; }
        public string Metal { get; set; } = "";
        public int ItemCount { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
