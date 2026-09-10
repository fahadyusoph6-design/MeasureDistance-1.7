using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Drawing.Drawing2D;

// Inside your form class

namespace TestCharp
{
    public partial class FrmMain : Form
    {

        // P/Invoke: match the native signature exactly (CallingConvention.Cdecl used here)
        [DllImport("DistanceDll.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Recognition")]
        private static extern int RecognitionUtf8(IntPtr imagePathUtf8, int mode, IntPtr dimensions);

        
        // Wrapper that returns the native int result and outputs the managed double[] (expectedCount = number of doubles the DLL writes)
        private int CallRecognitionUtf8(string fullPath, int mode, out double[] dims, int expectedCount = 5)
        {
            dims = null;
            if (!System.IO.File.Exists(fullPath)) return -1;

            // Marshal string to null-terminated UTF-8
            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(fullPath + "\0");
            IntPtr pUtf8 = Marshal.AllocHGlobal(utf8.Length);
            Marshal.Copy(utf8, 0, pUtf8, utf8.Length);

            // Allocate native buffer for doubles
            IntPtr pDims = Marshal.AllocHGlobal(sizeof(double) * expectedCount);

            try
            {
                int rc = RecognitionUtf8(pUtf8, mode, pDims);
                if (rc == 0)
                {
                    dims = new double[expectedCount];
                    Marshal.Copy(pDims, dims, 0, expectedCount);
                }
                return rc;
            }
            finally
            {
                Marshal.FreeHGlobal(pUtf8);
                Marshal.FreeHGlobal(pDims);
            }
        }

        private void LoadImageSafe(string path)
        {
            if (!File.Exists(path)) return;

            // dispose previous image to avoid leaks (and avoid persistent marks)
            var old = picImage.Image;
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    picImage.Image = new Bitmap(Image.FromStream(fs));
                }
            }
            catch
            {
                // handle load error
                picImage.Image = null;
            }
            finally
            {
                old?.Dispose();
            }

            // CLEAR any overlay when a new image is loaded
            _overlayImagePoint = null;
            picImage.Invalidate(); // forces repaint without overlay
        }
        // Alternative safe signature using unmanaged buffer:
        // [DllImport("DistanceDll.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        // private static extern int Recognition(string imageName, int mode, IntPtr dimensions);

        double m_scaleX = 0;
        double m_scaleY = 0;
         // 0: Auto, 1: Manual
        private const int MODE_AUTO = 0;
        private const int MODE_SEMIAUTO = 1;
        private int m_mode = MODE_AUTO; 
        //int m_mode = 0;
        string imagePath;

        private readonly List<Point> _overlayImagePoints = new List<Point>();
        private readonly int maxPoints = 2; // set to 2 for the behaviour you described
        private void InitOverlayHandlers()
        {
            picImage.SizeMode = PictureBoxSizeMode.Zoom; // recommended
            picImage.MouseClick += picImage_MouseClick;
            picImage.Paint += picImage_Paint;
            // clear overlay when selection changes - ensure you call this from your selection handler
            // e.g. OnListSelectionChanged -> ClearOverlay();
        }
        public FrmMain()
        {
            InitializeComponent();
            radioBTN_Auto.Checked = true;
            radioBTN_Manual.CheckedChanged += RadioManual_CheckedChanged;
            radioBTN_Auto .CheckedChanged += RadioManual_CheckedChanged;
            picImage.Enabled = true;
            picImage.Visible = true;
            picImage.SizeMode = PictureBoxSizeMode.Zoom;

            // wire events here (after InitializeComponent)
            picImage.MouseDown += picImage_MouseClick;
            picImage.Paint += picImage_Paint;

        }

        private void lblModule_Click(object sender, EventArgs e)
        {

        }
        private void RadioManual_CheckedChanged(object sender, EventArgs e)
        {
            if (radioBTN_Manual.Checked)
            {       
                m_mode = 1; // set mode to Manual
            }
            else
            {
                m_mode = 0; // set mode to Auto
            }
        }

       
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            imagePath = "";
            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                imagePath = openDlg.FileName;
                //picImage.Image = System.Drawing.Image.FromFile(imagePath);
                LoadImageSafe(imagePath);
                textPath.Text = imagePath; 
                resetControls();
                resetScale();
                init();
            }
        }
        private void resetControls()
        {
            

            bool isManual = false;

            //cbHorMode.SelectedIndex = 0;
            radioBTN_Auto.Enabled = true;
            radioBTN_Auto.Checked = true;

        }
        private void resetScale()
        {
            if (picImage.Bounds.IsEmpty)
            {
                m_scaleX = m_scaleY = 0;
                return;
            }
            //add in 2026.1.24
            var img = picImage.Image;
            if (img == null || img.Width <= 0 || img.Height <= 0)
            {
                m_scaleX = m_scaleY = 0;
                return;
            }
            //////////////////

            m_scaleX = picImage.Image.Width / (double)picImage.Width;
            m_scaleY = picImage.Image.Height / (double)picImage.Height;
        }

        private void init()
        {
           
        }

        private Point? _overlayImagePoint = null; // image coordinates (pixel)
       
        Point imgPt;


        //private Point? _overlayImagePoint = null;
        

        private void ClearOverlay()
        {
            if(_overlayImagePoints.Count > 0)
             {
                _overlayImagePoints.Clear();
                picImage.Invalidate();
            }
        }

        private double? firstX = null;
        private double? firstY = null;
        private double? secondX = null;
        private double? secondY = null;
        private int clickIndex = 0; // 0 -> next click writes first, 1 -> next click writes second


        private void ClearSemiautoPoints()
        {
            firstX = firstY = secondX = secondY = null;
            clickIndex = 0;
            picImage.Invalidate();
        }
        private void picImage_MouseClick(object sender, MouseEventArgs e)
        {
            if (m_mode != MODE_SEMIAUTO) return; // only in semiauto
            if (picImage.Image == null) return;
            LoadImageSafe(imagePath);
            imgPt = ControlToImage(e.Location, picImage);
            double ix = imgPt.X;
            double iy = imgPt.Y;
            // keep a single point
            if (_overlayImagePoints.Count < maxPoints)
            {
                _overlayImagePoints.Add(imgPt);
            }
            else
            {
                // cycle: start again from the new point
                _overlayImagePoints.Clear();
                _overlayImagePoints.Add(imgPt);
            }
            
            
            if (clickIndex == 0)
            {
                firstX = ix;
                firstY = iy;
            }
            else
            {
                secondX = ix;
                secondY = iy;
            }

            // toggle so sequence is: first, second, first, second...
            clickIndex = (clickIndex + 1) % 2;

            picImage.Invalidate();
            // optionally call semiauto processing here using imgPt
        }

        // Paint overlay: draw vertical line and a small dot at last clicked point
        private void picImage_Paint(object sender, PaintEventArgs e)
        {
            if (m_mode != MODE_SEMIAUTO) return;
            if (picImage.Image == null) return;
            if (_overlayImagePoints.Count == 0) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using (var pen = new Pen(Color.Red, 2f))
            using (var brush = new SolidBrush(Color.Red))
            using (var borderPen = new Pen(Color.Black, 1f))
            {
                const int r = 5;
                Rectangle imgRect = GetImageRectangle(picImage);

                // First point: dotted vertical line down to image bottom and a dot
                if (_overlayImagePoints.Count >= 1)
                {
                    Point imgPt = _overlayImagePoints[0];
                    Point ctrlPt = ImageToControl(imgPt, picImage);

                    pen.DashStyle = DashStyle.Dot;
                    int bottomY = imgRect.Bottom - 1; // draw down to image bottom
                    g.DrawLine(pen, ctrlPt.X, ctrlPt.Y, ctrlPt.X, bottomY);

                    g.FillEllipse(brush, ctrlPt.X - r, ctrlPt.Y - r, r * 2, r * 2);
                    g.DrawEllipse(borderPen, ctrlPt.X - r, ctrlPt.Y - r, r * 2, r * 2);
                }

                // Second point: only draw a dot (no line)
                if (_overlayImagePoints.Count >= 2)
                {
                    Point imgPt2 = _overlayImagePoints[1];
                    Point ctrlPt2 = ImageToControl(imgPt2, picImage);

                    // no line for second point
                    g.FillEllipse(brush, ctrlPt2.X - r, ctrlPt2.Y - r, r * 2, r * 2);
                    g.DrawEllipse(borderPen, ctrlPt2.X - r, ctrlPt2.Y - r, r * 2, r * 2);
                }
            }
        }

        // Convert control (mouse) coords → image pixel coords (handles Zoom)
        private Point ControlToImage(Point controlPt, PictureBox pb)
        {
            if (pb.Image == null) return Point.Empty;
            Rectangle imgRect = GetImageRectangle(pb);
            // if clicked outside image area return nearest
            if (!imgRect.Contains(controlPt))
            {
                //controlPt.X = Math.Clamp(controlPt.X, imgRect.Left, imgRect.Right);
                controlPt.X = Math.Max(imgRect.Left, Math.Min(controlPt.X, imgRect.Right));
                //controlPt.Y = Math.Clamp(controlPt.Y, imgRect.Top, imgRect.Bottom);
                controlPt.Y = Math.Max(imgRect.Top, Math.Min(controlPt.Y, imgRect.Bottom));
            }

            float scaleX = (float)pb.Image.Width / imgRect.Width;
            float scaleY = (float)pb.Image.Height / imgRect.Height;

            int imgX = (int)((controlPt.X - imgRect.Left) * scaleX + 0.5f);
            int imgY = (int)((controlPt.Y - imgRect.Top) * scaleY + 0.5f);

            //imgX = Math.Clamp(imgX, 0, pb.Image.Width - 1);
            imgX = Math.Max(0, Math.Min(imgX, pb.Image.Width - 1));
            //imgY = Math.Clamp(imgY, 0, pb.Image.Height - 1);
            imgY = Math.Max(0, Math.Min(imgY, pb.Image.Height - 1));
            return new Point(imgX, imgY);
        }

        // Convert image pixel coords -> control coords (for painting)
        private Point ImageToControl(Point imagePt, PictureBox pb)
        {
            if (pb.Image == null) return Point.Empty;
            Rectangle imgRect = GetImageRectangle(pb);

            float scaleX = (float)imgRect.Width / pb.Image.Width;
            float scaleY = (float)imgRect.Height / pb.Image.Height;

            int cx = imgRect.Left + (int)(imagePt.X * scaleX + 0.5f);
            int cy = imgRect.Top + (int)(imagePt.Y * scaleY + 0.5f);
            return new Point(cx, cy);
        }

        // Compute the rectangle where the image is actually drawn inside the PictureBox (for SizeMode = Zoom)
        private Rectangle GetImageRectangle(PictureBox pb)
        {
            if (pb.Image == null) return Rectangle.Empty;

            int imgW = pb.Image.Width;
            int imgH = pb.Image.Height;
            int boxW = pb.ClientSize.Width;
            int boxH = pb.ClientSize.Height;

            float imgRatio = (float)imgW / imgH;
            float boxRatio = (float)boxW / boxH;

            int drawW, drawH, offsetX, offsetY;
            if (imgRatio > boxRatio)
            {
                // image is constrained by width
                drawW = boxW;
                drawH = (int)(boxW / imgRatio);
                offsetX = 0;
                offsetY = (boxH - drawH) / 2;
            }
            else
            {
                // constrained by height
                drawH = boxH;
                drawW = (int)(boxH * imgRatio);
                offsetX = (boxW - drawW) / 2;
                offsetY = 0;
            }

            return new Rectangle(offsetX, offsetY, drawW, drawH);
        }

        private void btnRecog_Click(object sender, EventArgs e)
        {
            //var fullPath = imagePath;

            string fullPath = imagePath; // selected file path
            double[] resultDims;
            int resultCode = CallRecognitionUtf8(fullPath, m_mode, out resultDims, expectedCount: 5);
            double PixToMm = 0.008951113022; //pixel per MM
            double kOffsetMm = 0.004808987656; // the offset of the camera in MM

            try
            {
                //int result_code = Recognition(fullPath, m_mode, new double[5] { 0, 0, 0, 0, 0 });
                if(m_mode == MODE_SEMIAUTO)
                {
                    if (firstX.HasValue && firstY.HasValue && secondX.HasValue && secondY.HasValue)
                    {
                        resultDims[0] = firstX.Value;
                        resultDims[1] = firstY.Value;
                        resultDims[2] = secondX.Value;
                        resultDims[3] = secondY.Value;
                        double pixel = Math.Abs(resultDims[3] - resultDims[1]);
                        double distanceDistance = PixToMm * pixel + kOffsetMm;
                        string pixels = pixel.ToString("F0");       // format pixels to 2 decimals
                        string distance = distanceDistance.ToString("F3");     // format distance to 2 decimals

                        textBoxPixel.Text = pixels;
                        textBoxDis.Text = distance;
                    }
                }
                else
                {
                    if (resultCode == 0)
                    {
                        picImage.Image = System.Drawing.Image.FromFile("output1.png");
                        string pixels = resultDims[3].ToString("F0");       // format pixels to 2 decimals
                        string distance = resultDims[4].ToString("F3");     // format distance to 2 decimals

                        textBoxPixel.Text = pixels;
                        textBoxDis.Text = distance;
                    }
                }
                
            }
            finally
            {
                btnRecog.Enabled = true;
            }
        }

        private void radioBTN_Auto_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioBTN_Auto.Checked) return;
            m_mode = MODE_AUTO;
            ClearOverlay(); // remove any semiauto marks
        }

        private void radioBTN_Manual_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioBTN_Manual.Checked) return;
            m_mode = MODE_SEMIAUTO;
            ClearOverlay(); // start fresh until user clicks

        }
    }
}
