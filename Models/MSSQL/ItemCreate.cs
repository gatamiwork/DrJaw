using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrJaw.Models
{
    public sealed class ItemCreate
    {
        public int ArticulId { get; set; }
        public decimal Weight { get; set; }          // DECIMAL(10,2)
        public string Size { get; set; } = "";       // NVARCHAR(20)
        public int MartId { get; set; }
        public int ManufacturerId { get; set; }      // обязателен
        public decimal Price { get; set; }           // DECIMAL(18,2)
        public int StoneId { get; set; }             // обязателен
        public string? Comment { get; set; }         // <= 255 символов
    }
}
