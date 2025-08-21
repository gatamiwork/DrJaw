// Services/MSSQL/DesignTimeRepo.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using DrJaw.Models;

namespace DrJaw.Services.MSSQL
{
    public sealed class DesignTimeRepo : IMssqlRepository
    {
        public Task<IReadOnlyList<MSSQLUser>> GetUsersAsync()
            => Task.FromResult<IReadOnlyList<MSSQLUser>>(new List<MSSQLUser>());

        public Task<IReadOnlyList<MSSQLMart>> GetMartsAsync()
            => Task.FromResult<IReadOnlyList<MSSQLMart>>(new List<MSSQLMart>());
        public Task<IReadOnlyList<MSSQLMetal>> GetMetalsAsync()
            => Task.FromResult<IReadOnlyList<MSSQLMetal>>(new List<MSSQLMetal>());
        public Task<IReadOnlyList<MSSQLType>> GetTypesAsync()
            => Task.FromResult<IReadOnlyList<MSSQLType>>(new List<MSSQLType>());

        public Task<bool> CheckPasswordAsync(int userId, string password)
            => Task.FromResult(false); // дизайн-тайм: всегда "неверный"
        public Task<IReadOnlyList<DGMSSQLItem>> GetItemsAsync(MSSQLMart mart, MSSQLMetal metal)
            => Task.FromResult<IReadOnlyList<DGMSSQLItem>>(new List<DGMSSQLItem>());
        public Task<byte[]?> GetItemImageAsync(int itemId) 
            => Task.FromResult<byte[]?>(null);
        public Task<IReadOnlyList<MSSQLStone>> GetStonesAsync()
            => Task.FromResult<IReadOnlyList<MSSQLStone>>(new List<MSSQLStone>());
        public Task<IReadOnlyList<MSSQLManufacturer>> GetManufacturersAsync()
    => Task.FromResult<IReadOnlyList<MSSQLManufacturer>>(new List<MSSQLManufacturer>());
        public Task<MSSQLArticul?> GetArticulByNameAsync(string name)
    => Task.FromResult<MSSQLArticul?>(null);
        public Task<(int articulId, bool created)> EnsureArticulWithImageAsync(
            string name, int typeId, int metalId, byte[] imageBytes)
            => throw new NotImplementedException();

        public Task<int> AddItemAsync(ItemCreate dto)
            => throw new NotImplementedException();
        public Task<DeleteBatchOutcome> DeleteItemsByIdsAsync(int martId, IReadOnlyCollection<int> ids)
            => Task.FromResult(new DeleteBatchOutcome(ids?.Count ?? 0, 0, ids?.Count ?? 0));
        public Task TransferOutItemsByIdsAsync(int fromMartId, int toMartId, IReadOnlyCollection<int> ids)
            => Task.CompletedTask;
        private static int _lomId = 1000;

        public Task<int> CreateLomAsync(int userId, int martId, bool receiving, decimal weight, decimal? pricePerGram = null)
        {
            // Возвращаем фиктивный Id, чтобы XAML/дизайнеру было чем довольствоваться
            var id = Interlocked.Increment(ref _lomId);
            return Task.FromResult(id);
        }
        public Task<IReadOnlyList<DGMSSQLItem>> GetIncomingTransferItemsAsync(int martId)
        {
            // демо-данные
            var demo = new List<DGMSSQLItem>
    {
        new DGMSSQLItem { Articul="A-100", Metal="Au", Price=100000m, Ids = { 201,202,203 } },
        new DGMSSQLItem { Articul="B-200", Metal="Ag", Price=15000m,  Ids = { 301 } },
    };
            return Task.FromResult<IReadOnlyList<DGMSSQLItem>>(demo);
        }

        public Task TransferInItemsByIdsAsync(int martId, IReadOnlyCollection<int> ids)
            => Task.CompletedTask;
        public Task MarkReadyToSoldAsync(IReadOnlyCollection<int> ids)
            => Task.CompletedTask;
        public Task<IReadOnlyList<DGMSSQLItem>> GetReadyToSellGroupsAsync(int martId)
        {
            var demo = new List<DGMSSQLItem>
    {
        new DGMSSQLItem { Articul="R-100", Size="17", Stones="Фианиты", Price=125000m, Ids = { 11,12,13 } },
        new DGMSSQLItem { Articul="C-250", Size="—",  Stones="—",       Price= 38000m, Ids = { 21 } },
    };
            return Task.FromResult<IReadOnlyList<DGMSSQLItem>>(demo);
        }
        public Task UnmarkReadyToSoldAsync(IEnumerable<int> ids) => Task.CompletedTask;
        public Task<IReadOnlyList<MSSQLPaymentType>> GetPaymentTypesAsync()
    => Task.FromResult<IReadOnlyList<MSSQLPaymentType>>(
        new List<MSSQLPaymentType>
        {
            new MSSQLPaymentType{ Id=1, Name="Наличные"},
            new MSSQLPaymentType{ Id=2, Name="Карта"},
        });
        public Task<int> CreateCartWithItemsAsync(CartCreate dto) => Task.FromResult(777);
        public Task<int> GetReadyToSoldCountAsync(int martId) => Task.FromResult(3);
        public Task<int> GetIncomingTransferCountAsync(int martId) => Task.FromResult(1);
        public Task<IReadOnlyList<ReturnItemDto>> GetSoldItemsByDateAsync(int? martId, DateTime date, int? userId = null)
            => Task.FromResult<IReadOnlyList<ReturnItemDto>>(new List<ReturnItemDto>());

        public Task ReturnItemsAsync(IEnumerable<int> itemIds)
            => Task.CompletedTask;
        public Task<(decimal openingWeight, decimal openingSum)> GetLomOpeningAsync(
            DateTime beforeDate, int martId, int? userId)
        {
            // Для дизайна: вернём какие-то стабильные числа,
            // игнорируя фильтры (или с учётом их — не критично).
            return Task.FromResult((openingWeight: 12.34m, openingSum: 12345m));
        }

        public Task<IReadOnlyList<LomDto>> GetLomMovementsAsync(
            DateTime from, DateTime to, int martId, int? userId)
        {
            var demo = new List<LomDto>
    {
        new LomDto { Date = from.AddDays(2), IsIn = true,  Weight = 10.5m, PricePerGram = 3000m, Amount = 31500m, UserName = "Иван", Comment = "приём" },
        new LomDto { Date = from.AddDays(5), IsIn = false, Weight = 3.2m,  PricePerGram = 3100m, Amount =  9920m, UserName = "Ольга", Comment = "списание" },
        new LomDto { Date = to.AddDays(-2), IsIn = true,   Weight = 1.0m,  PricePerGram = 3200m, Amount =  3200m, UserName = "Пётр",  Comment = "приём" },
    };
            return Task.FromResult<IReadOnlyList<LomDto>>(demo);
        }
    }
}
