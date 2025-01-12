using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
        private System.Windows.Forms.PictureBox picResized;
        private System.Windows.Forms.TextBox txtResult;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;

        public Form1()
        {
            InitializeComponent();
            var encoderPath = "encoder_single.onnx";
            var ocrArgs = new[] { "got", "-m", "got_decoder-q4_k_m.gguf", "-ngl", "100", "--log-verbosity", "-1" };
            _ocrService = new OcrService(encoderPath, ocrArgs);
        }

        private void btnLoadImage_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var imagePath = openFileDialog.FileName;
                var originalImage = new Bitmap(imagePath);
                
                // Show original image
                picOriginal.Image = originalImage;
                
                // Create and show resized image
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
                picResized.Image = targetImage;

                // Perform OCR using the resized image and show result
                var result = _ocrService.PerformOcr(targetImage, 1);
                txtResult.Text = result;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _ocrService.Cleanup();
            base.OnFormClosing(e);
        }
    }

}
