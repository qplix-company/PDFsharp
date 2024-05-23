// PDFsharp - A .NET library for processing PDF
// See the LICENSE file in the solution root for more information.

using PdfSharp.Pdf.IO;

namespace PdfSharp.Drawing.Layout
{
    /// <summary>
    /// Represents a very simple text formatter.
    /// If this class does not satisfy your needs on formatting paragraphs, I recommend taking a look
    /// at MigraDoc Foundation. Alternatively, you should copy this class in your own source code and modify it.
    /// </summary>
    public class XTextFormatterEx
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XTextFormatter"/> class.
        /// </summary>
        public XTextFormatterEx(XGraphics gfx)
        {
            _gfx = gfx ?? throw new ArgumentNullException(nameof(gfx));
        }

        readonly XGraphics _gfx;

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        public string Text
        {
            get => _text;
            set => _text = value;
        }
        string _text = default!;

        /// <summary>
        /// Gets or sets the font.
        /// </summary>
        public XFont Font
        {
            get => _font;
            set
            {
                _font = value ?? throw new ArgumentNullException("Font");

                _lineSpace = _font.GetHeight(); // old: _font.GetHeight(_gfx);
                _cyAscent = _lineSpace * _font.CellAscent / _font.CellSpace;
                _cyDescent = _lineSpace * _font.CellDescent / _font.CellSpace;

                // The width of " " is 0, so we measure the space indirectly.
                _spaceWidth = _gfx.MeasureString("x x", value).Width;
                _spaceWidth -= _gfx.MeasureString("xx", value).Width;
            }
        }

        XFont _font = default!;
        double _lineSpace;
        double _cyAscent;
        double _cyDescent;
        double _spaceWidth;

        private bool _preparedText = false;

        /// <summary>
        /// Gets or sets the bounding box of the layout.
        /// </summary>
        public XRect LayoutRectangle
        {
            get => _layoutRectangle;
            set => _layoutRectangle = value;
        }

        XRect _layoutRectangle;

        /// <summary>
        /// Gets or sets the alignment of the text.
        /// </summary>
        public XParagraphAlignment Alignment
        {
            get => _alignment;
            set => _alignment = value;
        }

        XParagraphAlignment _alignment = XParagraphAlignment.Left;
        
        /// <summary>
        /// Prepares a given text for drawing, performs the layout, returns the index of the last fitting char and the needed height.
        /// </summary>
        /// <param name="text">The text to be drawn.</param>
        /// <param name="font">The font to be used.</param>
        /// <param name="layoutRectangle">The layout rectangle. Set the correct width.
        /// Either set the available height to find how many chars will fit.
        /// Or set height to double.MaxValue to find which height will be needed to draw the whole text.</param>
        /// <param name="lastFittingChar">Index of the last fitting character. Can be -1 if the character was not determined. Will be -1 if the whole text can be drawn.</param>
        /// <param name="neededHeight">The needed height - either for the complete text or the used height of the given rect.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void PrepareDrawString(string text, XFont font, XRect layoutRectangle, out int lastFittingChar, out double neededHeight)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            if (font == null)
                throw new ArgumentNullException("font");

            Text = text;
            Font = font;
            LayoutRectangle = layoutRectangle;

            lastFittingChar = -1;
            neededHeight = double.MinValue;

            if (text.Length == 0)
                return;

            CreateBlocks();

            CreateLayout();

            _preparedText = true;

            double dy = _cyDescent + _cyAscent;
            int count = _blocks.Count;
            for (int idx = 0; idx < count; idx++)
            {
                Block block = (Block)_blocks[idx];
                if (block.Stop)
                {
                    // We have a Stop block, so only part of the text will fit. We return the index of the last fitting char (and the height of the block, if available).
                    lastFittingChar = 0;
                    int idx2 = idx - 1;
                    while (idx2 >= 0)
                    {
                        Block block2 = (Block)_blocks[idx2];
                        if (block2.EndIndex >= 0)
                        {
                            lastFittingChar = block2.EndIndex;
                            neededHeight = dy + block2.Location.Y; // Test this!!!!!
                            return;
                        }
                        --idx2;
                    }
                    return;
                }
                if (block.Type == BlockType.LineBreak)
                    continue;
                //gfx.DrawString(block.Text, font, brush, dx + block.Location.x, dy + block.Location.y);
                neededHeight = dy + block.Location.Y; // Test this!!!!! Performance optimization?
            }
        }

        /// <summary>
        /// Draws the text that was previously prepared by calling PrepareDrawString or by passing a text to DrawString.
        /// </summary>
        /// <param name="brush">The brush used for drawing the text.</param>
        public void DrawString(XBrush brush)
        {
            DrawString(brush, XStringFormats.TopLeft);
        }
        
        /// <summary>
        /// Draws the text that was previously prepared by calling PrepareDrawString or by passing a text to DrawString.
        /// </summary>
        /// <param name="brush">The brush used for drawing the text.</param>
        /// <param name="format">Not yet implemented.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void DrawString(XBrush brush, XStringFormat format)
        {
            // TODO: Do we need "XStringFormat format" at PrepareDrawString or at DrawString? Not yet used anyway, but probably already needed at PrepareDrawString.
            if (!_preparedText)
                throw new ArgumentException("PrepareDrawString must be called first.");
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (format.Alignment != XStringAlignment.Near || format.LineAlignment != XLineAlignment.Near)
                throw new ArgumentException("Only TopLeft alignment is currently implemented.");

            if (_text.Length == 0)
                return;

            double dx = _layoutRectangle.Location.X;
            double dy = _layoutRectangle.Location.Y + _cyAscent;
            int count = _blocks.Count;
            for (int idx = 0; idx < count; idx++)
            {
                Block block = (Block)_blocks[idx];
                if (block.Stop)
                    break;
                if (block.Type == BlockType.LineBreak)
                    continue;
                _gfx.DrawString(block.Text, _font, brush, dx + block.Location.X, dy + block.Location.Y);
            }
        }

        /// <summary>
        /// Draws the text.
        /// </summary>
        /// <param name="text">The text to be drawn.</param>
        /// <param name="font">The font.</param>
        /// <param name="brush">The text brush.</param>
        /// <param name="layoutRectangle">The layout rectangle.</param>
        public void DrawString(string text, XFont font, XBrush brush, XRect layoutRectangle)
        {
            DrawString(text, font, brush, layoutRectangle, XStringFormats.TopLeft);
        }

        /// <summary>
        /// Draws the text.
        /// </summary>
        /// <param name="text">The text to be drawn.</param>
        /// <param name="font">The font.</param>
        /// <param name="brush">The text brush.</param>
        /// <param name="layoutRectangle">The layout rectangle.</param>
        /// <param name="format">The format. Must be <c>XStringFormat.TopLeft</c></param>
        public void DrawString(string text, XFont font, XBrush brush, XRect layoutRectangle, XStringFormat format)
        {
            int dummy1;
            double dummy2;
            PrepareDrawString(text, font, layoutRectangle, out dummy1, out dummy2);

            DrawString(brush);
        }

        void CreateBlocks()
        {
            _blocks.Clear();
            int length = _text.Length;
            bool inNonWhiteSpace = false;
            int startIndex = 0, blockLength = 0;
            for (int idx = 0; idx < length; idx++)
            {
                char ch = _text[idx];

                // Treat CR and CRLF as LF
                if (ch == Chars.CR)
                {
                    if (idx < length - 1 && _text[idx + 1] == Chars.LF)
                        idx++;
                    ch = Chars.LF;
                }
                if (ch == Chars.LF)
                {
                    if (blockLength != 0)
                    {
                        string token = _text.Substring(startIndex, blockLength);
                        _blocks.Add(new Block(token, BlockType.Text,
                          _gfx.MeasureString(token, _font).Width, startIndex, startIndex + blockLength - 1));
                    }
                    startIndex = idx + 1;
                    blockLength = 0;
                    _blocks.Add(new Block(BlockType.LineBreak));
                }
                // The non-breaking space is whitespace, so we treat it like non-whitespace.
                else if (ch != Chars.NonBreakableSpace && Char.IsWhiteSpace(ch))
                {
                    if (inNonWhiteSpace)
                    {
                        string token = _text.Substring(startIndex, blockLength);
                        _blocks.Add(new Block(token, BlockType.Text,
                          _gfx.MeasureString(token, _font).Width, startIndex, startIndex + blockLength - 1));
                        startIndex = idx + 1;
                        blockLength = 0;
                    }
                    else
                    {
                        blockLength++;
                    }
                }
                else
                {
                    inNonWhiteSpace = true;
                    blockLength++;
                }
            }
            if (blockLength != 0)
            {
                string token = _text.Substring(startIndex, blockLength);
                _blocks.Add(new Block(token, BlockType.Text,
                  _gfx.MeasureString(token, _font).Width, startIndex, startIndex + blockLength - 1));
            }
        }

        void CreateLayout()
        {
            double rectWidth = _layoutRectangle.Width;
            double rectHeight = _layoutRectangle.Height - _cyAscent - _cyDescent;
            int firstIndex = 0;
            double x = 0, y = 0;
            int count = _blocks.Count;
            for (int idx = 0; idx < count; idx++)
            {
                Block block = _blocks[idx];
                if (block.Type == BlockType.LineBreak)
                {
                    if (Alignment == XParagraphAlignment.Justify)
                        _blocks[firstIndex].Alignment = XParagraphAlignment.Left;
                    AlignLine(firstIndex, idx - 1, rectWidth);
                    firstIndex = idx + 1;
                    x = 0;
                    y += _lineSpace;
                    if (y > rectHeight)
                    {
                        block.Stop = true;
                        break;
                    }
                }
                else
                {
                    double width = block.Width;
                    if ((x + width <= rectWidth || x == 0) && block.Type != BlockType.LineBreak)
                    {
                        block.Location = new XPoint(x, y);
                        x += width + _spaceWidth;
                    }
                    else
                    {
                        AlignLine(firstIndex, idx - 1, rectWidth);
                        firstIndex = idx;
                        y += _lineSpace;
                        if (y > rectHeight)
                        {
                            block.Stop = true;
                            break;
                        }
                        block.Location = new XPoint(0, y);
                        x = width + _spaceWidth;
                    }
                }
            }
            if (firstIndex < count && Alignment != XParagraphAlignment.Justify)
                AlignLine(firstIndex, count - 1, rectWidth);
        }

        /// <summary>
        /// Align center, right, or justify.
        /// </summary>
        void AlignLine(int firstIndex, int lastIndex, double layoutWidth)
        {
            XParagraphAlignment blockAlignment = _blocks[firstIndex].Alignment;
            if (_alignment == XParagraphAlignment.Left || blockAlignment == XParagraphAlignment.Left)
                return;

            int count = lastIndex - firstIndex + 1;
            if (count == 0)
                return;

            double totalWidth = -_spaceWidth;
            for (int idx = firstIndex; idx <= lastIndex; idx++)
                totalWidth += _blocks[idx].Width + _spaceWidth;

            double dx = Math.Max(layoutWidth - totalWidth, 0);
            //Debug.Assert(dx >= 0);
            if (_alignment != XParagraphAlignment.Justify)
            {
                if (_alignment == XParagraphAlignment.Center)
                    dx /= 2;
                for (int idx = firstIndex; idx <= lastIndex; idx++)
                {
                    Block block = _blocks[idx];
                    block.Location += new XSize(dx, 0);
                }
            }
            else if (count > 1) // case: justify
            {
                dx /= count - 1;
                for (int idx = firstIndex + 1, i = 1; idx <= lastIndex; idx++, i++)
                {
                    Block block = _blocks[idx];
                    block.Location += new XSize(dx * i, 0);
                }
            }
        }

        readonly List<Block> _blocks = new List<Block>();

        enum BlockType
        {
            Text, Space, Hyphen, LineBreak,
        }

        /// <summary>
        /// Represents a single word.
        /// </summary>
        class Block
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Block"/> class.
            /// </summary>
            /// <param name="text">The text of the block.</param>
            /// <param name="type">The type of the block.</param>
            /// <param name="width">The width of the text.</param>
            /// <param name="startIndex"></param>
            /// <param name="endIndex"></param>
            public Block(string text, BlockType type, double width, int startIndex, int endIndex)
            {
                Text = text;
                Type = type;
                Width = width;
                StartIndex = startIndex;
                EndIndex = endIndex;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Block"/> class.
            /// </summary>
            /// <param name="type">The type.</param>
            public Block(BlockType type)
            {
                Type = type;
            }

            /// <summary>
            /// The text represented by this block.
            /// </summary>
            public readonly string Text = default!;
            
            public int StartIndex = -1;
            public int EndIndex = -1;

            /// <summary>
            /// The type of the block.
            /// </summary>
            public readonly BlockType Type;

            /// <summary>
            /// The width of the text.
            /// </summary>
            public readonly double Width;

            /// <summary>
            /// The location relative to the upper left corner of the layout rectangle.
            /// </summary>
            public XPoint Location;

            /// <summary>
            /// The alignment of this line.
            /// </summary>
            public XParagraphAlignment Alignment;

            /// <summary>
            /// A flag indicating that this is the last block that fits in the layout rectangle.
            /// </summary>
            public bool Stop;
        }
        // TODO: Possible Improvements for XTextFormatter:
        // - more XStringFormat variations
        // - calculate bounding box
        // - left and right indent
        // - first line indent
        // - margins and paddings
        // - background color
        // - text background color
        // - border style
        // - hyphens, soft hyphens, hyphenation
        // - kerning
        // - change font, size, text color etc.
        // - line spacing
        // - underline and strike-out variation
        // - super- and sub-script
        // - ...
    }
}
