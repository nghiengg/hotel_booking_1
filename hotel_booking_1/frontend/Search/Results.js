(function(){
    const p=new URLSearchParams(location.search);
    const params={checkin:p.get('checkin'),checkout:p.get('checkout'),adults:p.get('adults')||1,children:p.get('children')||0,rooms:p.get('rooms')||1,keyword:p.get('keyword')||''};
    const root=document.getElementById('results');
    document.getElementById('searchSummary').textContent=`${params.keyword||'Tất cả điểm đến'} · ${params.checkin} đến ${params.checkout} · ${params.adults} người lớn, ${params.children} trẻ em · ${params.rooms} phòng`;
    // Giữ lại ngày/số khách khi sang trang chi tiết khách sạn
    root.dataset.query=new URLSearchParams({checkin:params.checkin||'',checkout:params.checkout||'',rooms:params.rooms}).toString();
    HotelCard.bind(root);
    async function load(){
        root.textContent='Đang tải dữ liệu...';
        try{
            const q=new URLSearchParams(params);
            const data=await fetch('/Hotel/Search?'+q,{credentials:'same-origin'}).then(r=>r.json());
            if(!data.success){root.textContent=data.message;return;}
            root.innerHTML=data.hotels.length?data.hotels.map(HotelCard.html).join(''):'Không tìm thấy khách sạn phù hợp với tiêu chí của bạn.';
        }catch(e){root.textContent='Không thể tải dữ liệu. Vui lòng kiểm tra cơ sở dữ liệu và máy chủ.';}
    }
    load();
})();
