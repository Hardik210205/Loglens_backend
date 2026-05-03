using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using LogLens.Application.Interfaces;
using LogLens.Domain.Entities;
using LogLens.Domain.Enums;
using DomainLogLevel = LogLens.Domain.Enums.LogLevel;

namespace LogLens.Infrastructure.Data;

/// <summary>
/// Idempotent seed data for LogLens demo environment.
/// Creates admin/viewer users, Payment/Order services, and 50 sample logs.
/// </summary>
public static class SeedData
{
    private const string AdminEmail = "admin@loglens.dev";
    private const string AdminPassword = "Admin@123";
    private const string ViewerEmail = "viewer@loglens.dev";
    private const string ViewerPassword = "Viewer@123";
    private const string PaymentServiceName = "Payment Service";
    private const string OrderServiceName = "Order Service";

    public static async Task SeedAsync(LogLensDbContext db, IApiKeyCipher apiKeyCipher)
    {
        // Idempotent check: if both services exist, skip all seeding
        var paymentServiceExists = await db.Services.AnyAsync(s => s.Name == PaymentServiceName);
        var orderServiceExists = await db.Services.AnyAsync(s => s.Name == OrderServiceName);

        if (paymentServiceExists && orderServiceExists)
        {
            Console.WriteLine("\n╔══════════════════════════════════════════╗");
            Console.WriteLine("║      LogLens Seed Already Complete       ║");
            Console.WriteLine("║      (Skipping duplicate seeding)        ║");
            Console.WriteLine("╚══════════════════════════════════════════╝\n");
            return;
        }

        var adminUser = await SeedUsersAsync(db);
        var (paymentService, paymentApiKey) = await SeedPaymentServiceAsync(db, adminUser, apiKeyCipher);
        var (orderService, orderApiKey) = await SeedOrderServiceAsync(db, adminUser, apiKeyCipher);
        await SeedLogsAsync(db, paymentService, orderService);

        await db.SaveChangesAsync();

        PrintSeedSummary(paymentApiKey, orderApiKey);
    }

    private static async Task<User> SeedUsersAsync(LogLensDbContext db)
    {
        User? adminUser = null;
        User? viewerUser = null;

        // Check if admin user exists
        var existingAdmin = await db.Users.FirstOrDefaultAsync(u => u.Email == AdminEmail);
        if (existingAdmin == null)
        {
            adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = AdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            db.Users.Add(adminUser);
        }
        else
        {
            adminUser = existingAdmin;
        }

        // Check if viewer user exists
        var existingViewer = await db.Users.FirstOrDefaultAsync(u => u.Email == ViewerEmail);
        if (existingViewer == null)
        {
            viewerUser = new User
            {
                Id = Guid.NewGuid(),
                Email = ViewerEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(ViewerPassword),
                Role = UserRole.Viewer,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            db.Users.Add(viewerUser);
        }
        else
        {
            viewerUser = existingViewer;
        }

        await db.SaveChangesAsync();
        return adminUser;
    }

    private static async Task<(Service, string)> SeedPaymentServiceAsync(LogLensDbContext db, User adminUser, IApiKeyCipher apiKeyCipher)
    {
        var existingService = await db.Services.FirstOrDefaultAsync(s => s.Name == PaymentServiceName);

        if (existingService != null)
        {
            // Service exists; retrieve existing API key
            var existingKey = await db.ApiKeys
                .Where(k => k.ServiceId == existingService.Id && k.IsActive)
                .FirstOrDefaultAsync();

            return (existingService, existingKey?.KeyPrefix ?? "");
        }

        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = PaymentServiceName,
            DisplayName = "Payment Service",
            CreatedByUserId = adminUser.Id,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        db.Services.Add(service);
        await db.SaveChangesAsync();

        // Generate API key
        var rawKey = GenerateApiKey();
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            KeyHash = BCrypt.Net.BCrypt.HashPassword(rawKey),
            RawApiKeyCiphertext = apiKeyCipher.Protect(rawKey),
            KeyPrefix = rawKey.Substring(0, 8),
            Description = "Initial seed key",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync();

        return (service, rawKey);
    }

    private static async Task<(Service, string)> SeedOrderServiceAsync(LogLensDbContext db, User adminUser, IApiKeyCipher apiKeyCipher)
    {
        var existingService = await db.Services.FirstOrDefaultAsync(s => s.Name == OrderServiceName);

        if (existingService != null)
        {
            // Service exists; retrieve existing API key
            var existingKey = await db.ApiKeys
                .Where(k => k.ServiceId == existingService.Id && k.IsActive)
                .FirstOrDefaultAsync();

            return (existingService, existingKey?.KeyPrefix ?? "");
        }

        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = OrderServiceName,
            DisplayName = "Order Service",
            CreatedByUserId = adminUser.Id,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        db.Services.Add(service);
        await db.SaveChangesAsync();

        // Generate API key
        var rawKey = GenerateApiKey();
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            ServiceId = service.Id,
            KeyHash = BCrypt.Net.BCrypt.HashPassword(rawKey),
            RawApiKeyCiphertext = apiKeyCipher.Protect(rawKey),
            KeyPrefix = rawKey.Substring(0, 8),
            Description = "Initial seed key",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync();

        return (service, rawKey);
    }

    private static async Task SeedLogsAsync(LogLensDbContext db, Service paymentService, Service orderService)
    {
        // Idempotent: only seed if less than 50 logs exist
        var existingLogCount = await db.Logs.CountAsync();
        if (existingLogCount >= 50)
        {
            return;
        }

        var logs = new List<LogEntry>();
        var random = new Random(42); // Fixed seed for reproducibility

        // Payment Service: 25 logs
        var paymentMessages = GeneratePaymentServiceMessages();
        for (int i = 0; i < 25; i++)
        {
            var message = paymentMessages[random.Next(paymentMessages.Count)];
            var level = GetRandomLogLevel(random);
            var hoursAgo = random.Next(0, 168); // 0-7 days
            var minutesAgo = random.Next(0, 60);

            logs.Add(new LogEntry
            {
                Id = Guid.NewGuid(),
                ServiceId = paymentService.Id,
                ServiceName = paymentService.Name,
                Level = level,
                Message = message,
                Metadata = GeneratePaymentMetadata(random),
                Timestamp = DateTime.UtcNow.AddHours(-hoursAgo).AddMinutes(-minutesAgo),
                TraceId = Guid.NewGuid().ToString("N")
            });
        }

        // Order Service: 25 logs
        var orderMessages = GenerateOrderServiceMessages();
        for (int i = 0; i < 25; i++)
        {
            var message = orderMessages[random.Next(orderMessages.Count)];
            var level = GetRandomLogLevel(random);
            var hoursAgo = random.Next(0, 168); // 0-7 days
            var minutesAgo = random.Next(0, 60);

            logs.Add(new LogEntry
            {
                Id = Guid.NewGuid(),
                ServiceId = orderService.Id,
                ServiceName = orderService.Name,
                Level = level,
                Message = message,
                Metadata = GenerateOrderMetadata(random),
                Timestamp = DateTime.UtcNow.AddHours(-hoursAgo).AddMinutes(-minutesAgo),
                TraceId = Guid.NewGuid().ToString("N")
            });
        }

        db.Logs.AddRange(logs);
        await db.SaveChangesAsync();
    }

    private static string GenerateApiKey()
    {
        return "ll_" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    }

    private static DomainLogLevel GetRandomLogLevel(Random random)
    {
        var roll = random.Next(100);
        if (roll < 60) return DomainLogLevel.Information;
        if (roll < 85) return DomainLogLevel.Warning;
        return DomainLogLevel.Error;
    }

    private static List<string> GeneratePaymentServiceMessages()
    {
        return new List<string>
        {
            "Payment initiated: orderId={0}, amount={1}, currency=USD, provider=Stripe",
            "Payment authorized: paymentId={0}, provider=Stripe, durationMs={1}",
            "Payment captured: paymentId={0}, amount={1}",
            "Payment processing completed successfully",
            "Payment retry attempt 1: orderId={0}, reason=timeout",
            "Payment retry attempt 2: orderId={0}, reason=network_error",
            "Suspicious transaction flagged: orderId={0}, amount={1}, riskScore={2:F2}",
            "Fraud detection triggered: riskScore={0:F2}, action=manual_review",
            "Payment failed: orderId={0}, provider=Stripe, error=InsufficientFunds",
            "Payment failed: orderId={0}, provider=Stripe, error=CardDeclined",
            "Webhook delivery failed: orderId={0}, statusCode={1}, retry_attempt={2}",
            "Refund initiated: paymentId={0}, amount={1}",
            "Refund processed: paymentId={0}, amount={1}, status=completed",
            "Payment reconciliation: total_amount={0}, transaction_count={1}",
            "PCI compliance check passed: transactions_scanned={0}"
        };
    }

    private static List<string> GenerateOrderServiceMessages()
    {
        return new List<string>
        {
            "Order placed: orderId={0}, customerId={1}, items={2}, total={3}",
            "Order confirmed: orderId={0}, estimatedDelivery={1}",
            "Order shipped: orderId={0}, carrier={1}, trackingId={2}",
            "Order delivered: orderId={0}",
            "Order processing started: orderId={0}, warehouseId={1}",
            "Order picked from warehouse: orderId={0}, items_picked={1}",
            "Order packed and ready: orderId={0}",
            "Order delayed: orderId={0}, reason=warehouse_backlog, newEta={1}",
            "Order delayed: orderId={0}, reason=carrier_delay, delayHours={1}",
            "Inventory low: productId={0}, remaining={1}, threshold=10",
            "Inventory critical: productId={0}, remaining={1}",
            "Order fulfillment failed: orderId={0}, reason=out_of_stock",
            "Order fulfillment failed: orderId={0}, reason=payment_declined",
            "Order cancellation failed: orderId={0}, error=already_shipped",
            "Order cancellation requested: orderId={0}, reason={1}",
            "Return initiated: orderId={0}, reason={1}",
            "Return approved: orderId={0}, refundAmount={1}",
            "Inventory reconciliation: productId={0}, expected={1}, actual={2}",
            "Warehouse health check passed: warehouseId={0}",
            "Shipping rate calculated: orderId={0}, method={1}, cost={2}"
        };
    }

    private static string GeneratePaymentMetadata(Random random)
    {
        var orderId = Guid.NewGuid().ToString("N").Substring(0, 12);
        var amount = random.Next(50, 5001);
        var paymentId = Guid.NewGuid().ToString("N").Substring(0, 16);
        var duration = random.Next(50, 301);
        var riskScore = random.Next(0, 100) / 100.0;
        var statusCode = new[] { 400, 500, 503 }[random.Next(3)];

        var metadata = new
        {
            orderId,
            amount,
            paymentId,
            duration,
            riskScore = Math.Round(riskScore, 2),
            statusCode
        };

        return JsonSerializer.Serialize(metadata);
    }

    private static string GenerateOrderMetadata(Random random)
    {
        var orderId = Guid.NewGuid().ToString("N").Substring(0, 12);
        var customerId = Guid.NewGuid().ToString("N").Substring(0, 12);
        var productId = Guid.NewGuid().ToString("N").Substring(0, 12);
        var items = random.Next(1, 9);
        var total = random.Next(10, 501);
        var trackingId = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
        var delayHours = random.Next(1, 48);
        var remaining = random.Next(1, 6);
        var carrier = new[] { "FedEx", "UPS", "DHL" }[random.Next(3)];

        var metadata = new
        {
            orderId,
            customerId,
            productId,
            items,
            total,
            trackingId,
            delayHours,
            remaining,
            carrier
        };

        return JsonSerializer.Serialize(metadata);
    }

    private static void PrintSeedSummary(string paymentApiKey, string orderApiKey)
    {
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║            LogLens Seed Data Complete                    ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════╣");
        Console.WriteLine("║ Admin User:                                              ║");
        Console.WriteLine($"║   Email:    {AdminEmail,-43} ║");
        Console.WriteLine($"║   Password: {AdminPassword,-43} ║");
        Console.WriteLine("║ Viewer User:                                             ║");
        Console.WriteLine($"║   Email:    {ViewerEmail,-43} ║");
        Console.WriteLine($"║   Password: {ViewerPassword,-43} ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════╣");
        Console.WriteLine("║ Payment Service API Key:                                 ║");
        Console.WriteLine($"║   {paymentApiKey}... ║");
        Console.WriteLine("║ Order Service API Key:                                   ║");
        Console.WriteLine($"║   {orderApiKey}... ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════╣");
        Console.WriteLine("║ Sample logs inserted: 50 (25 per service)                ║");
        Console.WriteLine("║ Ready for demo!                                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");
    }
}
