using Rudoger.BuildingBlocks.Application;
using Rudoger.Modules.Authn.Domain;

namespace Rudoger.Modules.Authn.Application;

public interface IAuthnRepository : IEventRepository<UserAggregate>
{
    Task<UserCredential?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
}
