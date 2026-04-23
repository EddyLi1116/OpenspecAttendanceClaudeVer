# identity-access Specification

## Purpose
TBD - created by archiving change bootstrap-foundation. Update Purpose after archive.
## Requirements
### Requirement: 使用者登入與 Access Token 發行

系統 SHALL 提供 `POST /api/auth/login` 端點，接受 `email` 與 `password`，驗證成功後回傳 JWT access token（壽命 15 分鐘）並在回應中以 httpOnly + Secure + SameSite=Strict cookie 設定 refresh token（壽命 7 天）。系統 MUST 將 refresh token 的 SHA-256 雜湊與過期時間寫入 `refresh_tokens` 資料表。

#### Scenario: 以正確帳密登入

- **WHEN** 使用者以正確 email 與密碼呼叫 `POST /api/auth/login`
- **THEN** 回應狀態為 200，body 包含 `accessToken`、`tokenType=Bearer`、`expiresInSeconds=900`、`user` 基本資料與 `mustChangePassword` 旗標
- **AND** 回應的 `Set-Cookie: refresh_token=...; HttpOnly; Secure; SameSite=Strict; Path=/api/auth` 存在
- **AND** `refresh_tokens` 資料表新增一筆對應雜湊、`expires_at` 為簽發時間加 7 天、`revoked_at` 為 NULL 的紀錄

#### Scenario: 以錯誤密碼登入

- **WHEN** 使用者以正確 email 但錯誤密碼呼叫 `POST /api/auth/login`
- **THEN** 回應狀態為 401、body 包含通用錯誤訊息「帳號或密碼不正確」，不透露是 email 不存在或密碼錯誤
- **AND** 不發出任何 access token 或 refresh token cookie

#### Scenario: 登入被停用帳號

- **WHEN** 使用者以 `employment_status = 'inactive'` 的帳號正確密碼登入
- **THEN** 回應狀態為 403、body 提示「帳號已停用，請聯絡管理員」

### Requirement: Refresh Token 輪替與撤銷

系統 SHALL 提供 `POST /api/auth/refresh` 端點，讀取 httpOnly cookie 中的 refresh token，驗證雜湊、未過期且未撤銷後，撤銷原 token（寫入 `revoked_at`）並發行新的 access token 與新的 refresh token cookie。系統 MUST 在每次 refresh 後輪替 refresh token。系統 MUST 拒絕已撤銷或已過期的 refresh token。

#### Scenario: 成功輪替

- **WHEN** 使用者帶著未過期的 refresh token cookie 呼叫 `POST /api/auth/refresh`
- **THEN** 回應狀態 200，body 包含新的 `accessToken` 與 `expiresInSeconds=900`
- **AND** 回應包含新的 `Set-Cookie: refresh_token=...` header
- **AND** 原 refresh token 的 `refresh_tokens.revoked_at` 被寫入目前時間
- **AND** 新的 refresh token 以新雜湊寫入 `refresh_tokens`

#### Scenario: 使用已撤銷的 refresh token

- **WHEN** 使用者帶著已於 `refresh_tokens.revoked_at` 有值的 token 呼叫 `POST /api/auth/refresh`
- **THEN** 回應狀態 401
- **AND** 系統 MUST 將該使用者所有未撤銷的 refresh token 一併撤銷（防重放）

### Requirement: 登出

系統 SHALL 提供 `POST /api/auth/logout` 端點，撤銷目前 refresh token（寫入 `revoked_at`）並清除 refresh token cookie。Access token 因短壽命不需額外黑名單。

#### Scenario: 使用者登出

- **WHEN** 使用者呼叫 `POST /api/auth/logout`
- **THEN** 回應狀態 204
- **AND** 目前 refresh token 的 `revoked_at` 被寫入目前時間
- **AND** 回應帶有 `Set-Cookie: refresh_token=; Max-Age=0` 清除瀏覽器 cookie

### Requirement: 變更密碼

系統 SHALL 提供 `POST /api/auth/change-password` 端點（需通過 Bearer 驗證），接受 `oldPassword` 與 `newPassword`。驗證舊密碼正確、新密碼符合強度政策（至少 10 字元、含大小寫英文、數字、符號）且不等於舊密碼後，以 `PasswordHasher<User>` 重新雜湊儲存，並撤銷該使用者所有 refresh token。

#### Scenario: 成功變更密碼

- **WHEN** 已登入使用者以正確 `oldPassword` 與符合政策的 `newPassword` 呼叫端點
- **THEN** 回應狀態 204
- **AND** `users.password_hash` 被更新、`must_change_password` 設為 `false`
- **AND** 該使用者全部 `refresh_tokens.revoked_at` 被寫入目前時間，迫使其他裝置重新登入

#### Scenario: 新密碼不符政策

- **WHEN** 使用者提交少於 10 字元或缺少任一類字元的 `newPassword`
- **THEN** 回應狀態 400、body 說明違反哪條政策（例如「至少 10 字元」「需包含符號」）

### Requirement: 忘記密碼與重設

系統 SHALL 提供 `POST /api/auth/forgot-password`（接受 `email`）與 `POST /api/auth/reset-password`（接受 `token` 與 `newPassword`）。`forgot-password` 無論 email 是否存在都回傳相同成功訊息（防 enumeration），但只在 email 存在時產生一次性、30 分鐘內有效的重設 token 並透過 `IEmailSender` 寄出連結。`reset-password` 驗證 token 未過期、未使用後，重設密碼、標記 token 已使用、撤銷該使用者所有 refresh token。

#### Scenario: 對存在的 email 請求重設

- **WHEN** 呼叫 `POST /api/auth/forgot-password`，body 中 `email` 對應到啟用中的使用者
- **THEN** 回應狀態 202 並附通用訊息「若帳號存在，系統已寄出重設連結」
- **AND** `IEmailSender.SendAsync` 被呼叫一次、主旨為「密碼重設」、內容含一次性連結

#### Scenario: 對不存在的 email 請求重設

- **WHEN** 呼叫 `POST /api/auth/forgot-password`，`email` 在 `users` 中不存在
- **THEN** 回應狀態 202 並附與上一情境 **相同** 的訊息
- **AND** `IEmailSender.SendAsync` 不被呼叫

#### Scenario: 以過期 token 重設

- **WHEN** 使用者以建立時間超過 30 分鐘的重設 token 呼叫 `POST /api/auth/reset-password`
- **THEN** 回應狀態 400、body 訊息「連結已過期，請重新申請」

### Requirement: 首次登入強制改密

系統 SHALL 在使用者 `must_change_password = true` 時，登入回應中設定 `mustChangePassword = true`，且除 `POST /api/auth/change-password` 與 `POST /api/auth/logout` 外，所有受保護端點 MUST 回傳 403 與錯誤碼 `PASSWORD_CHANGE_REQUIRED`，直到密碼變更完成。

#### Scenario: 首次登入後嘗試呼叫受保護端點

- **WHEN** `must_change_password = true` 的使用者登入後以 access token 呼叫 `GET /api/users/me`
- **THEN** 回應狀態 403、body `errorCode = "PASSWORD_CHANGE_REQUIRED"`

#### Scenario: 首次登入後立即改密

- **WHEN** 上述使用者改以 `POST /api/auth/change-password` 提供正確舊密碼與合規新密碼
- **THEN** 回應狀態 204、`users.must_change_password` 設為 `false`
- **AND** 之後呼叫 `GET /api/users/me` 回應 200

