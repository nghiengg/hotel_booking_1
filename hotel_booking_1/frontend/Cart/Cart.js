(function(){
    const root=document.getElementById('items');
    const esc=HotelCard.esc;
    const fmt=s=>new Date(s).toLocaleString('vi-VN',{day:'2-digit',month:'2-digit',year:'numeric',hour:'2-digit',minute:'2-digit'});
    async function post(url,data){return fetch(url,{method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/x-www-form-urlencoded'},body:new URLSearchParams(data)}).then(r=>r.json());}
    async function load(){
        try{
            const d=await fetch('/Hotel/Cart',{credentials:'same-origin'}).then(r=>r.json());
            if(!d.success){root.innerHTML=esc(d.message)+(d.loginRequired?' <a href="../Sign_in/Signin.html">Đăng nhập</a>':'');return;}
            if(!d.items.length){root.textContent='Giỏ hàng đang trống.';return;}
            root.innerHTML=d.items.map(i=>{
                const img=esc(i.ImageUrl||HotelCard.fallback);
                if(!i.RoomID) return `<article class="cart-item"><img src="${img}" alt=""><div><h3>${esc(i.HotelName)}</h3><div class="muted">${esc(i.City||'')}</div><div class="muted">♥ Khách sạn đã lưu</div></div><div class="act"><a class="view" href="../Hotel/Hotel.html?id=${i.HotelID}">Xem chi tiết</a><button class="ghost" data-del="${i.CartID}">Xóa</button></div></article>`;
                const nights=Math.max(1,Math.round((new Date(i.CheckOut.slice(0,10))-new Date(i.CheckIn.slice(0,10)))/86400000));
                const total=Number(i.RoomPrice)*nights*i.Quantity;
                return `<article class="cart-item"><img src="${img}" alt=""><div><h3>${esc(i.HotelName)}</h3><div class="muted">${esc(i.RoomName)} × ${i.Quantity} phòng</div><div class="muted">Nhận: ${fmt(i.CheckIn)}</div><div class="muted">Trả: ${fmt(i.CheckOut)} (${nights} đêm)</div></div><div class="act"><b>${total.toLocaleString('vi-VN')} ₫</b><button data-book="${i.CartID}" data-room="${i.RoomID}" data-ci="${i.CheckIn.slice(0,10)}" data-co="${i.CheckOut.slice(0,10)}" data-qty="${i.Quantity}">Đặt phòng</button><a class="view" href="../Hotel/Hotel.html?id=${i.HotelID}">Xem chi tiết</a><button class="ghost" data-del="${i.CartID}">Xóa</button></div></article>`;
            }).join('');
            root.querySelectorAll('[data-del]').forEach(b=>b.onclick=async()=>{await post('/Hotel/RemoveFromCart',{id:b.dataset.del});load();});
            root.querySelectorAll('[data-book]').forEach(b=>b.onclick=async()=>{
                const r=await post('/Booking/Create',{roomId:b.dataset.room,checkin:b.dataset.ci,checkout:b.dataset.co,quantity:b.dataset.qty});
                alert(r.message);
                if(r.success){await post('/Hotel/RemoveFromCart',{id:b.dataset.book});load();}
                else if(r.loginRequired)location.href='../Sign_in/Signin.html';
            });
        }catch(e){root.textContent='Không thể tải giỏ hàng.';}
    }
    load();
})();
