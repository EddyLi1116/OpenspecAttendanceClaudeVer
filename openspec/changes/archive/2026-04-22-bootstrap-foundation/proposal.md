## Why

公司目前沒有出缺勤管理系統，員工打卡、請假、加班全靠紙本與口頭核可，資料分散且難以追蹤。在開始導入打卡、簽核、請假與代理人等功能之前，必須先有一套能管理身分、角色、部門與寄送通知信的基礎骨架；否則後續任何功能都無從依附。本 change 聚焦於建立這層地基，讓後續階段（打卡、簽核、請假、加班）可以在穩定的身分與組織資料之上擴充。

## What Changes

- 新增前後端專案骨架：後端 `src/AttendanceSystem.Api/`（ASP.NET Core Web API + EF Core + Pomelo MySQL），前端 `web/`（Vite + React + TypeScript + TanStack Query + Tailwind）。
- 建立身分驗證機制：JWT（access token 15 分鐘 + refresh token 7 天，refresh token 以 httpOnly Secure cookie 傳遞並於資料庫以雜湊值保存、支援輪替與撤銷）；密碼以 ASP.NET Core Identity `PasswordHasher<T>` 雜湊。
- 支援登入、登出、變更密碼、忘記密碼、重設密碼；首次登入強制改密。
- 提供管理員對使用者的完整 CRUD，包括指派部門與直屬主管、啟用／停用、重發邀請信（含重新產生初始密碼）。
- 提供單層部門維護（新增、編輯、刪除、列表）與下屬清單查詢。
- 建立抽象的 `IEmailSender` 介面，開發環境以寫入 log 檔的實作供測試；提供歡迎信（含初始密碼）、密碼重設兩種信件模板。
- 以 EF Core migration 建立四張資料表：`users`、`roles` + `user_roles`、`departments`、`refresh_tokens`；並種子一位首任系統管理員（密碼自環境變數讀取並 hash）。
- 時區策略：資料庫一律儲存 UTC，前端與跨日計算以 `Asia/Taipei` 呈現。
- 密碼強度政策：最少 10 字元，須包含大小寫字母、數字與符號。

## Capabilities

### New Capabilities

- `identity-access`：登入、登出、JWT 發行與輪替、變更密碼、忘記密碼重設、首次登入強制改密
- `user-management`：管理員對使用者的 CRUD、部門與直屬主管指派、啟用／停用、重發邀請
- `org-structure`：部門（單層）CRUD、下屬清單查詢
- `email-delivery`：`IEmailSender` 介面與開發環境寫檔實作、歡迎信與密碼重設信件模板、初始密碼產生器

### Modified Capabilities

（無。此為專案首個 change，尚無既有 spec 可修改。）

## Impact

- 新增檔案：後端 solution `AttendanceSystem.sln`、Web API 專案 `src/AttendanceSystem.Api/`、單元測試專案 `tests/AttendanceSystem.Api.Tests/`、前端 `web/`；OpenSpec spec 檔 `openspec/specs/identity-access/spec.md`、`openspec/specs/user-management/spec.md`、`openspec/specs/org-structure/spec.md`、`openspec/specs/email-delivery/spec.md`。
- 資料庫：在 `Attendance` 資料庫中建立 `users`、`roles`、`user_roles`、`departments`、`refresh_tokens` 五張表，並種子 admin 角色、employee 角色以及首任管理員帳號。
- 外部相依：後端新增 `Microsoft.AspNetCore.Authentication.JwtBearer`、`Microsoft.EntityFrameworkCore`、`Pomelo.EntityFrameworkCore.MySql`、`Microsoft.AspNetCore.Identity`；前端新增 `react`、`react-dom`、`@tanstack/react-query`、`react-router-dom`、`tailwindcss`、`axios`、`typescript`、`vite`。
- 組態：新增 `appsettings.json`（JWT signing key、token 壽命、MySQL 連線字串、Email sender mode、首任 admin email）與前端 `.env`（API base URL）。
- API 介面：新增以下路徑
  - `POST /api/auth/login`、`/refresh`、`/logout`、`/change-password`、`/forgot-password`、`/reset-password`
  - `GET/POST /api/users`、`GET/PUT/DELETE /api/users/{id}`、`POST /api/users/{id}/resend-invite`、`POST /api/users/{id}/deactivate`
  - `GET/POST /api/departments`、`GET/PUT/DELETE /api/departments/{id}`、`GET /api/users/{id}/subordinates`
- 後續 changes（`add-time-clock`、`add-approval-and-leave`、`add-overtime`）將以本 change 建立的身分、組織與通知基礎擴充。
