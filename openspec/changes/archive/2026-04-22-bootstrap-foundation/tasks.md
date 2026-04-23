## 1. Solution 與專案骨架

- [x] 1.1 於專案根目錄建立 `AttendanceSystem.sln`
- [x] 1.2 以 `dotnet new classlib -n AttendanceSystem.Domain -o src/AttendanceSystem.Domain` 建立 Domain 專案並加入 solution
- [x] 1.3 以 `dotnet new classlib -n AttendanceSystem.Infrastructure -o src/AttendanceSystem.Infrastructure` 建立 Infrastructure 專案並加入 solution
- [x] 1.4 以 `dotnet new webapi -n AttendanceSystem.Api -o src/AttendanceSystem.Api --no-openapi false` 建立 Web API 專案並加入 solution
- [x] 1.5 以 `dotnet new xunit` 建立 `tests/AttendanceSystem.Domain.Tests` 與 `tests/AttendanceSystem.Api.Tests` 兩個測試專案並加入 solution
- [x] 1.6 設定專案相依：Api → Infrastructure → Domain；測試專案分別參考各自目標專案
- [x] 1.7 執行 `dotnet build` 確認全部專案可編譯

## 2. 後端套件與基礎設定

- [x] 2.1 為 `AttendanceSystem.Infrastructure` 安裝 `Microsoft.EntityFrameworkCore`、`Pomelo.EntityFrameworkCore.MySql`、`Microsoft.AspNetCore.Identity`
- [x] 2.2 為 `AttendanceSystem.Api` 安裝 `Microsoft.AspNetCore.Authentication.JwtBearer`、`Microsoft.EntityFrameworkCore.Design`
- [x] 2.3 於 `AttendanceSystem.Api/appsettings.json` 新增 `ConnectionStrings:MySql`（使用指定的本機 MySQL 連線字串）、`Jwt:Issuer`、`Jwt:Audience`、`Jwt:SigningKey`、`Jwt:AccessTokenMinutes = 15`、`Jwt:RefreshTokenDays = 7`、`Email:Mode = FileLog`、`Email:OutboxDirectory = App_Data/outbox`、`Email:FromAddress`、`Admin:SeedEmail`（文件指出用環境變數覆寫）、`Admin:SeedInitialPassword`（同上）
- [x] 2.4 `appsettings.Development.json` 明確設 `Email:Mode = FileLog`，並在 `appsettings.json` 註解提醒 Production 禁用
- [x] 2.5 於 Program.cs 啟動流程加入安全閘門：若 `Environment.IsProduction() && Email:Mode == "FileLog"` 則擲 `InvalidOperationException`
- [x] 2.6 於 `.gitignore` 加入 `src/AttendanceSystem.Api/App_Data/outbox/`

## 3. 領域模型與介面（Domain 專案）

- [x] 3.1 建立 `Entities/User.cs`（欄位對應 `users` 表；含 `MustChangePassword` 旗標、`EmploymentStatus` enum `Active|Inactive`）
- [x] 3.2 建立 `Entities/Role.cs`、`Entities/UserRole.cs`（`Code` 限 `admin`｜`employee`）
- [x] 3.3 建立 `Entities/Department.cs`（`Code`、`Name`）
- [x] 3.4 建立 `Entities/RefreshToken.cs`（`UserId`、`TokenHash`、`ExpiresAt`、`RevokedAt`、`CreatedAt`）
- [x] 3.5 建立 `Entities/PasswordResetToken.cs`（`UserId`、`TokenHash`、`ExpiresAt`、`UsedAt`）
- [x] 3.6 建立 `Email/EmailMessage.cs`、`Email/IEmailSender.cs`（`Task SendAsync(EmailMessage, CancellationToken)`）
- [x] 3.7 建立 `Security/IInitialPasswordGenerator.cs`、`Security/IPasswordPolicy.cs`、`Security/ICurrentUser.cs`
- [x] 3.8 建立 `Exceptions/` 下 `DomainException` 與各 `errorCode` 對應子類別（例如 `EmailAlreadyExistsException`、`CannotRemoveLastAdminException`）

## 4. Infrastructure 實作

- [x] 4.1 建立 `AttendanceDbContext`：`DbSet<User>`、`DbSet<Role>`、`DbSet<UserRole>`、`DbSet<Department>`、`DbSet<RefreshToken>`、`DbSet<PasswordResetToken>`
- [x] 4.2 於 `OnModelCreating` 設定：`users.email` 唯一索引、`users.manager_id` 自參考外鍵、`departments.code` 唯一索引、`user_roles` 複合主鍵、`refresh_tokens.user_id` 索引、`refresh_tokens.token_hash` 唯一索引
- [x] 4.3 設定 EF Core `DateTimeKind.Utc` ValueConverter 套用到所有 `DateTime` 欄位
- [x] 4.4 以 `dotnet ef migrations add InitialCreate` 產生 initial migration
- [x] 4.5 於 migration 的 `Up` 附加 seed：`roles (admin, 系統管理員)` 與 `(employee, 一般員工)`
- [x] 4.6 建立 `DataSeeder.SeedInitialAdminAsync`：從 `Admin:SeedEmail`、`Admin:SeedInitialPassword`（或環境變數 `ATTENDANCE_ADMIN_EMAIL`、`ATTENDANCE_ADMIN_INITIAL_PASSWORD`）讀取；未設定則略過並 log warning；設定則建立具 `admin` 角色、`must_change_password = true` 的使用者
- [x] 4.7 於 `Program.cs` 啟動時呼叫 `DataSeeder.SeedInitialAdminAsync`
- [x] 4.8 實作 `FileLogEmailSender`：寫 `.eml` multipart/alternative 至 `App_Data/outbox/{yyyyMMdd-HHmmss-fff}-{safeTo}.eml`，目錄不存在時自動建立
- [x] 4.9 實作 `NullEmailSender`（僅給單元測試使用）
- [x] 4.10 於 `Program.cs` 依 `Email:Mode` 註冊 `IEmailSender` 實作
- [x] 4.11 實作 `SystemPasswordPolicy`：10 字元以上、含大小寫+數字+符號
- [x] 4.12 實作 `CryptoInitialPasswordGenerator`：用 `RandomNumberGenerator` 產 16 字元密碼並保證四類字元均至少 1 個
- [x] 4.13 實作 `BcryptOrPbkdf2PasswordHasherAdapter`：包裝 `PasswordHasher<User>`，暴露 `Hash(password)` 與 `Verify(hash, password)` 兩方法（支援 re-hash）

## 5. 身分驗證（identity-access capability）

- [x] 5.1 於 `Program.cs` 設定 `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` 使用 `Jwt:SigningKey`
- [x] 5.2 建立 `IJwtTokenService`：`IssueAccessToken(User)`、`IssueRefreshToken(User, out string raw, out string hash)`、`TryParseAccessToken(string token, out ClaimsPrincipal principal)`
- [x] 5.3 實作 `AuthController`：`POST /api/auth/login`（驗帳密、若 `must_change_password=true` 則在 response 附 `mustChangePassword`、檢查 `employment_status`）
- [x] 5.4 實作 `POST /api/auth/refresh`：讀 `refresh_token` cookie、驗雜湊與到期與未撤銷、輪替；若使用已撤銷者 MUST 撤銷該 user 全部 refresh token
- [x] 5.5 實作 `POST /api/auth/logout`：撤銷目前 refresh token 並清 cookie
- [x] 5.6 實作 `POST /api/auth/change-password`：需 `[Authorize]`；驗舊密碼、套 `IPasswordPolicy`、禁用與舊密碼相同、撤銷全部 refresh token
- [x] 5.7 實作 `POST /api/auth/forgot-password`：無論 email 是否存在回傳相同 202；存在則寫 `PasswordResetToken` 並呼叫 `IEmailSender`
- [x] 5.8 實作 `POST /api/auth/reset-password`：驗 token 未過期、未使用，重設密碼、標記 `used_at`、撤銷全部 refresh token
- [x] 5.9 建立 `PasswordChangeRequiredMiddleware` 或 Authorization Policy：若使用者 `must_change_password=true` 且目標不是 `change-password`、`logout`，回 403 + `PASSWORD_CHANGE_REQUIRED`
- [x] 5.10 設定 refresh token cookie `HttpOnly; Secure; SameSite=Strict; Path=/api/auth`

## 6. 使用者管理（user-management capability）

- [x] 6.1 建立 `UsersController`，套 `[Authorize(Roles = "admin")]` 或自訂 policy
- [x] 6.2 `POST /api/users`：`IInitialPasswordGenerator` 產密碼 → hash 存 `users` → `must_change_password=true` → 寄歡迎信（明文密碼僅透過 `IEmailSender`，MUST 不落地）
- [x] 6.3 `GET /api/users`：分頁查詢參數 `page`、`pageSize`、`search`、`departmentId`、`status`，回應 MUST 過濾掉 `passwordHash`
- [x] 6.4 `GET /api/users/{id}` 與 `GET /api/users/me`：本人或 admin 可讀
- [x] 6.5 `PUT /api/users/{id}`：僅 admin；驗證不得自我 manager 與形成循環（迴圈偵測最多查 10 層或直到命中自身）
- [x] 6.6 `POST /api/users/{id}/deactivate` 與 `POST /api/users/{id}/activate`：停用時撤銷所有 refresh token；檢查「不可停用最後一位 admin」
- [x] 6.7 `DELETE /api/users/{id}`：行為等同 deactivate，保留資料列，回 204
- [x] 6.8 `POST /api/users/{id}/resend-invite`：重產密碼、設 `must_change_password=true`、撤銷全部 refresh token、寄信
- [x] 6.9 角色規則：`roleCodes` 僅接受 `admin`｜`employee`；變更角色 MUST 檢查「不可移除最後一位 admin」

## 7. 組織結構（org-structure capability）

- [x] 7.1 建立 `DepartmentsController`
- [x] 7.2 `GET /api/departments`（任何 authenticated 使用者）、`POST`（admin）、`PUT`(admin)、`DELETE`（admin）
- [x] 7.3 Department `code` 唯一驗證；重複時回 409 `DEPARTMENT_CODE_EXISTS`
- [x] 7.4 `DELETE /api/departments/{id}`：若有使用者參照回 409 `DEPARTMENT_HAS_MEMBERS`
- [x] 7.5 `GET /api/users/{id}/subordinates`：回傳 `users.manager_id = id && employment_status = 'active'` 清單；admin 或本人可查

## 8. 信件模板（email-delivery capability）

- [x] 8.1 建立 `EmailTemplates/WelcomeEmailTemplate`：輸入 `displayName`、`email`、`loginUrl`、`initialPassword`；輸出 HTML + TextBody；主旨「歡迎加入出缺勤系統」
- [x] 8.2 建立 `EmailTemplates/ResetPasswordEmailTemplate`：輸入 `displayName`、`resetLink`、`expiresAtTaipei`；主旨「出缺勤系統密碼重設連結」
- [x] 8.3 模板 MUST 不含外連資源或追蹤像素

## 9. 後端單元與整合測試

- [x] 9.1 `CryptoInitialPasswordGenerator` 單元測試：跑 1000 次檢查長度、四類字元齊全
- [x] 9.2 `SystemPasswordPolicy` 單元測試：涵蓋長度不足、缺少大寫、缺少小寫、缺少數字、缺少符號、合規五情境
- [x] 9.3 `FileLogEmailSender` 測試：寫入暫存目錄、驗證 `.eml` multipart 結構
- [x] 9.4 Production + FileLog 啟動閘門測試
- [x] 9.5 `UserService.CreateUserAsync` 測試：驗證密碼不落地（檢查所有 log sink 不含明文）、`IEmailSender` 被呼叫一次
- [x] 9.6 `AuthController` 整合測試（WebApplicationFactory + InMemory MySQL 或 Testcontainers）：涵蓋 login 成功/密碼錯/帳號停用、refresh 輪替、重放偵測、forgot/reset 流程
- [x] 9.7 `UsersController` 整合測試：最後一位 admin 不可停用、不可移除角色、循環 manager 偵測

## 10. 前端專案骨架

- [x] 10.1 於 `web/` 目錄執行 `npm create vite@latest . -- --template react-ts`
- [x] 10.2 安裝相依 `npm install @tanstack/react-query axios react-router-dom`
- [x] 10.3 安裝並初始化 Tailwind：`npm install -D tailwindcss postcss autoprefixer` 並執行 `npx tailwindcss init -p`
- [x] 10.4 設定 `tailwind.config.ts` 的 `content: ['./index.html', './src/**/*.{ts,tsx}']`，於 `src/index.css` 引入 Tailwind directives
- [x] 10.5 `.env` 新增 `VITE_API_BASE_URL=http://localhost:5000/api`
- [x] 10.6 設定 Axios 實例 `src/api/client.ts`：`baseURL` 讀 `VITE_API_BASE_URL`、`withCredentials: true`（為了 refresh cookie）
- [x] 10.7 設定 TanStack Query `QueryClientProvider` 於 `src/main.tsx`
- [x] 10.8 設定 React Router 根路由 `src/App.tsx`：`/login`、`/force-change-password`、`/users`、`/departments`、`/me`

## 11. 前端頁面與狀態

- [x] 11.1 `features/auth/api.ts`：`login`、`logout`、`refresh`、`changePassword`、`forgotPassword`、`resetPassword` API 呼叫
- [x] 11.2 `features/auth/AuthContext.tsx`：以 React Context 保存記憶體中的 access token、`user`、`mustChangePassword`
- [x] 11.3 Axios 攔截器：401 自動呼叫 `/auth/refresh` 並重試（含單例 refresh promise 避免併發）
- [x] 11.4 `pages/LoginPage.tsx`：email+password 表單、錯誤訊息對應、登入後若 `mustChangePassword` 則導向 `/force-change-password`
- [x] 11.5 `pages/ForceChangePasswordPage.tsx`：`oldPassword`、`newPassword`、`confirm` 欄位，即時強度提示、成功後回首頁
- [x] 11.6 `pages/ForgotPasswordPage.tsx` 與 `pages/ResetPasswordPage.tsx`
- [x] 11.7 `pages/UsersListPage.tsx`（admin）：分頁、搜尋、篩選部門/狀態、列表欄位 `email`、`displayName`、`department`、`manager`、`status`
- [x] 11.8 `pages/UserFormPage.tsx`（admin 新增/編輯）：含部門與直屬主管下拉（資料來自 `GET /api/departments`、`GET /api/users?pageSize=500`）
- [x] 11.9 使用者列表行動：「停用」「啟用」「重發邀請」（確認對話）
- [x] 11.10 `pages/DepartmentsPage.tsx`（admin CRUD）
- [x] 11.11 `pages/ProfilePage.tsx`（本人資料 + 變更密碼入口）
- [x] 11.12 路由保護：未登入 → `/login`；`mustChangePassword` → `/force-change-password`；非 admin 存取 admin 頁 → 403 頁

## 12. 端對端驗證

- [x] 12.1 設定 `ATTENDANCE_ADMIN_EMAIL` 與 `ATTENDANCE_ADMIN_INITIAL_PASSWORD` 環境變數後 `dotnet run --project src/AttendanceSystem.Api` 啟動
- [x] 12.2 以 admin 帳號呼叫 `POST /api/auth/login`、確認首登回傳 `mustChangePassword=true`
- [x] 12.3 呼叫 `POST /api/auth/change-password` 變更密碼成功
- [x] 12.4 以 admin 呼叫 `POST /api/departments` 建立至少一個部門
- [x] 12.5 以 admin 呼叫 `POST /api/users` 建立一位測試員工，並檢查 `App_Data/outbox/` 下對應 `.eml` 有歡迎信與初始密碼
- [x] 12.6 以新員工的初始密碼登入、強制改密後呼叫 `GET /api/users/me` 確認回 200
- [x] 12.7 呼叫 `POST /api/users/{id}/resend-invite` 重寄邀請、確認 outbox 有新信
- [x] 12.8 嘗試停用最後一位 admin，確認回 409
- [x] 12.9 前端 `npm run dev` 啟動，以瀏覽器走完：登入 → 強制改密 → 建立部門 → 建立員工 → 登出 → 用新員工登入
- [x] 12.10 `dotnet test` 全綠、`npm run build` 無錯誤
