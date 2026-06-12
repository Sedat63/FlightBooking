using System.Reflection;
using FlightBooking.Services.BookingServices;
using FlightBooking.Services.CheckInServices;
using FlightBooking.Services.FlightServices;
using FlightBooking.Services.MachineLearningServices;
using FlightBooking.Services.NoShowServices;
using FlightBooking.Services.OverBookingNoShowServices;
using FlightBooking.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. AYARLAR VE VERÝTABANI YAPILANDIRMALARI (builder.Build öncesi)
// =========================================================================

// appsettings.json dosyasýndaki DatabaseSettings Key'ini sýnýfýmýza baðlýyoruz
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("DatabaseSettingsKey"));
builder.Services.AddScoped<IDatabaseSettings>(sp =>
{
    return sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
});

// Ayarlarý kod içinde de kullanabilmek için deðiþkene atýyoruz
var databaseSettings = builder.Configuration.GetSection("DatabaseSettingsKey").Get<DatabaseSettings>();

// IMongoClient Tanýmý
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    string connString = databaseSettings?.ConnectionString ?? "mongodb://localhost:27017";
    return new MongoClient(connString);
});

// IMongoDatabase Tanýmý (Koleksiyonlarýn türetilmesi için gerekli)
builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    string dbName = databaseSettings?.DatabaseName ?? "FlightBookingDb";
    return client.GetDatabase(dbName);
});

// FlightService ve BookingService'in constructor'da beklediði MongoDB Koleksiyon Tanýmlarý
// Bookings
builder.Services.AddScoped<IMongoCollection<FlightBooking.Entities.Booking>>(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    string collectionName = databaseSettings?.BookingCollectionName ?? "Bookings";
    return database.GetCollection<FlightBooking.Entities.Booking>(collectionName);
});

// Flights
builder.Services.AddScoped<IMongoCollection<FlightBooking.Entities.Flight>>(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    string collectionName = databaseSettings?.FlightCollectionName ?? "Flights";
    return database.GetCollection<FlightBooking.Entities.Flight>(collectionName);
});

// CheckIns
builder.Services.AddScoped<IMongoCollection<FlightBooking.Entities.CheckIn>>(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    // Burayý CheckInCollectionName ve varsayýlaný da "CheckIns" yaptýk
    string collectionName = databaseSettings?.CheckInCollectionName ?? "CheckIns";
    return database.GetCollection<FlightBooking.Entities.CheckIn>(collectionName);
});

// FlightDemandHistories (MongoDB'deki ham veriyi okumak için)
//builder.Services.AddScoped<IMongoCollection<FlightBooking.MachineLearningModels.FlightRawData>>(sp =>
//{
//    var database = sp.GetRequiredService<IMongoDatabase>();
//    // appsettings.json'da tanýmladýysan collection adýný çek, yoksa varsayýlan olarak "FlightDemandHistories" kullan
//    string collectionName = databaseSettings?.FlightDemandHistoryCollection ?? "FlightDemandHistories";
//    return database.GetCollection<FlightBooking.MachineLearningModels.FlightRawData>(collectionName);
//});

// =========================================================================
// 2. UYGULAMA SERVÝS KAYITLARI (Dependency Injection)
// =========================================================================

builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<ICheckInService, CheckInService>();

builder.Services.AddSingleton<FlightMlService>();
builder.Services.AddScoped<MongoFlightDataService>();

builder.Services.AddSingleton<FlightRegressionService>();

builder.Services.AddScoped<NoShowService>();

builder.Services.AddScoped<OverbookingRecommendationService>();

builder.Services.AddScoped<NoShowPredictionService>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(Assembly.GetExecutingAssembly());
});

// MVC Controller ve View yapýlarýný ekliyoruz
builder.Services.AddControllersWithViews();


// =========================================================================
// 3. UYGULAMANIN ÝNÞA EDÝLMESÝ (BUILD)
// TÜM SERVÝS KAYITLARI BU SATIRIN ÜSTÜNDE KALMALIDIR!
// =========================================================================
var app = builder.Build();


// =========================================================================
// 4. HTTP REQUEST PIPELINE (Middleware ve Rotalar)
// =========================================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Varsayýlan (Default) Route Tanýmý
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Admin Alaný (Areas) için Geliþmiþ Route Tanýmý
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
      name: "areas",
      pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
    );
});

// Uygulamayý Baþlat
app.Run();