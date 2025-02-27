using System;
using Avalonia;

namespace PLG_Exam;

public static class ThemeManager
{
    public static string CurrentBackgroundColor
    {
        get
        {
            return nameof(Application.RequestedThemeVariant) + "ModeBackground";
            //return isDarkMode ? "DarkModeBackground" : "LightModeBackground";
        }
    }
}
