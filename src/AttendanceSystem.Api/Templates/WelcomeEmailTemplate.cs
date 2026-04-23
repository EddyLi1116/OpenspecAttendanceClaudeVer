using System.Net;
using AttendanceSystem.Domain.Email;

namespace AttendanceSystem.Api.Templates;

public static class WelcomeEmailTemplate
{
    public static EmailMessage Build(string displayName, string email, string loginUrl, string initialPassword, string subject = "歡迎加入出缺勤系統")
    {
        var safeName = WebUtility.HtmlEncode(displayName);
        var safeEmail = WebUtility.HtmlEncode(email);
        var safeLogin = WebUtility.HtmlEncode(loginUrl);
        var safePwd = WebUtility.HtmlEncode(initialPassword);

        var html = $@"<!doctype html>
<html lang=""zh-Hant""><head><meta charset=""utf-8""><title>{WebUtility.HtmlEncode(subject)}</title></head>
<body style=""font-family:Arial,'Microsoft JhengHei',sans-serif;color:#111;"">
<p>{safeName} 您好，</p>
<p>管理員已為您開通出缺勤系統帳號。請使用下列資訊登入：</p>
<ul>
  <li>登入網址：<a href=""{safeLogin}"">{safeLogin}</a></li>
  <li>帳號（Email）：{safeEmail}</li>
  <li>一次性初始密碼：<code>{safePwd}</code></li>
</ul>
<p><strong>首次登入後系統會要求您變更密碼</strong>，請設定新密碼後再使用其他功能。</p>
<p>若您並未申請此帳號，請忽略此信並通知管理員。</p>
<p>— 出缺勤系統</p>
</body></html>";

        var text = $@"{displayName} 您好，

管理員已為您開通出缺勤系統帳號。請使用下列資訊登入：

登入網址：{loginUrl}
帳號（Email）：{email}
一次性初始密碼：{initialPassword}

首次登入後系統會要求您變更密碼，請設定新密碼後再使用其他功能。

若您並未申請此帳號，請忽略此信並通知管理員。

— 出缺勤系統";

        return new EmailMessage
        {
            To = email,
            Subject = subject,
            HtmlBody = html,
            TextBody = text
        };
    }
}
