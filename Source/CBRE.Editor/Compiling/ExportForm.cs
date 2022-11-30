﻿using CBRE.Editor.Documents;
using CBRE.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace CBRE.Editor.Compiling
{
    public partial class ExportForm : Form
    {
        public Document Document;
        
        private readonly List<string> _GenericExtensions = new List<string>()
        {
            ".fbx",
            ".obj",
            ".dae",
            ".stl",
            ".ply"
        };

        public ExportForm()
        {
            InitializeComponent();
        }

        private void textureDims_ValueChanged(object sender, EventArgs e)
        {
            LightmapConfig.TextureDims = (int)textureDims.Value;
        }

        private void blurRadius_ValueChanged(object sender, EventArgs e)
        {
            LightmapConfig.BlurRadius = (int)blurRadius.Value;
        }

        private void downscaleFactor_ValueChanged(object sender, EventArgs e)
        {
            LightmapConfig.DownscaleFactor = (int) downscaleFactor.Value;
        }

        private void threadCount_ValueChanged(object sender, EventArgs e)
        {
            LightmapConfig.MaxThreadCount = (int)threadCount.Value;
        }

        private string SaveFileName = "";
        private void render_Click(object sender, EventArgs e)
        {
            if (LightmapConfig.BakeModelShadows)
            {
                DialogResult result = MessageBox.Show("Baking model shadows will SEVERELY impact lightmapping performance, " +
                                                      "especially if there are lots of models in your room.\n" +
                                                      "It's recommended to leave it off unless you're exporting the room.\n" +
                                                      "Are you sure you want to continue?",
                    "Warning!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    return;
                }
            }

            ProgressLog.Text = "Rendering lightmap";
            ProgressBar.Enabled = true;

            actionThread = new Thread(() => { PerformAction(false, LightmapConfig.ViewAfterExport); }) { CurrentCulture = CultureInfo.InvariantCulture };
            actionThread.Start();
        }

        private void export_Click(object sender, EventArgs e)
        {
            if (LightmapConfig.BakeModelShadows)
            {
                DialogResult result = MessageBox.Show("Baking model shadows will SEVERELY impact lightmapping performance, " +
                                                      "especially if there are lots of models in your room.\n" +
                                                      "It's recommended to leave it off unless you're exporting the room.\n" +
                                                      "Are you sure you want to continue?",
                    "Warning!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    return;
                }
            }

            using (SaveFileDialog save = new SaveFileDialog())
            {
                string filter = "";
#if RM2
                filter += "SCP-CB v1.4 RM2 (*.rm2)|*.rm2|";
#endif
                filter += "SCP-CB v1.3.11 RMesh (*.rmesh)|*.rmesh";
                filter += "|Autodesk Filmbox (*.fbx)|*.fbx";
                filter += "|Wavefront Object (*.obj)|*.obj";
                filter += "|Stereolithography (*.stl)|*.stl";
                filter += "|Stanford Polygon Library (*.ply)|*.ply";
                save.Filter = filter;
                if (save.ShowDialog() == DialogResult.OK)
                {
                    SaveFileName = save.FileName;

                    ProgressLog.Text = "Exporting to " + save.FileName;
                    ProgressBar.Enabled = true;

                    actionThread = new Thread(() => { PerformAction(true, LightmapConfig.ViewAfterExport); }) { CurrentCulture = CultureInfo.InvariantCulture };
                    actionThread.Start();
                }
            }
        }

        private void ambientRed_LostFocus(object sender, EventArgs e)
        {
            int r = -1; int.TryParse(((TextBox)sender).Text, out r);
            if (r >= 0 && r <= 255)
            {
                LightmapConfig.AmbientColorR = r;
                ambientColorBox.BackColor = Color.FromArgb(LightmapConfig.AmbientColorR, LightmapConfig.AmbientColorG, LightmapConfig.AmbientColorB);
            }
            else
            {
                ((TextBox)sender).Text = LightmapConfig.AmbientColorR.ToString();
            }
        }

        private void ambientGreen_LostFocus(object sender, EventArgs e)
        {
            int g = -1; int.TryParse(((TextBox)sender).Text, out g);
            if (g >= 0 && g <= 255)
            {
                LightmapConfig.AmbientColorG = g;
                ambientColorBox.BackColor = Color.FromArgb(LightmapConfig.AmbientColorR, LightmapConfig.AmbientColorG, LightmapConfig.AmbientColorB);
            }
            else
            {
                ((TextBox)sender).Text = LightmapConfig.AmbientColorG.ToString();
            }
        }

        private void ambientBlue_LostFocus(object sender, EventArgs e)
        {
            int b = -1; int.TryParse(((TextBox)sender).Text, out b);
            if (b >= 0 && b <= 255)
            {
                LightmapConfig.AmbientColorB = b;
                ambientColorBox.BackColor = Color.FromArgb(LightmapConfig.AmbientColorR, LightmapConfig.AmbientColorG, LightmapConfig.AmbientColorB);
            }
            else
            {
                ((TextBox)sender).Text = LightmapConfig.AmbientColorB.ToString();
            }
        }

        private void ambientRed_TextChanged(object sender, EventArgs e)
        {
            int r = -1; int.TryParse(((TextBox)sender).Text, out r);
            if (r >= 0 && r <= 255)
            {
                LightmapConfig.AmbientColorR = r;
                ambientColorBox.BackColor = Color.FromArgb(LightmapConfig.AmbientColorR, LightmapConfig.AmbientColorG, LightmapConfig.AmbientColorB);
            }
        }

        private void ambientGreen_TextChanged(object sender, EventArgs e)
        {
            int g = -1; int.TryParse(((TextBox)sender).Text, out g);
            if (g >= 0 && g <= 255)
            {
                LightmapConfig.AmbientColorG = g;
                ambientColorBox.BackColor = Color.FromArgb(LightmapConfig.AmbientColorR, LightmapConfig.AmbientColorG, LightmapConfig.AmbientColorB);
            }
        }

        private void ambientBlue_TextChanged(object sender, EventArgs e)
        {
            int b = -1; int.TryParse(((TextBox)sender).Text, out b);
            if (b >= 0 && b <= 255)
            {
                LightmapConfig.AmbientColorB = b;
                ambientColorBox.BackColor = Color.FromArgb(LightmapConfig.AmbientColorR, LightmapConfig.AmbientColorG, LightmapConfig.AmbientColorB);
            }
        }

        private void ambientColorBox_Click(object sender, EventArgs e)
        {
            if (ambientRed.Enabled)
            {
                using (ColorDialog cb = new System.Windows.Forms.ColorDialog())
                {
                    if (cb.ShowDialog() == DialogResult.OK)
                    {
                        LightmapConfig.AmbientColorR = cb.Color.R;
                        LightmapConfig.AmbientColorG = cb.Color.G;
                        LightmapConfig.AmbientColorB = cb.Color.B;
                        ambientColorBox.BackColor = Color.FromArgb(LightmapConfig.AmbientColorR, LightmapConfig.AmbientColorG, LightmapConfig.AmbientColorB);
                        ambientRed.Text = LightmapConfig.AmbientColorR.ToString();
                        ambientGreen.Text = LightmapConfig.AmbientColorG.ToString();
                        ambientBlue.Text = LightmapConfig.AmbientColorB.ToString();
                    }
                }
            }
        }
        
        private void viewAfterCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            LightmapConfig.ViewAfterExport = viewAfterCheckbox.Checked;
        }

        private void modelBakeYes_CheckedChanged(object sender, EventArgs e)
        {
            LightmapConfig.BakeModelShadows = modelBakeYes.Checked;
        }

        private void SetCancelEnabled(bool enabled)
        {
            Invoke((MethodInvoker)(() =>
            {
                textureDims.Enabled = !enabled;
                downscaleFactor.Enabled = !enabled;
                blurRadius.Enabled = !enabled;
                threadCount.Enabled = !enabled;

                ambientRed.Enabled = !enabled;
                ambientGreen.Enabled = !enabled;
                ambientBlue.Enabled = !enabled;

                modelBakeYes.Enabled = !enabled;
                modelBakeNo.Enabled = !enabled;

                render.Enabled = !enabled;
                export.Enabled = !enabled;
                cancel.Enabled = enabled;
                viewAfterCheckbox.Enabled = !enabled;

                ProgressBar.Enabled = enabled;
            }));
        }
        
        Thread actionThread = null;
        private void PerformAction(bool export, bool viewAfterwards)
        {
            try
            {
                SetCancelEnabled(true);
                if (export)
                {
                    string extension = System.IO.Path.GetExtension(SaveFileName);
                    if (extension.Equals(".rm2", StringComparison.OrdinalIgnoreCase))
                    {
                        RM2Export.SaveToFile(SaveFileName, Document, this);
                    }
                    else if (extension.Equals(".rmesh", StringComparison.OrdinalIgnoreCase))
                    {
                        RMeshExport.SaveToFile(SaveFileName, Document, this);
                    }
                    else if (_GenericExtensions.Contains(extension.ToLower()))
                    {
                        GenericExport.SaveToFile(SaveFileName, Document, this, extension.Substring(1));
                    }
                    else
                    {
                        throw new Exception($"Unknown file extension ({extension})");
                    }

                    if (viewAfterwards)
                    {
                        string fullOutputPath = System.IO.Path.GetFullPath(SaveFileName);
                        
                        //explorer.exe /select,"path"
                        string explorerArguments = string.Format("/select,\"{0}\"", fullOutputPath);

                        Process.Start("explorer.exe", explorerArguments);
                    }
                }
                else
                {
                    Lightmap.Lightmapper.Render(Document, this, out _, out _);
                }
            }
            catch (ThreadAbortException)
            {
                foreach (Thread thread in (Lightmap.Lightmapper.FaceRenderThreads ?? Enumerable.Empty<Thread>()))
                {
                    if (thread.IsAlive)
                    {
                        thread.Abort();
                    }
                }

                ProgressLog.Invoke((MethodInvoker)(() => ProgressLog.AppendText("\nCancelled by the user")));
                ProgressBar.Invoke((MethodInvoker)(() => ProgressBar.Value = 0));
            }
            catch (Exception e)
            {
                ProgressLog.Invoke((MethodInvoker)(() =>
                {
                    ProgressLog.SelectionStart = ProgressLog.TextLength;
                    ProgressLog.SelectionLength = 0;
                    ProgressLog.SelectionColor = Color.Red;
                    ProgressLog.AppendText("\nError: " + e.Message + "\n" + e.StackTrace);
                    ProgressLog.SelectionColor = ProgressLog.ForeColor;
                }));
                ProgressBar.Invoke((MethodInvoker)(() => ProgressBar.Value = 0));
            }
            finally
            {
                SetCancelEnabled(false);
            }
        }

        private void formClosing(object sender, FormClosingEventArgs args)
        {
            if (actionThread != null && actionThread.IsAlive)
            {
                args.Cancel = true;
            }
        }

        private void cancel_Click(object sender, EventArgs e)
        {
            if (actionThread != null && actionThread.IsAlive)
            {
                actionThread.Abort();
            }
        }

        private void ExportForm_Load(object sender, EventArgs e)
        {
            ambientRed.Text = LightmapConfig.AmbientColorR.ToString();
            ambientGreen.Text = LightmapConfig.AmbientColorG.ToString();
            ambientBlue.Text = LightmapConfig.AmbientColorB.ToString();

            ambientColorBox.BackColor = Color.FromArgb(LightmapConfig.AmbientColorR, LightmapConfig.AmbientColorG, LightmapConfig.AmbientColorB);

            textureDims.Value = LightmapConfig.TextureDims;

            downscaleFactor.Value = (decimal)LightmapConfig.DownscaleFactor;

            blurRadius.Value = LightmapConfig.BlurRadius;

            threadCount.Value = LightmapConfig.MaxThreadCount;

            viewAfterCheckbox.Checked = LightmapConfig.ViewAfterExport;

            if (LightmapConfig.BakeModelShadows)
            {
                modelBakeYes.Checked = true;
            }
        }
    }
}
