using Microsoft.AspNetCore.Components;
using myenergy.Services;

namespace myenergy.Components;

/// <summary>
/// Base component for all pages that ensures data is initialized before rendering.
/// Pages can inherit from this to automatically have data ready.
/// </summary>
public class DataAwareComponentBase : ComponentBase
{
    [Inject]
    protected DataInitializationService DataInit { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        // Ensure data is loaded (this is idempotent - safe to call multiple times)
        await DataInit.InitializeAsync();
        
        await base.OnInitializedAsync();
    }
}
