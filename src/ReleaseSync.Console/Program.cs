using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReleaseSync.Application.Exporters;
using ReleaseSync.Application.Importers;
using ReleaseSync.Application.Services;
using ReleaseSync.Console.Commands;
using ReleaseSync.Console.Handlers;
using ReleaseSync.Console.Services;
using ReleaseSync.Infrastructure.Configuration;
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

        // 從組態讀取 SyncOptions
        var syncOptionsSettings = new SyncOptionsSettings();
        configuration.GetSection("SyncOptions").Bind(syncOptionsSettings);

        // 讀取 Seq 設定
        var seqServerUrl = configuration["Seq:ServerUrl"] ?? Environment.GetEnvironmentVariable("SEQ_SERVER_URL");
        var seqApiKey = configuration["Seq:ApiKey"];

        // 設定 Serilog (根據組態中的 Verbose 設定日誌等級)
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(syncOptionsSettings.Verbose ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information)
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
                builder.SetMinimumLevel(syncOptionsSettings.Verbose ? LogLevel.Debug : LogLevel.Information);
            });

            // 註冊 Configuration
            services.AddSingleton<IConfiguration>(configuration);

            // 註冊 SyncOptions 配置
            services.AddSyncOptions(configuration);

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

            // 加入 sync 命令 (無參數,所有設定從 appsettings.json 讀取)
            var syncCommand = SyncCommand.Create();
            rootCommand.AddCommand(syncCommand);

            // 設定 handler - 從組態檔讀取所有參數
            syncCommand.SetHandler(async () =>
            {
                var options = new SyncCommandOptions
                {
                    StartDate = syncOptionsSettings.StartDate,
                    EndDate = syncOptionsSettings.EndDate,
                    EnableGitLab = syncOptionsSettings.EnableGitLab,
                    EnableBitBucket = syncOptionsSettings.EnableBitBucket,
                    EnableAzureDevOps = syncOptionsSettings.EnableAzureDevOps,
                    EnableExport = syncOptionsSettings.EnableExport,
                    OutputFile = syncOptionsSettings.OutputFile,
                    Force = syncOptionsSettings.Force,
                    Verbose = syncOptionsSettings.Verbose,
                    EnableGoogleSheet = syncOptionsSettings.EnableGoogleSheet,
                    GoogleSheetId = syncOptionsSettings.GoogleSheetId,
                    GoogleSheetName = syncOptionsSettings.GoogleSheetName
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
