using DrJaw.Models;
using System.Data;

namespace DrJaw.Utils
{
    public static class MSSQLConverters
    {
        public static List<MSSQLUser> ConvertToUsers(DataTable dt) =>
            dt.AsEnumerable().Select(row => new MSSQLUser
            {
                Id = Convert.ToInt32(row["Id"]),
                Name = row["Name"].ToString() ?? "",
                Password = row["Password"].ToString() ?? "",
                Role = row["Role"].ToString() ?? "",
                Display = Convert.ToBoolean(row["Display"]),
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                UpdatedAt = row["UpdatedAt"] == DBNull.Value ? null : (DateTime?)row["UpdatedAt"]
            }).ToList();
        public static List<DGMSSQLReadyToSold> ConvertToReadyToSold(DataTable table)
        {
            return table.AsEnumerable().Select(row => new DGMSSQLReadyToSold
            {
                Id = Convert.ToInt32(row["Id"]),
                ImageData = row["Image"] is DBNull ? null : (byte[])row["Image"],
                Articul = row["Articul"]?.ToString() ?? "",
                Weight = row["Weight"] is DBNull ? 0 : Convert.ToDecimal(row["Weight"]),
                Size = row["Size"]?.ToString() ?? "",
                Price = row["Price"] is DBNull ? 0 : Convert.ToDecimal(row["Price"])
            }).ToList();
        }
        public static List<MSSQLArticul> ConvertToArticuls(DataTable dt) =>
            dt.AsEnumerable().Select(row => new MSSQLArticul
            {
                Id = Convert.ToInt32(row["Id"]),
                Name = row["Name"].ToString() ?? "",
                TypeId = Convert.ToInt32(row["TypeId"]),
                MetalId = Convert.ToInt32(row["MetalId"]),
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                UpdatedAt = row["UpdatedAt"] == DBNull.Value ? null : (DateTime?)row["UpdatedAt"]
            }).ToList();
        public static List<MSSQLArticulByName> ConvertToArticulsByName(DataTable dt) =>
            dt.AsEnumerable().Select(row => new MSSQLArticulByName
            {
                Id = Convert.ToInt32(row["Id"]),
                Name = row["Name"].ToString() ?? "",
                TypeId = Convert.ToInt32(row["TypeId"]),
                MetalId = Convert.ToInt32(row["MetalId"]),
                ImageData = row["ImageData"] == DBNull.Value ? null : (byte[])row["ImageData"]
            }).ToList();

        public static List<MSSQLArticulImage> ConvertToArticulImages(DataTable dt) =>
            dt.AsEnumerable().Select(row => new MSSQLArticulImage
            {
                Id = Convert.ToInt32(row["Id"]),
                ArticulId = Convert.ToInt32(row["ArticulId"]),
                Image = row["Image"] == DBNull.Value ? Array.Empty<byte>() : (byte[])row["Image"],
                UploadedAt = Convert.ToDateTime(row["UploadedAt"])
            }).ToList();

        public static List<MSSQLMart> ConvertToMarts(DataTable dt) =>
            dt.AsEnumerable().Select(row => new MSSQLMart
            {
                Id = Convert.ToInt32(row["Id"]),
                Name = row["Name"].ToString() ?? "",
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                UpdatedAt = row["UpdatedAt"] == DBNull.Value ? null : (DateTime?)row["UpdatedAt"]
            }).ToList();

        public static List<MSSQLManufacturer> ConvertToManufacturers(DataTable dt) =>
            dt.AsEnumerable().Select(row => new MSSQLManufacturer
            {
                Id = Convert.ToInt32(row["Id"]),
                Name = row["Name"].ToString() ?? ""
            }).ToList();

        public static List<MSSQLStone> ConvertToStones(DataTable dt) =>
            dt.AsEnumerable().Select(row => new MSSQLStone
            {
                Id = Convert.ToInt32(row["Id"]),
                Name = row["Name"].ToString() ?? ""
            }).ToList();

        public static List<MSSQLStatus> ConvertToStatuses(DataTable dt) =>
            dt.AsEnumerable().Select(row => new MSSQLStatus
            {
                Id = Convert.ToInt32(row["Id"]),
                Name = row["Name"].ToString() ?? "",
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                UpdatedAt = row["UpdatedAt"] == DBNull.Value ? null : (DateTime?)row["UpdatedAt"]
            }).ToList();
        public static List<MSSQLType> ConvertToTypes(DataTable dt) =>
            dt.AsEnumerable().Select(row => new MSSQLType
            {
                Id = Convert.ToInt32(row["Id"]),
                Name = row["Name"].ToString() ?? ""
            }).ToList();

        public static List<MSSQLPaymentType> ConvertToPaymentTypes(DataTable dt) =>
            dt.AsEnumerable().Select(row => new MSSQLPaymentType
            {
                Id = Convert.ToInt32(row["Id"]),
                Name = row["Name"].ToString() ?? "",
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                UpdatedAt = row["UpdatedAt"] == DBNull.Value ? null : (DateTime?)row["UpdatedAt"]
            }).ToList();
        public static List<MSSQLMetal> ConvertToMetal(DataTable dt) =>
            dt.AsEnumerable().Select(row => new MSSQLMetal
            {
                Id = Convert.ToInt32(row["Id"]),
                Name = row["Name"].ToString() ?? "",
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                UpdatedAt = row["UpdatedAt"] == DBNull.Value ? null : (DateTime?)row["UpdatedAt"]
            }).ToList();

        public static List<MSSQLLom> ConvertToLom(DataTable dt) =>
            dt.AsEnumerable().Select(row => new MSSQLLom
            {
                Id = Convert.ToInt32(row["Id"]),
                MartId = Convert.ToInt32(row["MartId"]),
                Weight = Convert.ToDecimal(row["Weight"]),
                PricePerGram = row["PricePerGram"] == DBNull.Value ? null : (decimal?)row["PricePerGram"],
                Receiving = Convert.ToBoolean(row["Receiving"]),
                UserId = Convert.ToInt32(row["UserId"]),
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                UpdatedAt = row["UpdatedAt"] == DBNull.Value ? null : (DateTime?)row["UpdatedAt"]
            }).ToList();

        public static List<MSSQLCart> ConvertToCart(DataTable dt) =>
            dt.AsEnumerable().Select(row => new MSSQLCart
            {
                Id = Convert.ToInt32(row["Id"]),
                UserId = Convert.ToInt32(row["UserId"]),
                MartId = Convert.ToInt32(row["MartId"]),
                PaymentTypeId = Convert.ToInt32(row["PaymentTypeId"]),
                PurchaseDate = row["PurchaseDate"] == DBNull.Value ? null : (DateTime?)row["PurchaseDate"],
                Bonus = Convert.ToDecimal(row["Bonus"]),
                TotalSum = Convert.ToDecimal(row["TotalSum"]),
                LomId = row["LomId"] == DBNull.Value ? null : (int?)row["LomId"],
                CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
                UpdatedAt = row["UpdatedAt"] == DBNull.Value ? null : (DateTime?)row["UpdatedAt"]
            }).ToList();

        public static List<DGMSSQLItem> ConvertToItems(DataTable dt) =>
            dt.AsEnumerable().Select(row => new DGMSSQLItem
            {
                Type = row["Type"].ToString() ?? "",
                Metal = row["Metal"].ToString() ?? "",
                Articul = row["Articul"].ToString() ?? "",
                Weight = Convert.ToDecimal(row["Weight"]),
                ItemCount = Convert.ToInt32(row["ItemCount"]),
                Size = Convert.ToDecimal(row["Size"]),
                Stones = row["Stones"].ToString() ?? "",
                Comment = row["Comment"].ToString() ?? "",
                Price = Convert.ToDecimal(row["Price"]),
                Manufacturer = row["Manufacturer"].ToString() ?? "",
                mid = Convert.ToInt32(row["mid"])
            }).ToList();

        public static List<MSSQLCartItem> ConvertToCartItems(DataTable table)
        {
            /*return table.AsEnumerable().Select(row => new MSSQLCartItem
            {
                ImageData = row["ImageData"] is DBNull ? null : (byte[])row["ImageData"],
                Articul = row["Articul"]?.ToString() ?? "",
                Weight = row["Weight"] is DBNull ? 0 : Convert.ToDecimal(row["Weight"]),
                Size = row["Size"]?.ToString() ?? "",
                Manufacturer = row["Manufacturer"]?.ToString() ?? "",
                Stone = row["Stone"]?.ToString() ?? "",
                ItemBonus = row["ItemBonus"] is DBNull ? 0 : Convert.ToInt32(row["ItemBonus"]),
                Comment = row["Comment"]?.ToString() ?? "",
                CiStatus = row["CiStatus"]?.ToString() ?? ""
            }).ToList();*/
            var list = new List<MSSQLCartItem>();
            return list;
        }
        public static List<DGMSSQLTransferItem> ConvertToTransferItems(DataTable table)
        {
            var list = new List<DGMSSQLTransferItem>();
            foreach (DataRow row in table.Rows)
            {
                list.Add(new DGMSSQLTransferItem
                {
                    Id = row.Field<int>("Id"),
                    Articul = row.Field<string>("Articul") ?? "",
                    Metal = row.Field<string>("Metal") ?? "",
                    Weight = row.Field<decimal>("Weight"),
                    Size = row.Field<string>("Size") ?? "",
                    Stone = row.Field<string>("Stone") ?? "",
                    InMartName = row.Field<string>("InMartName") ?? ""
                });
            }
            return list;
        }
        public static List<DGMSSQLLomItem> ConvertToLomItems(DataTable table)
        {
            var list = new List<DGMSSQLLomItem>();

            foreach (DataRow row in table.Rows)
            {
                var item = new DGMSSQLLomItem
                {
                    Id = row.Field<int>("Id"),
                    Mart = row.Field<string>("Mart") ?? string.Empty,
                    Weight = row.Field<decimal>("Weight"),
                    PricePerGram = row.Field<decimal?>("PricePerGram"),
                    Receiving = row.Field<bool>("Receiving"),
                    UserName = row.Field<string>("UserName") ?? string.Empty,
                    CreatedAt = row.Field<DateTime>("CreatedAt"),
                    CartId = row.IsNull("CartId") ? null : row.Field<int?>("CartId")
                };

                list.Add(item);
            }

            return list;
        }
        public static List<DGMSSQLOrders> ConvertToOrders(DataTable table)
        {
            var list = new List<DGMSSQLOrders>(table?.Rows.Count ?? 0);
            if (table == null) return list;

            foreach (DataRow r in table.Rows)
            {
                list.Add(new DGMSSQLOrders
                {
                    CartId = r.Field<int>("CartId"),
                    UserId = r.Field<int>("UserId"),
                    MartId = r.Field<int>("MartId"),
                    PaymentType = r.Field<string?>("PaymentType") ?? "",
                    Bonus = r.Field<decimal?>("Bonus"),
                    TotalPrice = r.Field<decimal?>("TotalPrice"),
                    PurchaseDate = r.Field<DateTime?>("PurchaseDate"),
                    LomId = r.Field<int?>("LomId"),
                    Status = r.Field<string?>("Status") ?? ""
                });
            }
            return list;
        }
        public static List<DGMSSQLOrderItem> ConvertToOrderItems(DataTable table)
        {
            var list = new List<DGMSSQLOrderItem>(table?.Rows.Count ?? 0);
            if (table == null) return list;

            foreach (DataRow r in table.Rows)
            {
                list.Add(new DGMSSQLOrderItem
                {
                    ImageData = r["ImageData"] as byte[],
                    Articul = r.Field<string?>("Articul") ?? "",
                    Weight = r.Field<decimal?>("Weight") ?? 0m,
                    Size = r.Field<string?>("Size") ?? "",
                    Manufacturer = r.Field<string?>("Manufacturer") ?? "",
                    Stone = r.Field<string?>("Stone") ?? "",
                    ItemBonus = r.Field<decimal?>("ItemBonus"),
                    Comment = r.Field<string?>("Comment") ?? "",
                    CiStatus = r.Field<string?>("CiStatus") ?? "",
                });
            }
            return list;
        }
        public static List<MSSQLOrderTotals> ConvertToOrderTotals(DataTable table)
        {
            var list = new List<MSSQLOrderTotals>(table?.Rows.Count ?? 0);
            if (table == null) return list;

            foreach (DataRow r in table.Rows)
            {
                list.Add(new MSSQLOrderTotals
                {
                    UserId = r.Field<int>("UserId"),
                    MartId = r.Field<int>("MartId"),
                    Metal = r.Field<string?>("Metal") ?? "",
                    ItemCount = r.Field<int?>("ItemCount") ?? 0,
                    TotalWeight = r.Field<decimal?>("TotalWeight") ?? 0m,
                    TotalPrice = r.Field<decimal?>("TotalPrice") ?? 0m
                });
            }
            return list;
        }
    }
}
