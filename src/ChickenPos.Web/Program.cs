using Microsoft.AspNetCore.Http.Features;
using System.Security.Claims;
using System.Threading.RateLimiting;
using ChickenPos.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;

var builder=WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.WebHost.ConfigureKestrel(o=>o.Limits.MaxRequestBodySize=65536);
builder.Services.AddSingleton<Store>();
builder.Services.AddSingleton<SessionRegistry>();
builder.Services.AddAntiforgery(o=>{o.HeaderName="X-CSRF-TOKEN";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Strict;});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o=>{
    o.Cookie.Name="ChickenPOS.Session";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Strict;o.ExpireTimeSpan=TimeSpan.FromHours(4);
    o.Events.OnRedirectToLogin=c=>{c.Response.StatusCode=401;return Task.CompletedTask;};
    o.Events.OnRedirectToAccessDenied=c=>{c.Response.StatusCode=403;return Task.CompletedTask;};
    o.Events.OnValidatePrincipal=async c=>{if(!int.TryParse(c.Principal?.FindFirstValue(ClaimTypes.NameIdentifier),out var id)||!c.HttpContext.RequestServices.GetRequiredService<SessionRegistry>().Valid(c.Principal?.FindFirstValue("session"),id)||!await c.HttpContext.RequestServices.GetRequiredService<Store>().Active(id)){c.RejectPrincipal();await c.HttpContext.SignOutAsync();}};
});
builder.Services.AddAuthorization(o=>{
    o.AddPolicy("sell",p=>p.RequireRole("Sales","Cashier"));o.AddPolicy("cash",p=>p.RequireRole("Cashier"));
    o.AddPolicy("kitchen",p=>p.RequireRole("Kitchen"));o.AddPolicy("manager",p=>p.RequireRole("Manager"));
    o.AddPolicy("cancel",p=>p.RequireRole("Sales","Cashier","Manager"));
});
builder.Services.AddRateLimiter(o=>{
    o.RejectionStatusCode=429;
    o.AddPolicy("login",c=>RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString()??"local",_=>new FixedWindowRateLimiterOptions{PermitLimit=30,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
});
var app=builder.Build();
await app.Services.GetRequiredService<Store>().Initialize();
app.Use(async(ctx,next)=>{
    ctx.Response.Headers["X-Content-Type-Options"]="nosniff";ctx.Response.Headers["X-Frame-Options"]="DENY";
    ctx.Response.Headers["Referrer-Policy"]="same-origin";
    ctx.Response.Headers["Content-Security-Policy"]="default-src 'self'; img-src 'self' data:; style-src 'self'; script-src 'self'; frame-ancestors 'none'; base-uri 'self'";
    if(ctx.Request.Path.StartsWithSegments("/api"))ctx.Response.Headers.CacheControl="no-store";
    if ((HttpMethods.IsPost(ctx.Request.Method) || HttpMethods.IsPut(ctx.Request.Method))
        && ctx.Request.Path.StartsWithSegments("/api/products")
        && ctx.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
        limit.MaxRequestBodySize=600*1024;
    try{await next();}
    catch(BadHttpRequestException e){ctx.Response.StatusCode=e.StatusCode;await ctx.Response.WriteAsJsonAsync(new{error="Dữ liệu gửi lên không hợp lệ hoặc quá lớn."});}
    catch(ApiError e){ctx.Response.StatusCode=e.Status;await ctx.Response.WriteAsJsonAsync(new{error=e.Message});}
    catch(AntiforgeryValidationException){ctx.Response.StatusCode=400;await ctx.Response.WriteAsJsonAsync(new{error="Phiên thao tác hết hạn. Hãy tải lại trang."});}
    catch(SqlException e) when(e.Number is 2601 or 2627 or 1205){ctx.Response.StatusCode=409;await ctx.Response.WriteAsJsonAsync(new{error="Thao tác bị trùng hoặc dữ liệu vừa thay đổi. Hãy tải lại và thử lại."});}
    catch(Exception e){app.Logger.LogError(e,"Lỗi xử lý {Path}",ctx.Request.Path);ctx.Response.StatusCode=500;await ctx.Response.WriteAsJsonAsync(new{error="Không thể xử lý thao tác. Kiểm tra kết nối và nhật ký máy chủ."});}
});
app.UseDefaultFiles();app.UseStaticFiles();app.UseRateLimiter();app.UseAuthentication();app.UseAuthorization();
var api=app.MapGroup("/api").AddEndpointFilter(async(context,next)=>{
    if(HttpMethods.IsPost(context.HttpContext.Request.Method)||HttpMethods.IsPut(context.HttpContext.Request.Method)||HttpMethods.IsDelete(context.HttpContext.Request.Method))await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext);
    return await next(context);
});
static int Actor(HttpContext ctx)=>int.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
api.MapGet("/health",()=>new{status="ok",product="ChickenPOS",storage="SQL Server",version="demo-0.1",testInstance=app.Environment.IsEnvironment("Testing")});
api.MapGet("/csrf",(HttpContext c,IAntiforgery a)=>new{token=a.GetAndStoreTokens(c).RequestToken});
api.MapPost("/login",async(LoginInput input,HttpContext ctx,Store store,SessionRegistry sessions)=>{
    var u=await store.Login(input.Username,input.Password);if(u is null)return Results.Json(new{error="Tài khoản hoặc mật khẩu không đúng."},statusCode:401);
    var claims=new[]{new Claim(ClaimTypes.NameIdentifier,u["id"]!.ToString()!),new Claim(ClaimTypes.Name,(string)u["displayName"]!),new Claim(ClaimTypes.Role,(string)u["role"]!),new Claim("session",sessions.Create((int)u["id"]!))};
    await ctx.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)));
    return Results.Ok(new{id=u["id"],name=u["displayName"],role=u["role"]});
}).RequireRateLimiting("login");
api.MapPost("/logout",async(HttpContext c,SessionRegistry sessions)=>{sessions.Revoke(c.User.FindFirstValue("session"));await c.SignOutAsync();return Results.Ok();}).RequireAuthorization();
api.MapGet("/me",(HttpContext c)=>new{id=Actor(c),name=c.User.Identity!.Name,role=c.User.FindFirstValue(ClaimTypes.Role)}).RequireAuthorization();
api.MapGet("/products",(Store s)=>s.Products()).RequireAuthorization();
api.MapGet("/products/{id:int}/photo",async(int id,Store s)=>
    await s.ProductPhoto(id) is { } photo ? Results.File(photo,"image/jpeg") : Results.NotFound()).RequireAuthorization();
api.MapPost("/products",async(ProductInput input,Store s)=>new{id=await s.SaveProduct(null,input)}).RequireAuthorization("manager");
api.MapPut("/products/{id:int}",async(int id,ProductInput input,Store s)=>new{id=await s.SaveProduct(id,input)}).RequireAuthorization("manager");
api.MapDelete("/products/{id:int}",async(int id,Store s)=>{
    await s.DeleteProduct(id);
    return Results.NoContent();
}).RequireAuthorization("manager");
api.MapGet("/tables",(Store s)=>s.Tables()).RequireAuthorization();
api.MapGet("/orders",(Store s)=>s.Orders()).RequireAuthorization();
api.MapGet("/orders/{id:int}",(int id,Store s)=>s.Order(id)).RequireAuthorization();
api.MapPost("/orders",async(NewOrder input,HttpContext c,Store s)=>new{id=await s.Create(input,Actor(c))}).RequireAuthorization("sell");
api.MapPost("/orders/{id:int}/items",async(int id,AddItems input,HttpContext c,Store s)=>new{id=await s.Add(id,input,Actor(c))}).RequireAuthorization("sell");
api.MapPost("/orders/{id:int}/send",async(int id,RequestInput input,HttpContext c,Store s)=>new{sent=await s.Send(id,input.RequestKey,Actor(c))}).RequireAuthorization("sell");
api.MapGet("/kitchen",(Store s)=>s.Kitchen()).RequireAuthorization();
api.MapPost("/details/{id:int}/start",async(int id,HttpContext c,Store s)=>new{status=await s.Start(id,Actor(c))}).RequireAuthorization("kitchen");
api.MapPost("/details/{id:int}/ready",async(int id,HttpContext c,Store s)=>new{status=await s.Progress(id,Actor(c),false)}).RequireAuthorization("kitchen");
api.MapPost("/details/{id:int}/serve",async(int id,HttpContext c,Store s)=>new{status=await s.Progress(id,Actor(c),true)}).RequireAuthorization("sell");
api.MapPost("/orders/{id:int}/pay",async(int id,PayInput input,HttpContext c,Store s)=>await s.Pay(id,input,Actor(c))).RequireAuthorization("cash");
api.MapPost("/orders/{id:int}/cancel",async(int id,CancelInput input,HttpContext c,Store s)=>await s.Cancel(id,input.Reason,Actor(c),c.User.IsInRole("Manager"))).RequireAuthorization("cancel");
api.MapGet("/cancellations",(Store s)=>s.Cancellations()).RequireAuthorization("manager");
api.MapPost("/cancellations/{id:int}/review",async(int id,ReviewInput input,HttpContext c,Store s)=>new{status=await s.Review(id,input.Approve,Actor(c))}).RequireAuthorization("manager");
api.MapGet("/inventory",(Store s)=>s.Inventory()).RequireAuthorization("manager");
api.MapPost("/inventory/adjust",async(AdjustInput input,HttpContext c,Store s)=>new{stock=await s.Adjust(input,Actor(c))}).RequireAuthorization("manager");
api.MapGet("/dashboard",(DateTime? from,DateTime? to,Store s)=>s.Dashboard(from,to)).RequireAuthorization("manager");
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();
