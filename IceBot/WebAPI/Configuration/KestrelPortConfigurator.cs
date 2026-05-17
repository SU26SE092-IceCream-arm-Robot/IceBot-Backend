namespace WebAPI.Configuration
{
    public static class KestrelPortConfigurator
    {
        public static void ConfigureKestrelWithEnvPort(this IWebHostBuilder webHostBuilder)
        {
            var portValue = Environment.GetEnvironmentVariable("PORT");

            webHostBuilder.ConfigureKestrel(options =>
            {
                if (int.TryParse(portValue, out var port) && port > 0)
                {
                    options.ListenAnyIP(port);
                }
                else
                {
                    // fallback để dev vẫn chạy trên 8080 khi không set PORT
                    options.ListenAnyIP(8080);
                }
            });
        }
    }
}
