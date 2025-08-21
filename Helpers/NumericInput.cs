using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DrJaw.Helpers
{
    public static class NumericInput
    {
        // ВКЛ/ВЫКЛ поведения
        public static readonly DependencyProperty DecimalOnlyProperty =
            DependencyProperty.RegisterAttached(
                "DecimalOnly",
                typeof(bool),
                typeof(NumericInput),
                new PropertyMetadata(false, OnDecimalOnlyChanged));

        public static void SetDecimalOnly(DependencyObject d, bool value) => d.SetValue(DecimalOnlyProperty, value);
        public static bool GetDecimalOnly(DependencyObject d) => (bool)d.GetValue(DecimalOnlyProperty);

        // Макс. кол-во знаков после точки (-1 = без ограничения)
        public static readonly DependencyProperty MaxDecimalPlacesProperty =
            DependencyProperty.RegisterAttached(
                "MaxDecimalPlaces",
                typeof(int),
                typeof(NumericInput),
                new PropertyMetadata(-1));

        public static void SetMaxDecimalPlaces(DependencyObject d, int value) => d.SetValue(MaxDecimalPlacesProperty, value);
        public static int GetMaxDecimalPlaces(DependencyObject d) => (int)d.GetValue(MaxDecimalPlacesProperty);

        private static void OnDecimalOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox tb) return;

            if ((bool)e.NewValue)
            {
                tb.PreviewTextInput += OnPreviewTextInput;
                tb.PreviewKeyDown += OnPreviewKeyDown;
                DataObject.AddPastingHandler(tb, OnPaste);
            }
            else
            {
                tb.PreviewTextInput -= OnPreviewTextInput;
                tb.PreviewKeyDown -= OnPreviewKeyDown;
                DataObject.RemovePastingHandler(tb, OnPaste);
            }

            // чтобы IME/русская раскладка не вмешивалась
            InputMethod.SetIsInputMethodEnabled(tb, false);
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Разрешаем навигацию/редактирование
            if (e.Key is Key.Back or Key.Delete or Key.Tab or Key.Enter
                or Key.Left or Key.Right or Key.Home or Key.End)
                return;

            // Разрешаем модификаторы (коп/вст/вырез)
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                return;

            // Запрещаем пробел
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb) return;

            var maxDec = GetMaxDecimalPlaces(tb);

            // нормализуем ввод
            string incoming = e.Text;
            string sanitized = incoming == "," ? "." : incoming;

            // разрешаем только цифру или точку
            if (!Regex.IsMatch(sanitized, @"^[0-9.]$"))
            {
                e.Handled = true;
                return;
            }

            // проверим, что итоговый текст был бы валиден
            string candidate = ComposeCandidate(tb, sanitized);
            if (!IsValid(candidate, maxDec))
            {
                e.Handled = true;
                return;
            }

            if (incoming != sanitized) // это была запятая — заменяем вручную
            {
                int selStart = tb.SelectionStart;
                // заменяем выделение/вставляем на место каретки
                tb.SelectedText = sanitized;
                tb.CaretIndex = selStart + sanitized.Length;
                e.Handled = true; // сами вставили, WPF больше ничего делать не должен
                return;
            }

            // цифры и точку пропускаем штатно (WPF сам вставит)
            e.Handled = false;
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox tb) return;

            if (!e.SourceDataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var raw = e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;

            // Нормализуем: запятая -> точка, выбрасываем всё, кроме цифр и одной точки
            var sanitized = SanitizePaste(raw);

            var maxDec = GetMaxDecimalPlaces(tb);
            string candidate = ComposeCandidate(tb, sanitized);

            if (!IsValid(candidate, maxDec))
            {
                e.CancelCommand();
                return;
            }

            // Подменим вставляемый текст на "очищенный"
            e.DataObject = new DataObject(DataFormats.Text, sanitized);
        }

        private static string ComposeCandidate(TextBox tb, string incoming)
        {
            var text = tb.Text ?? string.Empty;

            int selStart = tb.SelectionStart;
            int selLen = tb.SelectionLength;

            // удаляем выделенный фрагмент
            string withoutSelection = selLen > 0
                ? text.Remove(selStart, selLen)
                : text;

            // вставляем ввод на позицию каретки (которая = SelectionStart)
            return withoutSelection.Insert(selStart, incoming);
        }

        private static bool IsValid(string s, int maxDecimalPlaces)
        {
            if (string.IsNullOrEmpty(s))
                return true; // разрешаем пустое (очистка поля)

            // Только цифры и не более одной точки
            if (!Regex.IsMatch(s, @"^\d*\.?\d*$"))
                return false;

            if (maxDecimalPlaces >= 0)
            {
                int dot = s.IndexOf('.');
                if (dot >= 0)
                {
                    int decimals = s.Length - dot - 1;
                    if (decimals > maxDecimalPlaces) return false;
                }
            }
            return true;
        }

        private static string SanitizePaste(string input)
        {
            input = input.Replace(',', '.');

            var sb = new StringBuilder(input.Length);
            bool dotSeen = false;

            foreach (var ch in input)
            {
                if (char.IsDigit(ch))
                {
                    sb.Append(ch);
                }
                else if (ch == '.' && !dotSeen)
                {
                    sb.Append('.');
                    dotSeen = true;
                }
                // игнорируем остальные символы
            }
            return sb.ToString();
        }
    }
}
