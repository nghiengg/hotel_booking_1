// Dùng chung: vẽ thẻ khách sạn (giống thẻ ở trang chủ), bấm thẻ để xem chi tiết, bấm trái tim để lưu vào giỏ hàng.
(function () {
    "use strict";
    var FALLBACK = "https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=800&q=85";

    function esc(s) {
        return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
        });
    }

    function cardHtml(h) {
        var img = (h.ImageUrl || FALLBACK).replace(/["'()\\]/g, encodeURIComponent);
        var price = h.MinPrice != null
            ? '<span>Từ</span> <strong>' + Number(h.MinPrice).toLocaleString("vi-VN") + ' ₫</strong> <small>/đêm</small>'
            : '<small>Chưa có phòng</small>';
        var place = [h.City, h.Address].filter(function (x) { return x; }).join(" · ");
        return '<article class="hotel-card" data-id="' + h.HotelID + '">' +
            '<div class="hotel-image" style="background-image:url(\'' + img + '\')">' +
            '<button type="button" class="heart' + (h.Liked ? ' active' : '') + '" aria-label="Lưu khách sạn">' + (h.Liked ? '♥' : '♡') + '</button>' +
            '</div>' +
            '<div class="hotel-info"><h3>' + esc(h.HotelName) + '</h3>' +
            '<div class="location">' + esc(place) + '</div>' +
            '<div class="price">' + price + '</div></div></article>';
    }

    function setHeart(btn, liked) {
        btn.classList.toggle("active", liked);
        btn.textContent = liked ? "♥" : "♡";
    }

    // Gắn sự kiện 1 lần cho cả vùng chứa (dùng được cả khi thẻ bị nhân bản trong carousel).
    function bind(container) {
        container.addEventListener("click", function (e) {
            var heart = e.target.closest(".heart");
            var card = e.target.closest(".hotel-card[data-id]");
            if (!card) return;
            var id = card.dataset.id;
            if (heart) {
                e.stopPropagation();
                fetch("/Hotel/ToggleFavorite", {
                    method: "POST",
                    credentials: "same-origin",
                    headers: { "Content-Type": "application/x-www-form-urlencoded" },
                    body: new URLSearchParams({ hotelId: id })
                }).then(function (r) { return r.json(); }).then(function (r) {
                    if (!r.success) {
                        alert(r.message || "Không thể lưu khách sạn.");
                        if (r.loginRequired) location.href = "../Sign_in/Signin.html";
                        return;
                    }
                    container.querySelectorAll('.hotel-card[data-id="' + id + '"] .heart').forEach(function (b) { setHeart(b, r.liked); });
                }).catch(function () { alert("Không thể kết nối máy chủ."); });
                return;
            }
            var extra = container.dataset.query ? "&" + container.dataset.query : "";
            location.href = "../Hotel/Hotel.html?id=" + encodeURIComponent(id) + extra;
        });
    }

    window.HotelCard = { html: cardHtml, bind: bind, esc: esc, fallback: FALLBACK };
})();
