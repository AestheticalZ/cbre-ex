using CBRE.DataStructures.GameData;
using CBRE.Editor.Properties;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CBRE.Editor.UI.ObjectProperties.SmartEdit
{
    [SmartEdit(VariableType.Color255)]
    internal class SmartEditColor255 : SmartEditControl
    {
        private readonly TextBox _textBox;
        public SmartEditColor255()
        {
            _textBox = new TextBox { Width = 200 };
            _textBox.TextChanged += (sender, e) => OnValueChanged();
            Controls.Add(_textBox);

            Button btn = new Button { Image = Resources.Button_ColourPicker, Text = "", Margin = new Padding(1), Width = 24, Height = 24 };
            btn.Click += OpenColorPicker;
            Controls.Add(btn);
        }

        private void OpenColorPicker(object sender, EventArgs e)
        {
            string[] spl = _textBox.Text.Split(' ');
            int r = 0, g = 0, b = 0;
            if (spl.Length >= 3)
            {
                int.TryParse(spl[0], out r);
                int.TryParse(spl[1], out g);
                int.TryParse(spl[2], out b);
            }
            using (ColorDialog cd = new ColorDialog { Color = Color.FromArgb(r, g, b) })
            {
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    r = cd.Color.R;
                    g = cd.Color.G;
                    b = cd.Color.B;
                    if (spl.Length < 3) spl = new string[3];
                    spl[0] = r.ToString();
                    spl[1] = g.ToString();
                    spl[2] = b.ToString();
                    _textBox.Text = String.Join(" ", spl);
                }
            }
        }

        protected override string GetName()
        {
            return OriginalName;
        }

        protected override string GetValue()
        {
            return _textBox.Text;
        }

        protected override void OnSetProperty()
        {
            _textBox.Text = PropertyValue;
        }
    }
}