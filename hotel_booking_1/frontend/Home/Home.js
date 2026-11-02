console.log("Home.js moi - co menu");

const checkin = document.getElementById("checkin");
const checkout = document.getElementById("checkout");

// Ngày check-in không được ở quá khứ
const today = new Date();
const localToday = [
    today.getFullYear(),
    String(today.getMonth() + 1).padStart(2, "0"),
    String(today.getDate()).padStart(2, "0")
].join("-");

checkin.min = localToday;
checkout.min = localToday;

checkin.addEventListener("change", function () {
    checkout.min = this.value || localToday;

    if (checkout.value && checkout.value <= this.value) {
        checkout.value = "";
    }
});

const guestTrigger = document.getElementById("guestTrigger");
const guestPanel = document.getElementById("guestPanel");

const guestCounts = {
    rooms: 1,
    adults: 1,
    children: 0
};

function updateGuests() {
    Object.keys(guestCounts).forEach(function (key) {
        document.getElementById(key + "Count").textContent =
            guestCounts[key];

        document.getElementById(key + "Input").value =
            guestCounts[key];
    });

    const adults = guestCounts.adults;
    const children = guestCounts.children;

    let summary = adults + " người lớn";

    if (children > 0) {
        summary += ", " + children + " trẻ em";
    }

    document.getElementById("guestSummary").textContent = summary;
    document.getElementById("roomSummary").textContent =
        guestCounts.rooms + " phòng";

    document.querySelectorAll("[data-count]").forEach(function (button) {
        const key = button.dataset.count;
        const change = Number(button.dataset.change);

        const minimum = key === "children" ? 0 : 1;
        button.disabled = guestCounts[key] + change < minimum;
    });
}

guestTrigger.addEventListener("click", function () {
    const isOpen = !guestPanel.hidden;

    guestPanel.hidden = isOpen;
    guestTrigger.setAttribute("aria-expanded", String(!isOpen));
});

document.querySelectorAll("[data-count]").forEach(function (button) {
    button.addEventListener("click", function () {
        const key = this.dataset.count;
        const change = Number(this.dataset.change);
        const minimum = key === "children" ? 0 : 1;

        guestCounts[key] = Math.max(
            minimum,
            guestCounts[key] + change
        );

        updateGuests();
    });
});

document.addEventListener("click", function (event) {
    if (!event.target.closest(".guest-picker")) {
        guestPanel.hidden = true;
        guestTrigger.setAttribute("aria-expanded", "false");
    }
});

updateGuests();

// Menu trên điện thoại: bấm ☰ để mở/đóng
const header = document.querySelector(".header");
const menuBtn = document.querySelector(".menu-btn");

menuBtn.addEventListener("click", function () {
    const isOpen = header.classList.toggle("menu-open");

    menuBtn.setAttribute("aria-expanded", String(isOpen));
    menuBtn.textContent = isOpen ? "✕" : "☰";
});

// Trending stays: bấm mũi tên thì các thẻ trượt theo hướng mũi tên, xoay vòng mãi
(function () {
    const track = document.getElementById("hotelTrack");
    const prevBtn = document.getElementById("hotelPrev");
    const nextBtn = document.getElementById("hotelNext");

    if (!track || !prevBtn || !nextBtn) return;

    const DURATION = 450; // ms, khớp với thời gian trượt
    let busy = false;

    // Khoảng cách trượt = bề rộng 1 thẻ + khoảng hở giữa các thẻ
    function getStep() {
        const card = track.firstElementChild;
        const gap = parseFloat(getComputedStyle(track).columnGap) || 0;
        return card.getBoundingClientRect().width + gap;
    }

    function finish() {
        track.style.transition = "none";
        track.style.transform = "none";
        track.offsetWidth; // ép trình duyệt cập nhật ngay
        busy = false;
    }

    // direction = "right": các thẻ trượt sang phải (thẻ cuối chạy vòng lên đầu)
    // direction = "left" : các thẻ trượt sang trái (thẻ đầu chạy vòng xuống cuối)
    function slide(direction) {
        if (busy) return;
        busy = true;

        const step = getStep();

        if (direction === "right") {
            track.prepend(track.lastElementChild.cloneNode(true));

            track.style.transition = "none";
            track.style.transform = "translateX(" + (-step) + "px)";
            track.offsetWidth;

            track.style.transition = "transform " + DURATION + "ms ease";
            track.style.transform = "translateX(0)";

            setTimeout(function () {
                track.lastElementChild.remove();
                finish();
            }, DURATION);
        } else {
            track.append(track.firstElementChild.cloneNode(true));

            track.style.transition = "transform " + DURATION + "ms ease";
            track.style.transform = "translateX(" + (-step) + "px)";

            setTimeout(function () {
                track.firstElementChild.remove();
                finish();
            }, DURATION);
        }
    }

    prevBtn.addEventListener("click", function () { slide("left"); });
    nextBtn.addEventListener("click", function () { slide("right"); });
})();
// Tìm phòng theo ngày, số khách và điểm đến (gửi sang trang kết quả).
(function () {
    var btn = document.querySelector('.search-btn');
    if (!btn) return;
    btn.addEventListener('click', function () {
        var ci = document.getElementById('checkin').value;
        var co = document.getElementById('checkout').value;
        if (!ci || !co || co <= ci) { alert('Vui lòng chọn ngày nhận phòng và ngày trả phòng hợp lệ.'); return; }
        var destination = document.querySelector('.destination input').value.trim();
        var params = new URLSearchParams({ checkin: ci, checkout: co, adults: document.getElementById('adultsInput').value, children: document.getElementById('childrenInput').value, rooms: document.getElementById('roomsInput').value, keyword: destination });
        window.location.href = '../Search/Results.html?' + params.toString();
    });
})();

// Khách sạn nổi bật: lấy 5 khách sạn từ database, bấm vào thẻ để xem chi tiết, bấm tim để lưu vào giỏ hàng.
(function () {
    var track = document.getElementById("hotelTrack");
    if (!track || !window.HotelCard) return;
    HotelCard.bind(track);
    fetch("/Hotel/Featured", { credentials: "same-origin" })
        .then(function (r) { return r.json(); })
        .then(function (list) {
            track.innerHTML = list.length
                ? list.map(HotelCard.html).join("")
                : '<p class="hotel-empty">Chưa có khách sạn nổi bật.</p>';
        })
        .catch(function () {
            track.innerHTML = '<p class="hotel-empty">Không tải được khách sạn nổi bật.</p>';
        });
})();
