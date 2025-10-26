using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReleaseSync.Application.Exporters;
using ReleaseSync.Application.Services;
using ReleaseSync.Console.Commands;
using ReleaseSync.Console.Handlers;
using ReleaseSync.Infrastructure.DependencyInjection;
using Serilog;

namespace ReleaseSync.Console;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // 檢查是否有 --verbose 參數以設定日誌等級
        var verbose = args.Contains("--verbose") || args.Contains("-v");

        // 建立設定
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<Program>(optional: true)
            .Build();

        // 設定 Serilog (根據 verbose 參數設定日誌等級)
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(verbose ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information)
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            // 建立 DI 容器
            var services = new ServiceCollection();

            // 註冊 Logging
            services.AddLogging(builder =>
            {
                builder.AddSerilog(dispose: true);
                builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
            });

            // 註冊 Configuration
            services.AddSingleton<IConfiguration>(configuration);

            // 註冊 Infrastructure 平台服務 (使用 Extension Methods)
            services.AddGitLabServices(configuration);
            services.AddBitBucketServices(configuration);
            services.AddAzureDevOpsServices(configuration);
            services.AddUserMappingServices(configuration);
            services.AddTeamMappingServices();

            // 註冊 Application 服務
            services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();
            services.AddScoped<IResultExporter, JsonFileExporter>();

            // 註冊 Command Handler
            services.AddScoped<SyncCommandHandler>();

            var serviceProvider = services.BuildServiceProvider();

            // 建立 RootCommand
            var rootCommand = new RootCommand("ReleaseSync - PR/MR 變更資訊聚合工具");

            // 加入 sync 命令
            var syncCommand = SyncCommand.Create();
            rootCommand.AddCommand(syncCommand);

            // 設定 handler
            syncCommand.SetHandler(async (DateTime startDate, DateTime endDate, bool enableGitLab,
                bool enableBitBucket, bool enableAzureDevOps, string? outputFile, bool force, bool verboseParam) =>
            {
                using var scope = serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<SyncCommandHandler>();
                await handler.HandleAsync(startDate, endDate, enableGitLab, enableBitBucket,
                    enableAzureDevOps, outputFile, force, verboseParam, CancellationToken.None);
            },
            syncCommand.Options.OfType<Option<DateTime>>().First(o => o.HasAlias("-s")),
            syncCommand.Options.OfType<Option<DateTime>>().First(o => o.HasAlias("-e")),
            syncCommand.Options.OfType<Option<bool>>().First(o => o.HasAlias("--gitlab")),
            syncCommand.Options.OfType<Option<bool>>().First(o => o.HasAlias("--bitbucket")),
            syncCommand.Options.OfType<Option<bool>>().First(o => o.HasAlias("--azdo")),
            syncCommand.Options.OfType<Option<string?>>().First(o => o.HasAlias("-o")),
            syncCommand.Options.OfType<Option<bool>>().First(o => o.HasAlias("-f")),
            syncCommand.Options.OfType<Option<bool>>().First(o => o.HasAlias("-v"))
            );

            // 執行命令
            return await rootCommand.InvokeAsync(args);
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
}
