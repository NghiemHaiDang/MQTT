using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace ChatMQTT.Client.WPF.Behaviors;

public class ScrollToTopBehavior : Behavior<ScrollViewer>
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ScrollToTopBehavior));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.ScrollChanged += OnScrollChanged;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.ScrollChanged -= OnScrollChanged;
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalOffset < 50 && e.VerticalChange < 0)
        {
            if (Command?.CanExecute(null) == true)
            {
                Command.Execute(null);
            }
        }
    }
}
