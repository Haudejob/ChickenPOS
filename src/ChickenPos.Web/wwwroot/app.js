const app=document.querySelector('#app'),dialog=document.querySelector('#dialog');
let csrf='',user=null,page='',cart=[],selectedId=null,currentOrder=null,products=[],ingredients=[],search='',adminSearch='',adminCategory='all',adminStatus='all',cartRestored=false,busy=false;
const cartStorageKey='chickenpos.pos.cart.v1';
const money=n=>new Intl.NumberFormat('vi-VN',{style:'currency',currency:'VND',maximumFractionDigits:0}).format(n||0);
const esc=s=>String(s??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const key=()=>crypto.randomUUID();
const productImages=Object.freeze({1:'ga-gion-cay.png',2:'ga-sot-mat-ong.png',3:'khoai-tay-chien.png',4:'coca-cola.png',5:'combo-hai-mieng-ga.png',6:'tra-dao.png'});
function productImageSource(p){return p.imagePath||(productImages[p.id]?'/images/menu/'+productImages[p.id]:'');}
function productImage(p,thumbnail=false){const src=productImageSource(p);return src?`<img class="${thumbnail?'product-thumbnail':'product-photo'}" src="${esc(src)}" alt="${esc(p.name)}" width="1536" height="1024" loading="lazy" decoding="async">`:esc(p.icon);}
const names={Manager:'Quản lý',Sales:'Bán hàng',Cashier:'Thu ngân',Kitchen:'Nhân viên bếp',Draft:'Chưa gửi',Waiting:'Chờ chế biến',Cooking:'Đang chế biến',Ready:'Sẵn sàng',Served:'Đã giao',Cancelled:'Đã hủy',Open:'Đang phục vụ',Completed:'Hoàn tất',Pending:'Chờ duyệt',Approved:'Đã duyệt',Rejected:'Từ chối',Cash:'Tiền mặt',Transfer:'Chuyển khoản',Opening:'Tồn đầu kỳ',Adjustment:'Điều chỉnh',Consume:'Chế biến',Created:'Tạo đơn',ItemsAdded:'Gọi thêm món',SentToKitchen:'Gửi bếp',CookingStarted:'Bắt đầu chế biến',PaymentRecorded:'Ghi nhận thanh toán',CancellationApproved:'Duyệt hủy',CancellationRequested:'Yêu cầu hủy',CancellationRejected:'Từ chối hủy',Paid:'Đã thanh toán',Unpaid:'Chưa thanh toán'};
const badge=s=>`<span class="badge ${esc(s)}">${esc(names[s]||s)}</span>`;
const time=s=>s?new Date(s+'Z').toLocaleString('vi-VN',{timeZone:'Asia/Ho_Chi_Minh',hour:'2-digit',minute:'2-digit',day:'2-digit',month:'2-digit'}):'—';
const sell=()=>['Sales','Cashier'].includes(user?.role);
function toast(message,error=false){const t=document.querySelector('#toast'),animate=['pos','products'].includes(page);t.textContent=message;t.className=`${error?'error':'ok'}${animate?' is-animated':''}`;t.style.display='block';if(animate)requestAnimationFrame(()=>t.classList.add('is-visible'));clearTimeout(toast.timer);clearTimeout(toast.hideTimer);toast.timer=setTimeout(()=>{if(animate){t.classList.remove('is-visible');toast.hideTimer=setTimeout(()=>{if(!t.classList.contains('is-visible'))t.style.display='none';},180);}else t.style.display='none';},5000);}
async function api(url,body,method=body?'POST':'GET'){
  const r=await fetch('/api'+url,{method,credentials:'same-origin',headers:body?{'Content-Type':'application/json','X-CSRF-TOKEN':csrf}:{},...(body?{body:JSON.stringify(body)}:{})});
  const value=await r.json().catch(()=>null);
  if(!r.ok){const e=new Error(value?.error||(r.status===403?'Vai trò này không có quyền thực hiện.':r.status===401?'Phiên đăng nhập đã hết hạn.':'Không thể xử lý yêu cầu.'));e.status=r.status;throw e;}return value;
}
async function token(){csrf=(await api('/csrf')).token;}
function login(){app.innerHTML=`<main class="login"><section class="login-hero"><div class="brand"><span class="brandmark">♨</span>Chicken<b>POS</b></div><div class="eyebrow">Hệ thống quản lý quán gà rán</div><h1>Phục vụ rõ ràng.<br>Quản lý trong<br>một nơi.</h1><p>Bán hàng nhanh chóng, phối hợp bếp nhịp nhàng và quản lý quán mỗi ngày.</p><div class="login-illustration" aria-hidden="true"><img src="/images/menu/ga-gion-cay.png" alt="" width="1536" height="1024"><img src="/images/menu/khoai-tay-chien.png" alt="" width="1536" height="1024"><img src="/images/menu/coca-cola.png" alt="" width="1536" height="1024"></div></section><section class="login-form-wrap"><form class="login-card" data-form="login"><div class="eyebrow">Chào mừng trở lại</div><h1>Đăng nhập hệ thống</h1><p class="muted">Đăng nhập bằng tài khoản nhân viên để bắt đầu làm việc.</p><label>Tên đăng nhập<input name="username" autocomplete="username" required></label><label>Mật khẩu<input name="password" type="password" autocomplete="current-password" required></label><p id="login-error" class="error-text"></p><button class="primary" type="submit">Vào ca làm việc →</button><p class="login-foot">Cần hỗ trợ đăng nhập? Vui lòng liên hệ quản lý cửa hàng.</p></form></section></main>`;}
const navigation=()=>[
  ...(user.role==='Manager'?[['dashboard','◫','Tổng quan'],['products','▦','Thực đơn'],['inventory','▤','Kho nguyên liệu'],['cancellations','✓','Duyệt hủy'],['revenue','▥','Doanh thu']]:[]),
  ...(sell()?[['pos','▦','Bán hàng']]:[]),['orders','≡','Đơn hàng'],['kitchen','♨','Màn hình bếp']];
function frame(title,description,content,toolbar=''){
  app.innerHTML=`<div class="shell"><aside class="sidebar"><div class="brand"><span class="brandmark">♨</span>Chicken<b>POS</b></div><nav class="nav">${navigation().map(([id,icon,n])=>`<button data-action="nav" data-page="${id}" class="${page===id?'active':''}">${icon} &nbsp; ${n}</button>`).join('')}</nav><div class="sidebar-foot"><strong>ChickenPOS</strong><br>Quản lý bán hàng tại cửa hàng</div></aside><div class="workspace"><header class="topbar"><div><strong>Không gian làm việc</strong><small>ChickenPOS · Quản lý cửa hàng</small></div><div class="user"><div class="avatar">${esc(user.name[0])}</div><div><strong>${esc(user.name)}</strong><small>${names[user.role]}</small></div><button class="small secondary" data-action="logout">Đăng xuất</button></div></header><main class="content"><header class="page-head"><div><div class="eyebrow">ChickenPOS / ${esc(title)}</div><h1>${esc(title)}</h1><p>${esc(description)}</p></div><div class="toolbar">${toolbar}<button data-action="refresh" class="secondary">↻ Cập nhật</button></div></header>${content}</main></div></div>`;
}
function modal(html,onSubmit){document.querySelector('#modal-body').innerHTML=html;document.querySelector('#modal-form').onsubmit=async e=>{e.preventDefault();const b=e.submitter;b.disabled=true;try{await onSubmit(new FormData(e.target));dialog.close();await render();}catch(err){const error=dialog.querySelector('[data-modal-error]');if(error)error.textContent=err.message;else toast(err.message,true);}finally{b.disabled=false;}};dialog.showModal();}
async function pos(){
  products=await api('/products');restoreCart();const tables=await api('/tables');
  frame('Bán hàng','Chọn món, tạo đơn mới hoặc gọi thêm vào đơn chưa thanh toán.',`<div class="split"><section class="pos-menu"><label class="sr-only" for="product-search">Tìm món theo tên hoặc nhóm món</label><input id="product-search" class="search" type="search" placeholder="Tìm món hoặc nhóm món…" value="${esc(search)}" autocomplete="off"><p class="filter-note" id="product-count" aria-live="polite">${matchingProducts().length} món phù hợp</p><div class="menu-grid" id="menu">${menu()}</div></section><aside class="panel cart"><div class="cart-title"><div><div class="eyebrow">${selectedId?'Gọi thêm món':'Đơn hàng mới'}</div><h2>${selectedId?'Đơn #'+selectedId:'Giỏ hàng'}</h2></div><div id="cart-controls">${cart.length?'<button type="button" class="small secondary" data-action="clear-cart">Xóa tất cả</button>':''}</div></div><form data-form="order">${selectedId?`<p class="note">Món mới được thêm vào đơn #${selectedId}. Dòng đã gửi bếp giữ nguyên.</p><button type="button" class="small secondary" data-action="new-order">Tạo đơn khác</button>`:`<label>Hình thức phục vụ<select name="orderType" id="service"><option value="Table">Tại bàn · trả sau</option><option value="Counter">Tại quầy · trả trước</option></select></label><label id="table-field">Chọn bàn<select name="tableId">${tables.map(t=>`<option value="${t.id}" ${t.sessionId?'disabled':''}>${esc(t.name)}${t.sessionId?' · đang phục vụ':''}</option>`).join('')}</select></label>`}<div id="cart-items">${cartLines()}</div><div class="summary"><span>Tạm tính</span><span id="cart-total">${money(cart.reduce((s,i)=>s+i.price*i.quantity,0))}</span></div><button id="order-submit" class="primary full" type="submit" ${cart.length?'':'disabled'}>✓ ${selectedId?'Xác nhận gọi thêm':'Tạo đơn và lưu'}</button><p class="note">Xác nhận giỏ chưa trừ kho. Nguyên liệu chỉ trừ khi bếp bắt đầu chế biến.</p></form></aside></div>`);
}
function normalizeSearch(value){return String(value??'').normalize('NFD').replace(/[\u0300-\u036f]/g,'').replace(/đ/g,'d').replace(/Đ/g,'D').toLocaleLowerCase('vi').trim();}
function matchingProducts(){const query=normalizeSearch(search);return products.filter(p=>p.isActive&&(!query||normalizeSearch(`${p.name} ${p.category}`).includes(query)));}
function menu(){return matchingProducts().map(p=>`<button class="product" data-action="add-cart" data-id="${p.id}"><div class="product-art">${productImage(p)}</div><div class="product-text"><strong>${esc(p.name)}</strong><small>${esc(p.category)}</small><b>${money(p.price)}</b> <span class="muted">&nbsp;＋</span></div></button>`).join('')||'<p class="empty">Không tìm thấy món phù hợp.</p>';}
function persistCart(){try{const value={selectedId:Number.isSafeInteger(selectedId)&&selectedId>0?selectedId:null,items:cart.map(i=>({id:i.id,quantity:i.quantity}))};if(!value.selectedId&&!value.items.length)sessionStorage.removeItem(cartStorageKey);else sessionStorage.setItem(cartStorageKey,JSON.stringify(value));}catch{}}
function restoreCart(){if(cartRestored)return;cartRestored=true;try{const saved=JSON.parse(sessionStorage.getItem(cartStorageKey)||'null');if(!saved||!Array.isArray(saved.items))return;selectedId=Number.isSafeInteger(saved.selectedId)&&saved.selectedId>0?saved.selectedId:null;const restored=new Map();for(const row of saved.items){if(!Number.isSafeInteger(row?.id)||!Number.isInteger(row?.quantity)||row.quantity<1||row.quantity>100)continue;const p=products.find(product=>product.id===row.id&&product.isActive);if(!p)continue;const quantity=Math.min(100,(restored.get(p.id)?.quantity||0)+row.quantity);restored.set(p.id,{...p,quantity});}cart=[...restored.values()];}catch{cart=[];selectedId=null;}}
function clearStoredCart(){try{sessionStorage.removeItem(cartStorageKey);}catch{}}
function cartLines(changedId=null,effect=''){return cart.length?cart.map(i=>`<div class="cart-row${i.id===changedId&&effect?` cart-row--${effect}`:''}" data-cart-row="${i.id}"><div><strong>${esc(i.name)}</strong><p class="muted">${money(i.price)}</p></div><div class="cart-item-controls"><div class="qty"><button type="button" data-action="minus-cart" data-id="${i.id}" aria-label="Giảm ${esc(i.name)}">−</button><b aria-live="polite">${i.quantity}</b><button type="button" data-action="add-cart" data-id="${i.id}" aria-label="Tăng ${esc(i.name)}">＋</button></div><button type="button" class="cart-remove" data-action="remove-cart" data-id="${i.id}">Xóa</button></div></div>`).join(''):'<div class="cart-empty">🍗<p>Chọn món bên trái để bắt đầu</p></div>';}
function renderCart(changedId=null,effect=''){const items=document.querySelector('#cart-items');if(!items)return;items.innerHTML=cartLines(changedId,effect);items.classList.remove('cart-feedback');void items.offsetWidth;items.classList.add('cart-feedback');document.querySelector('#cart-controls').innerHTML=cart.length?'<button type="button" class="small secondary" data-action="clear-cart">Xóa tất cả</button>':'';document.querySelector('#cart-total').textContent=money(cart.reduce((sum,item)=>sum+item.price*item.quantity,0));document.querySelector('#order-submit').disabled=!cart.length;}
function motionWait(){return new Promise(resolve=>setTimeout(resolve,matchMedia('(prefers-reduced-motion: reduce)').matches?0:150));}
async function animateCartRemoval(id=null){const rows=id===null?[...document.querySelectorAll('.cart-row')]:[document.querySelector(`[data-cart-row="${id}"]`)].filter(Boolean);rows.forEach(row=>row.classList.add('cart-row--removing'));if(rows.length)await motionWait();}
async function orders(){
  const list=await api('/orders');if(selectedId&&!list.some(o=>o.id===selectedId))selectedId=null;if(!selectedId&&list.length)selectedId=list[0].id;
  currentOrder=selectedId?await api('/orders/'+selectedId):null;
  const content=`<div class="split"><section class="stack order-list">${list.length?list.map(o=>`<article class="order-row ${selectedId===o.id?'selected':''}"><div><h3>${esc(o.code)}</h3><small class="muted">${esc(o.tableName||'Mang về / tại quầy')} · ${time(o.createdAt)}</small><div class="actions">${badge(o.status)}<span class="badge ${o.paid?'Approved':'Pending'}">${o.paid?'Đã thanh toán':'Chưa thanh toán'}</span></div></div><div><strong>${money(o.total)}</strong><p><button class="small secondary" data-action="select-order" data-id="${o.id}">Xem chi tiết</button></p></div></article>`).join(''):'<div class="panel empty">Chưa có đơn hàng. Hãy tạo đơn đầu tiên ở mục Bán hàng.</div>'}</section><aside class="panel order-detail">${currentOrder?detail(currentOrder):'<p class="empty">Chọn đơn để xem chi tiết.</p>'}</aside></div>`;
  frame('Đơn hàng','Theo dõi trạng thái đơn, gửi bếp, giao món và thanh toán.',content,sell()?'<button class="primary" data-action="new-pos">＋ Tạo đơn</button>':'');
}
function detail(o){const h=o.header,pending=o.cancellations.some(c=>c.status==='Pending'),open=h.status==='Open',unpaid=!h.paymentId;
return `<div class="print-only"><h1>ChickenPOS</h1><p>Hóa đơn nội bộ</p></div><div class="eyebrow">Chi tiết đơn</div><h2>${esc(h.code)}</h2><div class="receipt-meta">${esc(h.tableName||'Tại quầy / mang về')} · ${time(h.createdAt)}<br>${badge(h.status)} ${h.paymentId?'<span class="badge Approved">Đã thanh toán</span>':'<span class="badge Pending">Chưa thanh toán</span>'}</div>${o.items.map(i=>`<div class="detail-line"><div><strong>${i.quantity} × ${esc(i.name)}</strong><small>${money(i.price)} / phần · ${badge(i.status)}</small>${i.batchId?`<small>Đợt bếp: ${esc(i.batchId.slice(0,8))}</small>`:''}${sell()&&i.status==='Ready'?`<div class="actions"><button class="small primary" data-action="serve" data-id="${i.id}">✓ Xác nhận giao</button></div>`:''}</div><strong>${money(i.price*i.quantity)}</strong></div>`).join('')}<div class="summary"><span>Tổng tiền</span><span>${money(o.total)}</span></div><div class="actions">${sell()&&open&&unpaid&&!pending?'<button class="secondary" data-action="add-to-order">＋ Gọi thêm</button>':''}${sell()&&open&&!pending&&o.items.some(i=>i.status==='Draft')?'<button class="primary" data-action="send">Gửi món mới → bếp</button>':''}${user.role==='Cashier'&&open&&unpaid&&!pending?'<button class="primary" data-action="pay">Thanh toán</button>':''}${['Sales','Cashier','Manager'].includes(user.role)&&open&&unpaid&&!pending?'<button class="danger small" data-action="cancel">Yêu cầu hủy</button>':''}${h.paymentId?'<button class="secondary" data-action="print">In hóa đơn</button>':''}</div>${pending?'<p class="note">Đang chờ quản lý duyệt hủy. Tạm khóa thao tác đơn.</p>':''}${h.paymentId?`<p class="receipt-meta">${esc(names[h.method])} · ${money(h.amount)}<br>${time(h.paidAt)}${h.method==='Cash'?'<br>Tiền thừa: '+money(h.cashReceived-h.amount):''}</p>`:''}<details class="history"><summary>Lịch sử thao tác (${o.history.length})</summary>${o.history.map(i=>`<p>${esc(i.actor)} · ${time(i.occurredAt)}<br>${esc(names[i.eventType]||i.eventType)}: ${esc(names[i.newState]||i.newState)}</p>`).join('')}</details>`;
}
async function kitchen(){const rows=await api('/kitchen');frame('Màn hình bếp','Hàng đợi theo thời điểm gửi. Kiểm tra và trừ kho ngay khi bắt đầu.',`<div class="kitchen-grid">${['Waiting','Cooking','Ready'].map(state=>`<section class="kitchen-col"><h3>${names[state]} <span class="badge">${rows.filter(i=>i.status===state).length}</span></h3>${rows.filter(i=>i.status===state).map(i=>`<article class="ticket"><div class="eyebrow">${esc(i.code)}</div><p><strong>${esc(i.tableName||'Tại quầy')}</strong> · ${time(i.sentAt)}</p><h2>${i.quantity} × ${esc(i.name)}</h2><small>Đợt ${esc(i.batchId.slice(0,8))}</small>${user.role==='Kitchen'&&state!=='Ready'?`<button class="primary" data-action="${state==='Waiting'?'start':'ready'}" data-id="${i.id}">${state==='Waiting'?'▶ Bắt đầu chế biến':'✓ Món đã sẵn sàng'}</button>`:''}${sell()&&state==='Ready'?`<button class="primary" data-action="serve" data-id="${i.id}">✓ Xác nhận giao món</button>`:''}</article>`).join('')||'<p class="empty">Không có món trong cột này</p>'}</section>`).join('')}</div>`);}
async function inventory(){const data=await api('/inventory');ingredients=data.ingredients;frame('Kho nguyên liệu','Tồn kho đối soát từ giao dịch. Mọi điều chỉnh đều ghi người và lý do.',`<div class="stats"><div class="stat"><span>NGUYÊN LIỆU</span><strong>${ingredients.length}</strong><span>Đang theo dõi trong kho</span></div><div class="stat"><span>CẢNH BÁO TỒN THẤP</span><strong>${ingredients.filter(i=>i.stock<=i.minStock).length}</strong><span>Tồn nhỏ hơn hoặc bằng ngưỡng</span></div><div class="stat"><span>GIAO DỊCH GẦN ĐÂY</span><strong>${data.transactions.length}</strong><span>Hiển thị tối đa 80 lần gần nhất</span></div></div><section class="panel"><h2>Tồn kho hiện tại</h2><div class="table-wrap"><table><thead><tr><th>Nguyên liệu</th><th>Đơn vị</th><th>Tồn</th><th>Ngưỡng</th><th>Trạng thái</th></tr></thead><tbody>${ingredients.map(i=>`<tr><td><strong>${esc(i.name)}</strong></td><td>${esc(i.unit)}</td><td class="${i.stock<=i.minStock?'low':''}">${i.stock}</td><td>${i.minStock}</td><td>${i.stock<=i.minStock?'<span class="badge Waiting">Cần bổ sung</span>':'<span class="badge Approved">Trong ngưỡng</span>'}</td></tr>`).join('')}</tbody></table></div></section><section class="panel"><h2>Lịch sử nhập, xuất và điều chỉnh</h2><div class="table-wrap"><table><thead><tr><th>Thời gian</th><th>Nguyên liệu</th><th>Lượng thay đổi</th><th>Loại</th><th>Hao hụt</th><th>Người thực hiện</th></tr></thead><tbody>${data.transactions.map(t=>`<tr><td>${time(t.occurredAt)}</td><td>${esc(t.ingredient)}</td><td>${t.quantity>0?'+':''}${t.quantity} ${esc(t.unit)}</td><td>${esc(names[t.type])}${t.reason?'<small>'+esc(t.reason)+'</small>':''}</td><td>${t.waste||'—'}</td><td>${esc(t.actor)}</td></tr>`).join('')}</tbody></table></div></section>`,'<button class="primary" data-action="adjust">＋ Nhập / điều chỉnh kho</button>');}
async function revenue(){
  try {
    const summary = (await api('/revenue/summary')) || {};
    const dailyReports = (await api('/revenue/daily-report')) || [];
    const totalOrders = summary.totalOrders ?? 0;
    const totalRevenue = summary.totalRevenue ?? 0;
    const avgValue = summary.averageOrderValue ?? 0;
    const reportDate = summary.date ?? 'Hôm nay';

    const content = `
      <div class="stats">
        <article class="stat"><span>TỔNG ĐƠN HÔM NAY</span><strong>${totalOrders}</strong><span>Đơn hàng đã ghi nhận</span></article>
        <article class="stat"><span>DOANH THU HÔM NAY</span><strong>${money(totalRevenue)}</strong><span>Cập nhật ${esc(reportDate)}</span></article>
        <article class="stat"><span>GIÁ TRỊ TRUNG BÌNH / ĐƠN</span><strong>${money(avgValue)}</strong><span>Doanh thu / Tổng đơn</span></article>
      </div>
      <section class="panel">
        <h2>Báo cáo doanh thu theo ngày</h2>
        <div class="table-wrap">
          <table>
            <thead><tr><th>Ngày</th><th>Số lượng đơn</th><th>Tổng doanh thu</th></tr></thead>
            <tbody>
              ${dailyReports.length ? dailyReports.map(r=>`<tr><td><strong>${esc(r.date)}</strong></td><td>${r.ordersCount} đơn</td><td><strong>${money(r.totalAmount)}</strong></td></tr>`).join('') : '<tr><td colspan="3" class="empty">Chưa có dữ liệu báo cáo.</td></tr>'}
            </tbody>
          </table>
        </div>
      </section>`;
    frame('Quản lý doanh thu','Thống kê tổng quan và báo cáo doanh thu chi tiết.',content);
  } catch (err) {
    frame('Quản lý doanh thu','Thống kê tổng quan và báo cáo doanh thu chi tiết.',`<div class="panel error-text" style="color:red;padding:15px;"><p>⚠️ ${esc(err.message)}</p><p class="muted">Vui lòng đăng nhập tài khoản Quản lý (Manager) để xem dữ liệu.</p></div>`);
  }
}
async function dashboard(){const d=await api('/dashboard');const max=Math.max(1,...d.top.map(i=>i.quantity));frame('Tổng quan kinh doanh','Theo dõi doanh thu, đơn hàng và nguyên liệu trong ngày.',`<div class="stats"><article class="stat"><span>DOANH THU HÔM NAY</span><strong>${money(d.totals.revenue)}</strong><span>Từ các đơn đã thanh toán</span></article><article class="stat"><span>ĐƠN ĐÃ THANH TOÁN</span><strong>${d.totals.paidOrders}</strong><span>Đã ghi nhận thanh toán</span></article><article class="stat"><span>CẦN BỔ SUNG KHO</span><strong>${d.lowStock.length}</strong><span>Tồn không vượt ngưỡng tối thiểu</span></article></div><div class="split"><section class="panel"><h2>Món bán chạy hôm nay</h2><div class="ranking">${d.top.map(i=>`<div><strong>${esc(i.name)}</strong><span>${i.quantity} phần</span><progress max="${max}" value="${i.quantity}"></progress></div>`).join('')||'<p class="empty">Chưa có đơn thanh toán hôm nay.</p>'}</div></section><section class="panel"><h2>Cảnh báo nguyên liệu</h2>${d.lowStock.map(i=>`<p class="low">${esc(i.name)}: ${i.stock} ${esc(i.unit)}</p>`).join('')||'<p class="muted">Nguyên liệu đang trên ngưỡng cảnh báo.</p>'}</section></div>`);}
async function cancellations(){const list=await api('/cancellations');frame('Duyệt yêu cầu hủy','Sau khi bắt đầu chế biến, chỉ quản lý được duyệt. Không hoàn hoặc trừ kho lần hai.',`<div class="stack">${list.map(i=>`<article class="panel"><div class="eyebrow">${esc(i.code)}</div><h2>${esc(i.reason)}</h2><p>${esc(i.requester)} · ${time(i.requestedAt)} · ${badge(i.status)}</p>${i.status==='Pending'?`<div class="actions"><button class="primary" data-action="review" data-id="${i.id}" data-approve="true">Duyệt hủy</button><button class="secondary" data-action="review" data-id="${i.id}" data-approve="false">Từ chối</button></div>`:''}</article>`).join('')||'<section class="panel empty">Chưa có yêu cầu hủy.</section>'}</div>`);}
function adminProductRows(){const query=normalizeSearch(adminSearch);const visible=products.filter(p=>(!query||normalizeSearch(`${p.name} ${p.category}`).includes(query))&&(adminCategory==='all'||p.category===adminCategory)&&(adminStatus==='all'||(adminStatus==='active'?p.isActive:!p.isActive)));return visible.map(p=>`<tr class="menu-admin-row"><td>${productImage(p,true)} <strong>${esc(p.name)}</strong></td><td>${esc(p.category)}</td><td>${money(p.price)}</td><td>${p.isActive?'<span class="badge Approved">Đang bán</span>':'<span class="badge Pending">Ngừng bán</span>'}</td><td><button class="small secondary" data-action="edit-product" data-id="${p.id}">Sửa</button> <button class="small danger" data-action="delete-product" data-id="${p.id}" aria-label="Xóa ${esc(p.name)}">Xóa</button></td></tr>`).join('')||'<tr><td colspan="5" class="empty">Không có món phù hợp với bộ lọc.</td></tr>';}
function updateAdminProductResults(){const body=document.querySelector('#menu-admin-rows');if(!body)return;body.innerHTML=adminProductRows();document.querySelector('#menu-admin-count').textContent=`${products.filter(p=>{const query=normalizeSearch(adminSearch);return(!query||normalizeSearch(`${p.name} ${p.category}`).includes(query))&&(adminCategory==='all'||p.category===adminCategory)&&(adminStatus==='all'||(adminStatus==='active'?p.isActive:!p.isActive));}).length} món`;}
async function adminProducts(){products=await api('/products');const categories=[...new Set(products.map(p=>p.category).filter(Boolean))].sort((a,b)=>a.localeCompare(b,'vi'));if(adminCategory!=='all'&&!categories.includes(adminCategory))adminCategory='all';frame('Quản lý thực đơn','Thay đổi giá không làm đổi giá đã chụp trong đơn cũ.',`<section class="panel menu-admin-panel"><div class="menu-admin-filters"><label class="sr-only" for="menu-admin-search">Tìm món theo tên hoặc nhóm</label><input id="menu-admin-search" type="search" placeholder="Tìm món hoặc nhóm…" value="${esc(adminSearch)}" autocomplete="off"><label class="sr-only" for="menu-admin-category">Lọc theo nhóm món</label><select id="menu-admin-category"><option value="all">Tất cả nhóm</option>${categories.map(category=>`<option value="${esc(category)}" ${adminCategory===category?'selected':''}>${esc(category)}</option>`).join('')}</select><label class="sr-only" for="menu-admin-status">Lọc theo trạng thái món</label><select id="menu-admin-status"><option value="all" ${adminStatus==='all'?'selected':''}>Tất cả trạng thái</option><option value="active" ${adminStatus==='active'?'selected':''}>Đang bán</option><option value="inactive" ${adminStatus==='inactive'?'selected':''}>Ngừng bán</option></select><span class="filter-note" id="menu-admin-count" aria-live="polite">${products.length} món</span></div><div class="table-wrap"><table><thead><tr><th>Món ăn</th><th>Nhóm</th><th>Giá</th><th>Trạng thái</th><th></th></tr></thead><tbody id="menu-admin-rows">${adminProductRows()}</tbody></table></div></section>`,'<button class="primary" data-action="edit-product">＋ Thêm món</button>');}
async function prepareProductPhoto(file){
  if(!['image/jpeg','image/png','image/webp'].includes(file.type)||file.size>10*1024*1024)
    throw new Error('Chọn ảnh JPG, PNG hoặc WebP tối đa 10 MB.');
  const bitmap=await createImageBitmap(file).catch(()=>{throw new Error('Không đọc được ảnh đã chọn.');});
  try{
    const scale=Math.min(1,1000/Math.max(bitmap.width,bitmap.height));
    const canvas=document.createElement('canvas');
    canvas.width=Math.max(1,Math.round(bitmap.width*scale));
    canvas.height=Math.max(1,Math.round(bitmap.height*scale));
    const context=canvas.getContext('2d',{alpha:false});
    context.fillStyle='#fff7ed';
    context.fillRect(0,0,canvas.width,canvas.height);
    context.drawImage(bitmap,0,0,canvas.width,canvas.height);
    const data=canvas.toDataURL('image/jpeg',.82);
    if(!data.startsWith('data:image/jpeg;base64,')||data.length>546150)
      throw new Error('Ảnh quá lớn sau khi xử lý. Hãy chọn ảnh nhỏ hơn.');
    return data;
  }finally{bitmap.close();}
}

function editProduct(id){
  const p=products.find(item=>item.id===id)||{name:'',price:39000,category:'Gà rán',isActive:true,icon:'🍗'};
  const current=productImageSource(p);
  let photo=null;
  modal(`<h2>${id?'Sửa món':'Thêm món mới'}</h2>
    <label>Tên món<input name="name" maxlength="120" value="${esc(p.name)}" required></label>
    <label>Giá (VND)<input name="price" type="number" min="0" max="10000000" step="1" value="${p.price}" required></label>
    <label>Nhóm món<input name="category" maxlength="40" value="${esc(p.category)}" required></label>
    <label>Ảnh món<input name="photo" type="file" accept="image/jpeg,image/png,image/webp" aria-describedby="product-photo-help"></label>
    <small id="product-photo-help" class="muted">JPG, PNG hoặc WebP, tối đa 10 MB.</small>
    <img class="product-edit-preview" alt="Xem trước ảnh món" ${current?`src="${esc(current)}"`:'hidden'}>
    <label><span><input type="checkbox" name="active" ${p.isActive?'checked':''}> Đang kinh doanh</span></label>
    <p class="note">Ngừng bán giữ nguyên dữ liệu và giá trong đơn cũ.</p>`,async d=>{
      const data=photo?await photo:null;
      await api(id?'/products/'+id:'/products',{
        name:d.get('name'),price:Number(d.get('price')),category:d.get('category'),icon:p.icon,isActive:d.has('active'),
        ...(data?{photo:data.split(',')[1]}:{})
      },id?'PUT':'POST');
      toast('Đã lưu món.');
    });
  const input=dialog.querySelector('[name=photo]');
  const preview=dialog.querySelector('.product-edit-preview');
  input.addEventListener('change',async()=>{
    input.setCustomValidity('');
    const file=input.files[0];
    const pending=file?prepareProductPhoto(file):null;
    photo=pending;
    preview.hidden=!current;
    if(current)preview.src=current;
    else preview.removeAttribute('src');
    try{
      const data=await pending;
      if(photo===pending&&data){
        preview.src=data;
        preview.hidden=false;
      }
    }catch(err){
      if(photo===pending){
        input.setCustomValidity(err.message);
        input.reportValidity();
      }
    }
  });
}

function confirmProductDeletion(id){
  const product=products.find(item=>item.id===id);
  if(!product)return;
  modal(`<h2>Xóa món</h2>
    <p>Bạn có muốn xóa <strong>${esc(product.name)}</strong> khỏi thực đơn?</p>
    <p class="muted">Món đã xuất hiện trong đơn hàng chỉ có thể ngừng bán.</p>
    <p class="error-text" data-modal-error role="alert"></p>`,async()=>{
      await api('/products/'+id,{},'DELETE');
      toast('Đã xóa món.');
    });
  const confirm=dialog.querySelector('[type=submit]');
  confirm.textContent='Xóa món';
  confirm.className='danger';
  dialog.addEventListener('close',()=>{
    confirm.textContent='Xác nhận';
    confirm.className='primary';
  },{once:true});
}

async function render(){try{if(!user)return login();if(page==='pos')await pos();else if(page==='orders')await orders();else if(page==='kitchen')await kitchen();else if(page==='inventory')await inventory();else if(page==='revenue')await revenue();else if(page==='cancellations')await cancellations();else if(page==='products')await adminProducts();else await dashboard();}catch(e){if(e.status===401){user=null;login();}toast(e.message,true);}}
document.addEventListener('input',e=>{if(e.target.id==='product-search'){search=e.target.value;const grid=document.querySelector('#menu');grid.innerHTML=menu();grid.classList.remove('menu--filtered');void grid.offsetWidth;grid.classList.add('menu--filtered');document.querySelector('#product-count').textContent=`${matchingProducts().length} món phù hợp`;}else if(e.target.id==='menu-admin-search'){adminSearch=e.target.value;updateAdminProductResults();}});
document.addEventListener('change',e=>{if(e.target.id==='service')document.querySelector('#table-field').hidden=e.target.value==='Counter';else if(e.target.id==='menu-admin-category'){adminCategory=e.target.value;updateAdminProductResults();}else if(e.target.id==='menu-admin-status'){adminStatus=e.target.value;updateAdminProductResults();}});
document.addEventListener('submit',async e=>{
  const form=e.target;if(!form.dataset.form)return;e.preventDefault();if(busy)return;busy=true;const button=e.submitter;if(button)button.disabled=true;
  try{const data=new FormData(form);
    if(form.dataset.form==='login'){await token();user=await api('/login',{username:data.get('username'),password:data.get('password')});await token();page=user.role==='Manager'?'dashboard':user.role==='Kitchen'?'kitchen':user.role==='Cashier'?'orders':'pos';await render();}
    if(form.dataset.form==='order'){const items=cart.map(i=>({productId:i.id,quantity:i.quantity}));const k=key();const r=selectedId?await api(`/orders/${selectedId}/items`,{items,requestKey:k}):await api('/orders',{orderType:data.get('orderType'),tableId:data.get('orderType')==='Table'?Number(data.get('tableId')):null,items,requestKey:k});cart=[];selectedId=r.id;clearStoredCart();cartRestored=true;toast('Đã lưu đơn hàng.');page='orders';await render();}
  }catch(err){if(form.dataset.form==='login'){const el=document.querySelector('#login-error');if(el)el.textContent=err.message;}toast(err.message,true);}finally{busy=false;if(button?.isConnected)button.disabled=false;}
});
document.addEventListener('click',async e=>{
  const b=e.target.closest('[data-action]');if(!b)return;const a=b.dataset.action,id=Number(b.dataset.id);
  if(a==='close-modal'){dialog.close();return;}
  if(busy)return;busy=true;b.disabled=true;
  try{
   if(a==='nav'&&navigation().some(([id])=>id===b.dataset.page)){page=b.dataset.page;await render();}
   else if(a==='refresh')await render();
    else if(a==='logout'){await api('/logout',{});user=null;cart=[];selectedId=null;cartRestored=false;clearStoredCart();await token();login();}
  else if(a==='add-cart'||a==='minus-cart'||a==='remove-cart'){const p=products.find(p=>p.id===id);let item=cart.find(i=>i.id===id);if(a==='add-cart'){if(!p?.isActive)throw new Error('Món này hiện không kinh doanh.');if(item){if(item.quantity>=100)throw new Error('Tối đa 100 phần mỗi dòng.');item.quantity++;}else cart.push({...p,quantity:1});persistCart();renderCart(id,item?'updated':'added');}else if(item&&(a==='remove-cart'||item.quantity===1)){await animateCartRemoval(id);cart=cart.filter(i=>i.id!==id);persistCart();renderCart(null,'removed');}else if(item){item.quantity--;persistCart();renderCart(id,'updated');}}
    else if(a==='clear-cart'){await animateCartRemoval();cart=[];persistCart();renderCart(null,'removed');}
    else if(a==='new-order'||a==='new-pos'){await animateCartRemoval();cart=[];selectedId=null;clearStoredCart();cartRestored=true;page='pos';await render();}
   else if(a==='select-order'){selectedId=id;await render();}
    else if(a==='add-to-order'){cart=[];persistCart();page='pos';await render();}
   else if(a==='send'){const r=await api(`/orders/${selectedId}/send`,{requestKey:key()});toast(`Đã gửi ${r.sent} dòng món mới sang bếp.`);await render();}
   else if(['start','ready','serve'].includes(a)){await api(`/details/${id}/${a}`,{});toast(a==='start'?'Đã bắt đầu và trừ kho đúng định lượng.':'Đã cập nhật trạng thái món.');await render();}
   else if(a==='pay'){modal(`<h2>Thanh toán ${esc(currentOrder.header.code)}</h2><div class="summary"><span>Phải thu</span><span>${money(currentOrder.total)}</span></div><label>Phương thức<select name="method"><option value="Cash">Tiền mặt</option><option value="Transfer">Chuyển khoản</option></select></label><label>Tiền khách đưa (VND)<input type="number" name="cash" min="0" max="1000000000" step="1" value="${currentOrder.total}"></label><label><span><input type="checkbox" name="confirmed"> Tôi đã kiểm tra và nhận đủ chuyển khoản</span></label><p class="note">Kiểm tra tiền đã vào tài khoản trước khi xác nhận chuyển khoản. Thanh toán toàn bộ giá trị đơn hàng.</p>`,async d=>{const r=await api(`/orders/${selectedId}/pay`,{method:d.get('method'),cashReceived:Number(d.get('cash')),transferConfirmed:d.has('confirmed'),requestKey:key()});toast('Đã ghi nhận thanh toán. '+(r.change?'Tiền thừa: '+money(r.change):''));});}
   else if(a==='cancel'){modal('<h2>Yêu cầu hủy đơn</h2><label>Lý do<input name="reason" maxlength="500" required placeholder="Ghi rõ lý do hủy"></label><p class="note">Sau bắt đầu chế biến cần quản lý duyệt. Đơn đã thanh toán chưa hỗ trợ hủy.</p>',async d=>{const r=await api(`/orders/${selectedId}/cancel`,{reason:d.get('reason')});toast(r.status==='Pending'?'Đã gửi quản lý duyệt.':'Đã hủy và lưu lịch sử.');});}
  else if(a==='review'){await api(`/cancellations/${id}/review`,{approve:b.dataset.approve==='true'});toast('Đã ghi quyết định.');await render();}
   else if(a==='adjust'){modal(`<h2>Nhập / điều chỉnh kho</h2><label>Nguyên liệu<select name="ingredient">${ingredients.map(i=>`<option value="${i.id}">${esc(i.name)} (${esc(i.unit)})</option>`).join('')}</select></label><label>Lượng tăng (+) hoặc giảm (−)<input name="quantity" type="number" min="-100000" max="100000" step="0.001" required></label><label>Lý do<input name="reason" maxlength="300" required></label>`,async d=>{await api('/inventory/adjust',{ingredientId:Number(d.get('ingredient')),quantity:Number(d.get('quantity')),reason:d.get('reason'),requestKey:key()});toast('Đã cập nhật kho và ghi lịch sử.');});}
   else if(a==='edit-product'){editProduct(id);}
   else if(a==='delete-product'){confirmProductDeletion(id);}
   else if(a==='print')window.print();
  }catch(err){toast(err.message,true);}finally{busy=false;if(b.isConnected)b.disabled=false;}
});
setInterval(()=>{if(user&&['kitchen','orders','dashboard','revenue'].includes(page)&&!busy&&!dialog.open&&!['INPUT','SELECT','TEXTAREA'].includes(document.activeElement?.tagName))render();},12000);
try{await token();user=await api('/me');page=user.role==='Manager'?'dashboard':user.role==='Kitchen'?'kitchen':user.role==='Cashier'?'orders':'pos';await render();}catch{login();}