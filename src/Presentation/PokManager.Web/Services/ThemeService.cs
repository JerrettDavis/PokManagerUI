using Microsoft.JSInterop;

namespace PokManager.Web.Services;

public enum ThemeMode
{
    Light,
    Dark,
    Auto
}

public class ThemeService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private ThemeMode _currentMode = ThemeMode.Auto;
    private bool _isDarkMode;
    private bool _isInitialized;
    private DotNetObjectReference<ThemeService>? _dotNetHelper;
    private IJSObjectReference? _themeModule;

    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public ThemeMode CurrentMode => _currentMode;
    public bool IsDarkMode => _isDarkMode;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            // Load the theme interop module
            _themeModule = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./themeInterop.js");

            // Load saved theme preference from localStorage
            var savedTheme = await _themeModule.InvokeAsync<string?>("themeInterop.getStoredThemePreference");

            if (!string.IsNullOrEmpty(savedTheme) && Enum.TryParse<ThemeMode>(savedTheme, out var mode))
            {
                _currentMode = mode;
            }
            else
            {
                // No saved preference, use Auto mode (respects system preference)
                _currentMode = ThemeMode.Auto;
            }

            // Determine if dark mode should be active
            await UpdateDarkModeState();

            // Set up system theme change listener if in Auto mode
            if (_currentMode == ThemeMode.Auto)
            {
                await SetupSystemThemeListener();
            }

            _isInitialized = true;
        }
        catch (Exception)
        {
            // If JavaScript is not available (during prerendering), use defaults
            _currentMode = ThemeMode.Auto;
            _isDarkMode = false;
            _isInitialized = true;
        }
    }

    public async Task SetThemeModeAsync(ThemeMode mode)
    {
        if (_currentMode == mode)
            return;

        var previousMode = _currentMode;
        _currentMode = mode;

        try
        {
            if (_themeModule != null)
            {
                // Save preference to localStorage
                await _themeModule.InvokeVoidAsync("themeInterop.setStoredThemePreference", mode.ToString());
            }
        }
        catch (Exception)
        {
            // Ignore if localStorage is not available
        }

        // Remove system listener if switching away from Auto mode
        if (previousMode == ThemeMode.Auto && mode != ThemeMode.Auto)
        {
            await RemoveSystemThemeListener();
        }
        // Add system listener if switching to Auto mode
        else if (previousMode != ThemeMode.Auto && mode == ThemeMode.Auto)
        {
            await SetupSystemThemeListener();
        }

        await UpdateDarkModeState();
        OnThemeChanged?.Invoke();
    }

    public async Task ToggleThemeAsync()
    {
        var nextMode = _currentMode switch
        {
            ThemeMode.Light => ThemeMode.Dark,
            ThemeMode.Dark => ThemeMode.Auto,
            ThemeMode.Auto => ThemeMode.Light,
            _ => ThemeMode.Auto
        };

        await SetThemeModeAsync(nextMode);
    }

    private async Task UpdateDarkModeState()
    {
        var previousDarkMode = _isDarkMode;

        _isDarkMode = _currentMode switch
        {
            ThemeMode.Dark => true,
            ThemeMode.Light => false,
            ThemeMode.Auto => await GetSystemPrefersDarkModeAsync(),
            _ => false
        };

        if (previousDarkMode != _isDarkMode)
        {
            OnThemeChanged?.Invoke();
        }
    }

    private async Task<bool> GetSystemPrefersDarkModeAsync()
    {
        try
        {
            if (_themeModule != null)
            {
                var systemTheme = await _themeModule.InvokeAsync<string>("themeInterop.getSystemThemePreference");
                return systemTheme == "dark";
            }
            return false;
        }
        catch (Exception)
        {
            // Default to light mode if system preference cannot be determined
            return false;
        }
    }

    private async Task SetupSystemThemeListener()
    {
        try
        {
            if (_themeModule != null && _dotNetHelper == null)
            {
                _dotNetHelper = DotNetObjectReference.Create(this);
                await _themeModule.InvokeVoidAsync("themeInterop.addSystemThemeListener", _dotNetHelper);
            }
        }
        catch (Exception)
        {
            // Ignore if listener setup fails
        }
    }

    private async Task RemoveSystemThemeListener()
    {
        try
        {
            if (_themeModule != null)
            {
                await _themeModule.InvokeVoidAsync("themeInterop.removeSystemThemeListener");
                _dotNetHelper?.Dispose();
                _dotNetHelper = null;
            }
        }
        catch (Exception)
        {
            // Ignore if listener removal fails
        }
    }

    [JSInvokable]
    public async Task OnSystemThemeChanged(string newTheme)
    {
        // Only respond to system theme changes if in Auto mode
        if (_currentMode == ThemeMode.Auto)
        {
            await UpdateDarkModeState();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await RemoveSystemThemeListener();

        if (_themeModule != null)
        {
            await _themeModule.DisposeAsync();
        }
    }
}
