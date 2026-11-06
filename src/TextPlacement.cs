using System;

namespace Tracos {
    // Geometry only, independent of WPF/font metrics and of recognition.
    public struct TextPlacement {
        public double X,Y,ScaleX,ScaleY;
        public static TextPlacement Fit(Bounds ink,double glyphWidth,double glyphHeight) {
            double heightScale=Math.Min(1,Math.Max(1,ink.Height)/Math.Max(1,glyphHeight));
            return new TextPlacement {
                X=ink.X,Y=ink.Y,ScaleY=heightScale,
                ScaleX=Math.Min(heightScale,Math.Max(1,ink.Right-ink.X)/Math.Max(1,glyphWidth))
            };
        }
    }
}
