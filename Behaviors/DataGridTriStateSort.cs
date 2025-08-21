using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DrJaw.Behaviors
{
    public static class DataGridTriStateSort
    {
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(DataGridTriStateSort),
                new PropertyMetadata(false, OnEnableChanged));

        public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);
        public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid dg) return;

            if ((bool)e.NewValue)
                dg.Sorting += OnSorting;
            else
                dg.Sorting -= OnSorting;
        }

        private static void OnSorting(object? sender, DataGridSortingEventArgs e)
        {
            if (sender is not DataGrid dg) return;

            // не трогаем колонки, у которых сортировка запрещена
            if (!e.Column.CanUserSort) return;

            e.Handled = true; // отменяем дефолтное поведение

            var view = CollectionViewSource.GetDefaultView(dg.ItemsSource);
            if (view is null) return;

            // текущая колонка и её состояние
            var current = e.Column;
            var curDir = current.SortDirection;

            // Сбрасываем иконки у всех колонок
            foreach (var col in dg.Columns)
                col.SortDirection = null;

            // Получаем/чистим существующие сортировки
            var sd = view.SortDescriptions;

            // третье состояние — убрать сортировку вовсе
            if (curDir == ListSortDirection.Descending)
            {
                sd.Clear();
                view.Refresh();
                return;
            }

            // первое/второе состояния — Asc / Desc
            sd.Clear();
            var next = curDir is null ? ListSortDirection.Ascending : ListSortDirection.Descending;

            // имя свойства берём из привязки колонки
            var bindingPath = GetBindingPath(current);
            if (string.IsNullOrEmpty(bindingPath)) return;

            sd.Add(new SortDescription(bindingPath, next));
            current.SortDirection = next;

            view.Refresh();
        }

        private static string? GetBindingPath(DataGridColumn column)
        {
            // DataGridTextColumn
            if (column is DataGridTextColumn textCol &&
                textCol.Binding is Binding b && !string.IsNullOrEmpty(b.Path?.Path))
                return b.Path.Path;

            // DataGridTemplateColumn — ищем Binding в TextBlock.Text, etc. (если нужно, расширишь)
            return null;
        }
    }
}
