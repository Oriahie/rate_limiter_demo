using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.RateLimiting;
using System.Net;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    rateLimiterOptions.AddPolicy("iplimiter", httpContext =>
                       RateLimitPartition.GetFixedWindowLimiter(
                           httpContext.Connection.RemoteIpAddress?.ToString(),
                           _ => new FixedWindowRateLimiterOptions
                           {
                               PermitLimit = 10,
                               Window = TimeSpan.FromSeconds(10)
                           }));

    rateLimiterOptions.AddPolicy("userType", httpContext =>
    {
        var user = httpContext.User;
        var key = httpContext.Request.Headers["x-api-key"].FirstOrDefault();

        if (user.IsInRole("Premium"))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                key,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromSeconds(10)
                });
        }
        else
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                key,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 3,
                    Window = TimeSpan.FromSeconds(10)
                });
        }
    });

    rateLimiterOptions.AddFixedWindowLimiter("fixedWindow", options =>
    {
        options.Window = TimeSpan.FromSeconds(10);
        options.PermitLimit = 3;
    });

    rateLimiterOptions.AddSlidingWindowLimiter("slidingWindow", options =>
    {
        options.Window = TimeSpan.FromSeconds(10);
        options.PermitLimit = 15;
        options.SegmentsPerWindow = 3;
    });

    rateLimiterOptions.AddTokenBucketLimiter("tokenWindow", options =>
    {
        options.TokenLimit = 100;
        options.ReplenishmentPeriod = TimeSpan.FromSeconds(5);
        options.TokensPerPeriod = 10;
    });

    rateLimiterOptions.AddConcurrencyLimiter("concurrency", options =>
    {
        options.PermitLimit = 5;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();
