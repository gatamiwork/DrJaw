using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrJaw.Models
{
    public sealed class LomDto
    {
        public DateTime Date { get; set; }
        public bool IsIn { get; set; }                // true = приём лома, false = отгрузка
        public decimal Weight { get; set; }           // вес операции
        public decimal? PricePerGram { get; set; }    // цена за грамм (если применимо)
        public decimal Amount { get; set; }           // сумма операции
        public string? UserName { get; set; }         // кто оформил
        public string? Comment { get; set; }          // опционально
    }
}
