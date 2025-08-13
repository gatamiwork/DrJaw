namespace DrJaw.Models
{
    public sealed class MSSQLMetal
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed class MSSQLType
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed class MSSQLArticul
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int TypeId { get; set; }
        public int MetalId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed class MSSQLArticulImage
    {
        public int Id { get; set; }
        public int ArticulId { get; set; }
        public byte[]? Image { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public sealed class MSSQLMart
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed class MSSQLManufacturer
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed class MSSQLStone
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed class MSSQLUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Password { get; set; }
        public string Role { get; set; } = "USER";
        public bool Display { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed class MSSQLStatus
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed class MSSQLPaymentType
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed class MSSQLLom
    {
        public int Id { get; set; }
        public int MartId { get; set; }
        public decimal Weight { get; set; }          // DECIMAL(10,2)
        public decimal? PricePerGram { get; set; }   // DECIMAL(10,2) NULL
        public bool Receiving { get; set; } = true;  // BIT
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed class MSSQLCart
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MartId { get; set; }
        public int PaymentTypeId { get; set; }
        public DateTime? PurchaseDate { get; set; }  // NULL по схеме
        public decimal? Bonus { get; set; }          // DECIMAL(10,2) NULL
        public decimal? TotalSum { get; set; }       // DECIMAL(18,2) NULL
        public int? LomId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed class MSSQLItem
    {
        public int Id { get; set; }
        public int ArticulId { get; set; }
        public decimal Weight { get; set; }          // DECIMAL(10,2)
        public string Size { get; set; } = "";
        public int MartId { get; set; }
        public int? ManufacturerId { get; set; }
        public decimal Price { get; set; }           // DECIMAL(18,2)
        public int? StonesId { get; set; }
        public int? TransferMartId { get; set; }
        public bool ReadyToSold { get; set; }        // BIT
        public bool CloudType { get; set; } = true;  // BIT
        public int? CloudId { get; set; }
        public int? CartId { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public sealed class MSSQLCartItem
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ItemId { get; set; }
        public decimal? Bonus { get; set; }          // DECIMAL(10,2) NULL
        public int StatusId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
