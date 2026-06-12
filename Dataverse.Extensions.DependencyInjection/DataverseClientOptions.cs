using Azure.Core;

namespace BauerApps.Dataverse.Extensions;

/// <summary>
/// Configuration options for the Dataverse ServiceClient.
/// </summary>
public class DataverseClientOptions
{
    /// <summary>
    /// Base URL of the Dataverse environment.
    /// Example: https://my-org.crm4.dynamics.com
    /// </summary>
    public Uri OrganizationUrl { get; set; } = null!;

    /// <summary>
    /// Custom <see cref="Azure.Core.TokenCredential"/> for authentication.
    /// When <c>null</c>, <see cref="Azure.Identity.DefaultAzureCredential"/> is used.
    /// </summary>
    public TokenCredential? TokenCredential { get; set; }

    /// <summary>
    /// When <c>true</c>, connection to Dataverse is deferred until first use.
    /// When <c>false</c> (default), connection is established at startup and
    /// an exception is thrown if it fails.
    /// </summary>
    public bool DeferConnection { get; set; }
}

