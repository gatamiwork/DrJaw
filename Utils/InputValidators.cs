using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DrJaw.Utils
{
    public static class InputValidators
    {
        public static void AttachNumericValidation(TextBox textBox)
        {
            textBox.PreviewTextInput += TextBox_PreviewTextInput;
            textBox.TextChanged += TextBox_TextChanged;
            textBox.PreviewKeyDown += TextBox_PreviewKeyDown;

            DataObject.AddPastingHandler(textBox, OnPaste);
        }

        private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb) return;

            // Заменить точку на запятую
            if (e.Text == ".")
            {
                e.Handled = true;
                InsertTextAtCaret(tb, ",");
                return;
            }

            // Разрешить только цифры и одну запятую
            if (char.IsDigit(e.Text, 0))
            {
                e.Handled = false;
            }
            else if (e.Text == "," && !tb.Text.Contains(","))
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox tb) return;

            string original = tb.Text;
            string filtered = Regex.Replace(original.Replace('.', ','), @"[^0-9,]", "");

            // Оставить только одну запятую
            int firstComma = filtered.IndexOf(',');
            if (firstComma != -1)
            {
                filtered = filtered.Substring(0, firstComma + 1) + filtered.Substring(firstComma + 1).Replace(",", "");
            }

            if (filtered != original)
            {
                int caret = tb.CaretIndex;
                tb.Text = filtered;
                tb.CaretIndex = Math.Min(caret, filtered.Length);
            }
        }

        private static void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Разрешаем клавиши навигации и удаления
            if (e.Key is Key.Back or Key.Delete or Key.Left or Key.Right or Key.Tab or Key.Enter)
                e.Handled = false;
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox tb) return;

            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string pastedText = e.DataObject.GetData(DataFormats.Text) as string ?? "";
                string newText = (tb.Text ?? "").Insert(tb.CaretIndex, pastedText);

                // Проверка — если некорректно, отменить вставку
                if (!Regex.IsMatch(newText.Replace('.', ','), @"^\d*(,\d*)?$"))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private static void InsertTextAtCaret(TextBox textBox, string text)
        {
            int caretIndex = textBox.CaretIndex;
            textBox.Text = textBox.Text.Insert(caretIndex, text);
            textBox.CaretIndex = caretIndex + text.Length;
        }
    }
}
