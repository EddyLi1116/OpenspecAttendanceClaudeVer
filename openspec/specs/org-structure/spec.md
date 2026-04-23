# org-structure Specification

## Purpose
TBD - created by archiving change bootstrap-foundation. Update Purpose after archive.
## Requirements
### Requirement: 部門 CRUD

系統 SHALL 提供 `GET /api/departments`（任何已登入者可讀）、`POST /api/departments`、`PUT /api/departments/{id}`、`DELETE /api/departments/{id}`（僅 `admin` 角色可寫）。部門實體包含 `id`、`code`（唯一 kebab-case 或英數）、`name`（顯示名稱，繁中）。系統 MUST 驗證 `code` 在 `departments` 表中唯一。

#### Scenario: 管理員建立部門

- **WHEN** 管理員呼叫 `POST /api/departments`，body `{ code: "engineering", name: "工程部" }`
- **THEN** 回應狀態 201、body 包含 `id`、`code`、`name`
- **AND** `departments` 新增一筆對應紀錄

#### Scenario: 建立重複 code 的部門

- **WHEN** 管理員以已存在的 `code` 呼叫 `POST /api/departments`
- **THEN** 回應狀態 409、`errorCode = "DEPARTMENT_CODE_EXISTS"`

#### Scenario: 一般使用者列出部門

- **WHEN** 具 `employee` 角色的使用者呼叫 `GET /api/departments`
- **THEN** 回應狀態 200，body 為 `Department[]`

### Requirement: 部門刪除限制

系統 MUST 拒絕刪除仍有使用者歸屬的部門。若 `departments.id` 被任何 `users.department_id` 參照，`DELETE /api/departments/{id}` 回應 409。

#### Scenario: 刪除仍有成員的部門

- **WHEN** 管理員呼叫 `DELETE /api/departments/{id}`，該部門下仍有 `employment_status = 'active'` 的使用者
- **THEN** 回應狀態 409、`errorCode = "DEPARTMENT_HAS_MEMBERS"`

#### Scenario: 刪除空部門

- **WHEN** 管理員呼叫 `DELETE /api/departments/{id}`，該部門沒有任何 `users` 參照
- **THEN** 回應狀態 204
- **AND** `departments` 對應紀錄被刪除

### Requirement: 下屬清單查詢

系統 SHALL 提供 `GET /api/users/{id}/subordinates` 端點，回傳以該 `userId` 作為 `users.manager_id` 的啟用中使用者清單。僅當呼叫者為 `admin`、或呼叫者為 `{id}` 本人時可取得；其他人回 403。此端點為第 3 階段 approval workflow 的「direct-manager」解析器所需的唯一介面。

#### Scenario: 主管查詢自己的直屬下屬

- **WHEN** 使用者 A 呼叫 `GET /api/users/A/subordinates`
- **THEN** 回應狀態 200，body 為 `users.manager_id = A` 且 `employment_status = 'active'` 的使用者清單

#### Scenario: 非主管且非本人查詢

- **WHEN** 使用者 C（既非 admin 也不是 A）呼叫 `GET /api/users/A/subordinates`
- **THEN** 回應狀態 403

#### Scenario: 管理員查詢任意人下屬

- **WHEN** 管理員呼叫 `GET /api/users/A/subordinates`
- **THEN** 回應狀態 200，body 為 A 的直屬下屬清單

