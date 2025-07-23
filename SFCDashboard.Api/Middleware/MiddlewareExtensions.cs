namespace SFCDashboard.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseUserRegistration(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserRegistrationMiddleware>();
        }

        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}