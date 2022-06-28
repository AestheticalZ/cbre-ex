﻿using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CBRE.Editor.UI
{
    public class ClosableTabControl : TabControl
    {
        public delegate void RequestCloseEventHandler(object sender, int index);

        public event RequestCloseEventHandler RequestClose;

        private void OnRequestClose(int index)
        {
            if (RequestClose != null)
            {
                RequestClose(this, index);
            }
        }

        public ClosableTabControl()
        {
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            ImageList = new ImageList();
            ImageList.Images.Add(new Bitmap(8, 8));

            TabPages.Add("Test 1");
            TabPages.Add("Test 2");
            TabPages.Add("Test 3");
            TabPages.Add("Test 4");
            TabPages.Add("Test 5");
            TabPages.Add("Test 6");
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Render(e.Graphics);
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            ((TabPage)e.Control).ImageIndex = 0;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Middle) return;
            for (int i = 0; i < TabPages.Count; i++)
            {
                Rectangle rect = e.Button == MouseButtons.Left ? GetCloseRect(i) : GetTabRect(i);
                if (!rect.Contains(e.Location)) continue;
                OnRequestClose(i);
                break;
            }
        }

        private const int WM_NULL = 0x0;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_LBUTTONDOWN = 0x0201;

        protected override void WndProc(ref Message m)
        {
            if (!DesignMode && (m.Msg == WM_LBUTTONDOWN || m.Msg == WM_MBUTTONDOWN))
            {
                Point pt = PointToClient(Cursor.Position);
                for (int i = 0; i < TabPages.Count; i++)
                {
                    Rectangle rect = m.Msg == WM_LBUTTONDOWN ? GetCloseRect(i) : GetTabRect(i);
                    if (!rect.Contains(pt)) continue;
                    m.Msg = WM_NULL;
                    OnRequestClose(i);
                    break;
                }
            }
            base.WndProc(ref m);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Invalidate();
        }

        private void Render(System.Drawing.Graphics g)
        {
            if (!Visible) return;
            using (SolidBrush b = new SolidBrush(BackColor))
            {
                g.FillRectangle(b, ClientRectangle);
            }
            Rectangle display = new Rectangle(DisplayRectangle.Location, DisplayRectangle.Size);
            if (TabPages.Count == 0) display.Y += 21;
            int border = SystemInformation.Border3DSize.Width;
            display.Inflate(border, border);
            g.DrawLine(SystemPens.ControlDark, display.X, display.Y, display.X + display.Width, display.Y);
            Region clip = g.Clip;
            g.SetClip(new Rectangle(display.Left, ClientRectangle.Top, display.Width, ClientRectangle.Height));
            for (int i = 0; i < TabPages.Count; i++)
            {
                RenderTab(g, i);
            }
            g.Clip = clip;
        }

        private void RenderTab(System.Drawing.Graphics g, int index)
        {
            Rectangle rect = GetTabRect(index);
            Rectangle closeRect = GetCloseRect(index);
            bool selected = SelectedIndex == index;
            TabPage tab = TabPages[index];

            Point[] points = new[]
            {
                new Point(rect.Left, rect.Bottom),
                new Point(rect.Left, rect.Top),
                new Point(rect.Right, rect.Top),
                new Point(rect.Right, rect.Bottom),
                new Point(rect.Left, rect.Bottom)
            };

            Point[] pointsUnselected = new[]
            {
                new Point(rect.Left, rect.Bottom),
                new Point(rect.Left, rect.Top + 2),
                new Point(rect.Right, rect.Top + 2),
                new Point(rect.Right, rect.Bottom),
                new Point(rect.Left, rect.Bottom)
            };

            // Background
            Point p = PointToClient(MousePosition);
            bool hoverClose = closeRect.Contains(p);
            bool hover = rect.Contains(p);
            Color backColour = tab.BackColor;
            if (selected) backColour = ControlPaint.Light(backColour, 1);
            else if (hover) backColour = ControlPaint.Light(backColour, 0.8f);
            using (SolidBrush b = new SolidBrush(backColour))
            {
                g.FillPolygon(b, selected ? points : pointsUnselected);
            }

            // Border
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.DrawPolygon(SystemPens.ControlDark, selected ? points : pointsUnselected);
            if (selected)
            {
                using (Pen pen = new Pen(tab.BackColor))
                {
                    g.DrawLine(pen, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
                }
            }

            // Text
            int textWidth = (int)g.MeasureString(tab.Text, Font).Width;
            int textLeft = rect.X + 5;
            int textRight = rect.Right;
            int offset = selected ? 2 : 4;
            Rectangle textRect = new Rectangle(textLeft, rect.Y + offset, rect.Width - 26, rect.Height - 5);
            using (SolidBrush b = new SolidBrush(tab.ForeColor))
            {
                //g.FillRectangle(SystemBrushes.ControlDarkDark, textRect);
                g.DrawString(tab.Text, Font, b, textRect);
            }

            // Close icon
            int borderOffset = selected ? 1 : 3;
            int crossOffset = selected ? 0 : 2;
            using (Pen pen = new Pen(tab.ForeColor))
            {
                if (hoverClose)
                {
                    g.DrawRectangle(pen, closeRect.Left + 1, closeRect.Top + borderOffset, closeRect.Width - 2, closeRect.Height - 2);
                }
                const int padding = 5;
                g.DrawLine(pen, closeRect.Left + padding, closeRect.Top + crossOffset + padding, closeRect.Right - padding, closeRect.Bottom + crossOffset - padding);
                g.DrawLine(pen, closeRect.Right - padding, closeRect.Top + crossOffset + padding, closeRect.Left + padding, closeRect.Bottom + crossOffset - padding);
            }
        }

        private Rectangle GetCloseRect(int index)
        {
            Rectangle rect = GetTabRect(index);
            return new Rectangle(rect.Right - 20, rect.Top + (rect.Height - 16) / 2, 16, 16);
        }
    }
}
