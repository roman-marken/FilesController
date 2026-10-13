using FileStorageApi.Data;
using FileStorageApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<FileDbContext>(options =>
    options.UseSqlite("Data Source=App_Data/files.db"));

builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

var appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");

Directory.CreateDirectory(appDataPath);
Directory.CreateDirectory(webRootPath);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FileDbContext>();
    db.Database.EnsureCreated();
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(webRootPath)
});
app.MapControllers();

app.Run();
