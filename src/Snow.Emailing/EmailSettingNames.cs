namespace Snow.Emailing;

/// <summary>
/// 邮件相关配置项的名字(对应 ABP 的 EmailSettingNames), 全部以 <c>Snow.Mailing.</c> 开头
/// <para>值从 <c>Settings</c> 配置节读, 例如 <c>"Settings": { "Snow.Mailing.Smtp.Host": "127.0.0.1" }</c>;
/// 也可以由其他取值来源(数据库等)提供。</para>
/// </summary>
public static class EmailSettingNames
{
    /// <summary>
    /// 默认发件人地址
    /// </summary>
    public const string DefaultFromAddress = "Snow.Mailing.DefaultFromAddress";

    /// <summary>
    /// 默认发件人显示名
    /// </summary>
    public const string DefaultFromDisplayName = "Snow.Mailing.DefaultFromDisplayName";

    /// <summary>
    /// SMTP 相关配置项
    /// </summary>
    public static class Smtp
    {
        /// <summary>
        /// 服务器地址
        /// </summary>
        public const string Host = "Snow.Mailing.Smtp.Host";

        /// <summary>
        /// 端口
        /// </summary>
        public const string Port = "Snow.Mailing.Smtp.Port";

        /// <summary>
        /// 登录账号
        /// </summary>
        public const string UserName = "Snow.Mailing.Smtp.UserName";

        /// <summary>
        /// 登录密码(加密存储)
        /// </summary>
        public const string Password = "Snow.Mailing.Smtp.Password";

        /// <summary>
        /// 认证域(部分企业邮箱需要)
        /// </summary>
        public const string Domain = "Snow.Mailing.Smtp.Domain";

        /// <summary>
        /// 是否 SSL 直连(465); false 走 STARTTLS(587/25)
        /// </summary>
        public const string EnableSsl = "Snow.Mailing.Smtp.EnableSsl";

        /// <summary>
        /// 是否使用默认凭据(内网中继、匿名 SMTP 时为 true, 不做认证)
        /// </summary>
        public const string UseDefaultCredentials = "Snow.Mailing.Smtp.UseDefaultCredentials";
    }
}
