namespace AttendanceSystem.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    public int HttpStatusCode { get; }

    protected DomainException(string errorCode, string message, int httpStatusCode) : base(message)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }
}

public sealed class EmailAlreadyExistsException : DomainException
{
    public EmailAlreadyExistsException(string email)
        : base("EMAIL_ALREADY_EXISTS", $"Email '{email}' 已被使用", 409) { }
}

public sealed class DepartmentCodeExistsException : DomainException
{
    public DepartmentCodeExistsException(string code)
        : base("DEPARTMENT_CODE_EXISTS", $"部門代碼 '{code}' 已存在", 409) { }
}

public sealed class DepartmentHasMembersException : DomainException
{
    public DepartmentHasMembersException()
        : base("DEPARTMENT_HAS_MEMBERS", "仍有使用者歸屬此部門，無法刪除", 409) { }
}

public sealed class CannotRemoveLastAdminException : DomainException
{
    public CannotRemoveLastAdminException()
        : base("CANNOT_REMOVE_LAST_ADMIN", "系統至少須保留一位啟用中的管理員", 409) { }
}

public sealed class CannotDeactivateLastAdminException : DomainException
{
    public CannotDeactivateLastAdminException()
        : base("CANNOT_DEACTIVATE_LAST_ADMIN", "系統至少須保留一位啟用中的管理員", 409) { }
}

public sealed class InvalidManagerSelfException : DomainException
{
    public InvalidManagerSelfException()
        : base("INVALID_MANAGER_SELF", "不能將自己設為直屬主管", 400) { }
}

public sealed class InvalidManagerCycleException : DomainException
{
    public InvalidManagerCycleException()
        : base("INVALID_MANAGER_CYCLE", "主管指派會形成循環關係", 400) { }
}

public sealed class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base("INVALID_CREDENTIALS", "帳號或密碼不正確", 401) { }
}

public sealed class AccountInactiveException : DomainException
{
    public AccountInactiveException()
        : base("ACCOUNT_INACTIVE", "帳號已停用，請聯絡管理員", 403) { }
}

public sealed class PasswordChangeRequiredException : DomainException
{
    public PasswordChangeRequiredException()
        : base("PASSWORD_CHANGE_REQUIRED", "請先完成首次密碼變更", 403) { }
}

public sealed class InvalidPasswordPolicyException : DomainException
{
    public IReadOnlyList<string> Violations { get; }
    public InvalidPasswordPolicyException(IReadOnlyList<string> violations)
        : base("INVALID_PASSWORD_POLICY", "新密碼不符合安全政策", 400)
    {
        Violations = violations;
    }
}

public sealed class PasswordSameAsOldException : DomainException
{
    public PasswordSameAsOldException()
        : base("PASSWORD_SAME_AS_OLD", "新密碼不可與舊密碼相同", 400) { }
}

public sealed class InvalidRefreshTokenException : DomainException
{
    public InvalidRefreshTokenException()
        : base("INVALID_REFRESH_TOKEN", "Refresh token 無效或已失效", 401) { }
}

public sealed class InvalidResetTokenException : DomainException
{
    public InvalidResetTokenException()
        : base("INVALID_RESET_TOKEN", "連結已過期或已使用，請重新申請", 400) { }
}

public sealed class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entity, object key)
        : base("NOT_FOUND", $"{entity} '{key}' 不存在", 404) { }
}

public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "沒有權限執行此操作")
        : base("FORBIDDEN", message, 403) { }
}

public sealed class InvalidRoleCodeException : DomainException
{
    public InvalidRoleCodeException(string code)
        : base("INVALID_ROLE_CODE", $"角色代碼 '{code}' 無效", 400) { }
}
