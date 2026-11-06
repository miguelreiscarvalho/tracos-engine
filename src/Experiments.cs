using System;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace Tracos {
    public static class Experiments {
        public static InkTrace Trace(params double[] xy) {
            var t=new InkTrace {Device="synthetic"};
            for(int i=0;i<xy.Length;i+=2)t.Points.Add(new InkPoint {X=xy[i],Y=xy[i+1],Time=i*8,Pressure=.5f});
            return t;
        }
        public static List<InkTrace> Sample() {
            return new List<InkTrace> {
                Trace(46,40,38,34,28,34,20,42,18,53,22,64,32,68,44,62), // c
                Trace(73,41,67,35,57,38,51,48,52,60,60,65,70,60,74,46,74,35,74,65,80,65), // a
                Trace(103,36,91,34,85,42,89,49,99,52,102,60,94,66,84,64), // s
                Trace(130,41,124,35,114,38,108,48,109,60,117,65,127,60,131,46,131,35,131,65,137,65)
            };
        }
        static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL "+name);Console.WriteLine("PASS "+name);}
        sealed class BlockingEngine : IRecognitionEngine {
            public readonly ManualResetEvent Started=new ManualResetEvent(false),Release=new ManualResetEvent(false);
            public readonly List<int> Counts=new List<int>();
            public InkAnswer Recognize(IList<InkTrace> traces,int language){
                lock(Counts)Counts.Add(traces.Count);
                Started.Set();if(!Release.WaitOne(3000))throw new TimeoutException();
                return new InkAnswer {Text="test"};
            }
        }
        public static int Test() {
            try {
                var d=new InkDocument();var r=new Region();r.Traces.Add(Trace(1,1,1,10));d.Regions.Add(r);
                var ticket=new Ticket {Session="s",Document=d.Id,Page=d.Page,Layer=d.Layer,Region=r.Id,Revision=0};
                Check(ticket.Matches("s",d,r),"ticket atual aceito");r.Revision++;
                Check(!ticket.Matches("s",d,r),"revisão antiga rejeitada");r.Revision--;
                Check(!ticket.Matches("new",d,r),"sessão antiga rejeitada");
                Check(!ticket.Matches("s",d,null),"região apagada rejeitada");
                r.Manual=true;Check(!ticket.Matches("s",d,r),"edição manual protegida");r.Manual=false;
                var history=new History();history.Record(d);r.Text="á";
                var original=history.Undo(d);Check(original.Regions[0].Text==""&&original.Regions[0].Traces.Count==1,"undo restaura tinta");
                var again=history.Redo(original);Check(again.Regions[0].Text=="á","redo sem motor");
                var encoded=Storage.Encode(again);var recovered=Storage.Decode(encoded);
                Check(recovered.Regions[0].Traces[0].Points[0].Pressure==.5f&&recovered.Regions[0].Text=="á","persistência unicode e pressão");
                var isolated=Trace(0,0,10,30);var accent=Trace(4,-9,8,-5);var second=Trace(45,0,55,30);
                Check(Spatial.Groups(new[]{isolated,accent},true).Count==1,"acento tardio preservado");
                Check(Spatial.Groups(new[]{isolated,second},true).Count==2,"lacuna separa palavras");
                var before=Storage.Encode(d);Spatial.Groups(r.Traces,true);Check(before.SequenceEqual(Storage.Encode(d)),"análise não altera originais");
                string path=Path.Combine(Path.GetTempPath(),"tracos-test-"+Guid.NewGuid()+".json");
                try{Storage.Save(path,d);r.Text="novo";Storage.Save(path,d);Check(Storage.Decode(File.ReadAllBytes(path)).Regions[0].Text=="novo"&&File.Exists(path+".bak"),"gravação atômica e backup");}
                finally{File.Delete(path);File.Delete(path+".bak");}
                Check(Metrics.Cer("ação","acão")==.25&&Metrics.Cer("á","a\u0301")==0,"CER unicode normalizado");
                Check(Metrics.Wer("casa bonita","casa")==.5,"WER por palavra");
                var page=new InkDocument();
                var word=Spatial.Append(page,Trace(100,100,105,130,115,110,120,130));
                word.Text="casa";word.RecognizedCount=1;
                var letter=Spatial.Append(page,Trace(124,104,129,128,135,110,138,130));
                Check(Object.ReferenceEquals(word,letter),"letras próximas revisam somente palavra ativa");
                var nextWord=Spatial.Append(page,Trace(175,103,180,130,190,112,195,130));
                Check(nextWord!=word&&word.Closed&&word.Text=="casa","espaço inicia bloco local e protege palavra anterior");
                var nextLine=Spatial.Append(page,Trace(178,143,185,165,195,146,200,168));
                Check(nextLine!=nextWord&&nextWord.Closed,"linha próxima não é anexada à linha anterior");
                var late=Spatial.Append(page,Trace(106,92,110,88,114,92));
                Check(late==word&&late.Closed&&nextLine.Traces.Count==1,"acento tardio só reabre a palavra sobreposta");
                var elsewhere=Spatial.Append(page,Trace(250,95,275,120));
                Check(elsewhere!=word&&word.Text=="casa","nova palavra não reinterpreta texto distante");
                var placement=TextPlacement.Fit(new Bounds {X=400,Y=200,Right=490,Bottom=228},180,40);
                Check(placement.X==400&&placement.Y==200&&placement.ScaleX*180<=90&&placement.ScaleY*40<=28,"texto permanece dentro da área original");
                var moved=TextPlacement.Fit(new Bounds {X=600,Y=500,Right=690,Bottom=528},180,40);
                Check(moved.X-placement.X==200&&moved.Y-placement.Y==300&&moved.ScaleX==placement.ScaleX,"layout independe de posição zoom e rolagem");
                var state=Storage.Decode(Storage.Encode(page));
                Check(state.Regions[0].Closed&&state.Regions[0].Traces.Count==3,"persistência de blocos fechados e acento original");
                var delayed=new BlockingEngine();var finished=new ManualResetEvent(false);int completions=0;
                using(var queue=new RecognitionScheduler(delayed,c=>{if(Interlocked.Increment(ref completions)==2)finished.Set();})){
                    queue.Enqueue(new Work {Ticket=new Ticket{Region="r",Revision=1},Traces=new List<InkTrace>{isolated}});
                    Check(delayed.Started.WaitOne(3000),"worker iniciou sem bloquear chamador");
                    queue.Enqueue(new Work {Ticket=new Ticket{Region="r",Revision=2},Traces=new List<InkTrace>{isolated,accent}});
                    queue.Enqueue(new Work {Ticket=new Ticket{Region="r",Revision=3},Traces=new List<InkTrace>{isolated,accent,second}});
                    delayed.Release.Set();Check(finished.WaitOne(3000),"fila conclui tarefas assíncronas");
                    lock(delayed.Counts)Check(delayed.Counts.SequenceEqual(new[]{1,3}),"fila coalesce revisão intermediária");
                }
                var guardDoc=new InkDocument();var guardRegion=new Region();guardDoc.Regions.Add(guardRegion);
                var captured=new Ticket{Session="old",Document=guardDoc.Id,Page=guardDoc.Page,Layer=guardDoc.Layer,Region=guardRegion.Id,Revision=0};
                var delayed2=new BlockingEngine();var rejected=new ManualResetEvent(false);bool applied=false;
                using(var queue=new RecognitionScheduler(delayed2,c=>{applied=c.Work.Ticket.Matches("new",guardDoc,guardRegion);rejected.Set();})){
                    queue.Enqueue(new Work{Ticket=captured,Traces=new List<InkTrace>{isolated}});
                    Check(delayed2.Started.WaitOne(3000),"reconhecimento pendente durante edição");guardRegion.Revision++;delayed2.Release.Set();
                    Check(rejected.WaitOne(3000)&&!applied,"resultado atrasado não aplicado após edição e nova sessão");
                }
                return 0;
            } catch(Exception e){Console.WriteLine(e);return 1;}
        }
        public static int Run() {
            Directory.CreateDirectory("local");
            using(var output=new StreamWriter("local/experiments.csv",false,System.Text.Encoding.UTF8)) {
                output.WriteLine("sample,policy,prefix,elapsed_ms,expected,recognized");
                var engine=new WindowsInkEngine();
                var samples=new Dictionary<string,List<InkTrace>> {
                    {"casa-synthetic",Sample()},
                    {"i-late-dot-synthetic",new List<InkTrace>{Trace(30,40,29,55,31,64,37,63),Trace(31,28,32,29)}},
                    {"cursive-loop-synthetic",new List<InkTrace>{Trace(20,55,26,40,33,45,30,60,23,60,25,49,40,55,46,40,53,45,50,60,43,60,45,49,60,55)}}
                };
                foreach(var sample in samples)foreach(string policy in new[]{"A-word","B-component","C-prefix"}) {
                    var traces=sample.Value;
                    int first=policy=="C-prefix"?1:traces.Count;
                    for(int n=first;n<=traces.Count;n++) {
                        var input=traces.Take(n).ToList();var watch=Stopwatch.StartNew();
                        var groups=policy=="C-prefix"?new List<List<InkTrace>>{input}:Spatial.Groups(input,policy=="A-word");
                        var text=String.Join(policy=="B-component"?"":" ",groups.Select(g=>engine.Recognize(g,1046).Text));
                        watch.Stop();
                        output.WriteLine(String.Join(",",new[]{sample.Key,policy,n.ToString(),watch.Elapsed.TotalMilliseconds.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),sample.Key.StartsWith("casa")&&n==traces.Count?"casa":"", "\""+text.Replace("\"","\"\"")+"\""}));
                        Console.WriteLine(sample.Key+" "+policy+" prefix="+n+" -> "+text+" ("+watch.ElapsedMilliseconds+" ms)");
                    }
                }
            }
            return 0;
        }
        public static int Replay(string path) {
            var original=Storage.Decode(File.ReadAllBytes(path));
            var replay=new InkDocument();
            foreach(var trace in original.Regions.SelectMany(r=>r.Traces).OrderBy(t=>t.Points[0].Time))
                Spatial.Append(replay,trace);
            var engine=new WindowsInkEngine();
            foreach(var region in replay.Regions) {
                var result=engine.Recognize(region.Traces,1046);
                region.Text=result.Text;region.RecognizedCount=region.Traces.Count;
                Console.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "x={0:F1} y={1:F1} w={2:F1} h={3:F1} strokes={4}: {5}",region.Box.X,region.Box.Y,region.Box.Right-region.Box.X,region.Box.Height,region.Traces.Count,region.Text));
            }
            // Local evidence only: never overwrite or automatically re-recognize
            // the user's recovered document.
            Storage.Save("local/replay-local-placement.json",replay);
            Console.WriteLine("Original traces: "+original.Regions.Sum(r=>r.Traces.Count)+"; replay traces: "+replay.Regions.Sum(r=>r.Traces.Count)+"; local groups: "+replay.Regions.Count);
            return 0;
        }
    }
    public static class Program {
        [STAThread] public static int Main(string[] args) {
            if(args.Contains("--test"))return Experiments.Test();
            if(args.Contains("--experiment"))return Experiments.Run();
            if(args.Length==2&&args[0]=="--replay")return Experiments.Replay(args[1]);
            return Launch();
        }
        static int Launch(){var app=new System.Windows.Application();return app.Run(new Editor());}
    }
}
