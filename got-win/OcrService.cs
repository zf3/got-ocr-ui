// filepath: /F:/work/ocr/got-win/got-win/OcrService.cs
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using static System.Net.Mime.MediaTypeNames;

namespace got_win
{
    public class OcrService
    {
        private readonly InferenceSession _session;
        private readonly IntPtr _ocrContext;

        [DllImport("libocr.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ocr_init(int argc, string[] argv);

        [DllImport("libocr.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ocr_run(IntPtr ctx, float[] image_embeds, int n_embeds, int got_type);

        [DllImport("libocr.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ocr_cleanup_ctx(IntPtr ctx);

        [DllImport("libocr.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ocr_free_result(IntPtr result);

        public OcrService(string encoderPath, string[] ocrArgs)
        {
            _session = new InferenceSession(encoderPath);
            _ocrContext = ocr_init(ocrArgs.Length, ocrArgs);
        }

        public string PerformOcr(Bitmap image, int gotType)
        {
            var result = "";
            var imageEmbeds = EncodeImage(image);
            result += "imageEmbed.len="+imageEmbeds.Length+"\r\n";
            var resultPtr = ocr_run(_ocrContext, imageEmbeds, imageEmbeds.Length / (256 * 1024), gotType);
            result += Marshal.PtrToStringAnsi(resultPtr);
            ocr_free_result(resultPtr);
            return result;
        }

        private float[] EncodeImage(Bitmap targetImage)
        {
            // Create target bitmap with black background
            var targetSize = new Size(1024, 1024);

            // Create tensor and copy pixel data
            var tensor = new DenseTensor<float>(new[] { 1, 3, targetSize.Height, targetSize.Width });

            // Convert image to tensor format (CxHxW) with normalized values
            for (int y = 0; y < targetSize.Height; y++)
            {
                for (int x = 0; x < targetSize.Width; x++)
                {
                    var pixel = targetImage.GetPixel(x, y);
                    tensor[0, 0, y, x] = pixel.R / 255.0f;  // Red channel
                    tensor[0, 1, y, x] = pixel.G / 255.0f;  // Green channel
                    tensor[0, 2, y, x] = pixel.B / 255.0f;  // Blue channel
                }
            }

            // Run the ONNX model
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input", tensor) };
            var results = _session.Run(inputs);
            using (var result = results.FirstOrDefault())
            {
                if (result != null)
                {
                    return result.AsEnumerable<float>().ToArray();
                }
                return Array.Empty<float>();
            }
        }

        public void Cleanup()
        {
            ocr_cleanup_ctx(_ocrContext);
        }
    }
}
