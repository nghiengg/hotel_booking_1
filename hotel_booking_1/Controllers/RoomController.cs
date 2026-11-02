using System;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using hotel_booking_1.Models;

namespace hotel_booking_1.Controllers
{
    public class RoomController : Controller
    {
        private readonly HotelBookingDbEntities db = new HotelBookingDbEntities();

        [HttpGet]
        public JsonResult Search(DateTime? checkin, DateTime? checkout, int adults = 1, int children = 0, int rooms = 1, string keyword = "", int? categoryId = null, int? amenityId = null, decimal? minPrice = null, decimal? maxPrice = null, string sort = "recommended")
        {
            var start = (checkin ?? DateTime.Today).Date;
            var end = (checkout ?? start.AddDays(1)).Date;
            if (end <= start) return Json(new { success = false, message = "Ngày trả phòng phải sau ngày nhận phòng." }, JsonRequestBehavior.AllowGet);
            if (adults < 1 || children < 0 || rooms < 1) return Json(new { success = false, message = "Số khách hoặc số phòng không hợp lệ." }, JsonRequestBehavior.AllowGet);

            var query = db.Room.Include(x => x.RoomCategory).AsQueryable();
            if (!String.IsNullOrWhiteSpace(keyword))
            {
                var matchingIds = db.Database.SqlQuery<int>("SELECT r.RoomID FROM dbo.Room r LEFT JOIN dbo.Hotel h ON r.HotelID=h.HotelID WHERE r.RoomName LIKE @p0 OR r.RoomDescription LIKE @p0 OR h.HotelName LIKE @p0 OR h.City LIKE @p0 OR h.Address LIKE @p0", "%" + keyword + "%").ToList();
                query = query.Where(x => matchingIds.Contains(x.RoomID));
            }
            if (categoryId.HasValue) query = query.Where(x => x.RoomCategoryID == categoryId.Value);
            if (amenityId.HasValue) { var amenityRooms = db.Database.SqlQuery<int>("SELECT RoomID FROM dbo.RoomAmenity WHERE AmenityID=@p0", amenityId.Value).ToList(); query = query.Where(x => amenityRooms.Contains(x.RoomID)); }
            if (minPrice.HasValue) query = query.Where(x => x.RoomPrice >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(x => x.RoomPrice <= maxPrice.Value);
            var roomsList = query.ToList();
            var availableIds = db.Database.SqlQuery<int>("SELECT r.RoomID FROM dbo.Room r WHERE ISNULL((SELECT SUM(d.Quantity) FROM dbo.BookingDetail d INNER JOIN dbo.Booking b ON b.BookingID=d.BookingID WHERE d.RoomID=r.RoomID AND ISNULL(b.BookingStatus,N'') NOT IN (N'Cancelled',N'Đã hủy') AND b.CheckInDate<@p1 AND b.CheckOutDate>@p0),0) + @p2 <= r.TotalQuantity AND r.MaxGuests*@p2>=@p3", start, end, rooms, adults + children).ToList();
            var available = roomsList.Where(r => availableIds.Contains(r.RoomID)).ToList();
            if (sort == "price-asc") available = available.OrderBy(r => r.RoomPrice).ToList();
            else if (sort == "price-desc") available = available.OrderByDescending(r => r.RoomPrice).ToList();
            else if (sort == "name") available = available.OrderBy(r => r.RoomName).ToList();
            var roomIds = available.Select(r => r.RoomID).ToList();
            var hotelInfo = db.Database.SqlQuery<RoomHotelInfo>("SELECT r.RoomID,ISNULL(h.HotelName,N'') AS HotelName,ISNULL(h.City,N'') AS City FROM dbo.Room r LEFT JOIN dbo.Hotel h ON r.HotelID=h.HotelID").ToList().Where(x => roomIds.Contains(x.RoomID)).ToDictionary(x => x.RoomID);
            return Json(new { success = true, checkin = start.ToString("yyyy-MM-dd"), checkout = end.ToString("yyyy-MM-dd"), rooms = available.Select(r => new { r.RoomID, r.RoomName, r.RoomDescription, r.RoomPrice, r.RoomImage, Category = r.RoomCategory.CategoryName, HotelName = hotelInfo.ContainsKey(r.RoomID) ? hotelInfo[r.RoomID].HotelName : "", City = hotelInfo.ContainsKey(r.RoomID) ? hotelInfo[r.RoomID].City : "" }) }, JsonRequestBehavior.AllowGet);
        }

        public class RoomHotelInfo { public int RoomID { get; set; } public string HotelName { get; set; } public string City { get; set; } }

        [HttpGet]
        public JsonResult Amenities()
        {
            var items = db.Database.SqlQuery<AmenityItem>("SELECT AmenityID, AmenityName FROM dbo.Amenity ORDER BY AmenityName").ToList();
            return Json(items, JsonRequestBehavior.AllowGet);
        }
        public class AmenityItem { public int AmenityID { get; set; } public string AmenityName { get; set; } }

        [HttpGet]
        public JsonResult Categories()
        {
            return Json(db.RoomCategory.Select(c => new { c.RoomCategoryID, c.CategoryName }).ToList(), JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
