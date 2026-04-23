## Context

此 change 是出缺勤管理系統的第一個階段，專案狀態為 greenfield（除 OpenSpec 設定外無任何程式碼）。系統共規劃四個階段：本階段先建立身分、組織與通知的基礎骨架；第 2 階段導入打卡（`add-time-clock`）；第 3 階段導入簽核引擎、請假、代理人與補卡單（`add-approval-and-leave`）；第 4 階段導入加班（`add-overtime`）。本階段的每個設計決策都需要考慮後續三個階段將如何擴充，避免在骨架上做出會鎖死未來的選擇。

技術棧已鎖定：前端 Vite + React + TypeScript + TanStack Query + Tailwind；後端 ASP.NET Core Web API + EF Core + `Pomelo.EntityFrameworkCore.MySql`；資料庫 MySQL 本機（`Server=127.0.0.1;Port=3306;Database=Attendance;User=root;Password=abc165162;`）。使用者僅有 `admin` 與 `employee` 兩種角色，單層部門結構，每位使用者指向一位直屬主管。

## Goals / Non-Goals

**Goals:**
- 在 greenfield 上建立可建置、可執行、可部署的前後端專案骨架。
- 提供安全的登入與 Token 機制，並讓管理員可建立其他使用者。
- 提供組織結構（部門 + 直屬主管）所需的資料表與 API，供第 3 階段簽核引擎解析「直屬主管」時直接使用。
- 將 Email 寄送抽象化，讓開發環境不必真的接 SMTP 也能完整走完「新增使用者 → 發送初始密碼 → 首次登入改密」流程。
- 對未來的 approval flow 版本化（snapshot 策略）預留資料模型與術語，但不在本階段實作。

**Non-Goals:**
- 不實作打卡、請假、加班、簽核、代理人、補卡單（分屬後續三個 change）。
- 不提供單一登入（SSO）、AD/LDAP 整合、多工廠或多公司切換。
- 不提供行動裝置原生 App；只提供響應式網頁。
- 不接真實 SMTP；`SmtpEmailSender` 留待未來 change 或部署前的組態調整。
- 不建立 CI/CD pipeline，只確保 `dotnet build`、`dotnet test`、`npm run build` 能通過。

## Decisions

### JWT + httpOnly Cookie 混合策略（而非純 cookie session 或純 localStorage JWT）

**決策**：Access token 以 JWT 發行、壽命 15 分鐘、前端存在 React Context（記憶體），透過 `Authorization: Bearer` 傳遞；Refresh token 壽命 7 天，存在 httpOnly + Secure + SameSite=Strict cookie，並於 `refresh_tokens` 資料表保存其 SHA-256 雜湊值與 `revoked_at`，每次 refresh 後輪替新 token。

**理由**：SPA + 獨立後端 API 的典型場景。純 localStorage 易受 XSS；純 cookie session 在跨網域 SPA 會碰上 SameSite 與 CSRF 雙重挑戰。此混合策略把「長壽命 token」放在 JS 無法讀取的 httpOnly cookie（XSS 安全），「短壽命 token」放記憶體（重新整理即失效，可容忍）。登出、管理員停用使用者、變更密碼都會撤銷對應的 refresh token。

**其他選項**：
- 純 ASP.NET Core Cookie Authentication：雖簡單，但後續若要擴充行動 App 就需要改架構。
- 純 Bearer JWT 存 localStorage：實作最簡單但 XSS 風險高。

### 密碼雜湊使用 `PasswordHasher<T>`，不採用 Full ASP.NET Core Identity

**決策**：只使用 `Microsoft.AspNetCore.Identity` 的 `PasswordHasher<User>`（PBKDF2 + HMAC-SHA256 + 100k 迭代、內建 format version 支援無痛升級），不使用 Identity 的 `UserManager`、`SignInManager`、Identity UI、Identity EF Core stores。自訂 `users` 資料表與驗證流程。

**理由**：Full Identity 帶來大量不需要的 schema（`AspNetUsers`、`AspNetRoles`、`AspNetUserClaims` 等）與約定；本系統的 `users` 欄位更貼近 HR 領域（`hire_date`、`department_id`、`manager_id`、`must_change_password`）。`PasswordHasher<T>` 是可以獨立使用的小元件，保留雜湊演算法的安全性與可升級性。

### 初始密碼與首次強制改密

**決策**：管理員新增使用者時，後端以 `RandomNumberGenerator` 產生 16 字元強密碼（含大小寫+數字+符號）並立即雜湊存庫；原始密碼僅透過 `IEmailSender` 寄出，永不寫入日誌或資料庫。`users.must_change_password` 預設 `true`，使用者首次登入成功後，後端回傳一個特殊狀態要求前端導向改密頁面，改完才能存取其他 API。`POST /api/users/{id}/resend-invite` 會重新產生密碼並重寄信。

**理由**：遵守「不儲存明文密碼」與「初始密碼為一次性」的原則。強制改密保證管理員無法長期得知員工密碼。

### `IEmailSender` 抽象 + Dev 寫檔實作

**決策**：定義 `IEmailSender` 介面（`Task SendAsync(EmailMessage msg, CancellationToken ct)`）。提供兩種實作：`FileLogEmailSender`（將信件完整內容寫到 `App_Data/outbox/{timestamp}-{to}.eml`）與 `NullEmailSender`（測試用）。`appsettings.json` 的 `Email:Mode` 切換 `FileLog`｜`Null`。未來 `add-time-clock` 或部署階段再新增 `SmtpEmailSender`，並不需要修改叫用端。

**理由**：開發環境不需要真實 SMTP 即可驗證「寄信 → 使用者拿到密碼 → 首次登入」流程。抽象介面讓後續 change 可以零侵入替換。寫到 `.eml` 檔而非純 log，方便用 Outlook 或任何 MUA 直接打開驗證模板渲染結果。

### 首任 admin 透過 EF Core Seed + 環境變數初始化

**決策**：EF Core migration 內執行 `SeedAdmin`，從環境變數 `ATTENDANCE_ADMIN_EMAIL`、`ATTENDANCE_ADMIN_INITIAL_PASSWORD` 讀取首任管理員資料；若兩者任一未設則略過 seed 並在啟動時以警告 log 提示。密碼以 `PasswordHasher<User>` 雜湊後存入，`must_change_password = true`。

**理由**：避免把硬編碼的預設密碼留在程式碼或 migration SQL 中。環境變數讓部署者必須主動設定才會產生 admin，同時首次登入強制改密確保原始值不會長期存在。

### 時區：資料庫 UTC，UI `Asia/Taipei`

**決策**：所有 `DateTime` 欄位在資料庫以 UTC 儲存、在 EF Core `ValueConverter` 統一標記 `DateTimeKind.Utc`；所有跨日計算（例如後續階段的「工作日」「第幾次請假」）以 `Asia/Taipei` 為基準透過 `TimeZoneInfo.ConvertTimeFromUtc` 完成；前端顯示一律轉為 `Asia/Taipei`。

**理由**：台灣雖無日光節約時間，但鎖定 UTC 儲存可避免日後若公司拓展到其他時區時資料無法比對。TZDB 使用 IANA 識別字 `Asia/Taipei` 而非偏移值 `+08:00`，保留未來政策變更彈性。

### 密碼強度

**決策**：最少 10 字元、至少包含 1 個大寫字母、1 個小寫字母、1 個數字、1 個符號；後端以 `IPasswordPolicy` 驗證，前端以相同規則做即時提示。拒絕與前次相同的密碼（比對雜湊）。

**理由**：符合多數企業內部的最低要求；10 字元門檻在可用性與安全性之間取得平衡。

### 為 approval flow 版本化預留空間（本階段不實作）

**決策**：本階段不建立 approval 資料表，但在 `design.md` 明文記載「第 3 階段加入 approval 時採 snapshot 版本化」——即 `approval_instances` 會冗餘儲存 `definition_id`、`definition_version` 以及 `steps` 的 JSON snapshot，使管理員日後調整 flow 定義不會影響進行中的 instance。本階段的 `users`、`departments` 欄位命名要與第 3 階段的 `direct-manager`、`department-head` 解析器相容——`users.manager_id` 為該員工直屬主管，`departments` 不含主管欄位（第 3 階段再擴充 `department_head_user_id`）。

**理由**：先把 naming 固定好，避免第 3 階段改欄位名造成 migration 斷裂。

### 前後端專案結構

**決策**：
- 根目錄：`AttendanceSystem.sln`
- 後端：`src/AttendanceSystem.Api/`（Web API 專案）、`src/AttendanceSystem.Domain/`（領域模型與介面，例如 `IEmailSender`、`IPasswordPolicy`、Entities）、`src/AttendanceSystem.Infrastructure/`（EF Core DbContext、`FileLogEmailSender`、`PasswordHasher` wrapper）。
- 測試：`tests/AttendanceSystem.Api.Tests/`、`tests/AttendanceSystem.Domain.Tests/`。
- 前端：`web/`（Vite + React + TS），內部結構 `web/src/pages/`、`web/src/features/<feature>/`、`web/src/api/`、`web/src/lib/`。

**理由**：三層切分讓後續 change 新增領域模型（leave request、overtime request）有天然落腳處；`Domain` 無外部框架相依，方便寫單元測試。前端 feature-based 而非 type-based，使每個 feature 的元件、hook、API 類型可共置。

## Risks / Trade-offs

- **[風險] httpOnly cookie + Bearer token 混合模式的 CSRF 風險**：雖然 access token 在 Bearer header 不受 CSRF 影響，但 refresh 端點仰賴 cookie。→ **緩解**：`SameSite=Strict` 加上僅在 `POST /api/auth/refresh` 接受 cookie、並在 refresh response 做 CSRF double-submit token 驗證（若未來需要跨站使用再加）。
- **[風險] 不採用 Full Identity 代表需自行維護角色/權限模型**：未來若要整合第三方認證（OAuth、SAML）會比採 Identity 多一些工；本階段只用兩個角色，損失可控。→ **緩解**：抽象 `ICurrentUser`、`IAuthorizationService` 介面，未來替換實作即可。
- **[風險] `FileLogEmailSender` 若誤用於正式環境會導致密碼洩漏**：檔案內容含明文初始密碼。→ **緩解**：啟動時若 `ASPNETCORE_ENVIRONMENT = Production` 且 `Email:Mode = FileLog` 則 fail fast 並以例外終止應用程式；`outbox` 資料夾列入 `.gitignore`。
- **[風險] 首任 admin seed 依賴環境變數，若兩者未設則系統無管理員可登入**：需另外記錄「如何啟動」的操作步驟。→ **緩解**：README 寫明初次啟動所需環境變數；啟動時若未 seed admin 則輸出明顯警告。
- **[風險] 密碼雜湊演算法未來升級**：`PasswordHasher<T>` 有版本機制，但升級時需批次 re-hash。→ **緩解**：在 login 成功後若發現 `VerifyHashedPassword` 回傳 `SuccessRehashNeeded` 自動 re-hash 並儲存。
- **[取捨] 僅支援單層部門**：公司成長到多事業部時需重構成樹狀結構與遷移。→ 當前需求明確為單公司，不過度設計；真需要時再在新 change 提出 schema 遷移。
