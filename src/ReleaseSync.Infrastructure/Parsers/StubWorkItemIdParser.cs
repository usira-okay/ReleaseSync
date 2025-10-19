using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Services;

namespace ReleaseSync.Infrastructure.Parsers;

/// <summary>
/// Stub Work Item ID Parser (MVP)
/// </summary>
public class StubWorkItemIdParser : IWorkItemIdParser
{
    public WorkItemId ParseWorkItemId(BranchName branchName)
    {
        throw new InvalidOperationException("MVP: Work Item 解析功能尚未實作");
    }

    public bool TryParseWorkItemId(BranchName branchName, out WorkItemId workItemId)
    {
        workItemId = null!;
        return false;
    }
}
