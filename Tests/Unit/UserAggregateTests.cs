using Rudoger.BuildingBlocks.Domain;
using Rudoger.Modules.Authn.Domain;

namespace Rudoger.UnitTests;

public sealed class UserAggregateTests
{
    [Fact]
    public void RegisterNormalizesUsername()
    {
        UserAggregate user = UserAggregate.Register(Guid.CreateVersion7(), " Abdullah ", "hash");
        Assert.Equal("abdullah", user.Username);
        Assert.Equal("hash", user.PasswordHash);
        Assert.IsType<UserRegistered>(Assert.Single(user.UncommittedEvents));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void RegisterRejectsBlankUsername(string username)
    {
        Assert.Throws<DomainException>(() => UserAggregate.Register(Guid.CreateVersion7(), username, "hash"));
    }

    [Fact]
    public void HistoryRestoresUser()
    {
        Guid id = Guid.CreateVersion7();
        var user = new UserAggregate();
        user.LoadFromHistory([new UserRegistered(id, "user", "hash")]);
        Assert.Equal(id, user.Id);
        Assert.Equal("user", user.Username);
        Assert.Empty(user.UncommittedEvents);
    }

    [Fact]
    public void RegisterValidatesIdentityAndPasswordHash()
    {
        Assert.Throws<DomainException>(() => UserAggregate.Register(Guid.Empty, "user", "hash"));
        Assert.Throws<DomainException>(() => UserAggregate.Register(Guid.CreateVersion7(), "user", string.Empty));
    }

    [Fact]
    public void HistoryRejectsUnknownEvent()
    {
        var user = new UserAggregate();
        Assert.Throws<InvalidOperationException>(() => user.LoadFromHistory([new OtherTestEvent()]));
    }
}
