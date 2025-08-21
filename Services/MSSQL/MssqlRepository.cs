using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using DrJaw.Models;

namespace DrJaw.Services.MSSQL
{
    public sealed class MssqlRepository : IMssqlRepository
    {
        private readonly IMssqlManager _mgr;
        public MssqlRepository(IMssqlManager mgr) => _mgr = mgr;

        public async Task<IReadOnlyList<MSSQLUser>> GetUsersAsync()
        {
            var list = new List<MSSQLUser>();
            await using var cn = _mgr.CreateConnection();
            await cn.OpenAsync();

            // TODO: замени SELECT на свой
            const string sql = @"SELECT Id, Name, Role, Display, CreatedAt, UpdatedAt FROM Users WHERE Display=1";
            await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
            await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            while (await rd.ReadAsync())
                list.Add(MssqlConverters.ToUser(rd));
            return list;
        }

        public async Task<IReadOnlyList<MSSQLMart>> GetMartsAsync()
        {
            var list = new List<MSSQLMart>();
            await using var cn = _mgr.CreateConnection();
            await cn.OpenAsync();

            // TODO: замени SELECT на свой
            const string sql = @"SELECT Id, Name FROM Marts ORDER BY Name DESC";
            await using var cmd = new SqlCommand(sql, (SqlConnection)cn);
            await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            while (await rd.ReadAsync())
                list.Add(MssqlConverters.ToMart(rd));
            return list;
        }
        public async Task<bool> CheckPasswordAsync(int userId, string password)
        {
            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();

            const string sql = @"SELECT 1 FROM Users WHERE Id = @Id AND Password = @Pwd";
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = userId });
            cmd.Parameters.Add(new SqlParameter("@Pwd", SqlDbType.NVarChar, 256) { Value = (object)password ?? DBNull.Value });

            var scalar = await cmd.ExecuteScalarAsync();
            return scalar != null; // есть строка → пароль верный
        }
        public async Task<IReadOnlyList<MSSQLMetal>> GetMetalsAsync()
        {
            var list = new List<MSSQLMetal>();
            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();

            const string sql = @"SELECT * FROM Metals";
            await using var cmd = new SqlCommand(sql, cn);
            await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            while (await rd.ReadAsync())
                list.Add(MssqlConverters.ToMetal(rd));
            return list;
        }
        public async Task<IReadOnlyList<DGMSSQLItem>> GetItemsAsync(MSSQLMart mart, MSSQLMetal metal)
        {
            var result = new List<DGMSSQLItem>();

            const string sql = @"
WITH F AS (
    SELECT
        i.Id,
        i.ArticulId,
        i.MartId,
        i.ManufacturerId,
        i.StonesId,
        i.Weight,
        i.Size,
        i.Price,
        i.Comment,
        a.Name  AS Articul,
        t.Name  AS [Type],
        m.Name  AS Metal,
        s.Name  AS Stones,
        mf.Name AS Manufacturer
    FROM Items i
    JOIN Articuls a  ON a.Id = i.ArticulId
    JOIN Types    t  ON t.Id = a.TypeId
    JOIN Metals   m  ON m.Id = a.MetalId
    LEFT JOIN Stones        s  ON s.Id  = i.StonesId
    LEFT JOIN Manufacturers mf ON mf.Id = i.ManufacturerId
    WHERE i.MartId = @MartId
      AND i.CartId IS NULL
      AND i.TransferMartId IS NULL
      AND i.ReadyToSold = 0
      AND m.Id = @MetalId
)
SELECT
    -- представительные поля (не в агрегате)
    MAX(F.[Type])        AS [Type],
    MAX(F.[Metal])       AS [Metal],
    MAX(F.Articul)       AS Articul,
    MAX(F.Weight)        AS Weight,
    MAX(F.Size)          AS Size,
    MAX(F.Stones)        AS Stones,
    MAX(F.Comment)       AS Comment,
    MAX(F.Price)         AS Price,
    MAX(F.Manufacturer)  AS Manufacturer,
    -- список Id через STRING_AGG (будем сортировать в C#)
    STRING_AGG(CONVERT(varchar(12), F.Id), ',') AS IdsCsv,
    MAX(F.Id)            AS MaxIdForOrder
FROM F
GROUP BY
    F.ArticulId, F.Weight, F.Size, F.MartId, F.ManufacturerId, F.Price, F.StonesId, F.Comment
ORDER BY MaxIdForOrder DESC;";

            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();

            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add(new SqlParameter("@MartId", SqlDbType.Int) { Value = mart.Id });
            cmd.Parameters.Add(new SqlParameter("@MetalId", SqlDbType.Int) { Value = metal.Id });

            await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            while (await rd.ReadAsync())
            {
                // индексы колонок ровно как в SELECT:
                var item = new DGMSSQLItem
                {
                    Type = rd.GetString(0),
                    Metal = rd.GetString(1),
                    Articul = rd.GetString(2),
                    Weight = rd.IsDBNull(3) ? 0m : rd.GetDecimal(3),
                    Size = rd.IsDBNull(4) ? null : rd.GetString(4),
                    Stones = rd.IsDBNull(5) ? null : rd.GetString(5),
                    Comment = rd.IsDBNull(6) ? null : rd.GetString(6),
                    Price = rd.IsDBNull(7) ? 0m : rd.GetDecimal(7),
                    Manufacturer = rd.IsDBNull(8) ? null : rd.GetString(8)
                };

                // парсим IdsCsv и сортируем по убыванию (как раньше по MAX(Id))
                var idsCsv = rd.IsDBNull(9) ? "" : rd.GetString(9);
                if (!string.IsNullOrWhiteSpace(idsCsv))
                {
                    item.Ids.AddRange(
                        idsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => int.TryParse(s, out var id) ? id : 0)
                              .Where(id => id > 0)
                              .OrderByDescending(id => id)
                    );
                }

                result.Add(item);
                // колонка [10] MaxIdForOrder нам не нужна в объект, только для ORDER BY
            }

            return result;
        }
        public async Task<byte[]?> GetItemImageAsync(int itemId)
        {
            const string sql = @"
                SELECT TOP (1) ai.Image
                FROM ArticulImages ai
                JOIN Items i ON i.ArticulId = ai.ArticulId
                WHERE i.Id = @Id";

            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();

            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = itemId });

            return await cmd.ExecuteScalarAsync() as byte[];

        }
        public async Task<IReadOnlyList<MSSQLType>> GetTypesAsync()
        {
            var list = new List<MSSQLType>();
            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();

            const string sql = @"SELECT id, Name, CreatedAt, UpdatedAt FROM dbo.Types ORDER BY Name";
            await using var cmd = new SqlCommand(sql, cn);
            await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);

            while (await rd.ReadAsync())
            {
                // или маппинг напрямую:
                list.Add(new MSSQLType
                {
                    Id = rd.GetInt32(rd.GetOrdinal("id")),
                    Name = rd.GetString(rd.GetOrdinal("Name")),
                    CreatedAt = rd.GetDateTime(rd.GetOrdinal("CreatedAt")),
                    UpdatedAt = rd.IsDBNull(rd.GetOrdinal("UpdatedAt"))
                                ? (DateTime?)null
                                : rd.GetDateTime(rd.GetOrdinal("UpdatedAt"))
                });
            }

            return list;
        }
        public async Task<IReadOnlyList<MSSQLStone>> GetStonesAsync()
        {
            var list = new List<MSSQLStone>();
            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();

            const string sql = @"SELECT id, Name, CreatedAt, UpdatedAt FROM dbo.Stones ORDER BY Name";
            await using var cmd = new SqlCommand(sql, cn);
            await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);

            while (await rd.ReadAsync())
            {
                list.Add(new MSSQLStone
                {
                    Id = rd.GetInt32(rd.GetOrdinal("id")),
                    Name = rd.GetString(rd.GetOrdinal("Name")),
                    CreatedAt = rd.IsDBNull(rd.GetOrdinal("CreatedAt"))
                                ? DateTime.MinValue
                                : rd.GetDateTime(rd.GetOrdinal("CreatedAt")),
                    UpdatedAt = rd.IsDBNull(rd.GetOrdinal("UpdatedAt"))
                                ? (DateTime?)null
                                : rd.GetDateTime(rd.GetOrdinal("UpdatedAt"))
                });
                // Или, если есть конвертер:
                // list.Add(MssqlConverters.ToStone(rd));
            }

            return list;
        }
        public async Task<IReadOnlyList<MSSQLManufacturer>> GetManufacturersAsync()
        {
            var list = new List<MSSQLManufacturer>();
            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();

            const string sql = @"SELECT id, Name, CreatedAt, UpdatedAt FROM dbo.Manufacturers ORDER BY Name";
            await using var cmd = new SqlCommand(sql, cn);
            await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);

            while (await rd.ReadAsync())
            {
                list.Add(new MSSQLManufacturer
                {
                    Id = rd.GetInt32(rd.GetOrdinal("id")),
                    Name = rd.GetString(rd.GetOrdinal("Name")),
                    CreatedAt = rd.IsDBNull(rd.GetOrdinal("CreatedAt"))
                                ? DateTime.MinValue
                                : rd.GetDateTime(rd.GetOrdinal("CreatedAt")),
                    UpdatedAt = rd.IsDBNull(rd.GetOrdinal("UpdatedAt"))
                                ? (DateTime?)null
                                : rd.GetDateTime(rd.GetOrdinal("UpdatedAt"))
                });
                // либо, если есть конвертер:
                // list.Add(MSSQLConverters.ToManufacturer(rd));
            }

            return list;
        }
        public async Task<MSSQLArticul?> GetArticulByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();

            const string sql = @"
                SELECT TOP (1)
                    a.Id, a.Name, a.TypeId, a.MetalId,
                    ai.Id AS ImageId,
                    ai.Image AS Image
                FROM dbo.Articuls a
                LEFT JOIN dbo.ArticulImages ai ON ai.ArticulId = a.Id
                WHERE a.Name = @name;";

            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 200) { Value = name.Trim() });

            await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            if (await rd.ReadAsync())
            {
                return new MSSQLArticul
                {
                    Id = rd.GetInt32(rd.GetOrdinal("Id")),
                    Name = rd.GetString(rd.GetOrdinal("Name")),
                    TypeId = rd.GetInt32(rd.GetOrdinal("TypeId")),
                    MetalId = rd.GetInt32(rd.GetOrdinal("MetalId")),
                    ImageId = rd.IsDBNull(rd.GetOrdinal("ImageId")) ? 0 : rd.GetInt32(rd.GetOrdinal("ImageId")),
                    Image = rd.IsDBNull(rd.GetOrdinal("Image")) ? null : (byte[])rd["Image"]
                };
            }
            return null;
        }
        public async Task<(int articulId, bool created)> EnsureArticulWithImageAsync(
        string name, int typeId, int metalId, byte[] imageBytes)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Empty articul", nameof(name));
            if (imageBytes == null || imageBytes.Length == 0) throw new ArgumentException("Image required", nameof(imageBytes));

            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();

            try
            {
                // 1) Пытаемся найти по имени (строгое совпадение)
                int articulId;
                const string sqlSelect = @"SELECT Id FROM dbo.Articuls WHERE Name = @name";
                await using (var cmd = new SqlCommand(sqlSelect, cn, (SqlTransaction)tx))
                {
                    cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 100) { Value = name.Trim() });
                    var obj = await cmd.ExecuteScalarAsync();
                    if (obj != null && obj != DBNull.Value)
                    {
                        articulId = Convert.ToInt32(obj);

                        // Не трогаем Type/Metal/Image (по твоей логике UI они залочены)
                        await tx.CommitAsync();
                        return (articulId, false);
                    }
                }

                // 2) Пробуем вставить новый артикул
                const string sqlInsertArt = @"
INSERT INTO dbo.Articuls (Name, TypeId, MetalId, CreatedAt)
VALUES (@name, @typeId, @metalId, SYSUTCDATETIME());
SELECT CAST(SCOPE_IDENTITY() AS int);";
                try
                {
                    await using var cmd = new SqlCommand(sqlInsertArt, cn, (SqlTransaction)tx);
                    cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 100) { Value = name.Trim() });
                    cmd.Parameters.Add(new SqlParameter("@typeId", SqlDbType.Int) { Value = typeId });
                    cmd.Parameters.Add(new SqlParameter("@metalId", SqlDbType.Int) { Value = metalId });
                    articulId = (int)(await cmd.ExecuteScalarAsync())!;
                }
                catch (SqlException ex) when (ex.Number is 2627 or 2601) // unique violation
                {
                    // кто-то успел вставить одновременно — читаем Id
                    await using var cmd = new SqlCommand(sqlSelect, cn, (SqlTransaction)tx);
                    cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 100) { Value = name.Trim() });
                    articulId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // 3) Записываем картинку для артикула (если строки ещё нет — вставляем)
                const string sqlUpsertImg = @"
IF EXISTS (SELECT 1 FROM dbo.ArticulImages WHERE ArticulId = @artId)
    UPDATE dbo.ArticulImages
    SET Image = @img, UploadedAt = SYSUTCDATETIME()
    WHERE ArticulId = @artId;
ELSE
    INSERT INTO dbo.ArticulImages (ArticulId, Image, UploadedAt)
    VALUES (@artId, @img, SYSUTCDATETIME());";

                await using (var cmd = new SqlCommand(sqlUpsertImg, cn, (SqlTransaction)tx))
                {
                    cmd.Parameters.Add(new SqlParameter("@artId", SqlDbType.Int) { Value = articulId });
                    cmd.Parameters.Add(new SqlParameter("@img", SqlDbType.VarBinary, -1) { Value = imageBytes });
                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return (articulId, true);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        public async Task<int> AddItemAsync(ItemCreate dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Size)) throw new ArgumentException("Size is required", nameof(dto.Size));
            // ограничим комментарий 255
            var comment = dto.Comment;
            if (comment != null && comment.Length > 255) comment = comment[..255];

            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();

            const string sql = @"
INSERT INTO dbo.Items
    (ArticulId, Weight, Size, MartId, ManufacturerId, Price, StonesId, Comment, CreatedAt)
VALUES
    (@ArticulId, @Weight, @Size, @MartId, @ManufacturerId, @Price, @StonesId, @Comment, SYSUTCDATETIME());
SELECT CAST(SCOPE_IDENTITY() AS int);";

            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add(new SqlParameter("@ArticulId", SqlDbType.Int) { Value = dto.ArticulId });
            cmd.Parameters.Add(new SqlParameter("@Weight", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = dto.Weight });
            cmd.Parameters.Add(new SqlParameter("@Size", SqlDbType.NVarChar, 20) { Value = dto.Size });
            cmd.Parameters.Add(new SqlParameter("@MartId", SqlDbType.Int) { Value = dto.MartId });
            cmd.Parameters.Add(new SqlParameter("@ManufacturerId", SqlDbType.Int) { Value = dto.ManufacturerId });
            cmd.Parameters.Add(new SqlParameter("@Price", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = dto.Price });
            cmd.Parameters.Add(new SqlParameter("@StonesId", SqlDbType.Int) { Value = dto.StoneId });
            cmd.Parameters.Add(new SqlParameter("@Comment", SqlDbType.NVarChar, 255) { Value = (object?)comment ?? DBNull.Value });

            var id = (int)(await cmd.ExecuteScalarAsync())!;
            return id;
        }
        public async Task<DeleteBatchOutcome> DeleteItemsByIdsAsync(int martId, IReadOnlyCollection<int> ids)
        {
            if (ids is null || ids.Count == 0)
                return new DeleteBatchOutcome(0, 0, 0);

            // уберём дубликаты, чтобы не считать дважды
            var uniqueIds = ids.Distinct().ToList();

            // Сформируем IN (@id0,@id1,...)
            var pNames = new List<string>(uniqueIds.Count);
            var parameters = new List<SqlParameter>
            {
                new("@MartId", SqlDbType.Int) { Value = martId }
            };

            for (int i = 0; i < uniqueIds.Count; i++)
            {
                string pn = "@id" + i;
                pNames.Add(pn);
                parameters.Add(new SqlParameter(pn, SqlDbType.Int) { Value = uniqueIds[i] });
            }

            var inClause = string.Join(",", pNames);

            var sql = new StringBuilder(@"
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

IF OBJECT_ID('tempdb..#picked') IS NOT NULL DROP TABLE #picked;
CREATE TABLE #picked (Id INT PRIMARY KEY, HasCart BIT NOT NULL);

INSERT INTO #picked(Id, HasCart)
SELECT i.Id,
       CASE WHEN EXISTS (SELECT 1 FROM CartItems ci WHERE ci.ItemId = i.Id) THEN 1 ELSE 0 END
FROM Items i
WHERE i.MartId = @MartId
  AND i.Id IN (")
            .Append(inClause)
            .Append(@");

-- Переносим тем, у кого есть CartItems (берём последний CartId)
UPDATE i
   SET i.CartId    = x.CartId,
       i.UpdatedAt = SYSUTCDATETIME()
FROM Items i
JOIN (
    SELECT p.Id,
           (SELECT TOP(1) ci.CartId
            FROM CartItems ci
            WHERE ci.ItemId = p.Id
            ORDER BY ci.CartId DESC) AS CartId
    FROM #picked p
    WHERE p.HasCart = 1
) x ON x.Id = i.Id;

-- Остальных удаляем
DELETE i
FROM Items i
JOIN #picked p ON p.Id = i.Id
WHERE p.HasCart = 0;

-- Итоги
SELECT
    (SELECT COUNT(*) FROM #picked)               AS Selected,
    (SELECT COUNT(*) FROM #picked WHERE HasCart=1) AS MovedToCart,
    (SELECT COUNT(*) FROM #picked WHERE HasCart=0) AS Deleted;

COMMIT;
");

            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();

            await using var cmd = new SqlCommand(sql.ToString(), cn)
            {
                CommandType = CommandType.Text
            };
            cmd.Parameters.AddRange(parameters.ToArray());

            int selected = 0, moved = 0, deleted = 0;
            await using (var rd = await cmd.ExecuteReaderAsync())
            {
                if (await rd.ReadAsync())
                {
                    selected = rd.IsDBNull(0) ? 0 : rd.GetInt32(0);
                    moved = rd.IsDBNull(1) ? 0 : rd.GetInt32(1);
                    deleted = rd.IsDBNull(2) ? 0 : rd.GetInt32(2);
                }
            }

            return new DeleteBatchOutcome(selected, moved, deleted);
        }
        public async Task TransferOutItemsByIdsAsync(int fromMartId, int toMartId, IReadOnlyCollection<int> ids)
        {
            if (ids == null || ids.Count == 0) return;

            var distinct = ids.Distinct().ToList();
            var p = new List<SqlParameter>
    {
        new("@FromMart", SqlDbType.Int){Value = fromMartId},
        new("@ToMart",   SqlDbType.Int){Value = toMartId},
    };
            var names = new List<string>();
            for (int i = 0; i < distinct.Count; i++)
            {
                var n = "@id" + i;
                names.Add(n);
                p.Add(new SqlParameter(n, SqlDbType.Int) { Value = distinct[i] });
            }
            var inClause = string.Join(",", names);

            var sql = $@"
                SET NOCOUNT ON;
                SET XACT_ABORT ON;
                BEGIN TRAN;

                UPDATE i
                   SET i.TransferMartId = @ToMart,
                       i.UpdatedAt      = SYSUTCDATETIME()
                FROM Items i
                WHERE i.MartId = @FromMart
                  AND i.Id IN ({inClause})
                  AND i.CartId IS NULL
                  AND i.TransferMartId IS NULL
                  AND i.ReadyToSold = 0;

                COMMIT;";

            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddRange(p.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }
        public async Task<int> CreateLomAsync(int userId, int martId, bool receiving, decimal weight, decimal? pricePerGram = null)
        {
            const string sql = @"
INSERT INTO Lom (MartId, Weight, PricePerGram, Receiving, UserId, CreatedAt)
OUTPUT INSERTED.Id
VALUES (@martId, @weight, @pricePerGram, @receiving, @userId, SYSUTCDATETIME());";

            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();

            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@martId", martId);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@receiving", receiving ? 1 : 0);
            cmd.Parameters.AddWithValue("@weight", weight);
            cmd.Parameters.AddWithValue("@pricePerGram", (object?)pricePerGram ?? DBNull.Value);

            var idObj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(idObj);
        }
        public async Task<IReadOnlyList<DGMSSQLItem>> GetIncomingTransferItemsAsync(int martId)
        {
            // Группируем по ключевым полям и собираем Ids
            const string sql = @"
                SELECT t.Name      AS [Type],
                       m.Name          AS Metal,
                       a.Name          AS Articul,
                       i.Price,
                       i.Weight,
                       i.Size,
                       s.Name          AS Stones,
                       mf.Name         AS Manufacturer,
                       STRING_AGG(CAST(i.Id AS varchar(20)), ',') AS IdsCsv
                FROM Items i
                JOIN Articuls a    ON a.Id = i.ArticulId
                JOIN Metals   m    ON m.Id = a.MetalId
                LEFT JOIN Types t  ON t.Id = a.TypeId
                LEFT JOIN Stones s ON s.Id = i.StonesId
                LEFT JOIN Manufacturers mf ON mf.Id = i.ManufacturerId
                WHERE i.TransferMartId = @MartId
                  AND i.CartId IS NULL
                  AND i.ReadyToSold = 0
                GROUP BY t.Name, m.Name, a.Name, i.Price, i.Weight, i.Size, s.Name, mf.Name;";

            var list = new List<DGMSSQLItem>();
            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add(new SqlParameter("@MartId", SqlDbType.Int) { Value = martId });

            await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            while (await rd.ReadAsync())
            {
                var item = new DGMSSQLItem
                {
                    Type = rd["Type"] as string ?? "",
                    Metal = rd["Metal"] as string ?? "",
                    Articul = rd["Articul"] as string ?? "",
                    Price = rd["Price"] is decimal p ? p : 0m,
                    Weight = rd["Weight"] is decimal w ? w : 0m,
                    Size = rd["Size"] as string,
                    Stones = rd["Stones"] as string,
                    Manufacturer = rd["Manufacturer"] as string
                };
                var csv = rd["IdsCsv"] as string ?? "";
                foreach (var s in csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    if (int.TryParse(s, out var id)) item.Ids.Add(id);

                list.Add(item);
            }
            return list;
        }

        public async Task TransferInItemsByIdsAsync(int martId, IReadOnlyCollection<int> ids)
        {
            if (ids == null || ids.Count == 0) return;
            var distinct = ids.Distinct().ToList();

            var p = new List<SqlParameter> { new("@MartId", SqlDbType.Int) { Value = martId } };
            var names = new List<string>();
            for (int i = 0; i < distinct.Count; i++)
            {
                var n = "@id" + i;
                names.Add(n);
                p.Add(new SqlParameter(n, SqlDbType.Int) { Value = distinct[i] });
            }
            var inClause = string.Join(",", names);

            var sql = $@"
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

UPDATE i
   SET i.MartId = @MartId,
       i.TransferMartId = NULL,
       i.UpdatedAt = SYSUTCDATETIME()
FROM Items i
WHERE i.Id IN ({inClause})
  AND i.TransferMartId = @MartId
  AND i.CartId IS NULL
  AND i.ReadyToSold = 0;

COMMIT;";

            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddRange(p.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }
        public async Task MarkReadyToSoldAsync(IReadOnlyCollection<int> ids)
        {
            if (ids == null || ids.Count == 0) return;

            var distinct = ids.Distinct().ToList();

            var p = new List<SqlParameter>();
            var names = new List<string>();
            for (int i = 0; i < distinct.Count; i++)
            {
                var n = "@id" + i;
                names.Add(n);
                p.Add(new SqlParameter(n, SqlDbType.Int) { Value = distinct[i] });
            }
            var inClause = string.Join(",", names);

            var sql = $@"
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

UPDATE Items
   SET ReadyToSold = 1,
       UpdatedAt   = SYSUTCDATETIME()
 WHERE Id IN ({inClause});

COMMIT;";

            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddRange(p.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }
        public async Task<IReadOnlyList<DGMSSQLItem>> GetReadyToSellGroupsAsync(int martId)
        {
            const string sql = @"
SELECT
    t.Name           AS [Type],
    m.Name           AS Metal,
    a.Name           AS Articul,
    i.Size,
    s.Name           AS Stones,
    i.Price,
    STRING_AGG(CAST(i.Id AS varchar(20)), ',') AS IdsCsv
FROM Items i
JOIN Articuls a     ON a.Id = i.ArticulId
JOIN Metals   m     ON m.Id = a.MetalId
JOIN Types    t     ON t.Id = a.TypeId
LEFT JOIN Stones s  ON s.Id = i.StonesId
WHERE i.MartId = @MartId              -- ← ВАЖНО: только текущий магазин
  AND i.ReadyToSold = 1
  AND i.CartId IS NULL
  AND i.TransferMartId IS NULL
GROUP BY t.Name, m.Name, a.Name, i.Size, s.Name, i.Price
ORDER BY a.Name;";

            var list = new List<DGMSSQLItem>();
            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add(new SqlParameter("@MartId", SqlDbType.Int) { Value = martId });

            await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            while (await rd.ReadAsync())
            {
                var item = new DGMSSQLItem
                {
                    Type = rd["Type"] as string ?? "",
                    Metal = rd["Metal"] as string ?? "",
                    Articul = rd["Articul"] as string ?? "",
                    Size = rd["Size"] as string,
                    Stones = rd["Stones"] as string,
                    Price = rd["Price"] is decimal p ? p : 0m
                };

                var csv = rd["IdsCsv"] as string ?? "";
                foreach (var s in csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    if (int.TryParse(s, out var id)) item.Ids.Add(id);

                list.Add(item);
            }
            return list;
        }
        public async Task UnmarkReadyToSoldAsync(IEnumerable<int> ids)
        {
            var idList = ids?.Distinct().ToList() ?? new List<int>();
            if (idList.Count == 0) return;

            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();

            // параметризованный IN (@p0,@p1,...)
            var pNames = idList.Select((_, i) => "@p" + i).ToArray();
            var sql = $"UPDATE Items SET ReadyToSold = 0 WHERE Id IN ({string.Join(",", pNames)})";

            await using var cmd = new SqlCommand(sql, cn);
            for (int i = 0; i < idList.Count; i++)
                cmd.Parameters.Add(new SqlParameter(pNames[i], SqlDbType.Int) { Value = idList[i] });

            await cmd.ExecuteNonQueryAsync();
        }
        public async Task<IReadOnlyList<MSSQLPaymentType>> GetPaymentTypesAsync()
        {
            var list = new List<MSSQLPaymentType>();
            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();

            const string sql = "SELECT Id, Name FROM PaymentTypes ORDER BY Name";
            await using var cmd = new SqlCommand(sql, cn);
            await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            while (await rd.ReadAsync())
                list.Add(new MSSQLPaymentType { Id = rd.GetInt32(0), Name = rd.GetString(1) });

            return list;
        }
        public async Task<int> CreateCartWithItemsAsync(CartCreate dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            if (dto.Lines is null || dto.Lines.Count == 0)
                throw new InvalidOperationException("Пустой список позиций для корзины.");

            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();
            await using var tx = cn.BeginTransaction();

            try
            {
                // 1) Cart
                const string sqlCart = @"
INSERT INTO Cart (UserId, MartId, PaymentTypeId, PurchaseDate, Bonus, TotalSum, LomId, CreatedAt)
OUTPUT INSERTED.Id
VALUES (@UserId, @MartId, @PaymentTypeId, @PurchaseDate, @Bonus, @TotalSum, @LomId, SYSUTCDATETIME());";

                await using var cmdCart = new SqlCommand(sqlCart, cn, tx);
                cmdCart.Parameters.Add("@UserId", SqlDbType.Int).Value = dto.UserId;
                cmdCart.Parameters.Add("@MartId", SqlDbType.Int).Value = dto.MartId;
                cmdCart.Parameters.Add("@PaymentTypeId", SqlDbType.Int).Value = dto.PaymentTypeId;
                cmdCart.Parameters.Add("@PurchaseDate", SqlDbType.DateTime2).Value = dto.PurchaseDateUtc;
                cmdCart.Parameters.Add("@Bonus", SqlDbType.Decimal).Value = dto.CartBonus;
                cmdCart.Parameters.Add("@TotalSum", SqlDbType.Decimal).Value = dto.TotalSum;
                cmdCart.Parameters.Add("@LomId", SqlDbType.Int).Value = (object?)dto.LomId ?? DBNull.Value;

                var cartIdObj = await cmdCart.ExecuteScalarAsync();
                var cartId = Convert.ToInt32(cartIdObj);

                // 2) CartItems
                const string sqlCartItem = @"
INSERT INTO CartItems (CartId, ItemId, Bonus, StatusId, CreatedAt)
VALUES (@CartId, @ItemId, @Bonus, @StatusId, SYSUTCDATETIME());";

                await using var cmdItem = new SqlCommand(sqlCartItem, cn, tx);
                cmdItem.Parameters.Add("@CartId", SqlDbType.Int).Value = cartId;
                var pItemId = cmdItem.Parameters.Add("@ItemId", SqlDbType.Int);
                var pBonus = cmdItem.Parameters.Add("@Bonus", SqlDbType.Decimal);
                var pStatusId = cmdItem.Parameters.Add("@StatusId", SqlDbType.Int);

                foreach (var line in dto.Lines)
                {
                    pItemId.Value = line.ItemId;
                    pBonus.Value = line.Bonus;
                    pStatusId.Value = line.StatusId;
                    await cmdItem.ExecuteNonQueryAsync();
                }

                // 3) Проставляем CartId у Items и снимаем ReadyToSold
                const string sqlUpd = @"UPDATE Items SET CartId = @CartId, ReadyToSold = 0 WHERE Id = @Id;";
                await using var cmdUpd = new SqlCommand(sqlUpd, cn, tx);
                cmdUpd.Parameters.Add("@CartId", SqlDbType.Int).Value = cartId;
                var pUpdId = cmdUpd.Parameters.Add("@Id", SqlDbType.Int);

                foreach (var line in dto.Lines)
                {
                    pUpdId.Value = line.ItemId;
                    await cmdUpd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return cartId;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        public async Task<int> GetReadyToSoldCountAsync(int martId)
        {
            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();
            const string sql = @"SELECT COUNT(*) FROM Items WHERE MartId=@MartId AND ReadyToSold=1 AND CartId IS NULL;";
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add(new SqlParameter("@MartId", SqlDbType.Int) { Value = martId });
            var obj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(obj);
        }

        public async Task<int> GetIncomingTransferCountAsync(int martId)
        {
            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();
            const string sql = @"SELECT COUNT(*) FROM Items WHERE TransferMartId=@MartId;";
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add(new SqlParameter("@MartId", SqlDbType.Int) { Value = martId });
            var obj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(obj);
        }

        public async Task<IReadOnlyList<ReturnItemDto>> GetSoldItemsByDateAsync(int? martId, DateTime date, int? userId = null)
        {
            var list = new List<ReturnItemDto>();
            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();

            const string sql = @"
SELECT  i.Id               AS ItemId,
        c.Id               AS CartId,
        c.PurchaseDate,
        pt.Name            AS PaymentTypeName,
        a.Name             AS Articul,
        i.Size,
        s.Name             AS Stones,
        m.Name             AS Metal,
        i.Weight,
        i.Price
FROM Items i
JOIN CartItems    ci ON ci.ItemId   = i.Id
JOIN Cart         c  ON c.Id        = ci.CartId
JOIN PaymentTypes pt ON pt.Id       = c.PaymentTypeId
JOIN Articuls     a  ON a.Id        = i.ArticulId
JOIN Metals       m  ON m.Id        = a.MetalId
LEFT JOIN Stones  s  ON s.Id        = i.StonesId
WHERE (@MartId IS NULL OR i.MartId = @MartId)
  AND c.PurchaseDate >= @Date
  AND c.PurchaseDate <  DATEADD(DAY, 1, @Date)
  AND (@UserId IS NULL OR c.UserId = @UserId);";

            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add(new SqlParameter("@MartId", SqlDbType.Int) { Value = (object?)martId ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@Date", SqlDbType.Date) { Value = date.Date });
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = (object?)userId ?? DBNull.Value });

            await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            while (await rd.ReadAsync())
            {
                list.Add(new ReturnItemDto
                {
                    ItemId = rd.GetInt32(rd.GetOrdinal("ItemId")),
                    CartId = rd.GetInt32(rd.GetOrdinal("CartId")),
                    PurchaseDate = rd.GetDateTime(rd.GetOrdinal("PurchaseDate")),
                    PaymentTypeName = rd.GetString(rd.GetOrdinal("PaymentTypeName")),
                    Articul = rd.GetString(rd.GetOrdinal("Articul")),
                    Size = rd.IsDBNull(rd.GetOrdinal("Size")) ? null : rd.GetString(rd.GetOrdinal("Size")),
                    Stones = rd.IsDBNull(rd.GetOrdinal("Stones")) ? null : rd.GetString(rd.GetOrdinal("Stones")),
                    Metal = rd.GetString(rd.GetOrdinal("Metal")),
                    Weight = rd.GetDecimal(rd.GetOrdinal("Weight")),
                    Price = rd.GetDecimal(rd.GetOrdinal("Price")),
                });
            }
            return list;
        }
        public async Task ReturnItemsAsync(IEnumerable<int> itemIds)
        {
            var ids = (itemIds ?? Array.Empty<int>()).Distinct().ToList();
            if (ids.Count == 0) return;

            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();

            try
            {
                const string sqlUpdateItem = @"UPDATE Items SET CartId = NULL, ReadyToSold = 0, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id;";
                const string sqlDeleteCI = @"DELETE FROM CartItems WHERE ItemId = @Id;";

                foreach (var id in ids)
                {
                    await using (var cmd1 = new SqlCommand(sqlUpdateItem, cn, (SqlTransaction)tx))
                    {
                        cmd1.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });
                        await cmd1.ExecuteNonQueryAsync();
                    }
                    await using (var cmd2 = new SqlCommand(sqlDeleteCI, cn, (SqlTransaction)tx))
                    {
                        cmd2.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });
                        await cmd2.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        public async Task<(decimal openingWeight, decimal openingSum)> GetLomOpeningAsync(
    DateTime beforeDate, int martId, int? userId)
        {
            const string sql = @"
SELECT
    OpeningWeight = ISNULL(SUM(CASE WHEN l.Receiving = 1 THEN l.Weight ELSE -l.Weight END), 0),
    OpeningSum    = ISNULL(SUM(CASE WHEN l.Receiving = 1
                                    THEN l.Weight * ISNULL(l.PricePerGram, 0)
                                    ELSE -l.Weight * ISNULL(l.PricePerGram, 0)
                               END), 0)
FROM Lom l
WHERE ( @MartId = 0 OR l.MartId = @MartId )
  AND ( @UserId IS NULL OR l.UserId = @UserId )
  AND CAST(l.CreatedAt AS date) < @BeforeDate;";

            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();

            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add(new SqlParameter("@MartId", SqlDbType.Int) { Value = martId });
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = (object?)userId ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@BeforeDate", SqlDbType.Date) { Value = beforeDate.Date });

            await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            if (await rd.ReadAsync())
            {
                var w = rd.IsDBNull(0) ? 0m : rd.GetDecimal(0);
                var s = rd.IsDBNull(1) ? 0m : rd.GetDecimal(1);
                return (w, s);
            }
            return (0m, 0m);
        }

        public async Task<IReadOnlyList<LomDto>> GetLomMovementsAsync(
            DateTime from, DateTime to, int martId, int? userId)
        {
            const string sql = @"
SELECT
    Date         = CAST(l.CreatedAt AS date),
    IsIn         = l.Receiving,
    l.Weight,
    l.PricePerGram,
    Amount       = l.Weight * ISNULL(l.PricePerGram, 0),
    u.Name       AS UserName,
    CAST(NULL AS nvarchar(255)) AS Comment
FROM Lom l
LEFT JOIN Users u ON u.Id = l.UserId
WHERE ( @MartId = 0 OR l.MartId = @MartId )
  AND ( @UserId IS NULL OR l.UserId = @UserId )
  AND CAST(l.CreatedAt AS date) BETWEEN @From AND @To
ORDER BY l.CreatedAt;";

            var list = new List<LomDto>();

            await using var cn = (SqlConnection)_mgr.CreateConnection();
            await cn.OpenAsync();

            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add(new SqlParameter("@MartId", SqlDbType.Int) { Value = martId });
            cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = (object?)userId ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@From", SqlDbType.Date) { Value = from.Date });
            cmd.Parameters.Add(new SqlParameter("@To", SqlDbType.Date) { Value = to.Date });

            await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            while (await rd.ReadAsync())
            {
                list.Add(new LomDto
                {
                    Date = rd.GetDateTime(0),
                    IsIn = rd.GetBoolean(1),
                    Weight = rd.GetDecimal(2),
                    PricePerGram = rd.IsDBNull(3) ? (decimal?)null : rd.GetDecimal(3),
                    Amount = rd.GetDecimal(4),
                    UserName = rd.IsDBNull(5) ? null : rd.GetString(5),
                    Comment = rd.IsDBNull(6) ? null : rd.GetString(6),
                });
            }

            return list;
        }

    }
}

