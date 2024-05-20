using Blazored.LocalStorage;
using ComponentsLibrary.Services;
using KerrysFlowersWebApp.Services;
using SharedLibrary.Providers;
using SharedLibrary.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

var dbAdmin = Environment.GetEnvironmentVariable("DBADMIN");
if (string.IsNullOrEmpty(dbAdmin))
{
    throw new ArgumentNullException(nameof(dbAdmin), "Environment variable DBADMIN is not set.");
}

var dbPassword = Environment.GetEnvironmentVariable("DBPASSWORD");
if (string.IsNullOrEmpty(dbPassword))
{
    throw new ArgumentNullException(nameof(dbPassword), "Environment variable DBPASSWORD is not set.");
}
        
var dbHost = Environment.GetEnvironmentVariable("DBHOST");
if (string.IsNullOrEmpty(dbHost))
{
    throw new ArgumentNullException(nameof(dbHost), "Environment variable DBHOST is not set.");
}
        
var dbPort = Environment.GetEnvironmentVariable("DBPORT");
if (string.IsNullOrEmpty(dbPort))
{
    throw new ArgumentNullException(nameof(dbPort), "Environment variable DBPORT is not set.");
}
        
var dbDatabase = Environment.GetEnvironmentVariable("DBDATABASE");
if (string.IsNullOrEmpty(dbDatabase))
{
    throw new ArgumentNullException(nameof(dbDatabase), "Environment variable DBDATABASE is not set.");
}

var daDataApiKey = Environment.GetEnvironmentVariable("DADATAKEY");
if (string.IsNullOrEmpty(daDataApiKey))
{
    throw new ArgumentNullException(nameof(daDataApiKey), "Environment variable DADATAKEY is not set.");
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<ClipboardService>();
builder.Services.AddSingleton<IVioletRepository>(_ => new VioletRepository($"mongodb://{dbAdmin}:{dbPassword}@{dbHost}:{dbPort}", dbDatabase));
builder.Services.AddSingleton<IFileService, FileService>();
builder.Services.AddHostedService<PrerenderImagesBackgroundService>();
builder.WebHost.UseUrls(urls: ["http://*:5000", "https://*:5001"]);

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();