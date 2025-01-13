
# GOT-OCR2 Windows GUI

A Windows desktop application for optical character recognition (OCR) using the GOT-OCR2 model. This application provides a graphical interface for converting images containing text into editable and formatted text.

## Features

- Load and preview images
- Automatic image resizing and preprocessing
- OCR processing with GOT-OCR2 model
- Formatted text output (Markdown)
- HTML preview of formatted output
- GPU acceleration support

## Requirements

- Windows 10/11 (64-bit)
- .NET Framework 4.8
- Visual Studio 2022 (for building from source)
- NVIDIA GPU with CUDA support (optional, for better performance)

## Installation

1. Download the latest release from [GitHub Releases](https://github.com/MosRat/got.cpp/releases)
2. Extract the files to a directory
3. Place the following model files in the application directory:
   - `encoder_single.onnx`
   - `got_decoder-q4_k_m.gguf`
4. (Optional) Install CUDA and cuDNN for GPU acceleration

## Building from Source

1. Clone the repository
2. Open `got-win.sln` in Visual Studio 2022
3. Restore NuGet packages
4. Build the solution (x64 platform)
5. Copy the required model files to the output directory

```bash
git clone https://github.com/MosRat/got.cpp.git
cd got-win
nuget restore
msbuild got-win.sln /p:Configuration=Release /p:Platform=x64
```

## Usage

1. Launch the application
2. Click "Load Image" to select an image file
3. The application will:
   - Display the original image
   - Show the preprocessed version
   - Process the image using OCR
4. View the OCR results in the text box
5. Use the "Preview" button to see formatted output
6. Check "Formatted Output" for Markdown formatting

## Troubleshooting

- Ensure the model files are in the correct location
- Verify GPU drivers are up to date if using GPU acceleration
- Check console output for any error messages
- Increase system memory if processing large images

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- GOT-OCR2 model by MosRat
- ONNX Runtime for model inference
- Windows Forms for the GUI


