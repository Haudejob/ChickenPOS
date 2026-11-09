using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;

namespace ChickenPos.Web;

// The service owns transaction boundaries; all SQL inputs are parameterized.
public sealed class Store(IConfiguration config, IWebHostEnvironment env)
{
    readonly string connection = config.GetConnectionString("ChickenPos") ?? throw new InvalidOperationException("Thiếu kết nối SQL.");
    static SqlCommand Command(SqlConnection c, SqlTransaction? tx, string sql, params (string,object?)[] args)
    {
        var cmd=new SqlCommand(sql,c,tx);
        foreach(var (k,v) in args) cmd.Parameters.AddWithValue("@"+k,v??DBNull.Value);
        return cmd;
    }
    static async Task<int> Exec(SqlConnection c,SqlTransaction? tx,string sql,params (string,object?)[] a)
    {using var cmd=Command(c,tx,sql,a);return await cmd.ExecuteNonQueryAsync();}
    static async Task<T> Value<T>(SqlConnection c,SqlTransaction? tx,string sql,params (string,object?)[] a)
    {using var cmd=Command(c,tx,sql,a);var v=await cmd.ExecuteScalarAsync();return v is null or DBNull?default!:(T)Convert.ChangeType(v,typeof(T));}
    static async Task<List<Dictionary<string,object?>>> Query(SqlConnection c,SqlTransaction? tx,string sql,params (string,object?)[] a)
    {
        using var cmd=Command(c,tx,sql,a);using var r=await cmd.ExecuteReaderAsync();var rows=new List<Dictionary<string,object?>>();
        while(await r.ReadAsync()){var d=new Dictionary<string,object?>(StringComparer.OrdinalIgnoreCase);for(int i=0;i<r.FieldCount;i++)d[r.GetName(i)]=r.IsDBNull(i)?null:r.GetValue(i);rows.Add(d);}return rows;
    }
    async Task<T> Transaction<T>(Func<SqlConnection,SqlTransaction,Task<T>> work)
    {
        using var c=new SqlConnection(connection);await c.OpenAsync();using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable);
        try{var result=await work(c,tx);await tx.CommitAsync();return result;}catch{await tx.RollbackAsync();throw;}
    }
    public async Task<List<Dictionary<string,object?>>> Read(string sql,params (string,object?)[] a)
    {using var c=new SqlConnection(connection);await c.OpenAsync();return await Query(c,null,sql,a);}
    static void Require(bool ok,string message,int status=400){if(!ok)throw new ApiError(message,status);}
    static string Key(string? key){Require(!string.IsNullOrWhiteSpace(key)&&key.Length<=80,"Thiếu hoặc sai mã thao tác.");return key!;}
    static string Clean(string? s,int max,string label){Require(!string.IsNullOrWhiteSpace(s)&&s.Trim().Length<=max,$"{label} không hợp lệ.");return s!.Trim();}

    public async Task Initialize()
    {
        var cs=new SqlConnectionStringBuilder(connection);var db=cs.InitialCatalog;
        if(!Regex.IsMatch(db,@"^ChickenPOS_[A-Za-z0-9_]+$"))throw new InvalidOperationException("Chỉ tự khởi tạo DB có tiền tố ChickenPOS_.");
        cs.InitialCatalog="master";using(var c=new SqlConnection(cs.ConnectionString)){await c.OpenAsync();if(await Value<int>(c,null,"SELECT COUNT(*) FROM sys.databases WHERE name=@db",("db",db))==0)await Exec(c,null,$"CREATE DATABASE [{db}]");}
        var schema=await File.ReadAllTextAsync(Path.Combine(env.ContentRootPath,"Data/schema.sql"));
        using(var c=new SqlConnection(connection)){await c.OpenAsync();await Exec(c,null,schema);}
        await Transaction(async(c,t)=>{
            await Exec(c,t,"UPDATE Product SET Name=@new WHERE Id=5 AND Name=@old AND Category=N'Combo'",("new","Combo 2 miếng gà + khoai tây chiên nhỏ"),("old","Combo 2 miếng gà"));
            if(await Value<int>(c,t,"SELECT COUNT(*) FROM Roles")>0)return 0;
            foreach(var r in new[]{"Manager","Sales","Cashier","Kitchen"})await Exec(c,t,"INSERT Roles(Name) VALUES(@r)",("r",r));
            string password=config["DemoPassword"]??"Chicken@2026!";
            var hasher=new PasswordHasher<string>();
            foreach(var (u,n,r) in new[]{("quanly","Quản lý","Manager"),("banhang","Nhân viên bán hàng","Sales"),("thungan","Thu ngân","Cashier"),("bep","Nhân viên bếp","Kitchen")})
                await Exec(c,t,"INSERT Users(RoleId,Username,DisplayName,PasswordHash) SELECT Id,@u,@n,@h FROM Roles WHERE Name=@r",("u",u),("n",n),("h",hasher.HashPassword(u,password)),("r",r));
            foreach(var (name,price,category,icon) in new[]{("Gà giòn cay",39000,"Gà rán","🍗"),("Gà sốt mật ong",45000,"Gà rán","🍗"),("Khoai tây chiên",29000,"Ăn kèm","🍟"),("Coca-Cola",19000,"Đồ uống","🥤"),("Combo 2 miếng gà + khoai tây chiên nhỏ",79000,"Combo","🍱"),("Trà đào",25000,"Đồ uống","🧋")})
                await Exec(c,t,"INSERT Product(Name,Price,Category,Icon) VALUES(@n,@p,@c,@i)",("n",name),("p",price),("c",category),("i",icon));
            for(int i=1;i<=8;i++)await Exec(c,t,"INSERT DiningTable(Name) VALUES(@n)",("n",$"Bàn {i:00}"));
            foreach(var (name,unit,stock,min) in new[]{("Thịt gà","kg",10m,2m),("Khoai tây","kg",5m,1m),("Nước ngọt","chai",20m,5m),("Trà đào nguyên liệu","phần",15m,3m)})
                await Exec(c,t,"INSERT Ingredient(Name,Unit,Stock,MinStock) VALUES(@n,@u,@s,@m)",("n",name),("u",unit),("s",stock),("m",min));
            foreach(var (p,i,q) in new[]{(1,1,.25m),(2,1,.25m),(3,2,.2m),(4,3,1m),(5,1,.5m),(5,2,.1m),(6,4,1m)})
                await Exec(c,t,"INSERT Recipe VALUES(@p,@i,@q)",("p",p),("i",i),("q",q));
            await Exec(c,t,"INSERT InventoryTransaction(IngredientId,QuantityDelta,Type,ActorId,RequestKey) SELECT Id,Stock,'Opening',1,CONCAT('opening-',Id) FROM Ingredient");return 0;
        });
        await ExpandMenu();
        await AddPastaMenu();
    }
    Task ExpandMenu(){
        var dishes=new[]{
            (Name:"Gà sốt phô mai",Price:49000,Category:"Gà rán",Image:"ga-sot-pho-mai.png",Recipe:new[]{("Thịt gà",.25m),("Bột chiên",.04m),("Dầu chiên",.02m),("Sốt phô mai",.05m)}),
            (Name:"Gà sốt tỏi cay",Price:49000,Category:"Gà rán",Image:"ga-sot-toi-cay.png",Recipe:new[]{("Thịt gà",.25m),("Bột chiên",.04m),("Dầu chiên",.02m),("Sốt tỏi cay",.05m)}),
            (Name:"Khoai tây lắc phô mai",Price:35000,Category:"Ăn kèm",Image:"khoai-tay-lac-pho-mai.png",Recipe:new[]{("Khoai tây",.2m),("Dầu chiên",.015m),("Bột phô mai",.015m)}),
            (Name:"Trà chanh",Price:22000,Category:"Đồ uống",Image:"tra-chanh.png",Recipe:new[]{("Trà khô",.005m),("Chanh tươi",.04m),("Đường",.025m),("Đá viên",.15m)})
        };
        return ApplyMenuUpdate("menu-four-dishes-20261107",new[]{("Thịt gà","kg",10m,2m),("Khoai tây","kg",5m,1m),("Bột chiên","kg",3m,.5m),("Dầu chiên","lít",5m,1m),("Sốt phô mai","kg",1.5m,.3m),("Sốt tỏi cay","kg",1.5m,.3m),("Bột phô mai","kg",.5m,.1m),("Trà khô","kg",.3m,.05m),("Chanh tươi","kg",2m,.4m),("Đường","kg",2m,.4m),("Đá viên","kg",10m,2m)},dishes);
    }

    Task AddPastaMenu(){
        var bolognese=new[]{("Mì spaghetti khô",.1m),("Thịt bò bằm",.1m),("Sốt cà chua",.12m)};
        var alfredo=new[]{("Mì spaghetti khô",.1m),("Thịt gà",.12m),("Kem nấu ăn",.08m),("Phô mai Parmesan",.02m)};
        var spicy=new[]{("Mì spaghetti khô",.1m),("Thịt gà",.15m),("Bột chiên",.025m),("Dầu chiên",.015m),("Sốt tỏi cay",.06m)};
        var lemonTea=new[]{("Trà khô",.005m),("Chanh tươi",.04m),("Đường",.025m),("Đá viên",.15m)};
        // Half a fries serving, rounded to the three decimal places supported by inventory.
        var smallFriesAndCola=new[]{("Khoai tây",.1m),("Dầu chiên",.008m),("Bột phô mai",.008m),("Nước ngọt",1m)};
        var spicyCombo=spicy.Concat(smallFriesAndCola).GroupBy(item=>item.Item1)
            .Select(group=>(group.Key,group.Sum(item=>item.Item2))).ToArray();
        var dishes=new[]{
            (Name:"Mì Ý sốt bò bằm",Price:69000,Category:"Mì Ý",Image:"mi-y-bo-bam.png",Recipe:bolognese),
            (Name:"Mì Ý sốt kem gà phô mai",Price:69000,Category:"Mì Ý",Image:"mi-y-kem-ga-pho-mai.png",Recipe:alfredo),
            (Name:"Mì Ý gà giòn sốt cay",Price:65000,Category:"Mì Ý",Image:"mi-y-ga-gion-sot-cay.png",Recipe:spicy),
            (Name:"Combo Mì Ý bò bằm + Trà chanh",Price:79000,Category:"Combo",Image:"combo-mi-y-bo-bam-tra-chanh.png",Recipe:bolognese.Concat(lemonTea).ToArray()),
            (Name:"Combo Mì Ý gà giòn + Khoai lắc + Coca-Cola",Price:99000,Category:"Combo",Image:"combo-mi-y-ga-gion-khoai-lac-coca.png",Recipe:spicyCombo)
        };
        return ApplyMenuUpdate("menu-pasta-combos-20261109",new[]{
            ("Mì spaghetti khô","kg",5m,1m),("Thịt bò bằm","kg",3m,.5m),("Sốt cà chua","kg",4m,.75m),
            ("Kem nấu ăn","kg",3m,.5m),("Phô mai Parmesan","kg",1m,.2m),
            ("Thịt gà","kg",10m,2m),("Bột chiên","kg",3m,.5m),("Dầu chiên","lít",5m,1m),
            ("Sốt tỏi cay","kg",1.5m,.3m),("Trà khô","kg",.3m,.05m),("Chanh tươi","kg",2m,.4m),
            ("Đường","kg",2m,.4m),("Đá viên","kg",10m,2m),("Khoai tây","kg",5m,1m),
            ("Bột phô mai","kg",.5m,.1m),("Nước ngọt","chai",20m,5m)
        },dishes);
    }

    Task ApplyMenuUpdate(string migration, (string Name,string Unit,decimal Stock,decimal Min)[] stockItems,
        (string Name,int Price,string Category,string Image,(string Ingredient,decimal Amount)[] Recipe)[] dishes) => Transaction(async(c,t)=>{
        if(await Value<int>(c,t,"SELECT COUNT(*) FROM CatalogMigration WHERE Id=@id",("id",migration))>0)return 0;
        var actor=await Value<int>(c,t,"SELECT TOP(1) Id FROM Users WHERE Username=N'quanly' ORDER BY Id");
        Require(actor>0,"Không tìm thấy quản lý để ghi nhận tồn đầu nguyên liệu.");
        var ingredients=new Dictionary<string,int>();
        foreach(var (name,unit,stock,min) in stockItems){
            var id=await Value<int>(c,t,"SELECT TOP(1) Id FROM Ingredient WHERE Name=@n AND Unit=@u ORDER BY Id",("n",name),("u",unit));
            if(id==0){
                id=await Value<int>(c,t,"INSERT Ingredient(Name,Unit,Stock,MinStock) OUTPUT INSERTED.Id VALUES(@n,@u,@s,@m)",("n",name),("u",unit),("s",stock),("m",min));
                await Exec(c,t,"INSERT InventoryTransaction(IngredientId,QuantityDelta,Type,ActorId,RequestKey,Reason) VALUES(@i,@s,'Opening',@a,@k,@r)",("i",id),("s",stock),("a",actor),("k",migration),("r","Tồn đầu cho thực đơn mở rộng"));
            }
            ingredients[name]=id;
        }
        foreach(var d in dishes){
            var id=await Value<int>(c,t,"SELECT TOP(1) Id FROM Product WHERE Name=@n ORDER BY Id",("n",d.Name));
            if(id==0)id=await Value<int>(c,t,"INSERT Product(Name,Price,Category,Icon,ImagePath) OUTPUT INSERTED.Id VALUES(@n,@p,@c,N'🍗',@im)",("n",d.Name),("p",d.Price),("c",d.Category),("im","/images/menu/"+d.Image));
            else await Exec(c,t,"UPDATE Product SET ImagePath=@im WHERE Id=@id",("im","/images/menu/"+d.Image),("id",id));
            foreach(var (name,amount) in d.Recipe)
                await Exec(c,t,"IF NOT EXISTS(SELECT 1 FROM Recipe WHERE ProductId=@p AND IngredientId=@i) INSERT Recipe(ProductId,IngredientId,QuantityPerUnit) VALUES(@p,@i,@q)",("p",id),("i",ingredients[name]),("q",amount));
        }
        await Exec(c,t,"INSERT CatalogMigration(Id) VALUES(@id)",("id",migration));return 0;
    });
    public async Task<Dictionary<string,object?>?> Login(string username,string password)
    {
        if(string.IsNullOrWhiteSpace(username)||string.IsNullOrEmpty(password)||password.Length>200)return null;
        var rows=await Read("SELECT u.Id id,u.Username username,u.DisplayName displayName,u.PasswordHash hash,r.Name role FROM Users u JOIN Roles r ON r.Id=u.RoleId WHERE u.Username=@u AND u.IsActive=1",("u",username.Trim()));
        if(rows.Count==0)return null;var user=rows[0];var result=new PasswordHasher<string>().VerifyHashedPassword(username,(string)user["hash"]!,password);return result==PasswordVerificationResult.Failed?null:user;
    }
    public async Task<bool> Active(int id)=> (await Read("SELECT Id FROM Users WHERE Id=@id AND IsActive=1",("id",id))).Count==1;
    public Task<List<Dictionary<string,object?>>> Products()=>Read("SELECT Id id,Name name,Price price,Category category,Icon icon,ImagePath imagePath,IsActive isActive FROM Product ORDER BY Id");
    public Task<List<Dictionary<string,object?>>> Tables()=>Read("SELECT t.Id id,t.Name name,s.Id sessionId,o.Id orderId,o.Code orderCode FROM DiningTable t LEFT JOIN DiningSession s ON s.TableId=t.Id AND s.ClosedAt IS NULL LEFT JOIN Orders o ON o.SessionId=s.Id ORDER BY t.Id");
    public Task<List<Dictionary<string,object?>>> Orders()=>Read("SELECT o.Id id,o.Code code,o.OrderType orderType,o.Status status,o.CreatedAt createdAt,t.Name tableName,COALESCE(SUM(CASE WHEN d.KitchenStatus<>'Cancelled' THEN d.Quantity*d.UnitPriceSnapshot ELSE 0 END),0) total,CASE WHEN p.Id IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END paid FROM Orders o LEFT JOIN OrderDetail d ON d.OrderId=o.Id LEFT JOIN DiningSession s ON s.Id=o.SessionId LEFT JOIN DiningTable t ON t.Id=s.TableId LEFT JOIN Payment p ON p.OrderId=o.Id GROUP BY o.Id,o.Code,o.OrderType,o.Status,o.CreatedAt,t.Name,p.Id ORDER BY o.Id DESC");
    public async Task<object> Order(int id)
    {
        var headers=await Read("SELECT o.Id id,o.Code code,o.OrderType orderType,o.Status status,o.CreatedAt createdAt,t.Name tableName,p.Id paymentId,p.Method method,p.Amount amount,p.CashReceived cashReceived,p.PaidAt paidAt FROM Orders o LEFT JOIN DiningSession s ON s.Id=o.SessionId LEFT JOIN DiningTable t ON t.Id=s.TableId LEFT JOIN Payment p ON p.OrderId=o.Id WHERE o.Id=@id",("id",id));
        Require(headers.Count>0,"Không tìm thấy đơn.",404);
        var items=await Read("SELECT Id id,ProductId productId,ProductNameSnapshot name,UnitPriceSnapshot price,Quantity quantity,KitchenStatus status,KitchenBatchId batchId,SentAt sentAt,StartedAt startedAt,ServedAt servedAt FROM OrderDetail WHERE OrderId=@id ORDER BY Id",("id",id));
        var history=await Read("SELECT h.Id id,h.EventType eventType,h.NewState newState,h.OccurredAt occurredAt,u.DisplayName actor FROM OrderStatusHistory h JOIN Users u ON u.Id=h.ActorId WHERE h.OrderId=@id ORDER BY h.Id DESC",("id",id));
        var cancellations=await Read("SELECT Id id,Reason reason,Status status FROM CancellationRequest WHERE OrderId=@id ORDER BY Id DESC",("id",id));
        return new{header=headers[0],items,history,cancellations,total=items.Where(i=>i["status"] as string != "Cancelled").Sum(i=>(decimal)i["price"]!*(int)i["quantity"]!)};
    }
    static async Task<Dictionary<string,object?>> LockOrder(SqlConnection c,SqlTransaction t,int id)
    {var list=await Query(c,t,"SELECT Id,OrderType,Status,SessionId FROM Orders WITH(UPDLOCK,HOLDLOCK) WHERE Id=@id",("id",id));Require(list.Count==1,"Không tìm thấy đơn.",404);return list[0];}
    static async Task NotPaid(SqlConnection c,SqlTransaction t,int id)
    {Require(await Value<int>(c,t,"SELECT COUNT(*) FROM Payment WHERE OrderId=@id",("id",id))==0,"Đơn đã thanh toán, không thể thay đổi.",409);}
    static async Task NoPending(SqlConnection c,SqlTransaction t,int id)
    {Require(await Value<int>(c,t,"SELECT COUNT(*) FROM CancellationRequest WHERE OrderId=@id AND Status='Pending'",("id",id))==0,"Đơn có yêu cầu hủy đang chờ duyệt.",409);}
    static Task<int> History(SqlConnection c,SqlTransaction t,int order,int? detail,int actor,string oldState,string newState,string kind,string key)
        =>Exec(c,t,"INSERT OrderStatusHistory(OrderId,OrderDetailId,OldState,NewState,ActorId,EventType,RequestKey) VALUES(@o,@d,@old,@new,@a,@k,@key)",("o",order),("d",detail),("old",oldState),("new",newState),("a",actor),("k",kind),("key",key));
    static async Task AddLines(SqlConnection c,SqlTransaction t,int id,List<ItemInput>? items,string key)
    {
        Require(items is {Count:>0 and <=50},"Đơn cần từ 1 đến 50 dòng món.");
        foreach(var item in items!){Require(item.Quantity is >=1 and <=100,"Số lượng phải là số nguyên từ 1 đến 100.");
            var products=await Query(c,t,"SELECT Name,Price FROM Product WHERE Id=@p AND IsActive=1",("p",item.ProductId));Require(products.Count==1,"Món không còn kinh doanh.");var p=products[0];
            await Exec(c,t,"INSERT OrderDetail(OrderId,ProductId,ProductNameSnapshot,UnitPriceSnapshot,Quantity,AddedKey) VALUES(@o,@p,@n,@price,@q,@k)",("o",id),("p",item.ProductId),("n",p["Name"]),("price",p["Price"]),("q",item.Quantity),("k",key));}
    }
    public Task<int> Create(NewOrder input,int actor)=>Transaction(async(c,t)=>{
        var key=Key(input.RequestKey);var exists=await Value<int>(c,t,"SELECT Id FROM Orders WHERE RequestKey=@k",("k",key));if(exists>0)return exists;
        Require(input.OrderType is "Counter" or "Table","Loại phục vụ không hợp lệ.");int? session=null;
        if(input.OrderType=="Table"){
            Require(input.TableId.HasValue,"Hãy chọn bàn.");Require(await Value<int>(c,t,"SELECT COUNT(*) FROM DiningTable WITH(UPDLOCK,HOLDLOCK) WHERE Id=@id",("id",input.TableId))==1,"Bàn không tồn tại.");
            session=await Value<int>(c,t,"SELECT Id FROM DiningSession WHERE TableId=@id AND ClosedAt IS NULL",("id",input.TableId));
            Require(session==0,"Bàn đang có đơn. Hãy mở đơn hiện tại để gọi thêm.",409);
            session=await Value<int>(c,t,"INSERT DiningSession(TableId) OUTPUT INSERTED.Id VALUES(@id)",("id",input.TableId));
        }
        var id=await Value<int>(c,t,"INSERT Orders(Code,SessionId,OrderType,CreatedBy,RequestKey) OUTPUT INSERTED.Id VALUES(@code,@s,@type,@a,@k)",("code","CP-"+Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()),("s",session),("type",input.OrderType),("a",actor),("k",key));
        await AddLines(c,t,id,input.Items,key);await History(c,t,id,null,actor,"","Open","Created",key);return id;
    });
    public Task<int> Add(int id,AddItems input,int actor)=>Transaction(async(c,t)=>{
        var o=await LockOrder(c,t,id);var key=Key(input.RequestKey);
        if(await Value<int>(c,t,"SELECT COUNT(*) FROM OrderDetail WHERE OrderId=@id AND AddedKey=@k",("id",id),("k",key))>0)return id;
        Require((string)o["Status"]! =="Open","Đơn không còn mở.",409);await NotPaid(c,t,id);await NoPending(c,t,id);
        await AddLines(c,t,id,input.Items,key);await History(c,t,id,null,actor,"Open","Open","ItemsAdded",key);return id;
    });
    public Task<int> Send(int id,string requestKey,int actor)=>Transaction(async(c,t)=>{
        var key=Key(requestKey);var o=await LockOrder(c,t,id);
        var previous=await Value<int>(c,t,"SELECT COUNT(*) FROM OrderDetail WHERE OrderId=@id AND KitchenBatchId=@k",("id",id),("k",key));if(previous>0)return previous;
        Require((string)o["Status"]! =="Open","Đơn không còn mở.",409);await NoPending(c,t,id);
        if((string)o["OrderType"]! =="Counter")Require(await Value<int>(c,t,"SELECT COUNT(*) FROM Payment WHERE OrderId=@id",("id",id))==1,"Đơn quầy cần thanh toán trước khi gửi bếp.",409);
        var n=await Exec(c,t,"UPDATE OrderDetail SET KitchenStatus='Waiting',SentAt=SYSUTCDATETIME(),KitchenBatchId=@k WHERE OrderId=@id AND KitchenStatus='Draft'",("id",id),("k",key));Require(n>0,"Không có món mới chưa gửi.",409);
        await History(c,t,id,null,actor,"Draft","Waiting","SentToKitchen",key);return n;
    });
    public Task<List<Dictionary<string,object?>>> Kitchen()=>Read("SELECT d.Id id,d.OrderId orderId,o.Code code,t.Name tableName,o.OrderType orderType,d.ProductNameSnapshot name,d.Quantity quantity,d.KitchenStatus status,d.SentAt sentAt,d.KitchenBatchId batchId FROM OrderDetail d JOIN Orders o ON o.Id=d.OrderId LEFT JOIN DiningSession s ON s.Id=o.SessionId LEFT JOIN DiningTable t ON t.Id=s.TableId WHERE d.KitchenStatus IN ('Waiting','Cooking','Ready') ORDER BY d.SentAt,d.Id");
    public Task<string> Start(int detail,int actor)=>Transaction(async(c,t)=>{
        int order=await Value<int>(c,t,"SELECT OrderId FROM OrderDetail WHERE Id=@d",("d",detail));Require(order>0,"Dòng món không tồn tại.",404);var o=await LockOrder(c,t,order);await NoPending(c,t,order);
        var lines=await Query(c,t,"SELECT * FROM OrderDetail WITH(UPDLOCK,HOLDLOCK) WHERE Id=@d",("d",detail));var line=lines[0];var state=(string)line["KitchenStatus"]!;
        if(state is "Cooking" or "Ready" or "Served")return state;
        Require((string)o["Status"]! =="Open"&&state=="Waiting","Món chưa gửi hoặc đã hủy.",409);
        var recipe=await Query(c,t,"SELECT IngredientId,QuantityPerUnit FROM Recipe WHERE ProductId=@p ORDER BY IngredientId",("p",line["ProductId"]));Require(recipe.Count>0,"Món chưa có công thức, không thể chế biến.",409);
        foreach(var r in recipe){decimal need=(decimal)r["QuantityPerUnit"]!*(int)line["Quantity"]!;var ingredients=await Query(c,t,"SELECT Name,Stock,Unit FROM Ingredient WITH(UPDLOCK,HOLDLOCK) WHERE Id=@i",("i",r["IngredientId"]));var i=ingredients[0];Require((decimal)i["Stock"]!>=need,$"Thiếu {i["Name"]}: cần {need} {i["Unit"]}, tồn {i["Stock"]}.",409);}
        foreach(var r in recipe){decimal need=(decimal)r["QuantityPerUnit"]!*(int)line["Quantity"]!;
            var updated=await Exec(c,t,"UPDATE Ingredient SET Stock=Stock-@q WHERE Id=@i AND Stock>=@q",("q",need),("i",r["IngredientId"]));Require(updated==1,"Tồn nguyên liệu vừa thay đổi. Hãy tải lại và thử lại.",409);
            await Exec(c,t,"INSERT InventoryTransaction(IngredientId,OrderDetailId,QuantityDelta,Type,RecipeAmountSnapshot,ActorId,RequestKey) VALUES(@i,@d,-@q,'Consume',@r,@a,@key)",("i",r["IngredientId"]),("d",detail),("q",need),("r",r["QuantityPerUnit"]),("a",actor),("key",$"start-{detail}"));}
        await Exec(c,t,"UPDATE OrderDetail SET KitchenStatus='Cooking',StartedAt=SYSUTCDATETIME() WHERE Id=@d",("d",detail));await History(c,t,order,detail,actor,"Waiting","Cooking","CookingStarted",$"start-{detail}");return "Cooking";
    });
    static async Task CloseIfComplete(SqlConnection c,SqlTransaction t,int id)
    {
        if(await Value<int>(c,t,"SELECT COUNT(*) FROM Payment WHERE OrderId=@id",("id",id))==1&&await Value<int>(c,t,"SELECT COUNT(*) FROM OrderDetail WHERE OrderId=@id AND KitchenStatus NOT IN ('Served','Cancelled')",("id",id))==0){
            await Exec(c,t,"UPDATE Orders SET Status='Completed' WHERE Id=@id;UPDATE DiningSession SET ClosedAt=SYSUTCDATETIME() WHERE Id=(SELECT SessionId FROM Orders WHERE Id=@id) AND ClosedAt IS NULL",("id",id));}
    }
    public Task<string> Progress(int detail,int actor,bool serve)=>Transaction(async(c,t)=>{
        int order=await Value<int>(c,t,"SELECT OrderId FROM OrderDetail WHERE Id=@d",("d",detail));Require(order>0,"Không tìm thấy món.",404);await LockOrder(c,t,order);await NoPending(c,t,order);
        string state=await Value<string>(c,t,"SELECT KitchenStatus FROM OrderDetail WITH(UPDLOCK,HOLDLOCK) WHERE Id=@d",("d",detail));string target=serve?"Served":"Ready",expected=serve?"Ready":"Cooking";
        if(state==target)return target;Require(state==expected,"Chuyển trạng thái không hợp lệ.",409);
        await Exec(c,t,"UPDATE OrderDetail SET KitchenStatus=@s,ServedAt=CASE WHEN @s='Served' THEN SYSUTCDATETIME() ELSE ServedAt END WHERE Id=@d",("s",target),("d",detail));await History(c,t,order,detail,actor,state,target,serve?"Served":"Ready",$"{target}-{detail}");await CloseIfComplete(c,t,order);return target;
    });
    public Task<object> Pay(int id,PayInput input,int actor)=>Transaction(async(c,t)=>{
        var key=Key(input.RequestKey);var o=await LockOrder(c,t,id);var previous=await Query(c,t,"SELECT Id id,Amount amount,CashReceived cashReceived,Method method FROM Payment WHERE OrderId=@id",("id",id));if(previous.Count>0)return (object)previous[0];
        Require((string)o["Status"]! =="Open","Đơn không thể thanh toán.",409);await NoPending(c,t,id);
        if((string)o["OrderType"]! =="Table")Require(await Value<int>(c,t,"SELECT COUNT(*) FROM OrderDetail WHERE OrderId=@id AND KitchenStatus='Draft'",("id",id))==0,"Hãy gửi toàn bộ món mới trước khi chốt thanh toán bàn.",409);
        decimal total=await Value<decimal>(c,t,"SELECT SUM(Quantity*UnitPriceSnapshot) FROM OrderDetail WHERE OrderId=@id AND KitchenStatus<>'Cancelled'",("id",id));
        Require(input.Method is "Cash" or "Transfer","Chỉ hỗ trợ tiền mặt và chuyển khoản.");
        if(input.Method=="Cash")Require(input.CashReceived>=total&&input.CashReceived<=1000000000&&decimal.Truncate(input.CashReceived)==input.CashReceived,"Tiền khách đưa không hợp lệ hoặc chưa đủ.");else Require(input.TransferConfirmed,"Thu ngân chưa xác nhận đã nhận chuyển khoản.");
        var payment=await Value<int>(c,t,"INSERT Payment(OrderId,Method,Amount,CashReceived,ActorId,RequestKey) OUTPUT INSERTED.Id VALUES(@id,@m,@a,@cash,@user,@key)",("id",id),("m",input.Method),("a",total),("cash",input.Method=="Cash"?input.CashReceived:null),("user",actor),("key",key));
        await History(c,t,id,null,actor,"Unpaid","Paid","PaymentRecorded",key);await CloseIfComplete(c,t,id);return (object)new{id=payment,amount=total,change=input.Method=="Cash"?input.CashReceived-total:0};
    });
    static async Task ApplyCancellation(SqlConnection c,SqlTransaction t,int order,int request,int actor)
    {
        await Exec(c,t,"UPDATE InventoryTransaction SET WasteQuantity=-QuantityDelta,CancellationRequestId=@r WHERE Type='Consume' AND OrderDetailId IN (SELECT Id FROM OrderDetail WHERE OrderId=@o AND KitchenStatus NOT IN ('Served','Cancelled'));UPDATE OrderDetail SET KitchenStatus='Cancelled' WHERE OrderId=@o AND KitchenStatus NOT IN ('Served','Cancelled')",("r",request),("o",order));
        bool hasServed=await Value<int>(c,t,"SELECT COUNT(*) FROM OrderDetail WHERE OrderId=@o AND KitchenStatus='Served'",("o",order))>0;
        if(!hasServed)await Exec(c,t,"UPDATE Orders SET Status='Cancelled' WHERE Id=@o;UPDATE DiningSession SET ClosedAt=SYSUTCDATETIME() WHERE Id=(SELECT SessionId FROM Orders WHERE Id=@o) AND ClosedAt IS NULL",("o",order));
        await History(c,t,order,null,actor,"Open",hasServed?"Open":"Cancelled","CancellationApproved",$"cancel-{request}");
    }
    public Task<object> Cancel(int id,string reason,int actor,bool manager)=>Transaction(async(c,t)=>{
        reason=Clean(reason,500,"Lý do hủy");var o=await LockOrder(c,t,id);Require((string)o["Status"]! =="Open","Đơn không còn mở.",409);await NotPaid(c,t,id);await NoPending(c,t,id);
        Require(await Value<int>(c,t,"SELECT COUNT(*) FROM OrderDetail WHERE OrderId=@id AND KitchenStatus NOT IN ('Served','Cancelled')",("id",id))>0,"Đơn không còn món chưa giao để hủy.",409);
        bool started=await Value<int>(c,t,"SELECT COUNT(*) FROM OrderDetail WHERE OrderId=@id AND StartedAt IS NOT NULL",("id",id))>0;bool approved=!started||manager;
        int request=await Value<int>(c,t,"INSERT CancellationRequest(OrderId,RequesterId,ReviewerId,Reason,Status,ReviewedAt) OUTPUT INSERTED.Id VALUES(@o,@a,@reviewer,@reason,@status,@time)",("o",id),("a",actor),("reviewer",approved?actor:null),("reason",reason),("status",approved?"Approved":"Pending"),("time",approved?DateTime.UtcNow:null));
        if(approved)await ApplyCancellation(c,t,id,request,actor);else await History(c,t,id,null,actor,"Open","Pending","CancellationRequested",$"request-{request}");return (object)new{id=request,status=approved?"Approved":"Pending"};
    });
    public Task<List<Dictionary<string,object?>>> Cancellations()=>Read("SELECT r.Id id,r.OrderId orderId,o.Code code,r.Reason reason,r.Status status,r.RequestedAt requestedAt,u.DisplayName requester FROM CancellationRequest r JOIN Orders o ON o.Id=r.OrderId JOIN Users u ON u.Id=r.RequesterId ORDER BY r.Id DESC");
    public Task<string> Review(int id,bool approve,int actor)=>Transaction(async(c,t)=>{
        int order=await Value<int>(c,t,"SELECT OrderId FROM CancellationRequest WHERE Id=@id",("id",id));Require(order>0,"Không có yêu cầu hủy.",404);await LockOrder(c,t,order);await NotPaid(c,t,order);
        var status=await Value<string>(c,t,"SELECT Status FROM CancellationRequest WITH(UPDLOCK,HOLDLOCK) WHERE Id=@id",("id",id));if(status!="Pending")return status;
        var target=approve?"Approved":"Rejected";await Exec(c,t,"UPDATE CancellationRequest SET Status=@s,ReviewerId=@a,ReviewedAt=SYSUTCDATETIME() WHERE Id=@id",("s",target),("a",actor),("id",id));
        if(approve)await ApplyCancellation(c,t,order,id,actor);else await History(c,t,order,null,actor,"Pending","Open","CancellationRejected",$"review-{id}");return target;
    });
    public async Task<object> Inventory()=>new{ingredients=await Read("SELECT Id id,Name name,Unit unit,Stock stock,MinStock minStock FROM Ingredient ORDER BY Id"),transactions=await Read("SELECT TOP(80) t.Id id,i.Name ingredient,i.Unit unit,t.QuantityDelta quantity,t.Type type,t.OccurredAt occurredAt,t.OrderDetailId detailId,t.WasteQuantity waste,u.DisplayName actor,t.RequestKey requestKey,t.Reason reason FROM InventoryTransaction t JOIN Ingredient i ON i.Id=t.IngredientId JOIN Users u ON u.Id=t.ActorId ORDER BY t.Id DESC")};
    public Task<decimal> Adjust(AdjustInput input,int actor)=>Transaction(async(c,t)=>{
        var key=Key(input.RequestKey);var reason=Clean(input.Reason,300,"Lý do điều chỉnh");Require(input.Quantity!=0&&Math.Abs(input.Quantity)<=100000&&decimal.Round(input.Quantity,3)==input.Quantity,"Lượng điều chỉnh phải khác 0, tối đa 3 số thập phân.");
        var stock=await Query(c,t,"SELECT Stock FROM Ingredient WITH(UPDLOCK,HOLDLOCK) WHERE Id=@id",("id",input.IngredientId));Require(stock.Count==1,"Nguyên liệu không tồn tại.",404);
        if(await Value<int>(c,t,"SELECT COUNT(*) FROM InventoryTransaction WHERE RequestKey=@k AND Type='Adjustment' AND IngredientId=@i",("k",key),("i",input.IngredientId))>0)return (decimal)stock[0]["Stock"]!;
        decimal next=(decimal)stock[0]["Stock"]!+input.Quantity;Require(next>=0,"Điều chỉnh làm tồn âm.",409);
        await Exec(c,t,"UPDATE Ingredient SET Stock=@s WHERE Id=@i;INSERT InventoryTransaction(IngredientId,QuantityDelta,Type,ActorId,RequestKey,Reason) VALUES(@i,@q,'Adjustment',@a,@k,@reason)",("s",next),("i",input.IngredientId),("q",input.Quantity),("a",actor),("k",key),("reason",reason));return next;
    });
    public async Task<object> Dashboard(DateTime? from,DateTime? to)
    {
        var now=DateTime.UtcNow.AddHours(7);var f=(from??now.Date).Date;var end=(to??f).Date.AddDays(1);Require(end>f&&end-f<=TimeSpan.FromDays(366),"Khoảng ngày không hợp lệ (tối đa 366 ngày).");
        var args=new(string,object?)[]{("from",f.AddHours(-7)),("to",end.AddHours(-7))};
        var totals=await Read("SELECT COALESCE(SUM(Amount),0) revenue,COUNT(*) paidOrders FROM Payment WHERE PaidAt>=@from AND PaidAt<@to",args);
        var top=await Read("SELECT TOP(5) d.ProductNameSnapshot name,SUM(d.Quantity) quantity FROM OrderDetail d JOIN Payment p ON p.OrderId=d.OrderId WHERE p.PaidAt>=@from AND p.PaidAt<@to AND d.KitchenStatus<>'Cancelled' GROUP BY d.ProductNameSnapshot ORDER BY quantity DESC,name",args);
        var low=await Read("SELECT Name name,Stock stock,Unit unit FROM Ingredient WHERE Stock<=MinStock ORDER BY Id");return new{totals=totals[0],top,lowStock=low,from=f.ToString("yyyy-MM-dd"),to=end.AddDays(-1).ToString("yyyy-MM-dd")};
    }
    public Task<int> SaveProduct(int? id,ProductInput input)=>Transaction(async(c,t)=>{
        var name=Clean(input.Name,120,"Tên món");var category=Clean(input.Category,40,"Nhóm món");Require(input.Price>=0&&input.Price<=10000000&&decimal.Truncate(input.Price)==input.Price,"Giá phải là VND nguyên không âm.");
        if(id is null)return await Value<int>(c,t,"INSERT Product(Name,Price,Category,Icon,IsActive) OUTPUT INSERTED.Id VALUES(@n,@p,@c,@i,@a)",("n",name),("p",input.Price),("c",category),("i",string.IsNullOrEmpty(input.Icon)?"🍗":input.Icon[..Math.Min(input.Icon.Length,10)]),("a",input.IsActive));
        Require(await Exec(c,t,"UPDATE Product SET Name=@n,Price=@p,Category=@c,IsActive=@a WHERE Id=@id",("n",name),("p",input.Price),("c",category),("a",input.IsActive),("id",id))>0,"Không tìm thấy món.",404);return id.Value;
    });
}


