using Rudoger.BuildingBlocks.Domain;
using Rudoger.BuildingBlocks.Infrastructure;

namespace Rudoger.UnitTests;

public sealed class EventTypeRegistryTests
{
    [Fact]
    public void RegisteredEventRoundTripsWithoutClrNameInContract()
    {
        var registry = new EventTypeRegistry();
        registry.Register(EventTypeRegistration.Create<TestEvent>("test.happened"));
        var source = new TestEvent("value");

        string payload = registry.Serialize(source);
        IDomainEvent restored = registry.Deserialize(registry.NameOf(source), 1, payload);

        Assert.Equal(source, Assert.IsType<TestEvent>(restored));
    }

    [Fact]
    public void RegistryRejectsDuplicatesUnknownTypesAndSchemaVersions()
    {
        var registry = new EventTypeRegistry();
        registry.Register(EventTypeRegistration.Create<TestEvent>("test.happened"));
        Assert.Throws<InvalidOperationException>(() => registry.Register(
            EventTypeRegistration.Create<OtherTestEvent>("test.happened")));
        Assert.Throws<InvalidOperationException>(() => registry.NameOf(new OtherTestEvent()));
        Assert.Throws<InvalidOperationException>(() => registry.Deserialize("test.happened", 2, "{}"));
        Assert.Throws<InvalidOperationException>(() => registry.Deserialize("unknown", 1, "{}"));
    }

}
