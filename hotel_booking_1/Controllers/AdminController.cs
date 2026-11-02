using System;
using System.Linq;
using System.Web.Mvc;
using hotel_booking_1.Models;

namespace hotel_booking_1.Controllers
{
    public class AdminController : Controller
    {
        private readonly HotelBookingDbEntities db = new HotelBookingDbEntities();
        private bool IsAdmin() { return Session["Role"] != null && Convert.ToString(Session["Role"]) == "A"; }
        public ActionResult Index() { if (!IsAdmin()) return RedirectToAction("Index", "Home"); return Redirect("~/frontend/Admin/Admin.html"); }

        [HttpGet] public JsonResult Dashboard()
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Bạn không có quyền truy cập." }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, users = db.User.Count(), rooms = db.Room.Count(), bookings = db.Booking.Count(), pending = db.Booking.Count(x => x.BookingStatus == "Pending") }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet] public JsonResult Categories() { if (!IsAdmin()) return Json(new { success = false }, JsonRequestBehavior.AllowGet); return Json(db.RoomCategory.Select(x => new { x.RoomCategoryID, x.CategoryName }).ToList(), JsonRequestBehavior.AllowGet); }
        [HttpPost] public JsonResult SaveCategory(int? id, string name)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });
            if (String.IsNullOrWhiteSpace(name)) return Json(new { success = false, message = "Tên danh mục không được để trống." });
            var item = id.HasValue ? db.RoomCategory.Find(id.Value) : new RoomCategory();
            if (item == null) return Json(new { success = false, message = "Không tìm thấy danh mục." });
            item.CategoryName = name.Trim(); if (!id.HasValue) db.RoomCategory.Add(item); db.SaveChanges();
            return Json(new { success = true, message = "Đã lưu danh mục." });
        }
        [HttpPost] public JsonResult DeleteCategory(int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });
            var item = db.RoomCategory.Find(id); if (item == null) return Json(new { success = false, message = "Không tìm thấy danh mục." });
            if (item.Room.Any()) return Json(new { success = false, message = "Không thể xóa danh mục đang có phòng." });
            db.RoomCategory.Remove(item); db.SaveChanges(); return Json(new { success = true });
        }
        [HttpGet] public JsonResult Hotels()
        {
            if (!IsAdmin()) return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            var items = db.Database.SqlQuery<HotelItem>("SELECT HotelID,HotelName,City,Address,Description,ImageUrl,IsFeatured FROM dbo.Hotel ORDER BY HotelName").ToList();
            return Json(items, JsonRequestBehavior.AllowGet);
        }
        public class HotelItem { public int HotelID { get; set; } public string HotelName { get; set; } public string City { get; set; } public string Address { get; set; } public string Description { get; set; } public string ImageUrl { get; set; } public bool IsFeatured { get; set; } }
        [HttpPost] public JsonResult SaveHotel(int? id, string name, string city, string address, string description, string imageUrl, string featured = null, decimal? price = null, int? categoryId = null, int maxGuests = 2, int totalQuantity = 1)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });
            if (String.IsNullOrWhiteSpace(name)) return Json(new { success = false, message = "Tên khách sạn không được để trống." });
            if (id.HasValue) { db.Database.ExecuteSqlCommand("UPDATE dbo.Hotel SET HotelName=@p0,City=@p1,Address=@p2,Description=@p3,ImageUrl=@p4 WHERE HotelID=@p5", name.Trim(), city ?? "", address ?? "", description ?? "", imageUrl ?? "", id.Value); return Json(new { success = true, message = "Đã lưu khách sạn." }); }
            var isFeatured = featured == "on" || featured == "true" || featured == "1";
            var newId = db.Database.SqlQuery<int>("INSERT dbo.Hotel(HotelName,City,Address,Description,ImageUrl,IsFeatured) VALUES(@p0,@p1,@p2,@p3,@p4,@p5); SELECT CAST(SCOPE_IDENTITY() AS INT)", name.Trim(), city ?? "", address ?? "", description ?? "", imageUrl ?? "", isFeatured).First();
            // Nếu admin nhập giá + loại phòng thì tạo luôn 1 phòng mặc định để khách sạn xuất hiện khi tìm kiếm / đặt phòng.
            if (price.HasValue && categoryId.HasValue)
            {
                if (price.Value < 0 || maxGuests < 1 || totalQuantity < 1 || !db.RoomCategory.Any(c => c.RoomCategoryID == categoryId.Value)) return Json(new { success = false, message = "Đã thêm khách sạn nhưng thông tin phòng mặc định không hợp lệ. Hãy thêm phòng ở mục Phòng khách sạn." });
                db.Database.ExecuteSqlCommand("INSERT dbo.Room(RoomCategoryID,RoomName,RoomDescription,RoomPrice,RoomImage,HotelID,MaxGuests,TotalQuantity) VALUES(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7)", categoryId.Value, "Phòng tiêu chuẩn", String.IsNullOrWhiteSpace(description) ? "Phòng tiêu chuẩn" : description.Trim(), price.Value, imageUrl ?? "", newId, maxGuests, totalQuantity);
            }
            return Json(new { success = true, message = "Đã thêm khách sạn mới." });
        }
        [HttpPost] public JsonResult ToggleFeaturedHotel(int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });
            db.Database.ExecuteSqlCommand("UPDATE dbo.Hotel SET IsFeatured = CASE WHEN IsFeatured=1 THEN 0 ELSE 1 END WHERE HotelID=@p0", id);
            return Json(new { success = true, message = "Đã cập nhật trạng thái nổi bật." });
        }
        [HttpPost] public JsonResult DeleteHotel(int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });
            var linked = db.Database.SqlQuery<int>("SELECT COUNT(1) FROM dbo.Room WHERE HotelID=@p0", id).FirstOrDefault();
            if (linked > 0) return Json(new { success = false, message = "Khách sạn đang có phòng, không thể xóa." });
            db.Database.ExecuteSqlCommand("DELETE dbo.Hotel WHERE HotelID=@p0", id); return Json(new { success = true });
        }

        [HttpGet] public JsonResult Amenities()
        {
            if (!IsAdmin()) return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            return Json(db.Database.SqlQuery<AmenityAdminItem>("SELECT AmenityID,AmenityName FROM dbo.Amenity ORDER BY AmenityName").ToList(), JsonRequestBehavior.AllowGet);
        }
        public class AmenityAdminItem { public int AmenityID { get; set; } public string AmenityName { get; set; } }
        [HttpPost] public JsonResult SaveAmenity(int? id, string name)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });
            if (String.IsNullOrWhiteSpace(name)) return Json(new { success = false, message = "Nhập tên tiện ích." });
            if (id.HasValue) db.Database.ExecuteSqlCommand("UPDATE dbo.Amenity SET AmenityName=@p0 WHERE AmenityID=@p1", name.Trim(), id.Value);
            else db.Database.ExecuteSqlCommand("INSERT dbo.Amenity(AmenityName) VALUES(@p0)", name.Trim());
            return Json(new { success = true, message = "Đã lưu tiện ích." });
        }
        [HttpPost] public JsonResult DeleteAmenity(int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });
            db.Database.ExecuteSqlCommand("DELETE dbo.RoomAmenity WHERE AmenityID=@p0; DELETE dbo.Amenity WHERE AmenityID=@p0", id);
            return Json(new { success = true });
        }
        [HttpPost] public JsonResult SetRoomAmenities(int roomId, string amenityIds)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });
            var ids = (amenityIds ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => { int n; return Int32.TryParse(x, out n) ? n : 0; }).Where(x => x > 0).Distinct().ToList();
            using (var tx = db.Database.BeginTransaction())
            {
                db.Database.ExecuteSqlCommand("DELETE dbo.RoomAmenity WHERE RoomID=@p0", roomId);
                foreach (var id in ids) db.Database.ExecuteSqlCommand("INSERT dbo.RoomAmenity(RoomID,AmenityID) VALUES(@p0,@p1)", roomId, id);
                tx.Commit();
            }
            return Json(new { success = true, message = "Đã cập nhật tiện ích cho phòng." });
        }

        [HttpGet] public JsonResult Rooms()
        {
            if (!IsAdmin()) return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            return Json(db.Database.SqlQuery<RoomAdminItem>("SELECT r.RoomID,r.RoomName,r.RoomDescription,r.RoomPrice,r.RoomImage,r.RoomCategoryID,c.CategoryName AS Category,r.HotelID,r.MaxGuests,r.TotalQuantity FROM dbo.Room r JOIN dbo.RoomCategory c ON r.RoomCategoryID=c.RoomCategoryID").ToList(), JsonRequestBehavior.AllowGet);
        }
        public class RoomAdminItem { public int RoomID { get; set; } public string RoomName { get; set; } public string RoomDescription { get; set; } public decimal RoomPrice { get; set; } public string RoomImage { get; set; } public int RoomCategoryID { get; set; } public string Category { get; set; } public int? HotelID { get; set; } public int MaxGuests { get; set; } public int TotalQuantity { get; set; } }
        [HttpPost] public JsonResult SaveRoom(int? id, int categoryId, string name, string description, decimal price, string image, int? hotelId = null, int maxGuests = 2, int totalQuantity = 1)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });
            if (String.IsNullOrWhiteSpace(name) || String.IsNullOrWhiteSpace(description) || price < 0 || maxGuests < 1 || totalQuantity < 1 || !db.RoomCategory.Any(c => c.RoomCategoryID == categoryId)) return Json(new { success = false, message = "Dữ liệu phòng không hợp lệ." });
            var item = id.HasValue ? db.Room.Find(id.Value) : new Room(); if (item == null) return Json(new { success = false, message = "Không tìm thấy phòng." });
            item.RoomCategoryID = categoryId; item.RoomName = name.Trim(); item.RoomDescription = description.Trim(); item.RoomPrice = price; item.RoomImage = image;
            if (!id.HasValue) db.Room.Add(item); db.SaveChanges();
            db.Database.ExecuteSqlCommand("UPDATE dbo.Room SET HotelID=@p0,MaxGuests=@p1,TotalQuantity=@p2 WHERE RoomID=@p3", (object)hotelId ?? DBNull.Value, maxGuests, totalQuantity, item.RoomID);
            return Json(new { success = true, message = "Đã lưu phòng." });
        }
        [HttpPost] public JsonResult DeleteRoom(int id)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });
            var item = db.Room.Find(id); if (item == null) return Json(new { success = false, message = "Không tìm thấy phòng." });
            if (item.BookingDetail.Any()) return Json(new { success = false, message = "Phòng đã có lịch sử đặt, không thể xóa." });
            db.Room.Remove(item); db.SaveChanges(); return Json(new { success = true });
        }
        [HttpGet] public JsonResult Bookings()
        {
            if (!IsAdmin()) return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            return Json(db.Booking.OrderByDescending(b => b.BookingDate).Select(b => new { b.BookingID, b.BookingDate, b.CheckInDate, b.CheckOutDate, b.TotalAmount, b.BookingStatus, b.PaymentStatus, Customer = b.Customer.CustomerName }).ToList(), JsonRequestBehavior.AllowGet);
        }
        [HttpPost] public JsonResult UpdateBookingStatus(int id, string status)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });
            var allowed = new[] { "Pending", "Approved", "Delivering", "Completed", "Cancelled" };
            if (!allowed.Contains(status)) return Json(new { success = false, message = "Trạng thái không hợp lệ." });
            var booking = db.Booking.Find(id); if (booking == null) return Json(new { success = false, message = "Không tìm thấy đơn." });
            booking.BookingStatus = status; db.SaveChanges(); return Json(new { success = true });
        }
        [HttpGet] public JsonResult Users()
        {
            if (!IsAdmin()) return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            return Json(db.User.Select(u => new { u.Username, u.UserRole, Name = u.Customer.Select(c => c.CustomerName).FirstOrDefault() }).ToList(), JsonRequestBehavior.AllowGet);
        }
        [HttpPost] public JsonResult UpdateUserRole(string username, string role)
        {
            if (!IsAdmin()) return Json(new { success = false, message = "Không có quyền." });
            if (String.IsNullOrWhiteSpace(username) || (role != "A" && role != "U")) return Json(new { success = false, message = "Thông tin vai trò không hợp lệ." });
            if (String.Equals(Convert.ToString(Session["Username"]), username, StringComparison.OrdinalIgnoreCase) && role != "A") return Json(new { success = false, message = "Không thể tự hạ quyền tài khoản đang đăng nhập." });
            var user = db.User.Find(username); if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });
            user.UserRole = role; db.SaveChanges(); return Json(new { success = true, message = "Đã cập nhật vai trò." });
        }
        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
