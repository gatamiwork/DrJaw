using System.Collections.Generic;
using System.Threading.Tasks;
using DrJaw.Models;

namespace DrJaw.Services.MSSQL
{
    public sealed record DeleteBatchOutcome(int Selected, int MovedToCart, int Deleted);

    public interface IMssqlRepository
    {
        Task<IReadOnlyList<MSSQLUser>> GetUsersAsync();
        Task<IReadOnlyList<MSSQLMart>> GetMartsAsync();
        Task<bool> CheckPasswordAsync(int userId, string password);
        Task<IReadOnlyList<MSSQLMetal>> GetMetalsAsync();
        Task<IReadOnlyList<DGMSSQLItem>> GetItemsAsync(MSSQLMart mart, MSSQLMetal metal);
        Task<byte[]?> GetItemImageAsync(int itemId);
        Task<IReadOnlyList<MSSQLType>> GetTypesAsync();
        Task<IReadOnlyList<MSSQLStone>> GetStonesAsync();
        Task<IReadOnlyList<MSSQLManufacturer>> GetManufacturersAsync();
        Task<MSSQLArticul?> GetArticulByNameAsync(string name);
        Task<(int articulId, bool created)> EnsureArticulWithImageAsync(
        string name, int typeId, int metalId, byte[] imageBytes);
        Task<int> AddItemAsync(ItemCreate dto);
        Task<DeleteBatchOutcome> DeleteItemsByIdsAsync(int martId, IReadOnlyCollection<int> ids);
        Task TransferOutItemsByIdsAsync(int fromMartId, int toMartId, IReadOnlyCollection<int> ids);
        Task<int> CreateLomAsync(int userId, int martId, bool receiving, decimal weight, decimal? pricePerGram = null);
        Task<IReadOnlyList<DGMSSQLItem>> GetIncomingTransferItemsAsync(int martId);
        Task TransferInItemsByIdsAsync(int martId, IReadOnlyCollection<int> ids);
        Task MarkReadyToSoldAsync(IReadOnlyCollection<int> ids);
        Task<IReadOnlyList<DGMSSQLItem>> GetReadyToSellGroupsAsync(int martId);
        Task UnmarkReadyToSoldAsync(IEnumerable<int> ids);
        Task<IReadOnlyList<MSSQLPaymentType>> GetPaymentTypesAsync();
        Task<int> CreateCartWithItemsAsync(CartCreate dto);
        Task<int> GetReadyToSoldCountAsync(int martId);
        Task<int> GetIncomingTransferCountAsync(int martId);
        Task<IReadOnlyList<ReturnItemDto>> GetSoldItemsByDateAsync(int? martId, DateTime date, int? userId = null);
        Task ReturnItemsAsync(IEnumerable<int> itemIds);
        Task<(decimal openingWeight, decimal openingSum)>  GetLomOpeningAsync(DateTime beforeDate, int martId, int? userId);
        Task<IReadOnlyList<LomDto>> GetLomMovementsAsync(DateTime from, DateTime to, int martId, int? userId);

    }

}
