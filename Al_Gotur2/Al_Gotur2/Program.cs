using Al_Gotur2.Models.Context;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// DbContext'i servislere ekleyin
builder.Services.AddDbContext<Al_Gotur2Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session servisini ekleyin
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS için
});

// Authentication servisini ekleyin
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "UserLoginCookie";
        options.LoginPath = "/Kullanici/GirisYap";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS için
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

// Anti-forgery token yapýlandýrmasý
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.HeaderName = "X-CSRF-TOKEN";
});

// CORS politikasý (birden fazla domain için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("PaymentPolicy",
        builder =>
        {
            builder.WithOrigins("https://your-payment-provider.com", "https://another-allowed-domain.com")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // HSTS baþlýðý eklenir
}
else
{
    app.UseDeveloperExceptionPage();
}

// Güvenlik baþlýklarý
app.Use(async (context, next) =>
{
    // Mevcut güvenlik baþlýklarý
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");

    // Ödeme sayfalarý için ek güvenlik baþlýklarý
    if (context.Request.Path.StartsWithSegments("/Odeme"))
    {
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
            "img-src 'self' data: https:; " +
            "form-action 'self'; " +
            "frame-ancestors 'none';");
    }

    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// CORS middleware
app.UseCors("PaymentPolicy");

// Session ve Authentication middleware'lerini ekleyin
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Rate limiting için basit bir middleware
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/Odeme"))
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString();

        // Örnek bir rate limiting kontrolü
        if (clientIp != null)
        {
            // Rate limiting mantýðý (örneðin, bir Redis veya bellek tabanlý sayaç kullanabilirsiniz)
            // Örnek: Eðer IP belirli bir limiti aþarsa, 429 döndür
            // context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            // return;
        }
    }
    await next.Invoke();
});

// Controller rotalarýný yapýlandýrýn
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();