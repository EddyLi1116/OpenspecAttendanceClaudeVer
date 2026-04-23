using System.Net;
using AttendanceSystem.Domain.Email;

namespace AttendanceSystem.Api.Templates;

public static class ResetPasswordEmailTemplate
{
    public static EmailMessage Build(string displayName, string resetLink, string expiresAtTaipei, string email)
    {
        var safeName = WebUtility.HtmlEncode(displayName);
        var safeLink = WebUtility.HtmlEncode(resetLink);
        var safeExp = WebUtility.HtmlEncode(expiresAtTaipei);

        var html = $@"<!doctype html>
<html lang=""zh-Hant""><head><meta charset=""utf-8""><title>出缺勤系統密碼重設連結</title></head>
<body style=""font-family:Arial,'Microsoft JhengHei',sans-serif;color:#111;"">
<p>{safeName} 您好，</p>
<p>我們收到您的密碼重設申請。請點選下列連結於 30 分鐘內完成重設：</p>
<p><a href=""{safeLink}"">{safeLink}</a></p>
<p>連結過期時間：{safeExp}（台北時間）</p>
<p>如非本人申請請忽略此信，您的帳號仍然安全。</p>
<p>— 出缺勤系統</p>
</body></html>";

        var text = $@"{displayName} 您好，

我們收到您的密碼重設申請。請點選下列連結於 30 分鐘內完成重設：

{resetLink}

連結過期時間：{expiresAtTaipei}（台北時間）

如非本人申請請忽略此信，您的帳號仍然安全。

— 出缺勤系統";

        return new EmailMessage
        {
            To = email,
            Subject = "出缺勤系統密碼重設連結",
            HtmlBody = html,
            TextBody = text
        };
    }
}
