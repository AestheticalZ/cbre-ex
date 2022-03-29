﻿using CBRE.Editor.Documents;
using CBRE.Providers.Texture;
using CBRE.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CBRE.Editor.UI
{
    public sealed class TextureListPanel : Panel
    {
        public enum TextureSortOrder
        {
            None,
            Name,
            Width,
            Height,
            Size,
            Package
        }

        public delegate void TextureSelectedEventHandler(object sender, TextureItem item);
        public delegate void SelectionChangedEventHandler(object sender, IEnumerable<TextureItem> selection);

        public event TextureSelectedEventHandler TextureSelected;

        private void OnTextureSelected(TextureItem item)
        {
            if (TextureSelected != null)
            {
                TextureSelected(this, item);
            }
        }

        public event SelectionChangedEventHandler SelectionChanged;

        private void OnSelectionChanged(IEnumerable<TextureItem> selection)
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(this, selection);
            }
        }

        private readonly VScrollBar _scrollBar;

        private readonly List<TextureItem> _textures;
        private int _imageSize;

        private readonly List<Rectangle> _rectangles;

        private TextureSortOrder _sortOrder;
        private bool _sortDescending;
        private bool _allowSelection;
        private bool _allowMultipleSelection;
        private TextureItem _lastSelectedItem;
        private readonly List<TextureItem> _selection;

        #region Properties

        public bool AllowSelection
        {
            get { return _allowSelection; }
            set
            {
                _allowSelection = value;
                if (!_allowSelection && _selection.Count > 0)
                {
                    _selection.Clear();
                    Refresh();
                }
            }
        }

        public bool AllowMultipleSelection
        {
            get { return _allowMultipleSelection; }
            set
            {
                _allowMultipleSelection = value;
                if (!_allowMultipleSelection && _selection.Count > 0)
                {
                    TextureItem first = _selection.First();
                    _selection.Clear();
                    _selection.Add(first);
                    Refresh();
                }
            }
        }

        public int ImageSize
        {
            get { return _imageSize; }
            set
            {
                _imageSize = value;

                if (DocumentManager.CurrentDocument != null)
                {
                    IEnumerable<TexturePackage> packs = _textures.Select(t => t.Package).Distinct();
                    if (_streamSource != null) _streamSource.Dispose();
                    _streamSource = DocumentManager.CurrentDocument.TextureCollection.GetStreamSource(_imageSize, _imageSize, packs);
                }

                UpdateRectangles();
            }
        }

        public TextureSortOrder SortOrder
        {
            get { return _sortOrder; }
            set
            {
                _sortOrder = value;
                UpdateRectangles();
            }
        }

        public bool SortDescending
        {
            get { return _sortDescending; }
            set
            {
                _sortDescending = value;
                UpdateRectangles();
            }
        }

        public bool EnableDrag { get; set; }

        #endregion

        public TextureListPanel()
        {
            BackColor = Color.Black;
            VScroll = true;
            AutoScroll = true;
            DoubleBuffered = true;

            AllowSelection = true;
            AllowMultipleSelection = true;

            _scrollBar = new VScrollBar { Dock = DockStyle.Right };
            _scrollBar.ValueChanged += (sender, e) => Refresh();
            _textures = new List<TextureItem>();
            _selection = new List<TextureItem>();
            _imageSize = 128;

            _rectangles = new List<Rectangle>();

            Controls.Add(_scrollBar);

            UpdateRectangles();
        }

        #region Selection

        public void SetSelectedTextures(IEnumerable<TextureItem> items)
        {
            _selection.Clear();
            _selection.AddRange(items);
            OnSelectionChanged(_selection);
            Refresh();
        }

        public void ScrollToItem(TextureItem item)
        {
            int index = GetTextures().ToList().IndexOf(item);
            if (index < 0) return;

            Rectangle rec = _rectangles[index];
            int yscroll = Math.Max(0, Math.Min(rec.Top - 3, _scrollBar.Maximum - ClientRectangle.Height));
            _scrollBar.Value = yscroll;
            Refresh();
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (KeyboardState.Ctrl || KeyboardState.Shift || _selection.Count != 1) return;

            int x = e.X;
            int y = _scrollBar.Value + e.Y;

            int clickedIndex = GetIndexAt(x, y);

            TextureItem item = GetTextures().ElementAt(clickedIndex);
            if (item == _selection[0])
            {
                OnTextureSelected(_selection[0]);
            }
        }

        private bool _down;
        private Point _downPoint;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (!AllowSelection) return;
            if (!AllowMultipleSelection || !KeyboardState.Ctrl) _selection.Clear();

            if (e.Button == MouseButtons.Left)
            {
                _down = true;
                _downPoint = e.Location;
            }

            int x = e.X;
            int y = _scrollBar.Value + e.Y;

            int clickedIndex = GetIndexAt(x, y);

            TextureItem item = clickedIndex >= 0 && clickedIndex < _textures.Count ? GetTextures().ElementAt(clickedIndex) : null;

            if (item == null)
            {
                _selection.Clear();
            }
            else if (AllowMultipleSelection && KeyboardState.Ctrl && _selection.Contains(item))
            {
                _selection.Remove(item);
                _lastSelectedItem = null;
            }
            else if (AllowMultipleSelection && KeyboardState.Shift && _lastSelectedItem != null)
            {
                int bef = GetTextures().ToList().IndexOf(_lastSelectedItem);
                int start = Math.Min(bef, clickedIndex);
                int count = Math.Abs(clickedIndex - bef) + 1;
                _selection.AddRange(GetTextures().ToList().GetRange(start, count).Where(i => !_selection.Contains(i)));
            }
            else
            {
                _selection.Add(item);
                _lastSelectedItem = item;
            }
            OnSelectionChanged(_selection);

            Refresh();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_down && EnableDrag && _selection.Any() && (Math.Abs(e.X - _downPoint.X) > 2 || Math.Abs(e.Y - _downPoint.Y) > 2))
            {
                _down = false;
                DoDragDrop(_selection.ToList(), DragDropEffects.Copy);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) _down = false;
            base.OnMouseUp(e);
        }

        public int GetIndexAt(int x, int y)
        {
            int pad = 3,
                font = 4 + SystemFonts.MessageBoxFont.Height;
            for (int i = 0; i < _rectangles.Count; i++)
            {
                Rectangle rec = _rectangles[i];
                if (rec.Left - pad <= x
                    && rec.Right + pad >= x
                    && rec.Top - pad <= y
                    && rec.Bottom + pad + font >= y)
                {
                    return i;
                }
            }
            return -1;
        }

        #endregion

        #region Add/Remove/Get Textures

        public IEnumerable<TextureItem> GetTextures()
        {
            IEnumerable<TextureItem> sorted;
            switch (SortOrder)
            {
                case TextureSortOrder.None:
                    sorted = _textures;
                    break;
                case TextureSortOrder.Name:
                    sorted = _textures.OrderBy(x => x.Name);
                    break;
                case TextureSortOrder.Width:
                    sorted = _textures.OrderBy(x => x.Width).ThenBy(x => x.Name);
                    break;
                case TextureSortOrder.Height:
                    sorted = _textures.OrderBy(x => x.Height).ThenBy(x => x.Name);
                    break;
                case TextureSortOrder.Size:
                    sorted = _textures.OrderBy(x => x.Width * x.Height).ThenBy(x => x.Name);
                    break;
                case TextureSortOrder.Package:
                    sorted = _textures.OrderBy(x => x.ToString()).ThenBy(x => x.Name);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (_sortDescending) return sorted.Reverse();
            return sorted;
        }

        public IEnumerable<TextureItem> GetSelectedTextures()
        {
            return _selection;
        }

        private ITextureStreamSource _streamSource;

        public void RemoveAllTextures()
        {
            _textures.Clear();
            _lastSelectedItem = null;
            _selection.Clear();

            if (_streamSource != null) _streamSource.Dispose();
            _streamSource = null;

            OnSelectionChanged(_selection);
            UpdateRectangles();
        }

        public void SetTextureList(IEnumerable<TextureItem> textures)
        {
            _textures.Clear();
            _lastSelectedItem = null;
            _selection.Clear();
            _textures.AddRange(textures);

            IEnumerable<TexturePackage> packs = _textures.Select(t => t.Package).Distinct();
            if (_streamSource != null) _streamSource.Dispose();
            if (DocumentManager.CurrentDocument == null) _streamSource = null;
            else _streamSource = DocumentManager.CurrentDocument.TextureCollection.GetStreamSource(_imageSize, _imageSize, packs);

            OnSelectionChanged(_selection);
            UpdateRectangles();
        }

        public void Clear()
        {
            _textures.Clear();
            _lastSelectedItem = null;
            _selection.Clear();

            if (_streamSource != null) _streamSource.Dispose();
            _streamSource = null;

            OnSelectionChanged(_selection);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Clear();
            base.Dispose(disposing);
        }

        #endregion

        #region Scrolling

        private void ScrollByAmount(int value)
        {
            int newValue = _scrollBar.Value + value;
            _scrollBar.Value = newValue < 0 ? 0 : Math.Min(newValue, Math.Max(0, _scrollBar.Maximum - ClientRectangle.Height));
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            ScrollByAmount(_scrollBar.SmallChange * (e.Delta / -120));
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.PageDown:
                    ScrollByAmount(_scrollBar.LargeChange);
                    break;
                case Keys.PageUp:
                    ScrollByAmount(-_scrollBar.LargeChange);
                    break;
                case Keys.End:
                    ScrollByAmount(int.MaxValue);
                    break;
                case Keys.Home:
                    ScrollByAmount(-int.MaxValue);
                    break;
                case Keys.Enter:
                    if (_selection.Count > 0) OnTextureSelected(_selection[0]);
                    break;
            }
            base.OnKeyDown(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            Focus();
            base.OnMouseEnter(e);
        }

        #endregion

        #region Updating Rectangles & Dimensions

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateRectangles();
        }

        private void UpdateRectangles()
        {
            int w = ClientRectangle.Width - _scrollBar.Width,
                pad = 3,
                font = 4 + SystemFonts.MessageBoxFont.Height,
                cx = 0,
                cy = 0,
                my = 0;
            _rectangles.Clear();
            foreach (TextureItem ti in GetTextures())
            {
                int rw = w - cx;
                int wid = (_imageSize > 0 ? _imageSize : ti.Width) + pad + pad;
                int hei = (_imageSize > 0 ? _imageSize : ti.Height) + pad + pad + font;
                if (rw < wid)
                {
                    // New row
                    cx = 0;
                    cy += my;
                    my = 0;
                }
                my = Math.Max(my, hei);
                Rectangle rect = new Rectangle(cx + pad, cy + pad, wid - pad - pad, hei - pad - pad - font);
                _rectangles.Add(rect);
                cx += wid;
            }
            _scrollBar.Maximum = cy + my;
            _scrollBar.SmallChange = (_imageSize > 0 ? _imageSize : 128) + pad + pad + font;
            _scrollBar.LargeChange = ClientRectangle.Height;

            if (_scrollBar.Value > _scrollBar.Maximum - ClientRectangle.Height)
            {
                _scrollBar.Value = Math.Max(0, _scrollBar.Maximum - ClientRectangle.Height);
            }

            Refresh();
        }

        #endregion

        #region Rendering

        private Dictionary<TextureItem, BitmapRef> _renderCache = new Dictionary<TextureItem, BitmapRef>();

        private void UpdateCacheableItems(int y, int height)
        {
            if (_streamSource == null) return;

            List<TextureItem> texs = GetTextures().ToList();
            List<TextureItem> cacheable = new List<TextureItem>();
            for (int i = 0; i < texs.Count; i++)
            {
                Rectangle rec = _rectangles[i];
                if (rec.Bottom < y) continue;
                if (rec.Top > y + height) break;
                cacheable.Add(texs[i]);
            }
            foreach (TextureItem ti in _renderCache.Keys.ToList())
            {
                if (!cacheable.Contains(ti))
                {
                    //if (_renderCache[ti] != null) _renderCache[ti].Dispose();
                    _renderCache.Remove(ti);
                }
            }
            foreach (TextureItem ti in cacheable)
            {
                if (!_renderCache.ContainsKey(ti))
                {
                    _renderCache.Add(ti, null);
                }
            }

            Parallel.ForEach(cacheable, item =>
            {
                if (_streamSource == null) return;
                BitmapRef img = _streamSource.GetImage(item);
                if (img == null) return;
                if (_renderCache.ContainsKey(item)) _renderCache[item] = img;
                //else img.Dispose();
            });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            RenderTextures(e.Graphics);
        }

        public void RenderTextures(System.Drawing.Graphics g)
        {
            if (_textures.Count == 0 || DocumentManager.CurrentDocument == null) return;

            int y = _scrollBar.Value;
            int height = ClientRectangle.Height;
            UpdateCacheableItems(y, height);

            List<TextureItem> texs = GetTextures().ToList();
            for (int i = 0; i < texs.Count; i++)
            {
                Rectangle rec = _rectangles[i];
                if (rec.Bottom < y) continue;
                if (rec.Top > y + height) break;
                TextureItem tex = texs[i];

                if (!_renderCache.ContainsKey(tex)) continue;
                BitmapRef bmp = _renderCache[tex];
                if (bmp == null) continue;

                DrawImage(g, bmp.Bitmap, tex, rec.X, rec.Y - y, rec.Width, rec.Height);
            }
        }

        private void DrawImage(System.Drawing.Graphics g, Image bmp, TextureItem ti, int x, int y, int w, int h)
        {
            if (bmp == null) return;

            int iw = bmp.Width;
            int ih = bmp.Height;
            if (iw > w && iw >= ih)
            {
                ih = (int)Math.Floor(h * (ih / (float)iw));
                iw = w;
            }
            else if (ih > h)
            {
                iw = (int)Math.Floor(w * (iw / (float)ih));
                ih = h;
            }
            g.FillRectangle(System.Drawing.Brushes.Black, x - 3, y - 3, w + 6, h + 10 + SystemFonts.MessageBoxFont.Height);

            g.DrawImage(bmp, x, y, iw, ih);
            if (_selection.Contains(ti))
            {
                g.DrawRectangle(Pens.Red, x - 1, y - 1, w + 2, h + 2);
                g.DrawRectangle(Pens.Red, x - 2, y - 2, w + 4, h + 4);
            }
            else
            {
                g.DrawRectangle(Pens.Gray, x - 2, y - 2, w + 4, h + 4);
            }
            g.DrawString(ti.Name, SystemFonts.MessageBoxFont, System.Drawing.Brushes.White, x - 2, y + h + 3);
        }

        #endregion
    }
}
