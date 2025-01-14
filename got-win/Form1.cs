using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using static System.Net.Mime.MediaTypeNames;

namespace got_win
{
    public partial class Form1 : Form
    {
        private OcrService _ocrService;
        private System.Windows.Forms.Button btnLoadImage;
        private System.Windows.Forms.PictureBox picOriginal;
        private System.Windows.Forms.TextBox txtResult;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lblLoading;
        private System.Windows.Forms.Timer timerLoading;
        private System.Windows.Forms.CheckBox chkFormatted;
        private System.Windows.Forms.Button btnPreview;
        private int loadingDots = 0;

        public Form1()
        {
            InitializeComponent();
            var encoderPath = "encoder_single.onnx";
            var ocrArgs = new[] { "got", "-m", "got_decoder-q4_k_m.gguf", "-ngl", "100", "--log-verbosity", "-1", "-n", "16384" };
            _ocrService = new OcrService(encoderPath, ocrArgs);
            
            // Initialize loading animation
            timerLoading = new Timer();
            timerLoading.Interval = 500;
            timerLoading.Tick += TimerLoading_Tick;
        }

        private void TimerLoading_Tick(object sender, EventArgs e)
        {
            loadingDots = (loadingDots + 1) % 4;
            lblLoading.Text = "Processing" + new string('.', loadingDots) + new string(' ', 3 - loadingDots);
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            string markdown = txtResult.Text;
            string html = $@"
                <html>
                    <head>
                        <link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.2.0/github-markdown.min.css'>
                        <script id=""MathJax-script"" async src=""https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js""></script>
                        <style>
                            .markdown-body {{
                                box-sizing: border-box;
                                min-width: 200px;
                                max-width: 980px;
                                margin: 0 auto;
                                padding: 45px;
                            }}
                            @media (max-width: 767px) {{
                                .markdown-body {{
                                    padding: 15px;
                                }}
                            }}
                        </style>
                    </head>
                    <body class='markdown-body'>
                        {markdown}
                    </body>
                </html>";
            
            string tempFile = Path.GetTempFileName() + ".html";
            File.WriteAllText(tempFile, html);
            System.Diagnostics.Process.Start(tempFile);
        }

        private async void btnLoadImage_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Disable button and show loading
                btnLoadImage.Enabled = false;
                lblLoading.Visible = true;
                timerLoading.Start();
                
                var imagePath = openFileDialog.FileName;
                var originalImage = new Bitmap(imagePath);
                
                // Show original image
                picOriginal.Image = originalImage;
                
                // Create resized image for OCR processing
                var targetSize = new Size(1024, 1024);
                var targetImage = new Bitmap(targetSize.Width, targetSize.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                // Calculate aspect ratio preserving dimensions
                float scale = Math.Min((float)targetSize.Width / originalImage.Width, (float)targetSize.Height / originalImage.Height);
                var newSize = new Size((int)(originalImage.Width * scale), (int)(originalImage.Height * scale));

                // Create graphics object and draw resized image centered on black background
                using (var graphics = Graphics.FromImage(targetImage))
                {
                    graphics.Clear(Color.Black);
                    var destRect = new Rectangle(
                        (targetSize.Width - newSize.Width) / 2,
                        (targetSize.Height - newSize.Height) / 2,
                        newSize.Width,
                        newSize.Height);
                    graphics.DrawImage(originalImage, destRect, new Rectangle(0, 0, originalImage.Width, originalImage.Height), GraphicsUnit.Pixel);
                }

                DenseTensor<float> tensor = OcrService.imageToTensor(targetImage);

                // Perform OCR using the resized image and show result
                txtResult.Text = "";
                int gotType = chkFormatted.Checked ? 2 : 1;
                string result = await Task.Run(() => _ocrService.PerformOcr(tensor, gotType));
                txtResult.Text = result;
                
                // Stop loading and re-enable button
                timerLoading.Stop();
                lblLoading.Visible = false;
                btnLoadImage.Enabled = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _ocrService.Cleanup();
            base.OnFormClosing(e);
        }
    }

}
