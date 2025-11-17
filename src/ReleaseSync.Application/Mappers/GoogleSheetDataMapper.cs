// <copyright file="GoogleSheetDataMapper.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

using ReleaseSync.Application.DTOs;
using ReleaseSync.Application.Models;

namespace ReleaseSync.Application.Mappers;

/// <summary>
/// Google Sheet 資料對應器實作。
/// 將 RepositoryBasedOutputDto 轉換為 SheetRowData 清單。
/// </summary>
public class GoogleSheetDataMapper : IGoogleSheetDataMapper
{
    /// <inheritdoc/>
    public IReadOnlyList<SheetRowData> MapToSheetRows(RepositoryBasedOutputDto repositoryData)
    {
        ArgumentNullException.ThrowIfNull(repositoryData);

        var result = new List<SheetRowData>();

        foreach (var repository in repositoryData.Repositories)
        {
            // 根據 (WorkItemId, RepositoryName) 分組 PR/MR
            var groupedByWorkItem = repository.PullRequests
                .Where(pr => pr.WorkItem != null)
                .GroupBy(pr => pr.WorkItem!.WorkItemId);

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
                var uniqueKey = GenerateUniqueKey(workItemId, repository.RepositoryName);

                var rowData = new SheetRowData
                {
                    RowNumber = 0, // 稍後由同步邏輯決定
                    UniqueKey = uniqueKey,
                    RepositoryName = repository.RepositoryName,
                    Feature = feature,
                    FeatureUrl = workItem.WorkItemUrl,
                    Team = workItem.WorkItemTeam ?? string.Empty,
                    Authors = authors,
                    PullRequestUrls = pullRequestUrls,
                    MergedAt = firstPr.MergedAt
                };

                result.Add(rowData);
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public string GenerateUniqueKey(int workItemId, string repositoryName)
    {
        if (string.IsNullOrWhiteSpace(repositoryName))
        {
            throw new ArgumentException("Repository 名稱不可為空", nameof(repositoryName));
        }

        // UK 格式: {WorkItemId}{RepositoryName}
        return $"{workItemId}{repositoryName}";
    }
}
