// <copyright file="GoogleSheetDataMapper.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

using System.Security.Cryptography.X509Certificates;
using ReleaseSync.Application.DTOs;
using ReleaseSync.Application.Models;
using ReleaseSync.Domain.Models;
using ReleaseSync.Domain.Services;

namespace ReleaseSync.Application.Mappers;

/// <summary>
/// Google Sheet 資料對應器實作。
/// 將 RepositoryBasedOutputDto 轉換為 SheetRowData 清單。
/// </summary>
public class GoogleSheetDataMapper : IGoogleSheetDataMapper
{
    private readonly IWorkItemIdParser _workItemIdParser;

    /// <summary>
    /// 初始化 GoogleSheetDataMapper 實例。
    /// </summary>
    /// <param name="workItemIdParser">Work Item ID 解析器，用於從 SourceBranch 或 PR Title 解析 VSTS ID。</param>
    public GoogleSheetDataMapper(IWorkItemIdParser workItemIdParser)
    {
        _workItemIdParser = workItemIdParser ?? throw new ArgumentNullException(nameof(workItemIdParser));
    }

    /// <inheritdoc/>
    public IReadOnlyList<SheetRowData> MapToSheetRows(RepositoryBasedOutputDto repositoryData)
    {
        ArgumentNullException.ThrowIfNull(repositoryData);

        var result = new List<SheetRowData>();

        foreach (var repository in repositoryData.Repositories)
        {
            // 分離 WorkItem != null 與 WorkItem == null 的 PR
            var prsWithWorkItem = repository.PullRequests.Where(pr => pr.WorkItem != null).ToList();
            var prsWithoutWorkItem = repository.PullRequests.Where(pr => pr.WorkItem == null).ToList();

            // 處理 WorkItem != null 的 PR
            var rowsWithWorkItem = MapPullRequestsWithWorkItem(prsWithWorkItem, repository.RepositoryName);
            result.AddRange(rowsWithWorkItem);

            // 處理 WorkItem == null 的 PR (從 SourceBranch 或 PR Title 解析 VSTS ID)
            var rowsWithoutWorkItem = MapPullRequestsWithoutWorkItem(prsWithoutWorkItem, repository.RepositoryName, repository.Platform);
            result.AddRange(rowsWithoutWorkItem);
        }

        return [.. result.OrderBy(x=>x.RepositoryName).ThenBy(x=>x.Team).ThenBy(x=>x.MergedAt)];
    }

    /// <summary>
    /// 處理有 WorkItem 的 PR，根據 WorkItemId 分組。
    /// </summary>
    /// <param name="pullRequests">有 WorkItem 的 PR 清單。</param>
    /// <param name="repositoryName">Repository 名稱。</param>
    /// <returns>轉換後的 SheetRowData 清單。</returns>
    private IEnumerable<SheetRowData> MapPullRequestsWithWorkItem(
        IEnumerable<RepositoryPullRequestDto> pullRequests,
        string repositoryName)
    {
        // 根據 WorkItemId 分組
        var groupedByWorkItem = pullRequests.GroupBy(pr => pr.WorkItem!.WorkItemId);

        foreach (var group in groupedByWorkItem)
        {
            var workItemId = group.Key;
            var firstPr = group.First();
            var workItem = firstPr.WorkItem!;

            // 收集所有 Authors (使用 HashSet 避免重複)
            var authors = new HashSet<string>(
                group.SelectMany(pr => pr.AuthorDisplayName != null
                    ? new[] { pr.AuthorDisplayName }
                    : Array.Empty<string>()),
                StringComparer.OrdinalIgnoreCase);

            // 收集所有 PR URLs (使用 HashSet 避免重複)
            var pullRequestUrls = new HashSet<string>(
                group.Where(pr => !string.IsNullOrWhiteSpace(pr.PullRequestUrl))
                     .Select(pr => pr.PullRequestUrl!),
                StringComparer.OrdinalIgnoreCase);

            // 建立 Feature 描述 (VSTS{ID} - {Title})
            var feature = $"VSTS{workItemId} - {workItem.WorkItemTitle}";

            // 產生 Unique Key
            var uniqueKey = GenerateUniqueKey(workItemId, repositoryName);

            var rowData = new SheetRowData
            {
                RowNumber = 0, // 稍後由同步邏輯決定
                UniqueKey = uniqueKey,
                RepositoryName = repositoryName,
                Feature = feature,
                FeatureUrl = workItem.WorkItemUrl,
                Team = workItem.WorkItemTeam ?? string.Empty,
                Authors = authors,
                PullRequestUrls = pullRequestUrls,
                MergedAt = firstPr.MergedAt
            };

            yield return rowData;
        }
    }

    /// <summary>
    /// 處理沒有 WorkItem 的 PR，從 SourceBranch 或 PR Title 解析 VSTS ID。
    /// </summary>
    /// <param name="pullRequests">沒有 WorkItem 的 PR 清單。</param>
    /// <param name="repositoryName">Repository 名稱。</param>
    /// <param name="platform">板控平台。</param>
    /// <returns>轉換後的 SheetRowData 清單。</returns>
    private IEnumerable<SheetRowData> MapPullRequestsWithoutWorkItem(
        IEnumerable<RepositoryPullRequestDto> pullRequests,
        string repositoryName,
        string platform)
    {
        // 為每個 PR 解析 WorkItemId，然後分組
        var prsWithParsedId = pullRequests
            .Select(pr => new
            {
                PullRequest = pr,
                ParsedWorkItemId = ParseWorkItemIdFromPr(pr)
            })
            .ToList();

        // 根據解析出的 WorkItemId 分組
        // var groupedByParsedId = prsWithParsedId.GroupBy(x => x.ParsedWorkItemId);

        foreach (var item in prsWithParsedId)
        {
            var workItemId = item.ParsedWorkItemId;

            // 收集 Authors
            var authors = new HashSet<string>([item.PullRequest.AuthorDisplayName ?? string.Empty], StringComparer.OrdinalIgnoreCase);

            // 收集 PR URLs
            var pullRequestUrls = new HashSet<string>([item.PullRequest.PullRequestUrl ?? string.Empty], StringComparer.OrdinalIgnoreCase);

            // 產生 Unique Key
            var uniqueKey = GenerateUniqueKey(workItemId, repositoryName, platform);

            var rowData = new SheetRowData
            {
                RowNumber = 0, // 稍後由同步邏輯決定
                UniqueKey = uniqueKey,
                RepositoryName = repositoryName,
                Feature = item.PullRequest.SourceBranch,
                FeatureUrl = string.Empty, // 沒有 WorkItem 資訊，無 URL
                Team = string.Empty, // 沒有 WorkItem 資訊，無 Team
                Authors = authors,
                PullRequestUrls = pullRequestUrls,
                MergedAt = item.PullRequest.MergedAt
            };

            yield return rowData;
        }
    }

    /// <summary>
    /// 從 PR 的 SourceBranch 或 Title 解析 Work Item ID。
    /// 優先從 SourceBranch 解析，失敗則嘗試從 PR Title 解析。
    /// 若都失敗則回傳 0。
    /// </summary>
    /// <param name="pr">Pull Request DTO。</param>
    /// <returns>解析出的 Work Item ID，若無法解析則為 0。</returns>
    private int ParseWorkItemIdFromPr(RepositoryPullRequestDto pr)
    {
        // 優先從 SourceBranch 解析
        if (!string.IsNullOrWhiteSpace(pr.SourceBranch))
        {
            var branchName = new BranchName(pr.SourceBranch);
            if (_workItemIdParser.TryParseWorkItemId(branchName, out var workItemId))
            {
                return workItemId.Value;
            }
        }

        // SourceBranch 解析失敗，嘗試從 PR Title 解析
        if (!string.IsNullOrWhiteSpace(pr.PullRequestTitle))
        {
            // 將 PR Title 當作 BranchName 傳入解析器（重用相同的 pattern）
            var titleAsBranch = new BranchName(pr.PullRequestTitle);
            if (_workItemIdParser.TryParseWorkItemId(titleAsBranch, out var workItemId))
            {
                return workItemId.Value;
            }
        }

        // 都無法解析，回傳 0
        return 0;
    }

    /// <inheritdoc/>
    public string GenerateUniqueKey(int workItemId, string repositoryName, string platform = "")
    {
        if (string.IsNullOrWhiteSpace(repositoryName))
        {
            throw new ArgumentException("Repository 名稱不可為空", nameof(repositoryName));
        }

        // UK 格式: {WorkItemId}{RepositoryName}
        return $"{workItemId}{repositoryName}{platform}";
    }
}
