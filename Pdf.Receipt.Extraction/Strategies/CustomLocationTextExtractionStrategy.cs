using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iText.Kernel.Geom;

namespace Pdf.Receipt.Extraction
{
    class CustomLocationTextExtractionStrategy : LocationTextExtractionStrategy
    {
        public List<TextChunk> TextChunks { get; } = new List<TextChunk>();
        List<TextChunk>? _processedChunks;
        private List<TextChunk> ProcessedTextChunks {
            get
            {
                if(_processedChunks == null)
                {
                    _processedChunks = TextChunks
                        .OrderByDescending(c => c.BoundingBox.GetY()).ThenBy(c => c.BoundingBox.GetX())
                        .ToList();

                    // Iterate through the list and adjust Y coordinates for nearby chunks
                    for (int i = 1; i < _processedChunks.Count; i++)
                    {
                        var previousChunk = _processedChunks[i - 1];
                        var currentChunk = _processedChunks[i];

                        if((int)currentChunk.BoundingBox.GetY() == 649)
                        {

                        }

                        // If the Y difference is 1 or less, adjust the Y coordinate of the current chunk
                        if (Math.Abs(currentChunk.BoundingBox.GetY() - previousChunk.BoundingBox.GetY()) <= 1)
                        {
                            currentChunk.BoundingBox = new Rectangle(
                                currentChunk.BoundingBox.GetX(),
                                previousChunk.BoundingBox.GetY(), // Set Y to the previous chunk's Y
                                currentChunk.BoundingBox.GetWidth(),
                                currentChunk.BoundingBox.GetHeight()
                            );
                        }
                    }
                    _processedChunks =_processedChunks.OrderByDescending(c => c.BoundingBox.GetY()).ThenBy(c => c.BoundingBox.GetX()).ToList();
                }

                return _processedChunks;
            }
        }

        public override void EventOccurred(IEventData data, EventType type)
        {
            if (data is TextRenderInfo textRenderInfo)
            {
                var rect = textRenderInfo.GetBaseline().GetStartPoint();
                TextChunks.Add(new TextChunk
                {
                    Text = textRenderInfo.GetText(),
                    BoundingBox = textRenderInfo.GetBaseline().GetBoundingRectangle()
                });
            }
        }

        public override ICollection<EventType> GetSupportedEvents()
        {
            return new List<EventType> { EventType.RENDER_TEXT };
        }

        public string GetResultandText()
        {
            var textChunksByLoc = ProcessedTextChunks    
                .GroupBy(c => c.BoundingBox.GetY()); 

            var resultantText = new StringBuilder();
            foreach (var chunksLine in textChunksByLoc)
            {
                var lineText = new StringBuilder();
                TextChunk? prevChunk = null;
                foreach (var chunk in chunksLine)
                {
                    if (prevChunk != null)
                    {
                        if ((prevChunk.BoundingBox.GetX() + prevChunk.BoundingBox.GetWidth() + 5 < chunk.BoundingBox.GetX()) && !chunk.Text.Contains(" "))
                        {
                            lineText.Append(" ");
                        }
                    }
                    lineText.Append(chunk.Text);
                    prevChunk = chunk;
                }
                resultantText.AppendLine(lineText.ToString());
            }

            return resultantText.ToString();
        }

        public List<string> GetCustomerDataLines(int? leftBoundary, int? rightBoundary, string topBoundary, string bottomBoundery)
        {
            var customerTextChunks = ProcessedTextChunks.ToList();
            if (leftBoundary != null && rightBoundary == null)
            {
                customerTextChunks = customerTextChunks.Where(c => c.BoundingBox.GetX() > leftBoundary).ToList();
            }
            else if (leftBoundary == null && rightBoundary != null)
            {
                customerTextChunks = customerTextChunks.Where(c => c.BoundingBox.GetX() <= rightBoundary).ToList();
            }
            
            bool isCustomerSection = false;
            var lines = new List<string>();

            var chunksLineGrp = customerTextChunks.GroupBy(c => c.BoundingBox.GetY());
            foreach (var chunksLine in chunksLineGrp)
            {
                
                var lineText = "";
                foreach (var chunk in chunksLine)
                {
                    lineText += chunk.Text;
                }

                if (lineText.Contains(bottomBoundery))
                {
                    isCustomerSection = false;
                    break;
                }
                if (isCustomerSection && !string.IsNullOrWhiteSpace(lineText) && lineText.Length <= 100)
                {
                    lines.Add(lineText);
                    if (lines.Count >= 10) break;
                }
                if (lineText.Contains(topBoundary))
                {
                    isCustomerSection = true;
                }
               
            }

            return lines;
        }
    }
}
