// Theme detection and management JavaScript interop

// Store listener references for cleanup
let _listener = null;
let _mediaQuery = null;

export const themeInterop = {
    // Get the system's preferred color scheme
    getSystemThemePreference: function () {
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        return 'light';
    },

    // Get theme preference from localStorage
    getStoredThemePreference: function () {
        return localStorage.getItem('theme-preference');
    },

    // Store theme preference in localStorage
    setStoredThemePreference: function (theme) {
        if (theme === null || theme === undefined || theme === '') {
            localStorage.removeItem('theme-preference');
        } else {
            localStorage.setItem('theme-preference', theme);
        }
    },

    // Add listener for system theme changes
    addSystemThemeListener: function (dotNetHelper) {
        const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

        const listener = function (e) {
            const newTheme = e.matches ? 'dark' : 'light';
            dotNetHelper.invokeMethodAsync('OnSystemThemeChanged', newTheme);
        };

        mediaQuery.addEventListener('change', listener);

        // Store the listener reference for cleanup
        _listener = listener;
        _mediaQuery = mediaQuery;
    },

    // Remove system theme listener (cleanup)
    removeSystemThemeListener: function () {
        if (_listener && _mediaQuery) {
            _mediaQuery.removeEventListener('change', _listener);
            _listener = null;
            _mediaQuery = null;
        }
    }
};
