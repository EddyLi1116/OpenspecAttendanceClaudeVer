# email-delivery Specification

## Purpose
TBD - created by archiving change bootstrap-foundation. Update Purpose after archive.
## Requirements
### Requirement: IEmailSender 抽象介面

系統 SHALL 在 `AttendanceSystem.Domain` 專案定義 `IEmailSender` 介面，簽章為 `Task SendAsync(EmailMessage message, CancellationToken cancellationToken)`。`EmailMessage` MUST 至少包含 `To`（收件者 email）、`Subject`（繁中主旨）、`HtmlBody`（HTML 內容）、`TextBody`（純文字備援內容）。所有需要寄送 email 的業務邏輯 MUST 透過此介面進行，不得直接呼叫 SMTP 或檔案 API。

#### Scenario: 業務服務透過 IEmailSender 寄信

- **WHEN** `UserService` 建立新使用者並需要寄送歡迎信
- **THEN** `UserService` MUST 注入 `IEmailSender` 並呼叫 `SendAsync`
- **AND** `UserService` MUST 不含任何 SMTP 或 `System.Net.Mail` 直接相依

### Requirement: 開發環境 FileLog 實作

系統 SHALL 提供 `FileLogEmailSender`，將 `EmailMessage` 寫成標準 `.eml` 格式檔案至 `App_Data/outbox/{yyyyMMdd-HHmmss-fff}-{to}.eml`。檔案 MUST 包含 `From`、`To`、`Subject`、`Date`、`Content-Type: multipart/alternative` 以及純文字與 HTML 兩個 part，使開發者可直接以 Outlook、Thunderbird 或 VSCode `.eml` 預覽工具檢視。

#### Scenario: FileLogEmailSender 成功寫檔

- **WHEN** `FileLogEmailSender.SendAsync` 被呼叫
- **THEN** `App_Data/outbox/` 下出現對應 `.eml` 檔，內容為合法 MIME multipart/alternative
- **AND** 方法回傳 `Task.CompletedTask`、不擲例外

#### Scenario: outbox 目錄不存在時自動建立

- **WHEN** `App_Data/outbox/` 不存在時呼叫 `SendAsync`
- **THEN** 實作 MUST 自動建立目錄再寫檔，不擲例外

### Requirement: 生產環境安全閘門

系統 MUST 在應用程式啟動時檢查：若 `ASPNETCORE_ENVIRONMENT = Production` 且 `Email:Mode = FileLog`，啟動 MUST 擲 `InvalidOperationException` 並終止，錯誤訊息為「FileLogEmailSender 禁止在 Production 使用」。此機制防止明文初始密碼在正式環境被寫入檔案系統。

#### Scenario: 正式環境誤用 FileLog

- **WHEN** 以 `ASPNETCORE_ENVIRONMENT=Production` 與 `Email:Mode=FileLog` 啟動應用程式
- **THEN** 應用程式啟動 MUST 失敗並擲 `InvalidOperationException`，訊息明確指出禁止原因

#### Scenario: 開發環境使用 FileLog

- **WHEN** 以 `ASPNETCORE_ENVIRONMENT=Development` 與 `Email:Mode=FileLog` 啟動
- **THEN** 應用程式正常啟動、`IEmailSender` 解析為 `FileLogEmailSender`

### Requirement: 初始密碼產生器

系統 SHALL 提供 `IInitialPasswordGenerator.Generate()` 方法，以 `System.Security.Cryptography.RandomNumberGenerator` 產生 16 字元密碼，MUST 至少包含 1 個大寫字母、1 個小寫字母、1 個數字、1 個符號（`!@#$%^&*?-_`）。回傳字串僅在記憶體與寄出的 `EmailMessage` 中存在，MUST 不得寫入日誌、資料庫、或任何持久儲存。

#### Scenario: 產生的密碼符合組成規則

- **WHEN** 呼叫 `IInitialPasswordGenerator.Generate()` 1000 次
- **THEN** 每次結果 MUST 為 16 字元、MUST 同時包含大寫、小寫、數字、符號各至少一個
- **AND** 結果 MUST 不出現在任何日誌輸出

### Requirement: 歡迎信模板

系統 SHALL 提供「歡迎信」模板，主旨「歡迎加入出缺勤系統」。HTML 與純文字兩種版本 MUST 包含：收件者顯示名稱、登入網址、email 帳號、一次性初始密碼、提示「首次登入後系統會要求您變更密碼」。模板 MUST 不含任何第三方追蹤像素或外連資源。

#### Scenario: 產生的歡迎信含所有必要欄位

- **WHEN** 系統在建立使用者後產生歡迎信
- **THEN** 產生的 `EmailMessage.HtmlBody` 與 `TextBody` MUST 包含 `displayName`、登入網址、email、初始密碼、首次強制改密提示

### Requirement: 密碼重設信模板

系統 SHALL 提供「密碼重設」模板，主旨「出缺勤系統密碼重設連結」。HTML 與純文字兩種版本 MUST 包含：收件者顯示名稱、重設連結（30 分鐘內有效）、過期時間（以 `Asia/Taipei` 顯示）、提示「如非本人申請請忽略此信」。

#### Scenario: 產生的重設信含 30 分鐘過期提示

- **WHEN** `POST /api/auth/forgot-password` 成功後系統產生重設信
- **THEN** 信件內容 MUST 含有以 `Asia/Taipei` 時區顯示、離產生時間 30 分鐘的過期時間字串

