using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrJaw.Models
{
    public sealed class ReturnItemDto
    {
        public int ItemId { get; set; }
        public int CartId { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string PaymentTypeName { get; set; } = "";
        public string Articul { get; set; } = "";
        public string Metal { get; set; } = "";
        public string? Size { get; set; }
        public decimal Weight { get; set; }
        public string? Stones { get; set; }
        public decimal Price { get; set; }

        // новое:
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
    }
}
