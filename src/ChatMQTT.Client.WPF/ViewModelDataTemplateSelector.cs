using System.Windows;
using System.Windows.Controls;
using ChatMQTT.Client.WPF.ViewModels;
using ChatMQTT.Client.WPF.Views;

namespace ChatMQTT.Client.WPF;

public class ViewModelDataTemplateSelector : DataTemplateSelector
{
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item == null) return null!;

        var element = container as FrameworkElement;

        if (item is LoginViewModel)
        {
            return element?.FindResource("LoginViewTemplate") as DataTemplate ?? null!;
        }

        if (item is TopicSelectionViewModel)
        {
            return element?.FindResource("TopicSelectionViewTemplate") as DataTemplate ?? null!;
        }

        if (item is ChatViewModel)
        {
            return element?.FindResource("ChatViewTemplate") as DataTemplate ?? null!;
        }

        return base.SelectTemplate(item, container);
    }
}
