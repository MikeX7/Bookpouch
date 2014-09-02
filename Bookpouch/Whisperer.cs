using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListBox = System.Windows.Controls.ListBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace Bookpouch
{
    /// <summary>
    ///     Display a list of suggestions based on a text typed in a bonded text field
    /// </summary>
    public class Whisperer
    {
        private readonly ListBox _listBox = new ListBox();
        public List<string> HintList = new List<string>(); //List of possible phrases offered in the whisperer
        public Window Parent;
        private Popup _popup;
        private TextBox _textBox; //Text element for which the whisperer is used

        public TextBox TextBox
        {
            set
            {
                _textBox = value;

                value.KeyUp += TextBox_KeyUp;
                value.PreviewMouseLeftButtonUp += TextBox_OnPreviewMouseLeftButtonUp;
                value.LostFocus += TextBox_LostFocus;
            }

            get { return _textBox; }
        }

        private void ListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox_LostFocus(sender, e);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_popup != null && _popup.IsKeyboardFocusWithin == false)
                _popup.IsOpen = false;
        }

        public void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            Pop();

            switch (e.Key)
            {
                case Key.Down:
                    Focus();
                    break;
                case Key.Enter:
                    SelectHint();
                    break;
                case Key.Escape:
                    _popup.IsOpen = false;
                    break;
            }
        }

        public void ListBox_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SelectHint();
        }

        public void ListBox_OnMouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            SelectHint();
        }

        private void TextBox_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Pop();
        }

        /// <summary>
        ///     Display the whisperer popup under an element
        /// </summary>
        public void Pop() 
        {
            var combedList =
                HintList.Distinct()
                    .Where(hl => hl.ToLower().StartsWith(TextBox.Text.ToLower()) && hl.Trim() != String.Empty)
                    .Take(Properties.Settings.Default.WhispererMaxItems)
                    .ToArray();

            if (!combedList.Any())
            {
                if (_popup != null)
                    _popup.IsOpen = false;

                return;
            }

            _listBox.Items.Clear();
            foreach (var b in combedList)
            {
                _listBox.Items.Add(new ListBoxItem {Content = b});
            }

            _listBox.SelectedIndex = 0;

            //TextBox.KeyUp += InlineHint;     


            if (_popup != null)
            {
                _popup.IsOpen = true;
                _popup.Child = _listBox;
            }
            else
            {
                _listBox.KeyUp += ListBox_OnKeyUp;
                _listBox.MouseLeftButtonUp += ListBox_OnMouseLeftButtonUp;
                _listBox.LostFocus += ListBox_LostFocus;

                _popup = new Popup
                {
                    Name = "Whisperer",
                    Placement = PlacementMode.Bottom,
                    PlacementTarget = TextBox,
                    PopupAnimation = PopupAnimation.None,
                    IsOpen = true,
                    StaysOpen = false,
                    Child = _listBox,
                    MinWidth = TextBox.Width
                };
            }
        }

        /// <summary>
        ///     Switch focus from the textbox to the whisperer listbox item
        /// </summary>
        public void Focus() 
        {
            var listBox = (ListBox) _popup.Child;

            if (!listBox.IsVisible)
                return;

            listBox.Focus();
            listBox.SelectedIndex = (listBox.Items.Count > 0 ? 1 : 0);
                //Since the first whisperer suggestion can be always accessed with enter (even when the whisperer listobx is unfocused), skip to the second  suggestion (if it exists) when down arrow is pressed 
            SendKeys.SendWait("{DOWN}");
            ((ListBoxItem) listBox.SelectedItem).Focus();
        }

        private void SelectHint() //Take selected hint from the whisperer popup and place it into the textbox
        {
            var listBox = (ListBox) _popup.Child;
            var hint = (string) ((ListBoxItem) listBox.SelectedItem).Content;

            TextBox.Text = hint;
            TextBox.Select(hint.Length, 0);
            TextBox.Focus();

            _popup.IsOpen = false;
        }

        private void InlineHint(object sender, KeyEventArgs e)
            //Take the first whisperer suggestion and display it in the text box, in a form of selected text appended behind the text cursor
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
                return;

            var textBox = (TextBox) sender;
            var hint = HintList.FirstOrDefault(hl => hl.StartsWith(textBox.Text));

            if (hint == null)
                return;

            var textLen = textBox.GetLineLength(0);

            if (textLen < 3)
                return;

            textBox.AppendText(hint.Substring(textLen));
            textBox.Select(textLen, (textBox.GetLineLength(0) - textLen));
        }
    }
}