"""End-to-end HTTP tests against an isolated ChickenPOS_Test_* SQL database.
Run the dedicated test server first; this file never drops or resets a database.
"""
import urllib.request,urllib.error,http.cookiejar,json,sys,uuid,time,datetime,pathlib,concurrent.futures
base=sys.argv[1] if len(sys.argv)>1 else 'http://127.0.0.1:5081'
if not base.startswith('http://127.0.0.1:5081'):raise SystemExit('Tests only target the dedicated localhost:5081 test server.')
results=[]
def key():return str(uuid.uuid4())
class Client:
 def __init__(self,username=None):
  self.jar=http.cookiejar.CookieJar();self.opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(self.jar));self.csrf=''
  self.csrf=self.get('/csrf')['token']
  if username:self.post('/login',{'username':username,'password':'Chicken@2026!'});self.csrf=self.get('/csrf')['token']
 def req(self,path,data=None,method=None,expect=200,token=True):
  headers={'Content-Type':'application/json'}
  if token:headers['X-CSRF-TOKEN']=self.csrf
  req=urllib.request.Request(base+'/api'+path,data=None if data is None else json.dumps(data).encode(),headers=headers,method=method or ('POST' if data is not None else 'GET'))
  try:
   with self.opener.open(req,timeout=20) as r:status=r.status;body=r.read()
  except urllib.error.HTTPError as e:status=e.code;body=e.read()
  value=json.loads(body) if body else None
  assert status in (expect if isinstance(expect,tuple) else (expect,)),f'{method or "request"} {path}: expected {expect}, got {status}: {value}'
  return value
 def get(self,p):return self.req(p)
 def post(self,p,d,expect=200):return self.req(p,d,expect=expect)
def test(code,fr,title,fn):
 start=time.perf_counter()
 try:fn();status='PASS';detail='Kết quả thực tế khớp các assertion trong test.'
 except Exception as e:status='FAIL';detail=str(e)
 results.append(dict(code=code,fr=fr,title=title,status=status,elapsedMs=round((time.perf_counter()-start)*1000),detail=detail))
 print(f'{status} {code}: {title}',flush=True)
 if status=='FAIL':print(detail,flush=True)

assert Client().get('/health').get('testInstance'), 'Refuse to run mutations on a non-Testing instance.'
sales=Client('banhang');manager=Client('quanly');cashier=Client('thungan');kitchen=Client('bep')
state={}
def inventory():return manager.get('/inventory')
def stocks():return {r['id']:r['stock'] for r in inventory()['ingredients']}
def create(items,table=None):return sales.post('/orders',{'orderType':'Table' if table else 'Counter','tableId':table,'items':[{'productId':p,'quantity':q} for p,q in items],'requestKey':key()})['id']
def detail(order):return sales.get('/orders/'+str(order))
def send(order):return sales.post(f'/orders/{order}/send',{'requestKey':key()})
def item(order,index=0):return detail(order)['items'][index]['id']
def stock_adjust(ingredient,target):
 old=stocks()[ingredient]
 if old!=target:manager.post('/inventory/adjust',{'ingredientId':ingredient,'quantity':target-old,'reason':'Dữ liệu fixture kiểm thử cục bộ','requestKey':key()})

test('TC01','FR10','Đăng nhập sai bị từ chối',lambda:Client().post('/login',{'username':'banhang','password':'wrong'},401))
test('TC02','FR11/BR17','Bếp không được quản lý món',lambda:kitchen.post('/products',{'name':'Sai quyền','price':1,'category':'Test','icon':'x','isActive':True},403))
test('TC03','NFR06','POST thiếu CSRF bị từ chối',lambda:sales.req('/orders',{},expect=400,token=False))
test('TC04','FR02','Giá âm bị chặn',lambda:manager.post('/products',{'name':'Giá âm','price':-1,'category':'Test','icon':'x','isActive':True},400))
test('TC05','FR06/BR11','Số lượng 0 bị chặn',lambda:sales.post('/orders',{'orderType':'Counter','tableId':None,'items':[{'productId':1,'quantity':0}],'requestKey':key()},400))

def create_persist():
 before=stocks();state['o1']=create([(1,2),(4,1)],1);d=detail(state['o1']);assert d['total']==97000 and len(d['items'])==2;assert stocks()==before;assert any(o['id']==state['o1'] for o in sales.get('/orders'))
test('TC06','FR08/FR09/FR13','Tạo đơn, tính 97.000đ, lưu DB và chưa trừ kho',create_persist)
def send_repeat():
 o=state['o1'];k=key();first=sales.post(f'/orders/{o}/send',{'requestKey':k});second=sales.post(f'/orders/{o}/send',{'requestKey':k});assert first==second and first['sent']==2;assert len([i for i in kitchen.get('/kitchen') if i['orderId']==o])==2
test('TC07','FR14','Gửi lặp không tạo công việc bếp trùng',send_repeat)
def add_more():
 o=state['o1'];k=key();data={'items':[{'productId':3,'quantity':1}],'requestKey':k};sales.post(f'/orders/{o}/items',data);sales.post(f'/orders/{o}/items',data);assert len(detail(o)['items'])==3;assert send(o)['sent']==1
test('TC08','FR13/FR14','Gọi thêm chỉ gửi dòng mới, gọi lặp không thêm trùng',add_more)
def start_repeat():
 d=item(state['o1']);before=stocks()[1];kitchen.post(f'/details/{d}/start',{});middle=stocks()[1];kitchen.post(f'/details/{d}/start',{});assert before-middle==.5 and stocks()[1]==middle
test('TC09','FR16/FR24','Bấm bắt đầu hai lần chỉ trừ kho một lần',start_repeat)
def concurrent_start():
 o=create([(1,1)],2);send(o);d=item(o);before=stocks()[1];k2=Client('bep')
 with concurrent.futures.ThreadPoolExecutor(2) as pool:r=list(pool.map(lambda client:client.post(f'/details/{d}/start',{}),[kitchen,k2]))
 assert all(i['status']=='Cooking' for i in r);assert before-stocks()[1]==.25
 assert len([t for t in inventory()['transactions'] if t['detailId']==d and t['type']=='Consume'])==1
test('TC10','FR24/BR13','Hai yêu cầu đồng thời cho cùng dòng chỉ có một Consume',concurrent_start)
def missing_rollsback():
 old=stocks()[2];stock_adjust(2,0);o=create([(5,1)],3);send(o);before=stocks();count=len(inventory()['transactions']);kitchen.post(f'/details/{item(o)}/start',{},409);assert stocks()==before;assert len(inventory()['transactions'])==count;assert detail(o)['items'][0]['status']=='Waiting';stock_adjust(2,old)
test('TC11','FR24/BR19','Thiếu thành phần thứ hai: không trừ phần thứ nhất, giữ Chờ',missing_rollsback)
def snapshot():
 p=next(p for p in manager.get('/products') if p['id']==1);old=p['price'];manager.req('/products/1',{'name':p['name'],'price':41000,'category':p['category'],'icon':p['icon'],'isActive':True},method='PUT');assert detail(state['o1'])['items'][0]['price']==39000;manager.req('/products/1',{'name':p['name'],'price':old,'category':p['category'],'icon':p['icon'],'isActive':True},method='PUT')
test('TC12','FR03/BR10','Đổi giá thực đơn không đổi đơn giá đã chụp',snapshot)
def counter_pay():
 o=create([(1,1)]);state['counter']=o;sales.post(f'/orders/{o}/send',{'requestKey':key()},409)
 cashier.post(f'/orders/{o}/pay',{'method':'Cash','cashReceived':10000,'transferConfirmed':False,'requestKey':key()},400)
 cashier.post(f'/orders/{o}/pay',{'method':'Transfer','cashReceived':0,'transferConfirmed':False,'requestKey':key()},400)
 payment=cashier.post(f'/orders/{o}/pay',{'method':'Cash','cashReceived':50000,'transferConfirmed':False,'requestKey':key()});assert payment['change']==11000;assert detail(o)['header']['status']=='Open';send(o);d=item(o);kitchen.post(f'/details/{d}/start',{});kitchen.post(f'/details/{d}/ready',{});sales.post(f'/details/{d}/serve',{});assert detail(o)['header']['status']=='Completed'
test('TC13','FR16/FR17/FR19','Quầy trả trước, tiền thừa đúng, chế biến tiếp và hoàn tất',counter_pay)
def concurrent_payment():
 o=create([(4,1)]);before=manager.get('/dashboard')['totals'];c2=Client('thungan');payload={'method':'Cash','cashReceived':20000,'transferConfirmed':False,'requestKey':key()}
 with concurrent.futures.ThreadPoolExecutor(2) as pool:r=list(pool.map(lambda client:client.post(f'/orders/{o}/pay',payload),[cashier,c2]))
 after=manager.get('/dashboard')['totals'];assert r[0]['id']==r[1]['id'];assert after['paidOrders']-before['paidOrders']==1;assert after['revenue']-before['revenue']==19000
test('TC14','FR19/FR26/BR12','Hai thu ngân đồng thời chỉ một Payment và doanh thu cộng một lần',concurrent_payment)
def cancel_after_cooking():
 o=create([(1,1)],4);send(o);d=item(o);kitchen.post(f'/details/{d}/start',{});before=stocks();r=sales.post(f'/orders/{o}/cancel',{'reason':'Khách đổi ý, ca kiểm thử'});assert r['status']=='Pending';cashier.post(f'/orders/{o}/pay',{'method':'Cash','cashReceived':50000,'transferConfirmed':False,'requestKey':key()},409);manager.post(f'/cancellations/{r["id"]}/review',{'approve':True});assert stocks()==before;assert detail(o)['header']['status']=='Cancelled';consume=[t for t in inventory()['transactions'] if t['detailId']==d and t['type']=='Consume'];assert len(consume)==1 and consume[0]['waste']==.25
test('TC15','FR18/BR20','Hủy sau chế biến cần duyệt, ghi hao hụt và không xuất/hoàn lần hai',cancel_after_cooking)
test('TC16','FR18/TK06','Đơn đã thanh toán không được hủy',lambda:manager.post(f'/orders/{state["counter"]}/cancel',{'reason':'Đã thu tiền'},409))
def adjustment_idempotent():
 before=stocks()[1];k=key();p={'ingredientId':1,'quantity':.1,'reason':'Nhập kiểm thử','requestKey':k};manager.post('/inventory/adjust',p);manager.post('/inventory/adjust',p);assert abs(stocks()[1]-before-.1)<.0001
 manager.post('/inventory/adjust',{'ingredientId':1,'quantity':-100000,'reason':'Âm tồn','requestKey':key()},409)
test('TC17','FR23/NFR08','Điều chỉnh có khóa chống lặp, không làm tồn âm',adjustment_idempotent)
def table_completion():
 o=state['o1'];d=detail(o)
 for line in d['items']:
  if line['status']=='Waiting':kitchen.post(f'/details/{line["id"]}/start',{})
  kitchen.post(f'/details/{line["id"]}/ready',{});sales.post(f'/details/{line["id"]}/serve',{})
 assert detail(o)['header']['status']=='Open';cashier.post(f'/orders/{o}/pay',{'method':'Transfer','cashReceived':0,'transferConfirmed':True,'requestKey':key()});assert detail(o)['header']['status']=='Completed';assert next(t for t in sales.get('/tables') if t['id']==1)['sessionId'] is None
 sales.post(f'/orders/{o}/items',{'items':[{'productId':1,'quantity':1}],'requestKey':key()},409)
test('TC18','FR12/FR17/FR19','Bàn giao món trước thu tiền, thu xong đóng phiên và chặn gọi thêm',table_completion)
def logout_blocks():
 c=Client('banhang');old='; '.join(cookie.name+'='+cookie.value for cookie in c.jar);c.post('/logout',{});c.req('/orders',expect=401)
 # Replaying the exact cookie copied before logout must also fail server-side.
 try:urllib.request.urlopen(urllib.request.Request(base+'/api/orders',headers={'Cookie':old}),timeout=10);raise AssertionError('Old signed cookie was accepted after logout')
 except urllib.error.HTTPError as e:assert e.code==401
test('TC19','FR10','Đăng xuất khiến phiên cũ không đọc được API',logout_blocks)

def competing_lines():
 stock_adjust(1,1);o=create([(1,3),(2,3)],5);send(o);details=detail(o)['items'];k2=Client('bep')
 def attempt(pair):
  client,line=pair;return client.post(f'/details/{line["id"]}/start',{},(200,409))
 with concurrent.futures.ThreadPoolExecutor(2) as pool:r=list(pool.map(attempt,[(kitchen,details[0]),(k2,details[1])]))
 assert sum(i.get('status')=='Cooking' for i in r)==1;assert stocks()[1]==.25
 assert sorted(i['status'] for i in detail(o)['items'])==['Cooking','Waiting']
test('TC20','FR24/BR19','Tồn 1kg, hai dòng cần 0,75kg: chỉ một dòng bắt đầu, không tồn âm',competing_lines)
test('TC21','FR19/BR17','Bán hàng không được gọi API thu tiền',lambda:sales.post(f'/orders/{state["counter"]}/pay',{'method':'Cash','cashReceived':50000,'transferConfirmed':False,'requestKey':key()},403))
def one_open_session():
 o=create([(4,1)],6);sales.post('/orders',{'orderType':'Table','tableId':6,'items':[{'productId':4,'quantity':1}],'requestKey':key()},409);assert next(t for t in sales.get('/tables') if t['id']==6)['orderId']==o
test('TC22','FR12/BR18','Một bàn không có hai phiên mở',one_open_session)

root=pathlib.Path(__file__).resolve().parents[1];folder=root/'docs/evidence';folder.mkdir(parents=True,exist_ok=True)
report={'executedAtUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'baseUrl':base,'testDatabase':'Isolated ChickenPOS_Test_* database; see test-server startup record','cases':results,'passed':sum(r['status']=='PASS' for r in results),'failed':sum(r['status']=='FAIL' for r in results),'boundary':'Functional HTTP + SQL persistence/transaction checks. Not a load test, penetration test or production certification.'}
(folder/'integration-results.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
md='# Kết quả kiểm thử tích hợp ChickenPOS\n\nThực thi UTC: '+report['executedAtUtc']+'\n\nKiểm thử HTTP trên ứng dụng ASP.NET và SQL Server LocalDB riêng. Không chạy trên DB demo buổi báo cáo.\n\n| Ca | FR/BR | Nội dung | Thực tế | Thời gian (ms) |\n|---|---|---|---|---|\n'
for r in results:md+=f"| {r['code']} | {r['fr']} | {r['title']} | {r['status']} | {r['elapsedMs']} |\n"
md+=f"\nTổng: {report['passed']} PASS, {report['failed']} FAIL. Thời gian là thời gian ca thử, không phải kết quả benchmark NFR02.\n"
for r in results:
 if r['status']=='FAIL':md+=f"\n{r['code']}: {r['detail']}\n"
(folder/'KET_QUA_KIEM_THU.md').write_text(md,encoding='utf8');print(json.dumps({'passed':report['passed'],'failed':report['failed']}));sys.exit(1 if report['failed'] else 0)

