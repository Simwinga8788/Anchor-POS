using System.Drawing;

namespace SurfPOS.Core.Interfaces
{
    public interface IBarcodeService
    {
        byte[] GenerateBarcodeImage(string barcodeText, int width = 300, int height = 100);
        Task<byte[]> GenerateBarcodeLabelAsync(string barcode, string productName, decimal price);
    }
}
