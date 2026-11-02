using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using hotel_booking_1.Models;

namespace hotel_booking_1.Controllers
{
    public class AuthController : Controller
    {
        private readonly HotelBookingDbEntities db = new HotelBookingDbEntities();

        [HttpPost]
        public JsonResult Register(string fullname, string email, string password, string phone)
        {
            if (String.IsNullOrWhiteSpace(fullname) || String.IsNullOrWhiteSpace(email) || String.IsNullOrWhiteSpace(password) || password.Length < 8)
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin; mật khẩu tối thiểu 8 ký tự." });
            email = email.Trim().ToLowerInvariant();
            if (db.User.Any(x => x.Username == email))
                return Json(new { success = false, message = "Email đã được đăng ký." });

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    db.User.Add(new User { Username = email, Password = HashPassword(password), UserRole = "U" });
                    db.SaveChanges();
                    db.Customer.Add(new Customer { Username = email, CustomerName = fullname.Trim(), CustomerEmail = email, CustomerPhone = String.IsNullOrWhiteSpace(phone) ? "Chưa cập nhật" : phone.Trim(), CustomerAddress = "" });
                    db.SaveChanges();
                    tx.Commit();
                    return Json(new { success = true, message = "Đăng ký thành công. Bạn có thể đăng nhập." });
                }
                catch
                {
                    tx.Rollback();
                    return Json(new { success = false, message = "Không thể tạo tài khoản. Hãy kiểm tra database đã chạy script cập nhật." });
                }
            }
        }

        [HttpPost]
        public JsonResult Login(string email, string password)
        {
            if (String.IsNullOrWhiteSpace(email) || String.IsNullOrEmpty(password))
                return Json(new { success = false, message = "Vui lòng nhập email và mật khẩu." });
            var username = email.Trim().ToLowerInvariant();
            var user = db.User.FirstOrDefault(x => x.Username == username);
            if (user == null || !VerifyPassword(password, user.Password))
                return Json(new { success = false, message = "Email hoặc mật khẩu không chính xác." });
            Session["Username"] = user.Username;
            Session["Role"] = user.UserRole;
            var customer = db.Customer.FirstOrDefault(x => x.Username == user.Username);
            Session["CustomerID"] = customer == null ? 0 : customer.CustomerID;
            Session["DisplayName"] = customer == null ? user.Username : customer.CustomerName;
            return Json(new { success = true, message = "Đăng nhập thành công.", displayName = Convert.ToString(Session["DisplayName"]), role = user.UserRole == "A" ? "Quản trị viên" : "Khách hàng", redirect = user.UserRole == "A" ? "/Admin/Index" : "/Home/Index" });
        }

        [HttpGet]
        public JsonResult Status()
        {
            var loggedIn = Session["Username"] != null;
            return Json(new {
                loggedIn = loggedIn,
                displayName = loggedIn ? Convert.ToString(Session["DisplayName"]) : "",
                role = Convert.ToString(Session["Role"]),
                isAdmin = loggedIn && Convert.ToString(Session["Role"]) == "A"
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return Json(new { success = true, redirect = "/Home/Index" });
        }

        public static string HashPassword(string password)
        {
            using (var derive = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("HotelBooking-v1-Salt"), 10000))
                return "P$" + Convert.ToBase64String(derive.GetBytes(30));
        }

        private static bool VerifyPassword(string password, string stored)
        {
            if (String.IsNullOrEmpty(stored)) return false;
            // Tương thích tài khoản cũ lưu mật khẩu dạng rõ; khi đăng nhập thành công sẽ nâng cấp hash.
            stored = stored.Trim();
            if (!stored.StartsWith("P$", StringComparison.Ordinal)) return stored == password;
            return HashPassword(password) == stored;
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
