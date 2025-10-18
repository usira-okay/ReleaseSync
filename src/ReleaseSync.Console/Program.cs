using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReleaseSync.Console.Extensions;
using Serilog;

namespace ReleaseSync.Console;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // 設定 Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Log.Information("ReleaseSync Console Tool 啟動中...");

            // 建立 Host
            var host = CreateHostBuilder(args).Build();

            // 解析 ApplicationRunner 服務並執行
            var runner = host.Services.GetRequiredService<Services.IApplicationRunner>();
            var exitCode = await runner.RunAsync(args);

            Log.Information("ReleaseSync Console Tool 執行完畢,退出碼: {ExitCode}", exitCode);
            return exitCode;
        }
        catch (NotImplementedException ex)
        {
            Log.Warning("功能尚未實作: {Message}", ex.Message);
            return 0; // 本階段視為正常,返回 0
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "應用程式發生未預期的錯誤");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // 載入設定檔案
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile("secure.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // 註冊應用程式服務
                services.AddApplicationServices();
            })
            .UseSerilog(); // 使用 Serilog 作為日誌提供者
}
