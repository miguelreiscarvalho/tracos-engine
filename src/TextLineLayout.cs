using System;
using System.Linq;
using System.Collections.Generic;

namespace Tracos {
    public sealed class TextWordMetrics {
        public Region Region;
        public double Width,Height,Baseline;
    }
    public struct TextWordPlacement {
        public double X,Y,Width,Height;
        public Bounds Box {get{return new Bounds {X=X,Y=Y,Right=X+Width,Bottom=Y+Height};}}
    }
    public static class TextLineLayout {
        sealed class Line {
            public readonly List<TextWordMetrics> Words=new List<TextWordMetrics>();
            public Bounds Source,Reference;
            public double Top;
        }
        public static Dictionary<string,TextWordPlacement> Compose(IEnumerable<TextWordMetrics> input,bool flow,double space) {
            var words=input.Where(w=>!w.Region.InkOnly&&!String.IsNullOrWhiteSpace(w.Region.Text)).ToArray();
            var output=new Dictionary<string,TextWordPlacement>();
            if(!flow){foreach(var word in words)output[word.Region.Id]=new TextWordPlacement{X=word.Region.Box.X,Y=word.Region.AnchorY,Width=word.Width,Height=word.Height};return output;}
            var lines=new List<Line>();
            foreach(var word in words) {
                var b=word.Region.Box;
                var line=lines.Where(l=>{
                    double overlap=Math.Min(l.Reference.Bottom,b.Bottom)-Math.Max(l.Reference.Y,b.Y);
                    double gap=Math.Max(l.Source.X-b.Right,b.X-l.Source.Right);
                    return overlap>=Math.Min(Math.Max(1,l.Reference.Height),Math.Max(1,b.Height))*.25 &&
                        Math.Abs((l.Reference.Y+l.Reference.Bottom-b.Y-b.Bottom)/2)<=Math.Max(12,Math.Max(l.Reference.Height,b.Height)*.65)&&gap<=200;
                }).OrderBy(l=>Math.Abs(l.Reference.Y-b.Y)).FirstOrDefault();
                if(line==null){line=new Line {Source=b,Reference=b,Top=word.Region.AnchorY};lines.Add(line);}
                else line.Source=Bounds.Union(new[]{line.Source,b});
                line.Words.Add(word);
            }
            foreach(var line in lines) {
                double x=line.Source.X;
                double baseline=line.Top+line.Words[0].Baseline;
                foreach(var word in line.Words.OrderBy(w=>w.Region.Box.X)) {
                    // One font size and baseline; handwriting width never scales
                    // or distorts glyphs. Recognition regions remain independent.
                    output[word.Region.Id]=new TextWordPlacement{X=x,Y=baseline-word.Baseline,Width=word.Width,Height=word.Height};
                    x+=word.Width+space;
                }
            }
            return output;
        }
    }
}
