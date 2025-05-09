namespace ParkAccess
{
    public class ApiSettings
    {
        public string Key { get; set; }
        public string BaseUrl { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string Audience { get; set; }
        public string RedirectUrl { get; set; }
    }
    class AppSettings
    {
        public ApiSettings Api { get; set; }
    }
}
