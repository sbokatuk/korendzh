using Korendzh.Domain;
using Korendzh.Infrastructure.Notifications;
using Korendzh.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Korendzh.Tests;

public class NotificationDispatcherTests
{
    [Fact]
    public async Task EnqueueAsync_creates_queued_entry_with_payload()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new NotificationDispatcher(db, NullLogger<NotificationDispatcher>.Instance);
        var userId = Guid.NewGuid();

        await sut.EnqueueAsync(userId, NotificationChannel.Email, NotificationTemplates.InviteCreated,
            "invite:1", new { name = "Иван" });

        Assert.Single(db.Notifications);
        var entry = db.Notifications.Single();
        Assert.Equal(NotificationStatus.Queued, entry.Status);
        Assert.Equal("invite:1", entry.EventKey);
        Assert.Equal(userId, entry.UserId);
        Assert.Contains("Иван", entry.PayloadJson);
    }

    [Fact]
    public async Task EnqueueAsync_is_idempotent_by_event_key()
    {
        using var db = TestDbContextFactory.Create();
        var sut = new NotificationDispatcher(db, NullLogger<NotificationDispatcher>.Instance);
        var userId = Guid.NewGuid();

        await sut.EnqueueAsync(userId, NotificationChannel.Email, "tag", "evt:1", new { a = 1 });
        await sut.EnqueueAsync(userId, NotificationChannel.Email, "tag", "evt:1", new { a = 2 });

        Assert.Single(db.Notifications);
    }
}
