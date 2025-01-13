using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace MergeFiles2;

public static class DragDropBehavior
{
    public static readonly DependencyProperty DropCommandProperty =
        DependencyProperty.RegisterAttached("DropCommand", typeof(ICommand), typeof(DragDropBehavior),
            new PropertyMetadata(null, OnDropCommandChanged));

    public static ICommand GetDropCommand(DependencyObject obj)
    {
        return (ICommand)obj.GetValue(DropCommandProperty);
    }

    public static void SetDropCommand(DependencyObject obj, ICommand value)
    {
        obj.SetValue(DropCommandProperty, value);
    }

    private static void OnDropCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            element.Drop -= OnDrop;
            if (e.NewValue != null)
            {
                element.AllowDrop = true;
                element.Drop += OnDrop;
            }
        }
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (sender is UIElement element)
        {
            var command = GetDropCommand(element);
            if (command != null && command.CanExecute(e))
            {
                command.Execute(e);
            }
        }
    }
}
