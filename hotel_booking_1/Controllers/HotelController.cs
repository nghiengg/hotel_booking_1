using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using hotel_booking_1.Models;

namespace hotel_booking_1.Controllers
{
    public class HotelController : Controller
    {
        private readonly HotelBookingDbEntities db = new HotelBookingDbEntities();

        public class HotelCardItem
        {
            public int HotelID { get; set; }
            public string HotelName { get; set; }
            public string City { get; set; }
            public string Address { get; set; }
            public string Description { get; set; }
            public string ImageUrl { get; set; }
            public decimal? MinPrice { get; set; }
            public bool Liked { get; set; }
        }
        public class HotelRoomItem
        {
            public int RoomID { get; set; }
            public string RoomName { get; set; }
            public string RoomDescription { get; set; }
            public decimal RoomPrice { get; set; }
            public string RoomImage { get; set; }
            public string Category { get; set; }
            public int MaxGuests { get; set; }
            public int TotalQuantity { get; set; }
        }
        public class CartRow
        {
            public int CartID { get; set; }
            public int HotelID { get; set; }
            public string HotelName { get; set; }
            public string City { get; set; }
            public string ImageUrl { get; set; }
            public int? RoomID { get; set; }
            public string RoomName { get; set; }
            public decimal? RoomPrice { get; set; }
            public DateTime? CheckIn { get; set; }
            public DateTime? CheckOut { get; set; }
            public int Quantity { get; set; }
        }

        private string CurrentUser { get { return Session["Username"] == null ? null : Convert.ToString(Session["Username"]); } }

        private HashSet<int> LikedIds()
        {
            if (CurrentUser == null) return new HashSet<int>();
            return new HashSet<int>(db.Database.SqlQuery<int>("SELECT HotelID FROM dbo.HotelCart WHERE Username=@p0 AND RoomID IS NULL", CurrentUser).ToList());
        }

        private static bool TryDate(string s, out DateTime d)
        {
            return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d);
        }

        // 5 khách sạn nổi bật cho trang chủ: ưu tiên khách sạn được admin đánh dấu nổi bật, sau đó theo số lượt đặt.
        [HttpGet]
        public JsonResult Featured()
        {
            var items = db.Database.SqlQuery<HotelCardItem>(
                "SELECT TOP 5 h.HotelID,h.HotelName,h.City,h.Address,h.Description,h.ImageUrl,(SELECT MIN(r.RoomPrice) FROM dbo.Room r WHERE r.HotelID=h.HotelID) AS MinPrice,CAST(0 AS BIT) AS Liked " +
                "FROM dbo.Hotel h ORDER BY h.IsFeatured DESC, (SELECT COUNT(1) FROM dbo.BookingDetail d INNER JOIN dbo.Room r2 ON r2.RoomID=d.RoomID WHERE r2.HotelID=h.HotelID) DESC, h.HotelID DESC").ToList();
            var liked = LikedIds();
            foreach (var i in items) i.Liked = liked.Contains(i.HotelID);
            return Json(items, JsonRequestBehavior.AllowGet);
        }

        // Tìm kiếm: trả về danh sách KHÁCH SẠN có ít nhất 1 loại phòng còn trống theo tiêu chí.
        [HttpGet]
        public JsonResult Search(DateTime? checkin, DateTime? checkout, int adults = 1, int children = 0, int rooms = 1, string keyword = "")
        {
            var start = (checkin ?? DateTime.Today).Date;
            var end = (checkout ?? start.AddDays(1)).Date;
            if (end <= start) return Json(new { success = false, message = "Ngày trả phòng phải sau ngày nhận phòng." }, JsonRequestBehavior.AllowGet);
            if (adults < 1 || children < 0 || rooms < 1) return Json(new { success = false, message = "Số khách hoặc số phòng không hợp lệ." }, JsonRequestBehavior.AllowGet);
            var kw = "%" + (keyword ?? "").Trim() + "%";
            var sql =
                "SELECT h.HotelID,h.HotelName,h.City,h.Address,h.Description,h.ImageUrl,MIN(r.RoomPrice) AS MinPrice,CAST(0 AS BIT) AS Liked " +
                "FROM dbo.Hotel h INNER JOIN dbo.Room r ON r.HotelID=h.HotelID " +
                "WHERE (@p4='%%' OR h.HotelName LIKE @p4 OR h.City LIKE @p4 OR h.Address LIKE @p4 OR r.RoomName LIKE @p4) " +
                "AND ISNULL((SELECT SUM(d.Quantity) FROM dbo.BookingDetail d INNER JOIN dbo.Booking b ON b.BookingID=d.BookingID WHERE d.RoomID=r.RoomID AND ISNULL(b.BookingStatus,N'') NOT IN (N'Cancelled',N'Đã hủy') AND b.CheckInDate<@p1 AND b.CheckOutDate>@p0),0)+@p2<=r.TotalQuantity " +
                "AND r.MaxGuests*@p2>=@p3 " +
                "GROUP BY h.HotelID,h.HotelName,h.City,h.Address,h.Description,h.ImageUrl ORDER BY h.HotelName";
            var items = db.Database.SqlQuery<HotelCardItem>(sql, start, end, rooms, adults + children, kw).ToList();
            var liked = LikedIds();
            foreach (var i in items) i.Liked = liked.Contains(i.HotelID);
            return Json(new { success = true, hotels = items }, JsonRequestBehavior.AllowGet);
        }

        // Chi tiết 1 khách sạn + các loại phòng.
        [HttpGet]
        public JsonResult Detail(int id)
        {
            var hotel = db.Database.SqlQuery<HotelCardItem>(
                "SELECT h.HotelID,h.HotelName,h.City,h.Address,h.Description,h.ImageUrl,(SELECT MIN(r.RoomPrice) FROM dbo.Room r WHERE r.HotelID=h.HotelID) AS MinPrice,CAST(0 AS BIT) AS Liked FROM dbo.Hotel h WHERE h.HotelID=@p0", id).FirstOrDefault();
            if (hotel == null) return Json(new { success = false, message = "Không tìm thấy khách sạn." }, JsonRequestBehavior.AllowGet);
            hotel.Liked = LikedIds().Contains(id);
            var rooms = db.Database.SqlQuery<HotelRoomItem>(
                "SELECT r.RoomID,r.RoomName,r.RoomDescription,r.RoomPrice,r.RoomImage,c.CategoryName AS Category,r.MaxGuests,r.TotalQuantity FROM dbo.Room r INNER JOIN dbo.RoomCategory c ON c.RoomCategoryID=r.RoomCategoryID WHERE r.HotelID=@p0 ORDER BY r.RoomPrice", id).ToList();
            return Json(new { success = true, hotel = hotel, rooms = rooms }, JsonRequestBehavior.AllowGet);
        }

        // Bấm trái tim: bật/tắt khách sạn trong giỏ hàng của người dùng.
        [HttpPost]
        public JsonResult ToggleFavorite(int hotelId)
        {
            if (CurrentUser == null) return Json(new { success = false, message = "Vui lòng đăng nhập để lưu khách sạn vào giỏ hàng.", loginRequired = true });
            var exists = db.Database.SqlQuery<int>("SELECT COUNT(1) FROM dbo.HotelCart WHERE Username=@p0 AND HotelID=@p1 AND RoomID IS NULL", CurrentUser, hotelId).FirstOrDefault();
            if (exists > 0)
            {
                db.Database.ExecuteSqlCommand("DELETE dbo.HotelCart WHERE Username=@p0 AND HotelID=@p1 AND RoomID IS NULL", CurrentUser, hotelId);
                return Json(new { success = true, liked = false });
            }
            var hotelOk = db.Database.SqlQuery<int>("SELECT COUNT(1) FROM dbo.Hotel WHERE HotelID=@p0", hotelId).FirstOrDefault();
            if (hotelOk == 0) return Json(new { success = false, message = "Không tìm thấy khách sạn." });
            db.Database.ExecuteSqlCommand("INSERT dbo.HotelCart(Username,HotelID,RoomID,CheckIn,CheckOut,Quantity) VALUES(@p0,@p1,NULL,NULL,NULL,1)", CurrentUser, hotelId);
            return Json(new { success = true, liked = true });
        }

        // Nút "Thêm vào giỏ hàng" ở trang chi tiết: chọn phòng + ngày giờ nhận/trả.
        [HttpPost]
        public JsonResult AddToCart(int hotelId, int roomId, string checkin, string checkout, int quantity = 1)
        {
            if (CurrentUser == null) return Json(new { success = false, message = "Vui lòng đăng nhập để thêm vào giỏ hàng.", loginRequired = true });
            DateTime ci, co;
            if (!TryDate(checkin, out ci) || !TryDate(checkout, out co)) return Json(new { success = false, message = "Vui lòng chọn ngày giờ nhận và trả phòng." });
            if (ci.Date < DateTime.Today || co.Date <= ci.Date) return Json(new { success = false, message = "Ngày trả phòng phải sau ngày nhận phòng." });
            if (quantity < 1 || quantity > 10) return Json(new { success = false, message = "Số lượng phòng không hợp lệ." });
            var ok = db.Database.SqlQuery<int>("SELECT COUNT(1) FROM dbo.Room WHERE RoomID=@p0 AND HotelID=@p1", roomId, hotelId).FirstOrDefault();
            if (ok == 0) return Json(new { success = false, message = "Phòng không thuộc khách sạn này." });
            db.Database.ExecuteSqlCommand("INSERT dbo.HotelCart(Username,HotelID,RoomID,CheckIn,CheckOut,Quantity) VALUES(@p0,@p1,@p2,@p3,@p4,@p5)", CurrentUser, hotelId, roomId, ci, co, quantity);
            return Json(new { success = true, message = "Đã thêm vào giỏ hàng." });
        }

        [HttpGet]
        public JsonResult Cart()
        {
            if (CurrentUser == null) return Json(new { success = false, message = "Vui lòng đăng nhập để xem giỏ hàng.", loginRequired = true }, JsonRequestBehavior.AllowGet);
            var items = db.Database.SqlQuery<CartRow>(
                "SELECT c.CartID,c.HotelID,h.HotelName,h.City,h.ImageUrl,c.RoomID,r.RoomName,r.RoomPrice,c.CheckIn,c.CheckOut,c.Quantity " +
                "FROM dbo.HotelCart c INNER JOIN dbo.Hotel h ON h.HotelID=c.HotelID LEFT JOIN dbo.Room r ON r.RoomID=c.RoomID WHERE c.Username=@p0 ORDER BY c.CreatedAt DESC", CurrentUser).ToList();
            return Json(new { success = true, items = items.Select(i => new { i.CartID, i.HotelID, i.HotelName, i.City, i.ImageUrl, i.RoomID, i.RoomName, i.RoomPrice, i.Quantity, CheckIn = i.CheckIn.HasValue ? i.CheckIn.Value.ToString("yyyy-MM-dd'T'HH:mm") : null, CheckOut = i.CheckOut.HasValue ? i.CheckOut.Value.ToString("yyyy-MM-dd'T'HH:mm") : null }) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult RemoveFromCart(int id)
        {
            if (CurrentUser == null) return Json(new { success = false, message = "Vui lòng đăng nhập.", loginRequired = true });
            db.Database.ExecuteSqlCommand("DELETE dbo.HotelCart WHERE CartID=@p0 AND Username=@p1", id, CurrentUser);
            return Json(new { success = true });
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
