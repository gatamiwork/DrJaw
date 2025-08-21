using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrJaw.Models
{
    public sealed record CartCreate(
    int UserId,
    int MartId,
    int PaymentTypeId,
    DateTime PurchaseDateUtc,
    decimal CartBonus,          // суммарная скидка по чеку (в деньгах)
    decimal TotalSum,           // сумма чека после скидок
    int? LomId,                 // если есть, иначе null
    IReadOnlyList<CartCreateLine> Lines
);

    public sealed record CartCreateLine(
        int ItemId,
        decimal Bonus,              // скидка по одной позиции (в деньгах)
        int StatusId                // статус для CartItems (см. ниже)
    );
}
