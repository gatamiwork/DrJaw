using DrJaw.Models;
using DrJaw.Utils;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace DrJaw
{
    public class MSSQLRepository
    {
        // === ITEMS ===
        public async Task<List<MSSQLItem>> LoadItems(MSSQLMart mart, MSSQLMetal metal)
        {
            string query = @"
                    SELECT Types.Name AS Type, Metals.Name AS Metal, a.Name AS Articul, i.Weight, i.ItemCount, i.Size, 
                           Stones.Name AS Stones, i.Comment AS Comment, i.Price, Manufacturers.Name AS Manufacturer, MaxId AS mid
                    FROM (
                        SELECT ArticulId, Weight, Size, MartId, ManufacturerId, COUNT(*) AS ItemCount, Price, MAX(Id) AS MaxId,
                               StonesId, Comment, CartId, TransferMartId, ReadyToSold
                        FROM Items
                        GROUP BY ArticulId, Weight, Size, MartId, ManufacturerId, Price, StonesId, Comment, CartId, TransferMartId, ReadyToSold
                    ) AS i
                    LEFT JOIN Articuls a ON a.Id = i.ArticulId
                    LEFT JOIN Types ON a.TypeId = Types.Id
                    LEFT JOIN Metals ON a.MetalId = Metals.Id
                    LEFT JOIN Stones ON i.StonesId = Stones.Id
                    LEFT JOIN Manufacturers ON i.ManufacturerId = Manufacturers.Id
                    WHERE i.MartId = @MartId AND i.CartId IS NULL AND i.TransferMartId IS NULL AND i.ReadyToSold = 0 AND Metals.Id = @MetalId";
            var parameters = new List<SqlParameter>
                    {
                        new SqlParameter("@MartId", mart.Id),
                        new SqlParameter("@MetalId", metal.Id)
                    };
            var table = await MSSQLManager.ExecuteQueryAsync(query, parameters);
            return MSSQLConverters.ConvertToItems(table);
        }
        public async Task<List<MSSQLReadyToSold>> LoadItemsInCart(MSSQLMart mart)
        {
            var query = "SELECT i.Id, ai.Image, a.Name AS Articul, i.Weight, i.Size, i.Price " +
                        "FROM Items AS i " +
                        "LEFT JOIN Articuls a ON a.Id = i.ArticulId " +
                        "LEFT JOIN ArticulImages ai ON ai.ArticulId = i.ArticulId " +
                        "WHERE ReadyToSold = 1 AND MartId = @MartId";
            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@MartId", SqlDbType.Int) { Value = mart.Id }
                };
            var table = await MSSQLManager.ExecuteQueryAsync(query, parameters);
            return MSSQLConverters.ConvertToReadyToSold(table);
        }
        public async Task<bool> DeleteItem(int id)
        {
            string checkQuery = "SELECT CartId FROM CartItems WHERE ItemId = @ItemId";
            var checkParams = new List<SqlParameter>
                    {
                        new SqlParameter("@ItemId", SqlDbType.Int) { Value = id }
                    };

            var cartItemsTable = await MSSQLManager.ExecuteQueryAsync(checkQuery, checkParams);

            if (cartItemsTable.Rows.Count > 0)
            {
                var cartId = Convert.ToInt32(cartItemsTable.Rows[0]["CartId"]);

                string updateQuery = "UPDATE Items SET CartId = @CartId WHERE Id = @Id";
                var updateParams = new List<SqlParameter>
                    {
                        new SqlParameter("@CartId", SqlDbType.Int) { Value = cartId },
                        new SqlParameter("@Id", SqlDbType.Int) { Value = id }
                    };

                await MSSQLManager.ExecuteNonQueryAsync(updateQuery, updateParams);
            }
            else
            {
                string deleteQuery = "DELETE FROM Items WHERE Id = @Id";
                var deleteParams = new List<SqlParameter>
                    {
                        new SqlParameter("@Id", SqlDbType.Int) { Value = id }
                    };

                await MSSQLManager.ExecuteNonQueryAsync(deleteQuery, deleteParams);
            }
            return true;
        }
        public async Task<bool> SetReadyToSold(int id, bool status)
        {
            var query = "UPDATE Items SET ReadyToSold = @status WHERE Id = @Id";
            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@Id", SqlDbType.Int) { Value = id },
                    new SqlParameter("@status", SqlDbType.Bit) { Value = status }
                };

            int result = await MSSQLManager.ExecuteNonQueryAsync(query, parameters);
            return result > 0;
        }
        public async Task<bool> SetReadyToSold(List<int> itemIds, bool status)
        {
            if (itemIds == null || itemIds.Count == 0)
                return false;

            var idParams = itemIds.Select((id, i) => $"@id{i}").ToList();
            string idsSql = string.Join(", ", idParams);

            string query = $"UPDATE Items SET ReadyToSold = @status WHERE Id IN ({idsSql})";

            var parameters = new List<SqlParameter>
    {
        new("@status", SqlDbType.Bit) { Value = status }
    };

            for (int i = 0; i < itemIds.Count; i++)
            {
                parameters.Add(new SqlParameter($"@id{i}", SqlDbType.Int) { Value = itemIds[i] });
            }

            int rowsAffected = await MSSQLManager.ExecuteNonQueryAsync(query, parameters);
            return rowsAffected > 0;
        }
        public async Task<int> ItemTransferCount(MSSQLMart mart)
        {
            string query = "SELECT COUNT(Id) FROM Items WHERE TransferMartId = @MartId";
            var parameters = new List<SqlParameter>
                    {
                        new SqlParameter("@MartId", mart.Id)
                    };

            var result = await MSSQLManager.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result);
        }
        public async Task<bool> TransferItem(int? martId, int id, int? newMartId = null)
        {
            string query;
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Id", SqlDbType.Int) { Value = id }
            };

            if (newMartId == null)
            {
                query = "UPDATE Items SET TransferMartId = @martId WHERE Id = @Id";
                parameters.Add(new SqlParameter("@martId", SqlDbType.Int)
                {
                    Value = martId.HasValue ? martId.Value : DBNull.Value
                });
            }
            else
            {
                query = "UPDATE Items SET TransferMartId = NULL, MartId = @newMartId WHERE Id = @Id";
                parameters.Add(new SqlParameter("@newMartId", SqlDbType.Int)
                {
                    Value = newMartId.Value
                });
            }

            await MSSQLManager.ExecuteNonQueryAsync(query, parameters);
            return true;
        }
        public async Task<List<MSSQLTransferItem>> LoadTransferItems(int martId)
        {
            var query = @"
                    SELECT i.Id AS Id, a.Name AS Articul, Metals.Name AS Metal, i.Weight AS Weight, i.Size AS Size, Stones.Name AS Stone,Marts.Name AS InMartName 
                    FROM Items i
                    LEFT JOIN Articuls a ON a.Id = i.ArticulId
                    LEFT JOIN Metals ON a.MetalId = Metals.Id
                    LEFT JOIN Stones ON i.StonesId = Stones.Id
                    LEFT JOIN Marts ON i.MartId = Marts.Id
                    WHERE TransferMartId = @TransferMartId;";

            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@TransferMartId", SqlDbType.Int) { Value = martId }
                };
            var table = await MSSQLManager.ExecuteQueryAsync(query, parameters);
            return MSSQLConverters.ConvertToTransferItems(table);
        }
        public async Task<int> ItemCartCount(MSSQLMart mart)
        {
            string query = @"SELECT COUNT(Id) FROM Items WHERE ReadyToSold = 1 AND MartId = @MartId";
            var parameters = new List<SqlParameter>
                    {
                        new SqlParameter("@MartId", mart.Id)
                    };
            var result = await MSSQLManager.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result);
        }
        public async Task<bool> CreateItem(string articulName, int typeId, int metalId, decimal weight, decimal size, int stoneId, int manufacturerId, decimal price, string comment, System.Drawing.Image? image = null, int articulId = 0)
        {
            if (articulId == 0)
            {
                string insertArticulQuery = @"
                    INSERT INTO Articuls (Name, TypeId, MetalId, CreatedAt)
                    OUTPUT INSERTED.Id
                    VALUES (@Name, @TypeId, @MetalId, GETDATE())";

                var articulParams = new List<SqlParameter>
                    {
                        new("@Name", SqlDbType.NVarChar)     { Value = articulName },
                        new("@TypeId", SqlDbType.Int)        { Value = typeId },
                        new("@MetalId", SqlDbType.Int)       { Value = metalId }
                    };
                var result = await MSSQLManager.ExecuteScalarAsync(insertArticulQuery, articulParams);
                articulId = Convert.ToInt32(result);

                if (image != null)
                {
                    byte[] imageBytes = Utils.ImageHelper.ImageToBytes(image); // 🧠 вынесено в утилиту

                    string insertImageQuery = @"
                INSERT INTO ArticulImages (ArticulId, Image)
                VALUES (@ArticulId, @Image)";

                    var imageParams = new List<SqlParameter>
            {
                new("@ArticulId", SqlDbType.Int)     { Value = articulId },
                new("@Image", SqlDbType.VarBinary)   { Value = imageBytes }
            };

                    await MSSQLManager.ExecuteNonQueryAsync(insertImageQuery, imageParams);
                }
            }

          string query2 = @"
              INSERT INTO Items (ArticulId, Weight, Size, MartId, ManufacturerId, Price, StonesId, Comment, CreatedAt)
              VALUES (@ArticulId, @Weight, @Size, @MartId, @ManufacturerId, @Price, @StonesId, @Comment, GETDATE())";

          var parameters2 = new List<SqlParameter>
          {
              new("@ArticulId", SqlDbType.Int)     { Value = articulId },
              new("@Weight", SqlDbType.Decimal)    { Value = weight },
              new("@Size", SqlDbType.NVarChar)     { Value = size },
              new("@MartId", SqlDbType.Int)        { Value = Storage.CurrentMart?.Id },
              new("@ManufacturerId", SqlDbType.Int){ Value = manufacturerId },
              new("@Price", SqlDbType.Decimal)     { Value = price },
              new("@StonesId", SqlDbType.Int)      { Value = stoneId },
              new("@Comment", SqlDbType.NVarChar)  { Value = comment }
          };
            int rows = await MSSQLManager.ExecuteNonQueryAsync(query2, parameters2);
            return rows > 0;
        }
        public async Task<bool> ReturnCartAndItemAsync(int cartItemId, CancellationToken ct = default)
        {
            const string sql = @"
		DECLARE @ReturnStatusId int = (SELECT TOP(1) Id FROM Statuses WHERE Name = N'Возврат');
		IF @ReturnStatusId IS NULL
			THROW 50000, N'Status ''Возврат'' not found', 1;

		DECLARE @r1 int = 0, @r2 int = 0;

		UPDATE Items
		   SET CartId = NULL
		 WHERE Id = (SELECT ItemId FROM CartItems WHERE Id = @CartItemId);
		SET @r1 = @@ROWCOUNT;

		IF COL_LENGTH('dbo.CartItems','ReturnedAt') IS NOT NULL
		BEGIN
			UPDATE CartItems
			   SET StatusId   = @ReturnStatusId
			 WHERE Id = @CartItemId
			   AND (StatusId IS NULL OR StatusId <> @ReturnStatusId);
		END
		ELSE
		BEGIN
			UPDATE CartItems
			   SET StatusId = @ReturnStatusId
			 WHERE Id = @CartItemId
			   AND (StatusId IS NULL OR StatusId <> @ReturnStatusId);
		END
		SET @r2 = @@ROWCOUNT;

		SELECT CASE WHEN @r1 > 0 THEN 1 ELSE 0 END;";

            int ok = 0;
            await MSSQLManager.WithTransactionAsync(async (conn, tx) =>
            {
                using var cmd = new SqlCommand(sql, conn, tx);
                cmd.Parameters.Add(new SqlParameter("@CartItemId", SqlDbType.Int) { Value = cartItemId });
                var scalar = await cmd.ExecuteScalarAsync(ct);
                ok = Convert.ToInt32(scalar);
            }, ct);

            return ok == 1;
        }


        public async Task<BitmapImage?> LoadImage(int id)
        {
            string query = @"
        SELECT Image
        FROM ArticulImages
        WHERE ArticulId = (SELECT ArticulId FROM Items WHERE Id = @Id);
    ";

            var parameters = new List<SqlParameter>
    {
        new SqlParameter("@Id", SqlDbType.Int) { Value = id }
    };

            DataTable result = await MSSQLManager.ExecuteQueryAsync(query, parameters);

            if (result.Rows.Count > 0 && result.Rows[0]["Image"] != DBNull.Value)
            {
                byte[] imageBytes = (byte[])result.Rows[0]["Image"];

                using var ms = new MemoryStream(imageBytes);
                var bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze(); // для поточной безопасности

                return bitmap;
            }

            return null;
        }
        // === USERS ===
        public async Task<List<MSSQLUser>> LoadUsers()
        {
            string query = "SELECT * FROM Users";
            var table = await MSSQLManager.ExecuteQueryAsync(query);
            return MSSQLConverters.ConvertToUsers(table);
        }
        public async Task<bool> CheckPassword(int id, string password)
        {
            string query = "SELECT Password FROM Users WHERE Id = @Id";
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Id", SqlDbType.Int) { Value = id }
            };

            var resultTable = await MSSQLManager.ExecuteQueryAsync(query, parameters);

            if (resultTable.Rows.Count == 0)
                return false;

            var storedPassword = resultTable.Rows[0]["Password"]?.ToString();

            return string.Equals(storedPassword, password, StringComparison.Ordinal);
        }
        // === MARTS ===
        public async Task<List<MSSQLMart>> LoadMarts()
        {
            string query = "SELECT * FROM Marts ORDER BY Name DESC";
            var table = await MSSQLManager.ExecuteQueryAsync(query);
            return MSSQLConverters.ConvertToMarts(table);
        }
        // === MAETALS ===
        public async Task<List<MSSQLMetal>> LoadMetals()
        {
            string query = "SELECT * FROM Metals";
            var table = await MSSQLManager.ExecuteQueryAsync(query);
            return MSSQLConverters.ConvertToMetal(table);
        }
        // === TYPES ===
        public async Task<List<MSSQLType>> LoadTypes()
        {
            string query = "SELECT id, Name AS Name FROM Types";
            var table = await MSSQLManager.ExecuteQueryAsync(query);
            return MSSQLConverters.ConvertToTypes(table);
        }
        // === STONES ===
        public async Task<List<MSSQLStone>> LoadStones()
        {
            string query = "SELECT id, Name FROM Stones";
            var table = await MSSQLManager.ExecuteQueryAsync(query);
            return MSSQLConverters.ConvertToStones(table);
        }
        // === MANUFACTURERS ===
        public async Task<List<MSSQLManufacturer>> LoadManufacturers()
        {
            string query = "SELECT id, Name FROM Manufacturers";
            var table = await MSSQLManager.ExecuteQueryAsync(query);
            return MSSQLConverters.ConvertToManufacturers(table);
        }
        // === ARTICULS ==
        public async Task<List<MSSQLArticulByName>> LoadArticulByName(string name)
        {
            string query = @"
                SELECT a.Id, a.Name, a.TypeId, a.MetalId, ai.Image AS ImageData
                FROM Articuls a
                LEFT JOIN ArticulImages ai ON ai.ArticulId = a.Id
                WHERE a.Name = @name"; // <--- ТОЧНОЕ совпадение

            var parameters = new List<SqlParameter>
                {
                    new("@name", SqlDbType.NVarChar) { Value = name }
                };

            var table = await MSSQLManager.ExecuteQueryAsync(query, parameters);
            return MSSQLConverters.ConvertToArticulsByName(table);
        }

        // === PAYMENTTYPES ==
        public async Task<List<MSSQLPaymentType>> LoadPaymentTypes()
        {
            string query = "SELECT * From PaymentTypes";
            var table = await MSSQLManager.ExecuteQueryAsync(query);
            return MSSQLConverters.ConvertToPaymentTypes(table);
        }
        // === CART ==
        public async Task<int> CreateCart(decimal totalSum, int martId, int userId, int paymentTypeId, int bonus, int lomId = 0)
        {
            string query = @"
                INSERT INTO Cart (TotalSum, MartId, UserId, PaymentTypeId, PurchaseDate, Bonus, LomId)
                OUTPUT INSERTED.Id
                VALUES (@TotalSum, @MartId, @UserId, @PaymentTypeId, GETDATE(), @Bonus, @LomId);";

            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@TotalSum", SqlDbType.Decimal)       { Value = totalSum },
                    new SqlParameter("@MartId", SqlDbType.Int)             { Value = martId },
                    new SqlParameter("@UserId", SqlDbType.Int)             { Value = userId },
                    new SqlParameter("@PaymentTypeId", SqlDbType.Int)      { Value = paymentTypeId },
                    new SqlParameter("@Bonus", SqlDbType.Int)              { Value = bonus },
                    new SqlParameter("@lomId", SqlDbType.Int)
                    {
                        Value = lomId != 0 ? lomId : DBNull.Value
                    }
                };

            object? result = await MSSQLManager.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result);
        }
        public async Task<List<MSSQLCart>> LoadCart(int martId, DateTime periodStart, DateTime periodEnd)
        {
            string query = @"
                SELECT 
                    c.Id AS CartId,
                    m.Name AS Mart,
                    pt.Name AS PaymentType,
                    c.Bonus AS Bonus,
                    c.TotalSum AS TotalPrice,
                    c.PurchaseDate AS PurchaseDate,
                    l.Id AS LomId,
                    CASE 
                        WHEN COUNT(DISTINCT ci.StatusId) = 1 AND MIN(s.Name) = 'Продано' THEN 'Продано'
                        WHEN COUNT(DISTINCT ci.StatusId) = 1 AND MIN(s.Name) = 'Возврат' THEN 'Возврат'
                        WHEN COUNT(DISTINCT ci.StatusId) > 1 THEN 'Частичный возврат'
                        ELSE 'Неизвестно'
                    END AS Status
                FROM Cart AS c
                    LEFT JOIN Marts m ON c.MartId = m.Id
                    LEFT JOIN PaymentTypes pt ON c.PaymentTypeId = pt.Id
                    LEFT JOIN Lom l ON c.LomId = l.Id
                    LEFT JOIN CartItems ci ON ci.CartId = c.Id
                    LEFT JOIN Statuses s ON s.Id = ci.StatusId
                WHERE {0} c.PurchaseDate >= @DateStart AND c.PurchaseDate <= @DateEnd
                GROUP BY c.Id, m.Name, pt.Name, c.Bonus, c.TotalSum, c.PurchaseDate, l.Id;
            ";
            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@DateStart", SqlDbType.DateTime) { Value = periodStart },
                    new SqlParameter("@DateEnd", SqlDbType.DateTime) { Value = periodEnd },
                };

            string martCondition = (martId == 0) ? "" : "m.Id = @MartId AND ";
            query = string.Format(query, martCondition);
            if (martId > 0)
            {
                parameters.Add(new SqlParameter("@MartId", SqlDbType.Int) { Value = martId });
            }
            var table = await MSSQLManager.ExecuteQueryAsync(query, parameters);
            return MSSQLConverters.ConvertToCart(table);
        }
        public async Task<List<MSSQLCartTotals>> LoadCartTotals(int martId, DateTime periodStart, DateTime periodEnd)
        {
            string query = @"
                 SELECT 
                     u.Name AS UserName,
                     m.Name AS Metal,
                     COUNT(i.Id) AS ItemCount,
                     SUM(i.Weight) AS TotalWeight,
                     SUM(i.Price) AS TotalPrice
                 FROM CartItems ci
                 JOIN Cart c ON ci.CartId = c.Id
                 JOIN Users u ON u.Id = c.UserId
                 JOIN Items i ON i.Id = ci.ItemId
                 JOIN Articuls a ON a.Id = i.ArticulId
                 JOIN Metals m ON m.Id = a.MetalId
                 WHERE c.Id IN (
                     SELECT c.Id
                     FROM Cart AS c
                         LEFT JOIN Marts m ON c.MartId = m.Id
                     WHERE {0} c.PurchaseDate >= @DateStart AND c.PurchaseDate <= @DateEnd
                 )
                 GROUP BY u.Name, m.Name
                 ORDER BY u.Name, m.Name;
             ";
            var parameters = new List<SqlParameter>
                    {
                        new SqlParameter("@DateStart", SqlDbType.DateTime) { Value = periodStart },
                        new SqlParameter("@DateEnd", SqlDbType.DateTime) { Value = periodEnd }
                    };
            string martCondition = (martId == 0) ? "" : "m.Id = @MartId AND ";
            query = string.Format(query, martCondition);

            if (martId > 0)
            {
                parameters.Add(new SqlParameter("@MartId", SqlDbType.Int) { Value = martId });
            }
            var table = await MSSQLManager.ExecuteQueryAsync(query, parameters);

            return table.AsEnumerable().Select(row => new MSSQLCartTotals
            {
                UserName = row["UserName"].ToString() ?? "",
                Metal = row["Metal"].ToString() ?? "",
                ItemCount = Convert.ToDecimal(row["ItemCount"]),
                TotalWeight = Convert.ToDecimal(row["TotalWeight"]),
                TotalPrice = Convert.ToDecimal(row["TotalPrice"])
            }).ToList();
        }
        // === CARTITEM ==
        public async Task<bool> CreateCartItem(int cartId, int itemId, decimal itemBonus)
        {
            string query1 = "INSERT INTO CartItems (CartId, ItemId, Bonus, StatusId) VALUES (@CartId, @ItemId, @Bonus, 1);";
            string query2 = "UPDATE Items SET ReadyToSold = 0, CartId = @CartId WHERE Id = @ItemId;";

            var parameters1 = new List<SqlParameter>
                    {
                        new SqlParameter("@CartId", SqlDbType.Int) { Value = cartId },
                        new SqlParameter("@ItemId", SqlDbType.Int) { Value = itemId },
                        new SqlParameter("@Bonus", SqlDbType.Decimal) { Value = itemBonus }
                    };
            var parameters2 = new List<SqlParameter>
                    {
                        new SqlParameter("@CartId", SqlDbType.Int) { Value = cartId },
                        new SqlParameter("@ItemId", SqlDbType.Int) { Value = itemId },
                    };

            await MSSQLManager.ExecuteNonQueryAsync(query1, parameters1);
            await MSSQLManager.ExecuteNonQueryAsync(query2, parameters2);
            return true;
        }
        public async Task<List<MSSQLCartItem>> LoadCartItems(int cartId)
        {
            string query = @"
                    SELECT ai.Image AS ImageData, a.Name AS Articul, i.Weight AS Weight, i.Size AS Size, 
                           m.Name AS Manufacturer, s.Name AS Stone, ci.Bonus AS ItemBonus, i.Comment AS Comment,
                           st.Name AS CiStatus
                    FROM CartItems ci
                    LEFT JOIN Items i ON ci.ItemId = i.Id
                    LEFT JOIN Articuls a ON i.ArticulId = a.Id
                    LEFT JOIN ArticulImages ai ON i.ArticulId = ai.ArticulId
                    LEFT JOIN Manufacturers m ON i.ManufacturerId = m.Id
                    LEFT JOIN Stones s ON i.StonesId = s.Id
                    LEFT JOIN Statuses st ON ci.StatusId = st.Id
                    WHERE ci.CartId = @CartId";

            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@CartId", SqlDbType.Int) { Value = cartId }
                };
            var table = await MSSQLManager.ExecuteQueryAsync(query, parameters);
            return MSSQLConverters.ConvertToCartItems(table);
        }
        public async Task<List<MSSQLReturnCartItem>> LoadReturnCartItems(int martId, DateTime dateStart, DateTime dateEnd)
        {
            string query = @"
            SELECT 
                ci.Id AS Id,
                a.Name AS Articul,
                i.Weight AS Weight,
                i.Size AS Size,
                st.Name AS Stones,
                i.Price AS Price,
                ci.Bonus AS Bonus,
                (i.Price - i.Price * ci.Bonus / 100.0) AS TotalPrice,
                ci.CartId AS CartId
            FROM CartItems AS ci
            LEFT JOIN Statuses s ON ci.StatusId = s.Id
            LEFT JOIN Items i ON ci.ItemId = i.Id
            LEFT JOIN Articuls a ON i.ArticulId = a.Id
            LEFT JOIN Cart c ON ci.CartId = c.Id
            LEFT JOIN Stones st ON i.StonesId = st.Id
            WHERE 
                s.Name IS NOT NULL AND s.Name <> 'Возврат'
                AND c.PurchaseDate >= @DateStart AND c.PurchaseDate <= @DateEnd
                AND i.MartId = @martId;
        ";
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@DateStart", SqlDbType.DateTime) { Value = dateStart },
                new SqlParameter("@DateEnd", SqlDbType.DateTime) { Value = dateEnd },
                new SqlParameter("@martId", SqlDbType.Int) { Value = martId }
            };
            var table = await MSSQLManager.ExecuteQueryAsync(query, parameters);

            return table.AsEnumerable().Select(row => new MSSQLReturnCartItem
            {
                Id = Convert.ToInt32(row["Id"]),
                Articul = row["Articul"]?.ToString() ?? "",
                Weight = Convert.ToDecimal(row["Weight"]),
                Size = Convert.ToDecimal(row["Size"]),
                Stones = row["Stones"]?.ToString() ?? "",
                Price = Convert.ToDecimal(row["Price"]),
                Bonus = Convert.ToDecimal(row["Bonus"]),
                TotalPrice = Convert.ToDecimal(row["TotalPrice"]),
                CartId = Convert.ToInt32(row["CartId"])
            }).ToList();
        }
        // === LOM ==
        public async Task<int> CreateLom(int? userId, int? martId, bool receiving, decimal weight, decimal? pricePerGram = null)
        {
            string query = @"
        INSERT INTO Lom (MartId, Weight, PricePerGram, Receiving, UserId, CreatedAt)
        OUTPUT INSERTED.Id
        VALUES (@martId, @weight, @pricePerGram, @receiving, @userId, GETDATE());";

            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@userId", SqlDbType.Int)     { Value = userId ?? (object)DBNull.Value },
                    new SqlParameter("@martId", SqlDbType.Int)     { Value = martId ?? (object)DBNull.Value },
                    new SqlParameter("@receiving", SqlDbType.Bit) {Value = (receiving) ? 1 : 0},
                    new SqlParameter("@weight", SqlDbType.Decimal) { Value = weight },
                    new SqlParameter("@pricePerGram", SqlDbType.Decimal)
                    {
                        Value = (pricePerGram.HasValue && pricePerGram.Value > 0)
                            ? pricePerGram.Value
                            : DBNull.Value
                    }
                };

            object? result = await MSSQLManager.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result);
        }
        public async Task<List<MSSQLLomTotals>> LoadLomTotals(DateTime dateStart, DateTime dateEnd)
        {
            string query = @"
                SELECT
                    l.MartId,
                    SUM(CASE WHEN l.CreatedAt < @DateStart THEN CASE WHEN l.Receiving = 1 THEN l.Weight ELSE -l.Weight END ELSE 0 END) AS StartWeight,
                    SUM(CASE WHEN l.CreatedAt < @DateStart THEN CASE WHEN l.Receiving = 1 THEN l.Weight * ISNULL(l.PricePerGram, 0) ELSE -l.Weight * ISNULL(l.PricePerGram, 0) END ELSE 0 END) AS StartPrice,

                    SUM(CASE WHEN l.CreatedAt BETWEEN @DateStart AND @DateEnd THEN CASE WHEN l.Receiving = 1 THEN l.Weight ELSE -l.Weight END ELSE 0 END) AS CurrentWeight,
                    SUM(CASE WHEN l.CreatedAt BETWEEN @DateStart AND @DateEnd THEN CASE WHEN l.Receiving = 1 THEN l.Weight * ISNULL(l.PricePerGram, 0) ELSE -l.Weight * ISNULL(l.PricePerGram, 0) END ELSE 0 END) AS CurrentPrice,

                    SUM(CASE WHEN l.CreatedAt <= @DateEnd THEN CASE WHEN l.Receiving = 1 THEN l.Weight ELSE -l.Weight END ELSE 0 END) AS EndWeight,
                    SUM(CASE WHEN l.CreatedAt <= @DateEnd THEN CASE WHEN l.Receiving = 1 THEN l.Weight * ISNULL(l.PricePerGram, 0) ELSE -l.Weight * ISNULL(l.PricePerGram, 0) END ELSE 0 END) AS EndPrice
                FROM Lom AS l
                GROUP BY l.MartId;";

            var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@DateStart", SqlDbType.DateTime) { Value = dateStart },
                    new SqlParameter("@DateEnd", SqlDbType.DateTime) { Value = dateEnd },
                };

            var table =  await MSSQLManager.ExecuteQueryAsync(query, parameters);
            return table.AsEnumerable().Select(row => new MSSQLLomTotals
            {
                MartId = Convert.ToInt32(row["MartId"]),
                StartWeight = Convert.ToDecimal(row["StartWeight"]),
                StartPrice = Convert.ToDecimal(row["StartPrice"]),
                CurrentWeight = Convert.ToDecimal(row["CurrentWeight"]),
                CurrentPrice = Convert.ToDecimal(row["CurrentPrice"]),
                EndWeight = Convert.ToDecimal(row["EndWeight"]),
                EndPrice = Convert.ToDecimal(row["EndPrice"])
            }).ToList();
        }
        public async Task<List<MSSQLLomItem>> LoadLom(DateTime dateStart, DateTime dateEnd)
        {
            string query = @"
                    SELECT l.Id AS Id, m.Name AS Mart, l.Weight, l.PricePerGram, l.Receiving, u.Name AS UserName, l.CreatedAt, c.Id AS CartId
                    FROM Lom AS l
                        LEFT JOIN Marts m ON l.MartId = m.Id
                        LEFT JOIN Users u ON l.UserId = u.Id
                        LEFT JOIN Cart c ON l.id = c.LomId
                    WHERE l.CreatedAt >= @DateStart AND l.CreatedAt <= @DateEnd;";

            var parameters = new List<SqlParameter>
                    {
                        new SqlParameter("@DateStart", SqlDbType.DateTime) { Value = dateStart },
                        new SqlParameter("@DateEnd", SqlDbType.DateTime) { Value = dateEnd },
                    };

            var table = await MSSQLManager.ExecuteQueryAsync(query, parameters);
            return MSSQLConverters.ConvertToLomItems(table);
        }
    }
}
