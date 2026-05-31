using CompanyOps.Domain.Auditing;
using CompanyOps.Domain.Common;
using CompanyOps.Domain.Requests;

namespace CompanyOps.Domain.Tests.Auditing;

public class AuditLogTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid RequestId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public void ForRequest_RecordsActionActorTargetAndStatuses()
    {
        var entry = AuditLog.ForRequest(AuditAction.RequestApproved, RequestId, Actor, RequestStatus.Submitted, RequestStatus.Approved, Now);

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(Now, entry.OccurredAtUtc);
        Assert.Equal(Actor, entry.ActorId);
        Assert.Equal(AuditAction.RequestApproved, entry.Action);
        Assert.Equal("Request", entry.TargetType);
        Assert.Equal(RequestId, entry.TargetId);
        Assert.Equal("Submitted", entry.FromStatus);
        Assert.Equal("Approved", entry.ToStatus);
    }

    [Fact]
    public void ForRequest_Creation_HasNullFromStatus()
    {
        var entry = AuditLog.ForRequest(AuditAction.RequestCreated, RequestId, Actor, null, RequestStatus.Draft, Now);

        Assert.Null(entry.FromStatus);
        Assert.Equal("Draft", entry.ToStatus);
    }

    [Fact]
    public void ForRequest_WithEmptyActor_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(
            () => AuditLog.ForRequest(AuditAction.RequestCreated, RequestId, Guid.Empty, null, RequestStatus.Draft, Now));
    }

    [Fact]
    public void ForRequest_WithEmptyRequestId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(
            () => AuditLog.ForRequest(AuditAction.RequestCreated, Guid.Empty, Actor, null, RequestStatus.Draft, Now));
    }

    [Fact]
    public void StampSource_SetsTheSourceIpWriteOnce()
    {
        var entry = AuditLog.ForRequest(AuditAction.RequestCreated, RequestId, Actor, null, RequestStatus.Draft, Now);
        Assert.Null(entry.SourceIp);

        entry.StampSource("203.0.113.7");
        Assert.Equal("203.0.113.7", entry.SourceIp);

        entry.StampSource("10.0.0.1"); // write-once — a second stamp is ignored
        Assert.Equal("203.0.113.7", entry.SourceIp);
    }
}
