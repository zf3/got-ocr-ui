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

        public readonly double CONFIG_RESCALE_FACTOR = 1.0 / 255.0;
        public readonly double CONFIG_NORM_MEAN_R = 0.48145466;
        public readonly double CONFIG_NORM_MEAN_G = 0.4578275;
        public readonly double CONFIG_NORM_MEAN_B = 0.40821073;
        public readonly double CONFIG_NORM_STD_R = 0.26862954;
        public readonly double CONFIG_NORM_STD_G = 0.26130258;
        public readonly double CONFIG_NORM_STD_B = 0.27577711;

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
            // Create target bitmap with black background
            var targetSize = new Size(1024, 1024);

            // Create a clone of the image to avoid access conflicts
            using (var clonedImage = new Bitmap(image))
            {
                // Create tensor and copy pixel data
                var tensor = new DenseTensor<float>(new[] { 1, 3, targetSize.Height, targetSize.Width });

            // Convert cloned image to tensor format (CxHxW) with normalized values
            for (int y = 0; y < targetSize.Height; y++)
            {
                for (int x = 0; x < targetSize.Width; x++)
                {
                    var pixel = clonedImage.GetPixel(x, y);
                    tensor[0, 0, y, x] = (float)((pixel.R * CONFIG_RESCALE_FACTOR - CONFIG_NORM_MEAN_R) / CONFIG_NORM_STD_R);  // Red channel
                    tensor[0, 1, y, x] = (float)((pixel.G * CONFIG_RESCALE_FACTOR - CONFIG_NORM_MEAN_G) / CONFIG_NORM_STD_G);  // Green channel
                    tensor[0, 2, y, x] = (float)((pixel.B * CONFIG_RESCALE_FACTOR - CONFIG_NORM_MEAN_B) / CONFIG_NORM_STD_B);  // Blue channel
                }
            }

            } // End of using clonedImage

            // Run the ONNX model
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input", tensor) };
            var results = _session.Run(inputs);
            var r = results.FirstOrDefault();
            var imageEmbeds  = r != null ? r.AsEnumerable<float>().ToArray() : Array.Empty<float>();

            // Print some debug info
        /*
            result += "image\r\n";
            var offsets = new[] {0, 128, 300, 632};
            for (int i = 0; i < 4; i ++)
            {
                result += "Offset " + offsets[i] + "\r\n";
                for (int j = 0; j < 256; j++)
                {
                    result += tensor[0, 0, offsets[i], j].ToString("F3") + " ";
                    if ((j + 1) % 16 == 0) 
                        result += "\r\n";
                }
                result += "\r\n";
            }

            result += "imageEmbed.len="+imageEmbeds.Length+"\r\n";
            offsets = new[] { 0, 16 * 1024, 64 * 1024, 128 * 1024 };
            for (int i = 0; i < 4; i++)
            {
                result += "Offset " + offsets[i] + "\r\n";
                for (int j = 0; j < 16; j++)
                {
                    result += imageEmbeds[offsets[i] + j].ToString("F3") + " ";
                }
                result += "\r\n";
            }
        */

            // Run llama.cpp on the image embeddings to do OCR
            IntPtr p = ocr_run(_ocrContext, imageEmbeds, 256, gotType);
            // struct OcrResult {
            //   char *result,
            //   char *error
            // }
            IntPtr resultPtr = Marshal.ReadIntPtr(p);       // OcrResult.result
        /*
            result += "OCR result in bytes: ";
            for (int i = 0; i < 64; i++)
            {
                result += Marshal.ReadByte(resultPtr, i) + " ";
            }
            result += "\r\n";
        */

            // PtrToStringAnsi does not support UTF-8...
            var bytes = System.Text.Encoding.Unicode.GetBytes(Marshal.PtrToStringUni(resultPtr));
            var txt = System.Text.Encoding.UTF8.GetString(bytes);
            txt.Replace("\n", "\r\n");
            result += txt;
            ocr_free_result(p);
            return result;
        }

        public void Cleanup()
        {
            ocr_cleanup_ctx(_ocrContext);
        }
    }
}
