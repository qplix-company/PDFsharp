// PDFsharp - A .NET library for processing PDF
// See the LICENSE file in the solution root for more information.

using PdfSharp.Drawing;

namespace PdfSharp.Charting.Renderers
{
    /// <summary>
    /// Represents the base for all pie plot area renderer.
    /// </summary>
    abstract class PiePlotAreaRenderer : PlotAreaRenderer
    {
        private readonly bool isDonut;
        private const int DonutWidthPercentage = 20;

        /// <summary>
        /// Initializes a new instance of the PiePlotAreaRenderer class
        /// with the specified renderer parameters.
        /// </summary>
        internal PiePlotAreaRenderer(RendererParameters parms, bool isDonut = false) : base(parms)
        {
          this.isDonut = isDonut;
        }

        /// <summary>
        /// Layouts and calculates the space used by the pie plot area.
        /// </summary>
        internal override void Format() 
            => CalcSectors();

        /// <summary>
        /// Draws the content of the pie plot area.
        /// </summary>
        internal override void Draw()
        {
            var cri = (ChartRendererInfo)_rendererParms.RendererInfo;
            XRect plotAreaRect = cri.PlotAreaRendererInfo.Rect;
            if (plotAreaRect.IsEmpty)
                return;

            if (cri.SeriesRendererInfos.Length == 0)
                return;

            XRect innerPlotAreaRect = cri.plotAreaRendererInfo.Rect;
            double w = Math.Min(innerPlotAreaRect.Width, innerPlotAreaRect.Height) * DonutWidthPercentage/100d;
            innerPlotAreaRect.X += w / 2;
            innerPlotAreaRect.Y += w / 2;
            innerPlotAreaRect.Width -= w;
            innerPlotAreaRect.Height -= w;

            XBrush backgroundBrush = new XSolidBrush(XColors.White); //cri.plotAreaRendererInfo.plotArea.FillFormat.Color

            var gfx = _rendererParms.Graphics;
            var state = gfx.Save();

            // Draw sectors.
            var sri = cri.SeriesRendererInfos[0];
            foreach (SectorRendererInfo sector in sri.PointRendererInfos)
            {
                if (!Double.IsNaN(sector.StartAngle) && !Double.IsNaN(sector.SweepAngle))
                    gfx.DrawPie(sector.FillFormat, sector.Rect, sector.StartAngle, sector.SweepAngle);
            }

            // Draw border of the sectors.
            if (isDonut)
            {
                XBrush backgroundBrush = new XSolidBrush(cri.plotAreaRendererInfo.plotArea.FillFormat.Color);
                gfx.DrawPie(backgroundBrush, innerPlotAreaRect, 0, 360);
            }
            else
            {
                foreach (SectorRendererInfo sector in sri.PointRendererInfos)
                {
                    if (!Double.IsNaN(sector.StartAngle) && !Double.IsNaN(sector.SweepAngle))
                        gfx.DrawPie(sector.LineFormat, sector.Rect, sector.StartAngle, sector.SweepAngle);
                }
            }

            gfx.Restore(state);
        }

        /// <summary>
        /// Calculates the specific positions for each sector.
        /// </summary>
        protected abstract void CalcSectors();
    }
}
