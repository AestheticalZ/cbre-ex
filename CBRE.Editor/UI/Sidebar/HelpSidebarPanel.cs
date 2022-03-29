﻿using CBRE.Common.Mediator;
using CBRE.Editor.Tools;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CBRE.Editor.UI.Sidebar
{
    public partial class HelpSidebarPanel : UserControl, IMediatorListener
    {
        public HelpSidebarPanel()
        {
            InitializeComponent();

            Mediator.Subscribe(EditorMediator.ContextualHelpChanged, this);
            Mediator.Subscribe(EditorMediator.ToolSelected, this);
        }

        private void UpdateHelp()
        {
            string help = "";
            if (ToolManager.ActiveTool != null) help = ToolManager.ActiveTool.GetContextualHelp();
            HelpTextBox.ResetFont();
            string rtf = ConvertSimpleMarkdownToRtf(help);
            HelpTextBox.Rtf = rtf;
            System.Drawing.Size size = TextRenderer.MeasureText(HelpTextBox.Text, HelpTextBox.Font, HelpTextBox.Size, TextFormatFlags.TextBoxControl | TextFormatFlags.WordBreak);
            Height = size.Height + HelpTextBox.Margin.Vertical + HelpTextBox.Lines.Length * 5;
        }

        public void Notify(string message, object data)
        {
            Mediator.ExecuteDefault(this, message, data);
        }

        private void ContextualHelpChanged()
        {
            UpdateHelp();
        }

        private void ToolSelected()
        {
            UpdateHelp();
        }

        /// <summary>
        /// Converts simple markdown into RTF.
        /// Simple markdown is a very limited subset of markdown. It supports:
        /// - Lists, delimited with -
        /// - Bold, delimited with *
        /// - Paragraphs/new lines
        /// </summary>
        /// <param name="simpleMarkdown"></param>
        private string ConvertSimpleMarkdownToRtf(string simpleMarkdown)
        {
            /*
             * {\rtf1\utf8\f0\pard
             *   This is some {\b bold} text.\par
             * }";
             */
            string escaped = simpleMarkdown
                .Replace("\\", "\\\\")
                .Replace("{", "\\{")
                .Replace("}", "\\}");

            StringBuilder sb = new StringBuilder();
            foreach (char c in escaped)
            {
                if (c > 127) sb.AppendFormat(@"\u{0}?", (int)c);
                else if (c == '\\') sb.Append("\\\\");
                else if (c == '{') sb.Append("\\{");
                else if (c == '}') sb.Append("\\}");
                else sb.Append(c);
            }

            string bolded = Regex.Replace(sb.ToString(), @"\*(?:\b(?=\w)|(?=\\))(.*?)\b(?!\w)\*", @"{\b $1}");
            string bulleted = Regex.Replace(bolded, @"^\s*-\s+", @" \bullet  ", RegexOptions.Multiline);
            string paragraphs = Regex.Replace(bulleted, @"(\r?\n){2,}", "\\par\\par ");
            string lines = Regex.Replace(paragraphs, @"(\r?\n)+", "\\par ");

            return @"{\rtf1\ansi\f0\pard\sa60 " + lines + " }";
        }
    }
}
