using iText.Kernel.Geom;

namespace Pdf.Receipt.Extraction
{
    public class TextChunk
    {
        public string Text { get; set; } = string.Empty;
        public Rectangle BoundingBox { get; set; } = null!;
    }
}
