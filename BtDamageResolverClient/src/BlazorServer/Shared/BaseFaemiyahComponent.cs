using Microsoft.AspNetCore.Components;

namespace Faemiyah.BtDamageResolver.Client.BlazorServer.Shared;

/// <summary>
/// Basic Faemiyah component utility methods.
/// </summary>
public class BaseFaemiyahComponent : ComponentBase
{
    /// <summary>
    /// Invoked async state change.
    /// </summary>
    protected void InvokeStateChange()
    {
        InvokeAsync(StateHasChanged);
    }
}