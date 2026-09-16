using Microsoft.AspNetCore.Http;
using Rudoger.Modules.Logging.Presentation;

namespace Rudoger.UnitTests;

public sealed class SensitiveDataMaskerTests
{
    [Fact]
    public void MaskJsonRedactsNestedSensitiveProperties()
    {
        var masker = new SensitiveDataMasker();
        string masked = masker.MaskJson("{\"username\":\"user\",\"password\":\"secret\",\"nested\":{\"accessToken\":\"jwt\"}}");

        Assert.Contains("user", masked, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", masked, StringComparison.Ordinal);
        Assert.DoesNotContain("jwt", masked, StringComparison.Ordinal);
        Assert.Contains("***MASKED***", masked, StringComparison.Ordinal);
    }

    [Fact]
    public void MaskHeadersRedactsAuthorizationAndCookie()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer secret";
        context.Request.Headers.Cookie = "session=secret";
        context.Request.Headers["X-Test"] = "visible";

        string masked = new SensitiveDataMasker().MaskHeaders(context.Request.Headers);

        Assert.DoesNotContain("Bearer secret", masked, StringComparison.Ordinal);
        Assert.DoesNotContain("session=secret", masked, StringComparison.Ordinal);
        Assert.Contains("visible", masked, StringComparison.Ordinal);
    }

    [Fact]
    public void MaskPathAndQueryRedactsTokenParameters()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api";
        context.Request.QueryString = new QueryString("?token=secret&filter=visible");

        string masked = new SensitiveDataMasker().MaskPathAndQuery(context.Request);

        Assert.DoesNotContain("secret", masked, StringComparison.Ordinal);
        Assert.Contains("visible", masked, StringComparison.Ordinal);
    }

    [Fact]
    public void MaskJsonOmitsMalformedBody()
    {
        Assert.Equal("[UNPARSEABLE BODY OMITTED]", new SensitiveDataMasker().MaskJson("not-json"));
    }
}
