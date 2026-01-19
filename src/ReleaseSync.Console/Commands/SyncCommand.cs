using System.CommandLine;

namespace ReleaseSync.Console.Commands;

/// <summary>
/// 同步 PR/MR 資訊的命令定義
/// </summary>
public static class SyncCommand
{
    /// <summary>
    /// 建立 sync 命令
    /// </summary>
    /// <returns>Sync 命令物件</returns>
    public static Command Create()
    {
        var command = new Command("sync", "從版控平台同步 PR/MR 資訊 (所有參數從 appsettings.json 讀取)");
        return command;
    }
}
