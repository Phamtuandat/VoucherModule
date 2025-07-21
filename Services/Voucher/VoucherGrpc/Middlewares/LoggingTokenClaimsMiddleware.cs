namespace VoucherGrpc.Middlewares
{
    public class LoggingTokenClaimsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingTokenClaimsMiddleware> _logger;
        public LoggingTokenClaimsMiddleware(RequestDelegate next, ILogger<LoggingTokenClaimsMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var claims = context.User.Claims
                    .Select(c => $"{c.Type}: {c.Value}")
                    .ToList();

                _logger.LogInformation("JWT Claims: \n{Claims}", string.Join("\n", claims));
            }

            await _next(context);
        }
    }
}
