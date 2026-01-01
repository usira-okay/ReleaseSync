using System.CommandLine;

namespace ReleaseSync.Console.Commands;

/// <summary>
/// 同步 PR/MR 資訊的命令定義
/// </summary>
/// <remarks>
/// 所有執行參數都從 appsettings.json 的 SyncOptions 區塊讀取,不再使用命令列參數
/// </remarks>
public static class SyncCommand
{
    /// <summary>
    /// 建立 sync 命令
    /// </summary>
    /// <returns>Sync 命令物件</returns>
    public static Command Create()
    {
        var command = new Command("sync", "從版控平台同步 PR/MR 資訊 (所有參數從 appsettings.json 讀取)");

        // 所有參數已移至 appsettings.json 的 SyncOptions 區塊
        // 不再接受命令列參數

        return command;
    }
}
