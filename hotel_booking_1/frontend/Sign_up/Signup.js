(function () {
    var form = document.getElementById('signupForm');
    var message = document.getElementById('message');
    if (!form) return;
    form.addEventListener('submit', async function (event) {
        event.preventDefault();
        var password = document.getElementById('password').value;
        var confirm = document.getElementById('confirmPassword').value;
        if (password !== confirm) { message.style.color = '#dc2626'; message.textContent = 'Mật khẩu xác nhận không khớp.'; return; }
        var body = new URLSearchParams({ fullname: document.getElementById('fullname').value.trim(), email: document.getElementById('email').value.trim(), password: password });
        try {
            var response = await fetch('/Auth/Register', { method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body: body.toString(), credentials: 'same-origin' });
            var result = await response.json(); message.textContent = result.message; message.style.color = result.success ? '#15803d' : '#dc2626';
            if (result.success) setTimeout(function () { window.location.href = '../Sign_in/Signin.html'; }, 1000);
        } catch (e) { message.style.color = '#dc2626'; message.textContent = 'Không thể kết nối máy chủ. Hãy chạy website từ Visual Studio/IIS Express.'; }
    });
})();
