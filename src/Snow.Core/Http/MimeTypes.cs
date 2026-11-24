namespace Snow.Http;

/// <summary>
/// 常用 MIME 类型(对应 ABP 的 <c>MimeTypes</c>)。
/// </summary>
public static class MimeTypes
{
    /// <summary>
    /// application/* 系列
    /// </summary>
    public static class Application
    {
        /// <summary>
        /// application/json
        /// </summary>
        public const string Json = "application/json";

        /// <summary>
        /// application/xml
        /// </summary>
        public const string Xml = "application/xml";

        /// <summary>
        /// application/x-www-form-urlencoded
        /// </summary>
        public const string FormUrlEncoded = "application/x-www-form-urlencoded";

        /// <summary>
        /// application/octet-stream
        /// </summary>
        public const string OctetStream = "application/octet-stream";
    }

    /// <summary>
    /// text/* 系列
    /// </summary>
    public static class Text
    {
        /// <summary>
        /// text/plain
        /// </summary>
        public const string Plain = "text/plain";

        /// <summary>
        /// text/html
        /// </summary>
        public const string Html = "text/html";

        /// <summary>
        /// text/css
        /// </summary>
        public const string Css = "text/css";

        /// <summary>
        /// text/javascript
        /// </summary>
        public const string JavaScript = "text/javascript";
    }
}
