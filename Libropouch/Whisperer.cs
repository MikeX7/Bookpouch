using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListBox = System.Windows.Controls.ListBox;
using TextBox = System.Windows.Controls.TextBox;


namespace Libropouch
{
     class Whisperer
    {
         public List<string> HintList = new List<string>(); //List of possible phrases offered in the whisperer
         private TextBox _textBox; //Text element for which the whisperer is used

         public TextBox TextBox 
         {
             set
             {
                 _textBox = value;
                 value.KeyUp -= TextBox_KeyUp;
                 value.KeyUp += TextBox_KeyUp;
             }

             get { return _textBox; }
         } 

         private Popup _popup;

         public Whisperer()
         {             
             //TextBox.KeyUp += (object sender, KeyEventArgs e) => { };
             
         }

         public void TextBox_KeyUp(object sender, KeyEventArgs e)
         {
             Pop();

             if (e.Key == Key.Down)
                 Focus();

             if (e.Key == Key.Enter)
                 SelectHint(sender, e);
         }

        public void Pop() //Display the whisperer popup under an element
        {
            //_lastPopup.Parent.

            var listBox = new ListBox(); 
            
            var combedList = HintList.Where(hl => hl.StartsWith(TextBox.Text));

            if (!combedList.Any() && _popup != null)
            {
                _popup.IsOpen = false;                
                return;
            }

            foreach (var b in combedList)
            {
                listBox.Items.Add(new ListBoxItem {Content = b});
            }

            listBox.SelectedIndex = 0;  

            //TextBox.KeyUp -= InlineHint; //Try to remove event handlers, before adding them, to make sure they aren't added multiple times
            listBox.KeyUp -= SelectHint;

            //TextBox.KeyUp += InlineHint;     
            listBox.KeyUp += SelectHint;                          

            if (_popup != null)
            {
                _popup.IsOpen = true;     
                _popup.Child = listBox;
                _popup.PlacementTarget = TextBox;
            }
            else
            {
                _popup = new Popup
                {
                    Name = "Whisperer",
                    PlacementTarget = TextBox,
                    PopupAnimation = PopupAnimation.None,
                    IsOpen = true,
                    StaysOpen = false,
                    Child = listBox,
                    MinWidth = TextBox.Width
                    
                };
                
            }
        }        

         public void Focus() //Switch focus from the textbox to the whisperer listbox item
        {
            var listBox = (ListBox) _popup.Child;
            listBox.Focus();
            listBox.SelectedIndex = (listBox.Items.Count > 0 ? 1 : 0);            
            SendKeys.SendWait("{DOWN}");
            ((ListBoxItem) listBox.SelectedItem).Focus();                       
        }

        private  void SelectHint(object sender, KeyEventArgs e) //Take selected hint from the whisperer popup and place it into the textbox
        {            
            if (e.Key != Key.Enter)
                return;

            var listBox = (ListBox) _popup.Child;
            var hint =  (string) ((ListBoxItem) listBox.SelectedItem).Content;

            TextBox.Text = hint;
            TextBox.Select(hint.Length, 0);
            TextBox.Focus();

            _popup.IsOpen = false;
        }

        private  void InlineHint(object sender, KeyEventArgs e) //Take the first whisperer sugestion and display it in the text box, in a form of selected text appended behind the text cursor
        {            
            if (e.Key == Key.Back || e.Key == Key.Delete)
                return;

            var textBox = (TextBox) sender;
            var hint = HintList.FirstOrDefault(hl => hl.StartsWith(textBox.Text));

            if(hint == null)
                return;

            var textLen = textBox.GetLineLength(0);

            if (textLen < 3)
                return;

            textBox.AppendText(hint.Substring(textLen)); 
            textBox.Select(textLen, (textBox.GetLineLength(0) - textLen));
        }
    }
}
