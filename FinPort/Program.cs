using FinPort.Data;
using FinPort.Middleware;
using FinPort.Services;
using Microsoft.EntityFrameworkCore;

namespace FinPort;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        string file = "./app_data.db";
        if (Environment.GetEnvironmentVariable("SUPERVISOR_TOKEN") != null)
        {
            builder.Configuration.AddJsonFile("/config/appsettings.json", optional: true, reloadOnChange: true);
            file = "/config/app_data.db";
        }

        builder.Services.AddDbContext<DataBaseContext>(options =>
            options.UseSqlite($"Data Source={file}"));

        builder.Services.AddControllersWithViews()
                        .AddRazorRuntimeCompilation();
        builder.Services.AddSingleton<HomeAssistantApiClient>();
        builder.Services.AddSingleton<WebSocketHandler>();
        builder.Services.AddSingleton<WebSocketMiddleware>();
        builder.Services.AddSingleton<JustEtfWebSocketClient>();
        builder.Services.AddHostedService<JustEtfWebSocketClient>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        using (var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>())
        {
            db.Database.Migrate();
        }

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.UseWebSockets();
        app.UseMiddleware<WebSocketMiddleware>();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}
