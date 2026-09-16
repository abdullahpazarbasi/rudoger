using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rudoger.Modules.Authn.Application;

namespace Rudoger.Modules.Authn.Presentation;

[ApiController]
[AllowAnonymous]
[Route("api/v1/authn/tokens")]
public sealed class TokensController(AuthnApplicationService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TokenResult>> ExchangeAsync(
        TokenExchangeRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await service.ExchangeAsync(request.Username, request.Password, cancellationToken));
    }
}
