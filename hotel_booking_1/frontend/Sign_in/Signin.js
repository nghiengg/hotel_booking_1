(function () {
    var form = document.getElementById('signinForm');
    var message = document.getElementById('message');
    if (!form) return;
    form.addEventListener('submit', async function (event) {
        event.preventDefault();
        var body = new URLSearchParams({ email: document.getElementById('email').value.trim(), password: document.getElementById('password').value });
        try {
            var response = await fetch('/Auth/Login', { method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body: body.toString(), credentials: 'same-origin' });
            var result = await response.json();
            message.textContent = result.message || 'Không thể đăng nhập.';
            message.style.color = result.success ? '#15803d' : '#dc2626';
            if (result.success) window.location.href = result.redirect || '/Home/Index';
        } catch (e) { message.textContent = 'Không thể kết nối máy chủ. Hãy chạy website từ Visual Studio/IIS Express.'; message.style.color = '#dc2626'; }
    });
    var forgot = document.getElementById('forgotLink');
    if (forgot) forgot.addEventListener('click', function (e) { e.preventDefault(); message.textContent = 'Vui lòng liên hệ quản trị viên để đặt lại mật khẩu.'; });
})();
