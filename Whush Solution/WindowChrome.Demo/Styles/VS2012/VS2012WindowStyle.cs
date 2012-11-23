using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Collections;

namespace WindowChrome.Demo.Styles.VS2012
{
    internal static class LocalExtensions
    {
        public static Window GetTemplatedWindow(
            this object templateFrameworkElement,
            Action<Window> action = null)
        {
            Window window = ((FrameworkElement)templateFrameworkElement).TemplatedParent as Window;
            if (window != null && action != null) action(window);
            return window;
        }
    }

    public partial class VS2012WindowStyle
    {
        void WindowLoaded(object sender, RoutedEventArgs e)
        {
            Hacks.FixWpfMaximizePositioning((Window)sender);
            Hacks.FixWpfMaximizeTaskbar((Window)sender);
        }

        void IconMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
                sender.GetTemplatedWindow(w => SystemCommands.CloseWindow(w));
        }

        void IconMouseUp(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            var point = element.PointToScreen(new Point(element.ActualWidth / 2, element.ActualHeight));
            sender.GetTemplatedWindow(w => SystemCommands.ShowSystemMenu(w, point));
        }

        void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            sender.GetTemplatedWindow(w => SystemCommands.CloseWindow(w));
        }

        void MinButtonClick(object sender, RoutedEventArgs e)
        {
            sender.GetTemplatedWindow(w => SystemCommands.MinimizeWindow(w));
        }

        void MaxButtonClick(object sender, RoutedEventArgs e)
        {
            sender.GetTemplatedWindow(w =>
                {
                    if (w.WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(w);
                    else SystemCommands.MaximizeWindow(w);
                });
        }
    }
}