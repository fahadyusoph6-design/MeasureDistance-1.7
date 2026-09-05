using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MeasureDistanceCSharp
{
    public class MainForm : Form
    {
        // P/Invoke: match the exact signature + calling convention used by your DLL.
        // Example: extern "C" __declspec(dllexport) int __cdecl Recognition(const char* imageName, int mode, double* dimensions);
        [DllImport("DistanceDll.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int Recognition(string imageName, int mode, [In, Out] double[] dimensions);

        private ListView listView;
        private PictureBox pictureBox;
        private Button btnOpen;
        private Button btnRecog;
        private Button btnStop;
        private RadioButton radioAuto, radioSemiauto, radioManual;
        private string currentFolder;
        private CancellationTokenSource cts;

        public MainForm()
        {
            Text = "MeasureDistance (C#)";
            Width = 1100;
            Height = 700;

            // Left panel: controls + list
            var leftPanel = new Panel { Dock = DockStyle.Left, Width = 320 };
            Controls.Add(leftPanel);

            var modeGroup = new GroupBox { Text = "Mode", Top = 8, Left = 8, Width = 300, Height = 100 };
            radioAuto = new RadioButton { Text = "Auto", Left = 8, Top = 18, Checked = true };
            radioSemiauto = new RadioButton { Text = "Semiauto", Left = 8, Top = 40 };
            radioManual = new RadioButton { Text = "Manual", Left = 8, Top = 62 };
            modeGroup.Controls.AddRange(new Control[] { radioAuto, radioSemiauto, radioManual });
            leftPanel.Controls.Add(modeGroup);

            btnOpen = new Button { Text = "Open Folder", Left = 8, Top = 120, Width = 140 };
            btnOpen.Click += BtnOpen_Click;
            leftPanel.Controls.Add(btnOpen);

            btnRecog = new Button { Text = "Recog", Left = 160, Top = 120, Width = 140 };
            btnRecog.Click += BtnRecog_Click;
            leftPanel.Controls.Add(btnRecog);

            btnStop = new Button { Text = "Stop", Left = 8, Top = 152, Width = 292, Enabled = false };
            btnStop.Click += BtnStop_Click;
            leftPanel.Controls.Add(btnStop);

            listView = new ListView
            {
                Left = 8,
                Top = 190,
                Width = 304,
                Height = 440,
                View = View.Details,
                FullRowSelect = true
            };
            listView.Columns.Add("Filename", 140);
            listView.Columns.Add("Pixels", 60, HorizontalAlignment.Right);
            listView.Columns.Add("Distance", 80, HorizontalAlignment.Right);
            listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
            leftPanel.Controls.Add(listView);

            // Right side: picture
            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            Controls.Add(pictureBox);
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() != DialogResult.OK) return;
            currentFolder = dlg.SelectedPath;
            PopulateList(currentFolder);
        }

        private void PopulateList(string folder)
        {
            listView.Items.Clear();
            var files = Directory.GetFiles(folder, "*.bmp");
            Array.Sort(files);
            for (int i = 0; i < files.Length; ++i)
            {
                var name = Path.GetFileName(files[i]);
                var item = new ListViewItem(new[] { name, string.Empty, string.Empty }) { Tag = i }; // Tag holds stable index
                listView.Items.Add(item);
            }
        }

        private void ListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count == 0) return;
            var item = listView.SelectedItems[0];
            var fname = Path.Combine(currentFolder, item.Text);
            ShowImage(fname);
        }

        private void ShowImage(string path)
        {
            if (!File.Exists(path)) return;
            // Load via stream to avoid file lock issues
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var img = Image.FromStream(fs);
                var old = pictureBox.Image;
                pictureBox.Image = new Bitmap(img);
                old?.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load image: " + ex.Message);
            }
        }

        private async void BtnRecog_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFolder))
            {
                MessageBox.Show("Open a folder first.");
                return;
            }

            if (radioAuto.Checked)
            {
                // run batch on background thread
                btnRecog.Enabled = false;
                btnStop.Enabled = true;
                cts = new CancellationTokenSource();
                try
                {
                    await Task.Run(() => ProcessAllFiles(cts.Token));
                }
                finally
                {
                    btnRecog.Enabled = true;
                    btnStop.Enabled = false;
                    cts.Dispose();
                    cts = null;
                }
            }
            else
            {
                // single selected file
                if (listView.SelectedItems.Count == 0) { MessageBox.Show("Select an item."); return; }
                var item = listView.SelectedItems[0];
                var path = Path.Combine(currentFolder, item.Text);
                await Task.Run(() => ProcessOne(path, item));
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            cts?.Cancel();
        }

        private void ProcessAllFiles(CancellationToken token)
        {
            for (int i = 0; i < listView.Items.Count; ++i)
            {
                if (token.IsCancellationRequested) break;
                var name = listView.Items[i].Text;
                var full = Path.Combine(currentFolder, name);
                double[] dims = new double[5];
                int rc = Recognition(full, 0, dims); // mode 0 = auto in example
                // copy to UI
                this.Invoke((Action)(() =>
                {
                    listView.Items[i].SubItems[1].Text = ((int)Math.Round(dims[2])).ToString();
                    listView.Items[i].SubItems[2].Text = dims[3].ToString("F2");
                    // If the native DLL writes output1.png, load it:
                    var outPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output1.png");
                    if (File.Exists(outPath))
                    {
                        // load safely
                        try
                        {
                            using var fs = new FileStream(outPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            var img = Image.FromStream(fs);
                            pictureBox.Image?.Dispose();
                            pictureBox.Image = new Bitmap(img);
                        }
                        catch { /* ignore load errors */ }
                    }
                }));
                Thread.Sleep(10); // small delay to let UI update
            }
        }

        private void ProcessOne(string path, ListViewItem item)
        {
            double[] dims = new double[5];
            int rc = Recognition(path, 0, dims);
            this.Invoke((Action)(() =>
            {
                item.SubItems[1].Text = ((int)Math.Round(dims[2])).ToString();
                item.SubItems[2].Text = dims[3].ToString("F2");
                // If processing produces output image, display it
                var outPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output1.png");
                if (File.Exists(outPath))
                {
                    try
                    {
                        using var fs = new FileStream(outPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        var img = Image.FromStream(fs);
                        pictureBox.Image?.Dispose();
                        pictureBox.Image = new Bitmap(img);
                    }
                    catch { }
                }
            }));
        }
    }
}