using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DrJaw.Services;
using DrJaw.Services.MSSQL;
using DrJaw.ViewModels.Common;

namespace DrJaw.Views.Common
{
    public partial class OrdersWindow : Window
    {
        public OrdersWindow(IMssqlRepository repo, IUserSessionService session)
        {
            InitializeComponent();
            DataContext = new OrdersViewModel(repo, session);
        }

        private void OnExpandAllClicked(object sender, RoutedEventArgs e) => SetAllGroupsExpanded(true);
        private void OnCollapseAllClicked(object sender, RoutedEventArgs e) => SetAllGroupsExpanded(false);

        private void SetAllGroupsExpanded(bool isExpanded)
        {
            // гарантируем, что визуальное дерево построено
            OrdersList.UpdateLayout();

            foreach (var groupItem in FindVisualChildren<GroupItem>(OrdersList))
            {
                var exp = FindVisualChild<Expander>(groupItem);
                if (exp != null) exp.IsExpanded = isExpanded;
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) yield break;
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T t) yield return t;
                foreach (var d in FindVisualChildren<T>(child))
                    yield return d;
            }
        }

        private static T? FindVisualChild<T>(DependencyObject root) where T : DependencyObject
        {
            foreach (var child in FindVisualChildren<T>(root))
                return child;
            return null;
        }
    }
}
