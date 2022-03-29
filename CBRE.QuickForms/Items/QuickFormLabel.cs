﻿using System.Collections.Generic;
using System.Windows.Forms;

namespace CBRE.QuickForms.Items
{
    /// <summary>
    /// An item that just displays text.
    /// </summary>
    public class QuickFormLabel : QuickFormItem
    {
        public QuickFormLabel(string text)
        {
            Name = text;
        }

        public override List<Control> GetControls(QuickForm qf)
        {
            List<Control> controls = new List<Control>();
            Label l = new Label { Text = Name };
            Anchor(l);
            Location(l, qf, true);
            Size(l, qf, 0);
            TextAlign(l);
            controls.Add(l);
            return controls;
        }
    }
}
