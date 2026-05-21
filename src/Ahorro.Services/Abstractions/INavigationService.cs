using Ahorro.Models.Enums;

namespace Ahorro.Services.Abstractions;

public interface INavigationService
{
    NavigationPage CurrentPage { get; }
    event Action<NavigationPage>? PageChanged;
    void Navigate(NavigationPage page);
}
