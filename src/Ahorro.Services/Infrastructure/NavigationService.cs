using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;

namespace Ahorro.Services.Infrastructure;

public class NavigationService : INavigationService
{
    public NavigationPage CurrentPage { get; private set; } = NavigationPage.Dashboard;
    public event Action<NavigationPage>? PageChanged;

    public void Navigate(NavigationPage page)
    {
        CurrentPage = page;
        PageChanged?.Invoke(page);
    }
}
