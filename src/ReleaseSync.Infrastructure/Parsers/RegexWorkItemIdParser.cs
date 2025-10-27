using System.Text.RegularExpressions;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Services;
using ReleaseSync.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace ReleaseSync.Infrastructure.Parsers;

/// <summary>
/// 使用 Regex 從 Branch 名稱解析 Work Item ID
/// </summary>
public class RegexWorkItemIdParser : IWorkItemIdParser
{
    private readonly IEnumerable<WorkItemIdPattern> _patterns;
    private readonly ParsingBehaviorSettings _behavior;
    private readonly ILogger<RegexWorkItemIdParser> _logger;

    public RegexWorkItemIdParser(
        AzureDevOpsSettings settings,
        ILogger<RegexWorkItemIdParser> logger)
    {
        _patterns = settings.WorkItemIdPatterns ?? new List<WorkItemIdPattern>();
        _behavior = settings.ParsingBehavior ?? new ParsingBehaviorSettings();
        _logger = logger;
    }

    public WorkItemId ParseWorkItemId(BranchName branchName)
    {
        if (TryParseWorkItemId(branchName, out var workItemId))
        {
            return workItemId;
        }

        throw new FormatException($"無法從 Branch 名稱解析 Work Item ID: {branchName.Value}");
    }

    public bool TryParseWorkItemId(BranchName branchName, out WorkItemId workItemId)
    {
        workItemId = null!;

        if (_patterns == null || !_patterns.Any())
        {
            _logger.LogWarning("無 Work Item ID 解析模式設定");
            return false;
        }

        foreach (var pattern in _patterns)
        {
            if (TryParseWithPattern(branchName, pattern, out workItemId))
            {
                return true;
            }
        }

        // 無法解析
        HandleParseFailure(branchName);
        return false;
    }

    private bool TryParseWithPattern(BranchName branchName, WorkItemIdPattern pattern, out WorkItemId workItemId)
    {
        workItemId = null!;

        try
        {
            var regex = new Regex(
                pattern.Regex,
                pattern.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);

            var match = regex.Match(branchName.Value);

            if (!match.Success || match.Groups.Count <= pattern.CaptureGroup)
            {
                return false;
            }

            var value = match.Groups[pattern.CaptureGroup].Value;

            if (!int.TryParse(value, out var id) || id <= 0)
            {
                return false;
            }

            workItemId = new WorkItemId(id);

            _logger.LogInformation(
                "成功解析 Work Item ID: {WorkItemId} from Branch: {BranchName} using pattern: {PatternName}",
                id, branchName.Value, pattern.Name);

            return true;
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogWarning(ex,
                "Regex 匹配超時: Pattern={PatternName}, Branch={BranchName}",
                pattern.Name, branchName.Value);
            return false;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex,
                "無效的 Regex 模式: Pattern={PatternName}, Regex={Regex}",
                pattern.Name, pattern.Regex);
            return false;
        }
    }

    private void HandleParseFailure(BranchName branchName)
    {
        if (_behavior.OnParseFailure == "LogWarningAndContinue")
        {
            _logger.LogWarning(
                "無法從 Branch 名稱解析 Work Item ID: {BranchName}",
                branchName.Value);
        }
        else if (_behavior.OnParseFailure == "ThrowException")
        {
            throw new FormatException($"無法從 Branch 名稱解析 Work Item ID: {branchName.Value}");
        }
    }
}
