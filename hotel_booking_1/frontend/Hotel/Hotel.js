(function(){
    const root=document.getElementById('detail');
    const p=new URLSearchParams(location.search);
    const id=p.get('id');
    const esc=HotelCard.esc;
    const pad=n=>String(n).padStart(2,'0');
    const toLocal=d=>d.getFullYear()+'-'+pad(d.getMonth()+1)+'-'+pad(d.getDate())+'T'+pad(d.getHours())+':'+pad(d.getMinutes());
    function defaults(){
        const today=new Date();today.setHours(0,0,0,0);
        let ci=p.get('checkin')?new Date(p.get('checkin')+'T14:00'):new Date(today.getTime()+14*3600000);
        let co=p.get('checkout')?new Date(p.get('checkout')+'T12:00'):new Date(ci.getTime()+22*3600000);
        if(isNaN(ci))ci=new Date(today.getTime()+14*3600000);
        if(isNaN(co)||co<=ci)co=new Date(ci.getTime()+22*3600000);
        return [toLocal(ci),toLocal(co)];
    }
    async function load(){
        if(!id){root.textContent='Không tìm thấy khách sạn.';return;}
        try{
            const d=await fetch('/Hotel/Detail?id='+encodeURIComponent(id),{credentials:'same-origin'}).then(r=>r.json());
            if(!d.success){root.textContent=d.message;return;}
            const h=d.hotel,[ci,co]=defaults();
            document.title=h.HotelName+' - LeeMinHon';
            const img=(h.ImageUrl||HotelCard.fallback).replace(/["'()\\]/g,encodeURIComponent);
            root.innerHTML=`<section class="hd-top"><div class="hd-image hotel-image" data-id="${h.HotelID}" style="background-image:url('${img}')"><button type="button" class="heart${h.Liked?' active':''}" aria-label="Lưu khách sạn">${h.Liked?'♥':'♡'}</button></div>
<div class="hd-info"><h1>${esc(h.HotelName)}</h1><div class="location">${esc([h.City,h.Address].filter(Boolean).join(' · '))}</div><p>${esc(h.Description||'Chưa có mô tả cho khách sạn này.')}</p>${h.MinPrice!=null?`<div class="hd-from">Giá từ <strong>${Number(h.MinPrice).toLocaleString('vi-VN')} ₫</strong> /đêm</div>`:''}</div></section>
<section class="hd-dates"><label>Nhận phòng (ngày giờ)<input type="datetime-local" id="ci" value="${ci}"></label><label>Trả phòng (ngày giờ)<input type="datetime-local" id="co" value="${co}"></label></section>
<section class="hd-rooms"><h2>Các loại phòng</h2>${d.rooms.length?d.rooms.map(r=>`<article class="hd-room"><img src="${esc(r.RoomImage||HotelCard.fallback)}" alt="${esc(r.RoomName)}"><div><h3>${esc(r.RoomName)}</h3><div class="muted">${esc(r.Category||'')} · Tối đa ${r.MaxGuests} khách/phòng</div><p class="muted">${esc(r.RoomDescription||'')}</p></div><div class="side"><b>${Number(r.RoomPrice).toLocaleString('vi-VN')} ₫/đêm</b><label>Số phòng <input type="number" min="1" max="10" value="1" data-qty="${r.RoomID}"></label><button type="button" class="add-cart" data-room="${r.RoomID}">Thêm vào giỏ hàng</button></div></article>`).join(''):'<p>Khách sạn chưa có phòng để đặt.</p>'}</section>`;
            const heart=root.querySelector('.heart');
            heart.addEventListener('click',async()=>{
                const r=await fetch('/Hotel/ToggleFavorite',{method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/x-www-form-urlencoded'},body:new URLSearchParams({hotelId:h.HotelID})}).then(x=>x.json());
                if(!r.success){alert(r.message);if(r.loginRequired)location.href='../Sign_in/Signin.html';return;}
                heart.classList.toggle('active',r.liked);heart.textContent=r.liked?'♥':'♡';
            });
            root.querySelectorAll('.add-cart').forEach(b=>b.addEventListener('click',async()=>{
                const body=new URLSearchParams({hotelId:h.HotelID,roomId:b.dataset.room,checkin:document.getElementById('ci').value,checkout:document.getElementById('co').value,quantity:root.querySelector(`[data-qty="${b.dataset.room}"]`).value});
                const r=await fetch('/Hotel/AddToCart',{method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/x-www-form-urlencoded'},body}).then(x=>x.json());
                alert(r.message);if(r.loginRequired)location.href='../Sign_in/Signin.html';
            }));
        }catch(e){root.textContent='Không thể tải dữ liệu. Vui lòng kiểm tra cơ sở dữ liệu và máy chủ.';}
    }
    load();
})();
