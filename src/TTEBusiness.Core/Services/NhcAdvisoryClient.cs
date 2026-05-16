using System.Net.Http;
using TTENET.TTEBusiness.Core.Models;

namespace TTENET.TTEBusiness.Core.Services;

public sealed class NhcAdvisoryClient(HttpClient httpClient) : INhcAdvisoryClient
{
    public async Task<string> GetAdvisoryContentAsync(AdvisorySource source, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(source.Url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}