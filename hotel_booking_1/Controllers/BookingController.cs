using System;
using System.Linq;
using System.Web.Mvc;
using hotel_booking_1.Models;

namespace hotel_booking_1.Controllers
{
    public class BookingController : Controller
    {
        private readonly HotelBookingDbEntities db = new HotelBookingDbEntities();

        [HttpPost]
        public JsonResult Create(int roomId, DateTime checkin, DateTime checkout, int quantity = 1)
        {
            if (Session["CustomerID"] == null || Convert.ToInt32(Session["CustomerID"]) <= 0)
                return Json(new { success = false, message = "Bạn cần đăng nhập trước khi đặt phòng.", loginRequired = true });
            checkin = checkin.Date; checkout = checkout.Date;
            if (checkin < DateTime.Today || checkout <= checkin || quantity < 1 || quantity > 10)
                return Json(new { success = false, message = "Thông tin ngày hoặc số lượng phòng không hợp lệ." });
            var room = db.Room.FirstOrDefault(r => r.RoomID == roomId);
            if (room == null) return Json(new { success = false, message = "Không tìm thấy phòng." });

            // Giữ khóa dòng phòng trong transaction để hai yêu cầu đồng thời không thể đặt vượt tồn.
            using (var tx = db.Database.BeginTransaction())
            {
                var inventory = db.Database.SqlQuery<int>("SELECT TotalQuantity FROM dbo.Room WITH(UPDLOCK,HOLDLOCK) WHERE RoomID=@p0", roomId).FirstOrDefault();
                var reserved = db.Database.SqlQuery<int>("SELECT ISNULL(SUM(d.Quantity),0) FROM dbo.BookingDetail d INNER JOIN dbo.Booking b ON b.BookingID=d.BookingID WHERE d.RoomID=@p0 AND ISNULL(b.BookingStatus,N'') NOT IN (N'Cancelled',N'Đã hủy') AND b.CheckInDate<@p2 AND b.CheckOutDate>@p1", roomId, checkin, checkout).FirstOrDefault();
                if (inventory < 1 || reserved + quantity > inventory)
                    return Json(new { success = false, message = "Không đủ số lượng phòng trống trong khoảng thời gian này." });

                var nights = (checkout - checkin).Days;
                var booking = new Booking { CustomerID = Convert.ToInt32(Session["CustomerID"]), BookingDate = DateTime.Now, CheckInDate = checkin, CheckOutDate = checkout, TotalAmount = room.RoomPrice * nights * quantity, PaymentStatus = "Unpaid", BookingStatus = "Pending" };
                booking.BookingDetail.Add(new BookingDetail { RoomID = roomId, Quantity = quantity, UnitPrice = room.RoomPrice });
                db.Booking.Add(booking);
                db.SaveChanges();
                tx.Commit();
                return Json(new { success = true, message = "Đặt phòng thành công. Đơn đang chờ xác nhận.", bookingId = booking.BookingID, total = booking.TotalAmount });
            }
        }

        [HttpGet]
        public JsonResult MyBookings()
        {
            if (Session["CustomerID"] == null) return Json(new { success = false, message = "Vui lòng đăng nhập." }, JsonRequestBehavior.AllowGet);
            int id = Convert.ToInt32(Session["CustomerID"]);
            var items = db.Booking.Where(b => b.CustomerID == id).OrderByDescending(b => b.BookingDate).Select(b => new { b.BookingID, b.BookingDate, b.CheckInDate, b.CheckOutDate, b.TotalAmount, b.PaymentStatus, b.BookingStatus, Rooms = b.BookingDetail.Select(d => d.Room.RoomName) }).ToList();
            return Json(new { success = true, bookings = items }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
