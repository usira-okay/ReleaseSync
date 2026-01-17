using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReleaseSync.Application.Exporters;
using ReleaseSync.Application.Importers;
using ReleaseSync.Application.Services;
using ReleaseSync.Console.Commands;
using ReleaseSync.Console.Configuration;
using ReleaseSync.Console.Handlers;
using ReleaseSync.Console.Services;
using ReleaseSync.Infrastructure.DependencyInjection;
using Serilog;

namespace ReleaseSync.Console;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // 建立設定
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<Program>(optional: true)
            .Build();

        // 從組態載入 SyncOptions
        var syncOptions = configuration.GetSection("SyncOptions").Get<SyncOptions>();
        if (syncOptions == null)
        {
            System.Console.WriteLine("錯誤: 無法從 appsettings.json 讀取 SyncOptions 設定");
            return 1;
        }

        // 從 SyncOptions 取得 verbose 設定
        var verbose = syncOptions.Verbose;

        // 讀取 Seq 設定
        var seqServerUrl = configuration["Seq:ServerUrl"] ?? Environment.GetEnvironmentVariable("SEQ_SERVER_URL");
        var seqApiKey = configuration["Seq:ApiKey"];

        // 設定 Serilog (根據 verbose 參數設定日誌等級)
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(verbose ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console();

        // 如果設定了 Seq Server URL，則啟用 Seq Sink
        if (!string.IsNullOrWhiteSpace(seqServerUrl))
        {
            if (string.IsNullOrWhiteSpace(seqApiKey))
            {
                loggerConfig.WriteTo.Seq(seqServerUrl);
            }
            else
            {
                loggerConfig.WriteTo.Seq(seqServerUrl, apiKey: seqApiKey);
            }
        }

        Log.Logger = loggerConfig.CreateLogger();

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
            services.AddGoogleSheetServices(configuration);

            // 註冊 Application 服務
            services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();
            services.AddScoped<IResultExporter, JsonFileExporter>();
            services.AddScoped<IResultImporter, JsonFileImporter>();

            // 註冊 Console 層服務
            services.AddScoped<IWorkItemEnricher, WorkItemEnricher>();

            // 註冊 Command Handler
            services.AddScoped<SyncCommandHandler>();

            var serviceProvider = services.BuildServiceProvider();

            // 建立 RootCommand
            var rootCommand = new RootCommand("ReleaseSync - PR/MR 變更資訊聚合工具");

            // 加入 sync 命令
            var syncCommand = SyncCommand.Create();
            rootCommand.AddCommand(syncCommand);

            // 設定 handler - 從組態讀取參數
            syncCommand.SetHandler(async () =>
            {
                var options = new SyncCommandOptions
                {
                    StartDate = syncOptions.StartDate,
                    EndDate = syncOptions.EndDate,
                    EnableGitLab = syncOptions.EnableGitLab,
                    EnableBitBucket = syncOptions.EnableBitBucket,
                    EnableAzureDevOps = syncOptions.EnableAzureDevOps,
                    EnableExport = syncOptions.EnableExport,
                    OutputFile = syncOptions.OutputFile,
                    Force = syncOptions.Force,
                    Verbose = syncOptions.Verbose,
                    EnableGoogleSheet = syncOptions.EnableGoogleSheet,
                    GoogleSheetId = syncOptions.GoogleSheetId,
                    GoogleSheetName = syncOptions.GoogleSheetName
                };

                using var scope = serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<SyncCommandHandler>();
                await handler.HandleAsync(options, CancellationToken.None);
            });

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
