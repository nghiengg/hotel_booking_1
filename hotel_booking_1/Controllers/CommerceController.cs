using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using System.Data.EntityClient;

namespace hotel_booking_1.Controllers
{
    public class CommerceController : Controller
    {
        private string SqlConnectionString
        {
            get { var entity = new EntityConnectionStringBuilder(ConfigurationManager.ConnectionStrings["HotelBookingDbEntities"].ConnectionString); return entity.ProviderConnectionString; }
        }
        private bool IsAdmin { get { return Session["Role"] != null && Convert.ToString(Session["Role"]) == "A"; } }
        private SqlConnection OpenConnection() { var c = new SqlConnection(SqlConnectionString); c.Open(); return c; }
        private static System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>> ToRows(DataTable table)
        {
            var rows = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();
            foreach (DataRow row in table.Rows) { var item = new System.Collections.Generic.Dictionary<string, object>(); foreach (DataColumn col in table.Columns) item[col.ColumnName] = row[col] == DBNull.Value ? null : row[col]; rows.Add(item); }
            return rows;
        }

        [HttpGet] public JsonResult Categories() { using (var c = OpenConnection()) { var t = new DataTable(); new SqlDataAdapter("SELECT CategoryID, CategoryName FROM ProductCategory ORDER BY CategoryName", c).Fill(t); return Json(ToRows(t), JsonRequestBehavior.AllowGet); } }
        [HttpGet] public JsonResult Products(int? categoryId = null, string keyword = "")
        {
            using (var c = OpenConnection()) using (var cmd = new SqlCommand("SELECT p.ProductID,p.ProductName,p.Description,p.Price,p.ImageUrl,p.StockQuantity,p.CategoryID,c.CategoryName FROM Product p JOIN ProductCategory c ON p.CategoryID=c.CategoryID WHERE (@cat IS NULL OR p.CategoryID=@cat) AND (@kw='' OR p.ProductName LIKE '%' + @kw + '%') AND p.IsActive=1 ORDER BY p.ProductID DESC", c))
            { cmd.Parameters.AddWithValue("@cat", (object)categoryId ?? DBNull.Value); cmd.Parameters.AddWithValue("@kw", keyword ?? ""); var t = new DataTable(); new SqlDataAdapter(cmd).Fill(t); return Json(ToRows(t), JsonRequestBehavior.AllowGet); }
        }
        [HttpPost] public JsonResult SaveCategory(int? id, string name)
        {
            if (!IsAdmin) return Json(new { success = false, message = "Không có quyền." }); if (String.IsNullOrWhiteSpace(name)) return Json(new { success = false, message = "Nhập tên danh mục." });
            using (var c = OpenConnection()) using (var cmd = new SqlCommand(id.HasValue ? "UPDATE ProductCategory SET CategoryName=@n WHERE CategoryID=@id" : "INSERT INTO ProductCategory(CategoryName) VALUES(@n)", c)) { cmd.Parameters.AddWithValue("@n", name.Trim()); if (id.HasValue) cmd.Parameters.AddWithValue("@id", id.Value); cmd.ExecuteNonQuery(); }
            return Json(new { success = true });
        }
        [HttpPost] public JsonResult DeleteCategory(int id)
        {
            if (!IsAdmin) return Json(new { success = false, message = "Không có quyền." }); using (var c = OpenConnection()) using (var cmd = new SqlCommand("IF EXISTS(SELECT 1 FROM Product WHERE CategoryID=@id) SELECT 0 ELSE BEGIN DELETE ProductCategory WHERE CategoryID=@id; SELECT 1 END", c)) { cmd.Parameters.AddWithValue("@id", id); return Json(Convert.ToInt32(cmd.ExecuteScalar()) == 1 ? (object)new { success = true } : new { success = false, message = "Danh mục đang có sản phẩm." }); }
        }
        [HttpPost] public JsonResult SaveProduct(int? id, int categoryId, string name, string description, decimal price, int stock, string imageUrl)
        {
            if (!IsAdmin) return Json(new { success = false, message = "Không có quyền." }); if (String.IsNullOrWhiteSpace(name) || price < 0 || stock < 0) return Json(new { success = false, message = "Thông tin sản phẩm không hợp lệ." });
            using (var c = OpenConnection()) using (var cmd = new SqlCommand(id.HasValue ? "UPDATE Product SET CategoryID=@cat,ProductName=@n,Description=@d,Price=@p,StockQuantity=@s,ImageUrl=@img WHERE ProductID=@id" : "INSERT Product(CategoryID,ProductName,Description,Price,StockQuantity,ImageUrl,IsActive) VALUES(@cat,@n,@d,@p,@s,@img,1)", c)) { cmd.Parameters.AddWithValue("@cat", categoryId); cmd.Parameters.AddWithValue("@n", name.Trim()); cmd.Parameters.AddWithValue("@d", (object)description ?? ""); cmd.Parameters.AddWithValue("@p", price); cmd.Parameters.AddWithValue("@s", stock); cmd.Parameters.AddWithValue("@img", (object)imageUrl ?? ""); if (id.HasValue) cmd.Parameters.AddWithValue("@id", id.Value); cmd.ExecuteNonQuery(); }
            return Json(new { success = true });
        }
        [HttpPost] public JsonResult DeleteProduct(int id)
        {
            if (!IsAdmin) return Json(new { success = false, message = "Không có quyền." }); using (var c = OpenConnection()) using (var cmd = new SqlCommand("UPDATE Product SET IsActive=0 WHERE ProductID=@id", c)) { cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); } return Json(new { success = true });
        }
        [HttpPost] public JsonResult AddToCart(int productId, int quantity = 1)
        {
            if (quantity < 1 || quantity > 99) return Json(new { success = false, message = "Số lượng không hợp lệ." });
            var cart = Session["ProductCart"] as Dictionary<int, int> ?? new Dictionary<int, int>();
            using (var c = OpenConnection()) using (var cmd = new SqlCommand("SELECT StockQuantity FROM Product WHERE ProductID=@id AND IsActive=1", c)) { cmd.Parameters.AddWithValue("@id", productId); var stock = cmd.ExecuteScalar(); if (stock == null) return Json(new { success = false, message = "Sản phẩm không tồn tại." }); if (cart.ContainsKey(productId)) cart[productId] += quantity; else cart[productId] = quantity; if (cart[productId] > Convert.ToInt32(stock)) return Json(new { success = false, message = "Số lượng vượt tồn kho." }); }
            Session["ProductCart"] = cart; return Json(new { success = true, count = cart.Values.Sum() });
        }
        [HttpPost] public JsonResult UpdateCart(int productId, int quantity)
        {
            var cart = Session["ProductCart"] as Dictionary<int, int> ?? new Dictionary<int, int>();
            if (quantity <= 0) cart.Remove(productId);
            else
            {
                if (quantity > 99) return Json(new { success = false, message = "Số lượng tối đa là 99." });
                using (var c = OpenConnection()) using (var cmd = new SqlCommand("SELECT StockQuantity FROM Product WHERE ProductID=@id AND IsActive=1", c))
                { cmd.Parameters.AddWithValue("@id", productId); var stock = cmd.ExecuteScalar(); if (stock == null) return Json(new { success = false, message = "Sản phẩm không tồn tại." }); if (quantity > Convert.ToInt32(stock)) return Json(new { success = false, message = "Số lượng vượt tồn kho." }); }
                cart[productId] = quantity;
            }
            Session["ProductCart"] = cart; return Json(new { success = true, count = cart.Values.Sum() });
        }

        [HttpGet] public JsonResult Cart()
        {
            var cart = Session["ProductCart"] as Dictionary<int, int> ?? new Dictionary<int, int>(); var rows = new List<object>(); decimal total = 0;
            using (var c = OpenConnection()) foreach (var item in cart) using (var cmd = new SqlCommand("SELECT ProductID,ProductName,Price,ImageUrl,StockQuantity FROM Product WHERE ProductID=@id AND IsActive=1", c)) { cmd.Parameters.AddWithValue("@id", item.Key); using (var r = cmd.ExecuteReader()) if (r.Read()) { var price = Convert.ToDecimal(r["Price"]); total += price * item.Value; rows.Add(new { ProductID = item.Key, Name = r["ProductName"].ToString(), Price = price, Quantity = item.Value, ImageUrl = r["ImageUrl"].ToString(), Subtotal = price * item.Value }); } }
            return Json(new { success = true, items = rows, total = total }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost] public JsonResult Checkout(string receiverName, string phone, string address)
        {
            if (Session["Username"] == null) return Json(new { success = false, message = "Vui lòng đăng nhập để thanh toán.", loginRequired = true });
            var cart = Session["ProductCart"] as Dictionary<int, int>; if (cart == null || cart.Count == 0) return Json(new { success = false, message = "Giỏ hàng đang trống." });
            if (String.IsNullOrWhiteSpace(receiverName) || String.IsNullOrWhiteSpace(phone) || String.IsNullOrWhiteSpace(address)) return Json(new { success = false, message = "Vui lòng nhập thông tin nhận hàng." });
            using (var c = OpenConnection()) using (var tx = c.BeginTransaction()) try
            {
                decimal total = 0; var details = new List<Tuple<int,int,decimal>>();
                foreach (var item in cart) using (var cmd = new SqlCommand("SELECT Price,StockQuantity FROM Product WITH(UPDLOCK,HOLDLOCK) WHERE ProductID=@id AND IsActive=1", c, tx)) { cmd.Parameters.AddWithValue("@id", item.Key); using (var r = cmd.ExecuteReader()) { if (!r.Read() || Convert.ToInt32(r["StockQuantity"]) < item.Value) throw new Exception("Sản phẩm không đủ tồn kho."); var p = Convert.ToDecimal(r["Price"]); total += p * item.Value; details.Add(Tuple.Create(item.Key,item.Value,p)); } }
                int orderId; using (var cmd = new SqlCommand("INSERT Orders(Username,ReceiverName,Phone,ShippingAddress,OrderDate,TotalAmount,OrderStatus) VALUES(@u,@n,@p,@a,GETDATE(),@t,N'Chờ duyệt'); SELECT SCOPE_IDENTITY();", c, tx)) { cmd.Parameters.AddWithValue("@u", Session["Username"].ToString()); cmd.Parameters.AddWithValue("@n", receiverName.Trim()); cmd.Parameters.AddWithValue("@p", phone.Trim()); cmd.Parameters.AddWithValue("@a", address.Trim()); cmd.Parameters.AddWithValue("@t", total); orderId = Convert.ToInt32(cmd.ExecuteScalar()); }
                foreach (var d in details) { using (var cmd = new SqlCommand("INSERT OrderItem(OrderID,ProductID,Quantity,UnitPrice) VALUES(@o,@p,@q,@u); UPDATE Product SET StockQuantity=StockQuantity-@q WHERE ProductID=@p", c, tx)) { cmd.Parameters.AddWithValue("@o", orderId); cmd.Parameters.AddWithValue("@p", d.Item1); cmd.Parameters.AddWithValue("@q", d.Item2); cmd.Parameters.AddWithValue("@u", d.Item3); cmd.ExecuteNonQuery(); } }
                tx.Commit(); Session["ProductCart"] = null; return Json(new { success = true, message = "Đặt hàng thành công.", orderId = orderId, total = total });
            }
            catch (Exception ex) { tx.Rollback(); return Json(new { success = false, message = ex.Message }); }
        }
        [HttpGet] public JsonResult MyOrders()
        {
            if (Session["Username"] == null) return Json(new { success = false, message = "Vui lòng đăng nhập." }, JsonRequestBehavior.AllowGet);
            using (var c = OpenConnection()) using (var cmd = new SqlCommand("SELECT OrderID,OrderDate,TotalAmount,OrderStatus,ShippingAddress FROM Orders WHERE Username=@u ORDER BY OrderDate DESC", c)) { cmd.Parameters.AddWithValue("@u", Session["Username"].ToString()); var t = new DataTable(); new SqlDataAdapter(cmd).Fill(t); return Json(ToRows(t), JsonRequestBehavior.AllowGet); }
        }
        [HttpGet] public JsonResult Orders()
        {
            if (!IsAdmin) return Json(new { success = false, message = "Không có quyền." }, JsonRequestBehavior.AllowGet); using (var c = OpenConnection()) { var t = new DataTable(); new SqlDataAdapter("SELECT OrderID,Username,ReceiverName,Phone,ShippingAddress,OrderDate,TotalAmount,OrderStatus FROM Orders ORDER BY OrderDate DESC", c).Fill(t); return Json(ToRows(t), JsonRequestBehavior.AllowGet); }
        }
        [HttpPost] public JsonResult UpdateOrderStatus(int id, string status)
        {
            if (!IsAdmin) return Json(new { success = false, message = "Không có quyền." }); var valid = new[] { "Chờ duyệt", "Đang giao", "Đã giao", "Đã hủy" }; if (!valid.Contains(status)) return Json(new { success = false, message = "Trạng thái không hợp lệ." });
            using (var c = OpenConnection()) using (var cmd = new SqlCommand("UPDATE Orders SET OrderStatus=@s WHERE OrderID=@id", c)) { cmd.Parameters.AddWithValue("@s", status); cmd.Parameters.AddWithValue("@id", id); return Json(new { success = cmd.ExecuteNonQuery() > 0 }); }
        }
    }
}
