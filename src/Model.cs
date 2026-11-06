using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Json;

namespace Tracos {
    [DataContract] public class InkPoint {
        [DataMember] public double X;
        [DataMember] public double Y;
        [DataMember] public long Time;
        [DataMember] public float Pressure;
        [DataMember] public float? TiltX;
        [DataMember] public float? TiltY;
    }
    [DataContract] public class InkTrace {
        [DataMember] public string Id = Guid.NewGuid().ToString();
        [DataMember] public List<InkPoint> Points = new List<InkPoint>();
        [DataMember] public string Device = "unknown";
        public Bounds Box { get { return Bounds.Of(Points); } }
    }
    public struct Bounds {
        public double X,Y,Right,Bottom;
        public double Height { get { return Bottom-Y; } }
        public static Bounds Of(IEnumerable<InkPoint> points) {
            var p=points.ToArray();
            return p.Length==0 ? new Bounds() : new Bounds {X=p.Min(v=>v.X),Y=p.Min(v=>v.Y),Right=p.Max(v=>v.X),Bottom=p.Max(v=>v.Y)};
        }
    }
    [DataContract] public class Region {
        [DataMember] public string Id=Guid.NewGuid().ToString();
        [DataMember] public int Revision;
        [DataMember] public List<InkTrace> Traces=new List<InkTrace>();
        [DataMember] public string Text="";
        [DataMember] public string[] Alternatives=new string[0];
        [DataMember] public bool Manual;
        [DataMember] public bool Suppressed;
        [DataMember] public int RecognizedCount;
        [DataMember] public double AnchorX;
        [DataMember] public double AnchorY;
        [DataMember] public bool Closed;
        public Bounds Box { get { return Bounds.Of(Traces.SelectMany(t=>t.Points)); } }
    }
    [DataContract] public class InkDocument {
        [DataMember] public int Version=1;
        [DataMember] public string Id=Guid.NewGuid().ToString();
        [DataMember] public string Page="page-1";
        [DataMember] public string Layer="ink-1";
        [DataMember] public string Font="Segoe UI";
        [DataMember] public double FontSize=26;
        [DataMember] public List<Region> Regions=new List<Region>();
    }
    public static class Storage {
        public static byte[] Encode(InkDocument d) {using(var m=new MemoryStream()){new DataContractJsonSerializer(typeof(InkDocument)).WriteObject(m,d);return m.ToArray();}}
        public static InkDocument Decode(byte[] bytes) {
            using(var m=new MemoryStream(bytes)) {
                var d=(InkDocument)new DataContractJsonSerializer(typeof(InkDocument)).ReadObject(m);
                if(d.Version!=1 || d.Regions==null || d.Regions.Any(r=>r.Traces==null || r.Traces.Any(t=>t.Points==null || t.Points.Count==0 || t.Points.Any(p=>double.IsNaN(p.X)||double.IsInfinity(p.X)||double.IsNaN(p.Y)||double.IsInfinity(p.Y)))) || d.FontSize<8 || d.FontSize>100)
                    throw new InvalidDataException("Documento inválido ou versão incompatível.");
                return d;
            }
        }
        public static void Save(string path, InkDocument d) {
            var temp=path+".tmp";
            var bytes=Encode(d);
            using(var f=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){f.Write(bytes,0,bytes.Length);f.Flush(true);}
            if(File.Exists(path)) File.Replace(temp,path,path+".bak"); else File.Move(temp,path);
        }
    }
    public sealed class History {
        readonly Stack<byte[]> undo=new Stack<byte[]>(),redo=new Stack<byte[]>();
        public void Record(InkDocument d){undo.Push(Storage.Encode(d));redo.Clear();}
        public InkDocument Undo(InkDocument d){if(undo.Count==0)return d;redo.Push(Storage.Encode(d));return Storage.Decode(undo.Pop());}
        public InkDocument Redo(InkDocument d){if(redo.Count==0)return d;undo.Push(Storage.Encode(d));return Storage.Decode(redo.Pop());}
    }
    public static class Spatial {
        // A region is a local word-sized writing group, not an entire line.
        public static double WritingHeight(Region region) {
            var heights=region.Traces.Select(t=>t.Box.Height).Where(h=>h>=8).OrderBy(h=>h).ToArray();
            return heights.Length==0?20:Math.Max(12,Math.Min(80,heights[(heights.Length-1)*3/4]));
        }
        public static bool IsDot(InkTrace trace) {
            var box=trace.Box;
            return box.Right-box.X<=10 && box.Height<=10;
        }
        static bool IsVerticalSymbolPart(Region region,InkTrace trace) {
            // A dot above/below a compact body belongs to the same tentative
            // symbol (?!:; or dotted letter), regardless of the body's current
            // recognition. Words are not pulled into punctuation clusters.
            if(region.Traces.Count!=1)return false;
            var previous=region.Traces[0];
            if(!IsDot(previous)&&!IsDot(trace))return false;
            var dot=IsDot(trace)?trace.Box:previous.Box;
            var body=IsDot(trace)?previous.Box:trace.Box;
            double height=Math.Max(20,body.Height);
            if(body.Height>80 || body.Right-body.X>Math.Max(12,body.Height*1.1))return false;
            double gap=Math.Max(dot.Y-body.Bottom,body.Y-dot.Bottom);
            double dotCenter=(dot.X+dot.Right)/2;
            return gap>=0 && gap<=Math.Max(12,height*.8) &&
                dotCenter>=body.X-Math.Max(4,height*.25) &&
                dotCenter<=body.Right+Math.Max(4,height*.25);
        }
        static bool IsLateMark(Region region, Bounds mark) {
            var box=region.Box;
            double height=WritingHeight(region);
            // Only small overlapping additions can reopen a closed word. A new
            // word above/below or beside it must never reinterpret its content.
            if(box.Height>height*1.8 || mark.Right<box.X || mark.X>box.Right)return false;
            bool accent=mark.Height<=height*.55 && mark.Right-mark.X<=height*.9 &&
                mark.Bottom>=box.Y-height*.55 && mark.Y<=box.Y+height*.4;
            bool completion=mark.Height<=height*1.8 && mark.Right-mark.X<=height*.75 &&
                (mark.Y+mark.Bottom)/2>=box.Y-height*.2 &&
                (mark.Y+mark.Bottom)/2<=box.Bottom+height*.2;
            bool cedilla=mark.Height<=height*.45 && mark.Right-mark.X<=height*.7 &&
                mark.Y>=box.Bottom-height*.15 && mark.Y<=box.Bottom+height*.35;
            return accent||completion||cedilla;
        }
        public static Region Find(InkDocument d, InkTrace trace) {
            var b=trace.Box;
            var symbol=d.Regions.Where(r=>!r.Manual&&IsVerticalSymbolPart(r,trace))
                .OrderBy(r=>Math.Abs((b.Y+b.Bottom)-(r.Box.Y+r.Box.Bottom))+Math.Abs((b.X+b.Right)-(r.Box.X+r.Box.Right)))
                .FirstOrDefault();
            if(symbol!=null)return symbol;
            // Prefer an actual accent overlap over proximity to another word.
            var marked=d.Regions.Where(r=>!r.Manual&&IsLateMark(r,b))
                .OrderBy(r=>Math.Abs((b.X+b.Right)-(r.Box.X+r.Box.Right))).FirstOrDefault();
            if(marked!=null)return marked;
            var active=d.Regions.LastOrDefault(r=>!r.Manual&&!r.Closed);
            if(active==null)return null;
            var a=active.Box;
            double h=WritingHeight(active);
            double overlap=Math.Min(a.Bottom,b.Bottom)-Math.Max(a.Y,b.Y);
            bool sameLine=overlap>=Math.Min(h,Math.Max(1,b.Height))*.25;
            double horizontalGap=Math.Max(b.X-a.Right,a.X-b.Right);
            bool local=horizontalGap<=Math.Max(9,h*.4);
            return sameLine&&local?active:null;
        }
        public static Region Append(InkDocument document, InkTrace trace) {
            var region=Find(document,trace);
            if(region==null) {
                foreach(var previous in document.Regions)previous.Closed=true;
                region=new Region {AnchorX=trace.Box.X,AnchorY=trace.Box.Y};
                document.Regions.Add(region);
            }
            region.Traces.Add(trace);
            region.Revision++;
            region.Suppressed=false;
            return region;
        }
        public static List<List<InkTrace>> Groups(IList<InkTrace> traces, bool words) {
            var groups=new List<List<InkTrace>>();
            var sorted=traces.OrderBy(t=>t.Box.X).ToArray();
            var heights=sorted.Select(t=>t.Box.Height).Where(height=>height>8).OrderBy(height=>height).ToArray();
            double h=heights.Length==0?30:heights[heights.Length/2];
            var gaps=new List<double>(); double right=double.NegativeInfinity;
            foreach(var t in sorted) {
                double gap=t.Box.X-right;
                var recent=gaps.OrderBy(x=>x).ToArray();
                double typical=recent.Length==0?h*.12:recent[recent.Length/2];
                double threshold=words?Math.Max(h*.6,typical*2.5):h*.08;
                if(groups.Count==0||gap>threshold)groups.Add(new List<InkTrace>());
                groups[groups.Count-1].Add(t);
                if(gap>0 && gap<h*.6)gaps.Add(gap);
                right=Math.Max(right,t.Box.Right);
            }
            // Restore temporal order within each spatial component, including late accents.
            return groups.Select(g=>g.OrderBy(t=>t.Points[0].Time).ToList()).ToList();
        }
    }
    public sealed class Ticket {
        public string Session,Document,Page,Layer,Region; public int Revision;
        public bool Matches(string session,InkDocument d,Region r) {
            return Session==session && Document==d.Id && Page==d.Page && Layer==d.Layer && r!=null && Region==r.Id && Revision==r.Revision && !r.Manual && !r.Suppressed;
        }
    }
}
