using System.IO;
using QRCoder;

namespace HOWA.Framework.QRCode
{
    public class QrCodeService
    {
        /// <summary>
        /// Generates a PNG representation of a QR code based on the provided text string payload.
        /// </summary>
        public byte[] GenerateQrCodePng(string payload)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                using (var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q))
                {
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        return qrCode.GetGraphic(20);
                    }
                }
            }
        }
    }
}
