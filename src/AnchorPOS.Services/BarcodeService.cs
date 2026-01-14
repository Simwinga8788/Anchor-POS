using ZXing;
using ZXing.Common;
using SurfPOS.Core.Interfaces;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace SurfPOS.Services
{
    [SupportedOSPlatform("windows")]
    public class BarcodeService : IBarcodeService
    {
        public byte[] GenerateBarcodeImage(string barcodeText, int width = 300, int height = 100)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 10,
                    PureBarcode = false
                }
            };

            var pixelData = writer.Write(barcodeText);
            
            using var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
            
            try
            {
                System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }

        public async Task<byte[]> GenerateBarcodeLabelAsync(string barcode, string productName, decimal price)
        {
            // Full label with product name
            return await GenerateBarcodeLabelAsync(barcode, productName, includeProductName: true);
        }

        public async Task<byte[]> GenerateBarcodeLabelAsync(string barcode, string productName, bool includeProductName)
        {
            int labelWidth = 400;
            int labelHeight = includeProductName ? 150 : 100; // Smaller if no name

            using var bitmap = new Bitmap(labelWidth, labelHeight);
            using var graphics = Graphics.FromImage(bitmap);
            
            // White background
            graphics.Clear(Color.White);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Generate barcode
            var barcodeImage = GenerateBarcodeImage(barcode, 350, 80);
            using var barcodeStream = new MemoryStream(barcodeImage);
            using var barcodeBitmap = new Bitmap(barcodeStream);

            // Draw barcode centered at top
            graphics.DrawImage(barcodeBitmap, 25, 10);

            if (includeProductName)
            {
                // Draw product name below barcode
                using var nameFont = new Font("Arial", 11, FontStyle.Bold);
                using var brush = new SolidBrush(Color.Black);
                var nameFormat = new StringFormat { Alignment = StringAlignment.Center };
                
                // Truncate long product names
                string displayName = productName.Length > 40 ? productName.Substring(0, 37) + "..." : productName;
                graphics.DrawString(displayName, nameFont, brush, labelWidth / 2, 100, nameFormat);

                // Draw barcode text at bottom
                using var barcodeFont = new Font("Arial", 9);
                graphics.DrawString(barcode, barcodeFont, brush, labelWidth / 2, 125, nameFormat);
            }
            else
            {
                // Barcode-only label - just show barcode text below
                using var barcodeFont = new Font("Arial", 10, FontStyle.Bold);
                using var brush = new SolidBrush(Color.Black);
                var nameFormat = new StringFormat { Alignment = StringAlignment.Center };
                graphics.DrawString(barcode, barcodeFont, brush, labelWidth / 2, 95, nameFormat);
            }

            // Convert to byte array
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return await Task.FromResult(stream.ToArray());
        }
    }
}
