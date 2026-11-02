(function () {
    "use strict";
    var area = document.getElementById("authArea");
    if (!area) return;

    function makeLink(href, text, className) {
        var a = document.createElement("a");
        a.href = href;
        a.textContent = text;
        if (className) a.className = className;
        return a;
    }

    fetch("/Auth/Status", { credentials: "same-origin" })
        .then(function (r) { return r.json(); })
        .then(function (user) {
            area.innerHTML = "";
            if (!user.loggedIn) {
                area.appendChild(makeLink("../Sign_in/Signin.html", "Đăng nhập", "btn-primary"));
                return;
            }

            var name = document.createElement("span");
            name.className = "account-name";
            name.textContent = "Xin chào, " + (user.displayName || "bạn");
            area.appendChild(name);

            if (user.isAdmin) {
                area.appendChild(makeLink("/Admin/Index", "Quản trị", "btn-outline"));
            }

            var history = makeLink("../Booking/History.html", "Đặt phòng của tôi", "btn-outline");
            area.appendChild(history);

            var logout = document.createElement("button");
            logout.type = "button";
            logout.className = "btn-primary logout-btn";
            logout.textContent = "Đăng xuất";
            logout.addEventListener("click", function () {
                logout.disabled = true;
                fetch("/Auth/Logout", {
                    method: "POST",
                    credentials: "same-origin",
                    headers: { "Content-Type": "application/x-www-form-urlencoded" },
                    body: ""
                }).then(function (r) { return r.json(); })
                  .then(function (result) {
                      if (result.success) window.location.href = "../Home/Home.html";
                      else { logout.disabled = false; alert("Không thể đăng xuất. Vui lòng thử lại."); }
                  }).catch(function () {
                      logout.disabled = false;
                      alert("Không thể kết nối máy chủ.");
                  });
            });
            area.appendChild(logout);
        })
        .catch(function () {
            area.innerHTML = "";
            area.appendChild(makeLink("../Sign_in/Signin.html", "Đăng nhập", "btn-primary"));
        });
})();