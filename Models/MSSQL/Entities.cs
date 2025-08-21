namespace DrJaw.Models
{
    public sealed class MSSQLUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Role { get; set; } = "USER";
        public bool Display { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public sealed class MSSQLMart
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public override string ToString() => Name;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
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
    public sealed class MSSQLArticul
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int TypeId { get; set; }
        public int MetalId { get; set; }
        public int ImageId { get; set; }
        public byte[]? Image { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public sealed class MSSQLPaymentType
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
