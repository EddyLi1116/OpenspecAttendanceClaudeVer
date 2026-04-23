# user-management Specification

## Purpose
TBD - created by archiving change bootstrap-foundation. Update Purpose after archive.
## Requirements
### Requirement: 管理員建立使用者

系統 SHALL 提供 `POST /api/users` 端點，僅允許具 `admin` 角色者呼叫。Body 包含 `email`、`displayName`、`departmentId`、`managerUserId`（可為 NULL）、`hireDate`、`roleCodes`（陣列，`admin` 或 `employee`）。系統 MUST 以 `RandomNumberGenerator` 產生 16 字元強密碼（含大小寫+數字+符號），以 `PasswordHasher<User>` 雜湊後寫入 `users.password_hash`、設定 `must_change_password = true`、`employment_status = 'active'`、`created_at = UTC now`，並觸發 `IEmailSender` 寄送歡迎信（主旨「歡迎加入出缺勤系統」），信件內容包含 email 與該次產生的明文初始密碼。明文密碼 MUST 不得寫入任何資料庫欄位或日誌。

#### Scenario: 管理員成功建立新使用者

- **WHEN** 管理員呼叫 `POST /api/users`，body 為 `{ email, displayName, departmentId, managerUserId, hireDate, roleCodes: ["employee"] }`
- **THEN** 回應狀態 201，body 包含新使用者的 `id`、`email`、`displayName`、`departmentId`、`managerUserId`、`hireDate`、`roleCodes`、`employmentStatus = "active"`，不含密碼任何資訊
- **AND** `users` 新增一筆紀錄，`must_change_password = true`
- **AND** `user_roles` 新增對應 `employee` 角色的關聯
- **AND** `IEmailSender.SendAsync` 被呼叫一次，信件正文包含一次性初始密碼

#### Scenario: 建立時 email 已存在

- **WHEN** 管理員以已存在於 `users.email` 的 email 呼叫端點
- **THEN** 回應狀態 409，body `errorCode = "EMAIL_ALREADY_EXISTS"`

#### Scenario: 非管理員嘗試建立

- **WHEN** 僅具 `employee` 角色的使用者呼叫 `POST /api/users`
- **THEN** 回應狀態 403

### Requirement: 使用者列表與單筆查詢

系統 SHALL 提供 `GET /api/users`（管理員）與 `GET /api/users/{id}`（管理員或使用者本人）。列表支援 `page`、`pageSize`、`search`（比對 email 或 displayName）、`departmentId`、`status` 查詢參數。回應 MUST 不含密碼雜湊。

#### Scenario: 管理員列出使用者

- **WHEN** 管理員呼叫 `GET /api/users?page=1&pageSize=20`
- **THEN** 回應狀態 200，body `{ items: User[], total, page, pageSize }`，每筆 `User` 不含 `passwordHash`

#### Scenario: 使用者查詢他人資料

- **WHEN** `employee` 角色使用者呼叫 `GET /api/users/{otherUserId}`
- **THEN** 回應狀態 403

#### Scenario: 使用者查詢本人資料

- **WHEN** 使用者呼叫 `GET /api/users/{selfId}` 或 `GET /api/users/me`
- **THEN** 回應狀態 200，body 為本人資料

### Requirement: 修改使用者

系統 SHALL 提供 `PUT /api/users/{id}` 端點，僅管理員可呼叫，可更新 `displayName`、`departmentId`、`managerUserId`、`hireDate`、`roleCodes`。`email` 與 `passwordHash` MUST 不得透過此端點修改。系統 MUST 驗證 `managerUserId` 不得為該使用者自身或形成循環（`A → B → A`）。

#### Scenario: 管理員更新使用者部門

- **WHEN** 管理員呼叫 `PUT /api/users/{id}`，body `{ departmentId: 2 }`
- **THEN** 回應狀態 200，body 包含更新後的使用者資料
- **AND** `users.department_id` 被更新

#### Scenario: 指派自己為直屬主管

- **WHEN** 管理員呼叫 `PUT /api/users/{id}`，body `{ managerUserId: <same id> }`
- **THEN** 回應狀態 400、`errorCode = "INVALID_MANAGER_SELF"`

#### Scenario: 指派會形成循環的 manager

- **WHEN** 使用者 A 已是使用者 B 的 manager，管理員呼叫 `PUT /api/users/A`，body `{ managerUserId: B }`
- **THEN** 回應狀態 400、`errorCode = "INVALID_MANAGER_CYCLE"`

### Requirement: 停用與啟用使用者

系統 SHALL 提供 `POST /api/users/{id}/deactivate` 與 `POST /api/users/{id}/activate` 端點，僅管理員可呼叫。停用 MUST 將 `employment_status` 改為 `inactive`、撤銷該使用者所有 refresh token。系統 MUST 不允許刪除使用者實體（避免破壞歷史資料關聯），`DELETE /api/users/{id}` 等同於停用且回傳 204。

#### Scenario: 停用使用者

- **WHEN** 管理員呼叫 `POST /api/users/{id}/deactivate`
- **THEN** 回應狀態 204
- **AND** `users.employment_status` 設為 `'inactive'`
- **AND** 該使用者 `refresh_tokens` 全部 `revoked_at` 被寫入目前時間

#### Scenario: 刪除使用者

- **WHEN** 管理員呼叫 `DELETE /api/users/{id}`
- **THEN** 回應狀態 204
- **AND** 行為等同於停用（`employment_status = 'inactive'`），資料列 MUST 保留

### Requirement: 重發邀請（重設初始密碼並寄信）

系統 SHALL 提供 `POST /api/users/{id}/resend-invite` 端點（僅管理員）。呼叫時 MUST 產生新的 16 字元初始密碼、以 `PasswordHasher<User>` 重新雜湊、將 `must_change_password` 設為 `true`、撤銷所有 refresh token、透過 `IEmailSender` 寄送歡迎信（主旨「出缺勤系統登入密碼已重設」）含新密碼。

#### Scenario: 管理員重發邀請

- **WHEN** 管理員呼叫 `POST /api/users/{id}/resend-invite`
- **THEN** 回應狀態 204
- **AND** `users.password_hash` 被更新
- **AND** `users.must_change_password = true`
- **AND** 該使用者所有 refresh token 撤銷
- **AND** `IEmailSender.SendAsync` 被呼叫一次，內容含新初始密碼

### Requirement: 角色指派

系統 SHALL 僅支援 `admin` 與 `employee` 兩種角色代碼。同一使用者可同時具備兩者。`admin` 角色至少須保留一位啟用中帳號——若嘗試將系統中最後一位啟用中 `admin` 停用或移除其 `admin` 角色，系統 MUST 拒絕並回 409。

#### Scenario: 嘗試移除最後一位 admin 的角色

- **WHEN** 目前僅有 1 位啟用中的 `admin`，管理員對其呼叫 `PUT /api/users/{id}` 並將 `roleCodes` 改為 `["employee"]`
- **THEN** 回應狀態 409、`errorCode = "CANNOT_REMOVE_LAST_ADMIN"`

#### Scenario: 嘗試停用最後一位 admin

- **WHEN** 目前僅有 1 位啟用中的 `admin`，管理員對其呼叫 `POST /api/users/{id}/deactivate`
- **THEN** 回應狀態 409、`errorCode = "CANNOT_DEACTIVATE_LAST_ADMIN"`

