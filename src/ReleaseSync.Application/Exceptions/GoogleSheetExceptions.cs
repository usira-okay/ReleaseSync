// <copyright file="GoogleSheetExceptions.cs" company="ReleaseSync">
// Copyright (c) ReleaseSync. All rights reserved.
// </copyright>

namespace ReleaseSync.Application.Exceptions;

/// <summary>
/// Google Sheet 同步相關例外的基底類別。
/// </summary>
public class GoogleSheetException : Exception
{
    /// <summary>
    /// 初始化 <see cref="GoogleSheetException"/> 類別的新執行個體。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    public GoogleSheetException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 初始化 <see cref="GoogleSheetException"/> 類別的新執行個體。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    /// <param name="innerException">內部例外。</param>
    public GoogleSheetException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Google Sheet 驗證失敗例外。
/// 當憑證驗證失敗時拋出。
/// </summary>
public class GoogleSheetAuthenticationException : GoogleSheetException
{
    /// <summary>
    /// 初始化 <see cref="GoogleSheetAuthenticationException"/> 類別的新執行個體。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    public GoogleSheetAuthenticationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 初始化 <see cref="GoogleSheetAuthenticationException"/> 類別的新執行個體。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    /// <param name="innerException">內部例外。</param>
    public GoogleSheetAuthenticationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Google Sheet 找不到例外。
/// 當指定的 Spreadsheet ID 或工作表不存在時拋出。
/// </summary>
public class GoogleSheetNotFoundException : GoogleSheetException
{
    /// <summary>
    /// 初始化 <see cref="GoogleSheetNotFoundException"/> 類別的新執行個體。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    public GoogleSheetNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 初始化 <see cref="GoogleSheetNotFoundException"/> 類別的新執行個體。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    /// <param name="innerException">內部例外。</param>
    public GoogleSheetNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Google Sheet 組態錯誤例外。
/// 當組態設定不完整或無效時拋出。
/// </summary>
public class GoogleSheetConfigurationException : GoogleSheetException
{
    /// <summary>
    /// 初始化 <see cref="GoogleSheetConfigurationException"/> 類別的新執行個體。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    public GoogleSheetConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 初始化 <see cref="GoogleSheetConfigurationException"/> 類別的新執行個體。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    /// <param name="innerException">內部例外。</param>
    public GoogleSheetConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Google Sheet API 錯誤例外。
/// 當 Google Sheets API 呼叫失敗時拋出。
/// </summary>
public class GoogleSheetApiException : GoogleSheetException
{
    /// <summary>
    /// 初始化 <see cref="GoogleSheetApiException"/> 類別的新執行個體。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    public GoogleSheetApiException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 初始化 <see cref="GoogleSheetApiException"/> 類別的新執行個體。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    /// <param name="innerException">內部例外。</param>
    public GoogleSheetApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Google Sheet 憑證檔案不存在例外。
/// 當指定的 Service Account 憑證檔案不存在時拋出。
/// </summary>
public class GoogleSheetCredentialNotFoundException : GoogleSheetException
{
    /// <summary>
    /// 憑證檔案路徑。
    /// </summary>
    public string CredentialPath { get; }

    /// <summary>
    /// 初始化 <see cref="GoogleSheetCredentialNotFoundException"/> 類別的新執行個體。
    /// </summary>
    /// <param name="credentialPath">憑證檔案路徑。</param>
    public GoogleSheetCredentialNotFoundException(string credentialPath)
        : base($"找不到 Service Account 憑證檔案。路徑: {credentialPath}。請確認憑證檔案路徑正確，並具有讀取權限。")
    {
        CredentialPath = credentialPath;
    }
}

/// <summary>
/// Google Sheet 權限不足例外。
/// 當 Service Account 沒有足夠權限存取 Sheet 時拋出。
/// </summary>
public class GoogleSheetPermissionDeniedException : GoogleSheetException
{
    /// <summary>
    /// 初始化 <see cref="GoogleSheetPermissionDeniedException"/> 類別的新執行個體。
    /// </summary>
    /// <param name="spreadsheetId">Google Sheet ID。</param>
    public GoogleSheetPermissionDeniedException(string spreadsheetId)
        : base($"沒有權限存取 Google Sheet。ID: {spreadsheetId}。請確認 Service Account 已被授予 Editor 權限。")
    {
    }

    /// <summary>
    /// 初始化 <see cref="GoogleSheetPermissionDeniedException"/> 類別的新執行個體。
    /// </summary>
    /// <param name="message">錯誤訊息。</param>
    /// <param name="innerException">內部例外。</param>
    public GoogleSheetPermissionDeniedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
