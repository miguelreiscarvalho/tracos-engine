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
                var question=new InkDocument();var hook=Spatial.Append(question,Trace(100,100,110,97,120,105,110,116,110,125));hook.Text="2";
                var questionDot=Spatial.Append(question,Trace(109,134,111,136));
                Check(questionDot==hook&&hook.Traces.Count==2,"interrogação reúne corpo e ponto inferior");
                var exclamation=new InkDocument();var stem=Spatial.Append(exclamation,Trace(200,100,201,125));stem.Closed=true;
                Check(Spatial.Append(exclamation,Trace(198,138,201,140))==stem,"exclamação reúne ponto mesmo após hipótese encerrada");
                var reverse=new InkDocument();var dotFirst=Spatial.Append(reverse,Trace(100,140,102,142));
                Check(Spatial.Append(reverse,Trace(101,100,102,130))==dotFirst,"símbolo reúne ponto desenhado antes do corpo");
                var colon=new InkDocument();var upper=Spatial.Append(colon,Trace(300,100,304,103));
                Check(Spatial.Append(colon,Trace(301,116,304,119))==upper,"dois pontos não viram regiões separadas");
                Check(PunctuationCandidates.Select("°",new[]{"°",":"},upper.Traces)==":","dois pontos seleciona alternativa nativa compatível");
                Check(PunctuationCandidates.Select("°",new[]{"°","o"},upper.Traces)=="°","não inventa dois pontos ausentes nas alternativas");
                Check(PunctuationCandidates.Select("oo",new[]{"oo",":"},new[]{Trace(0,0,4,3),Trace(18,0,22,3)})=="oo","pontos horizontais não forçam dois pontos");
                var cedillaDoc=new InkDocument();var cedillaWord=Spatial.Append(cedillaDoc,Trace(100,100,120,103,116,130));
                Spatial.Append(cedillaDoc,Trace(125,100,130,125));cedillaWord.Closed=true;
                Check(Spatial.Append(cedillaDoc,Trace(106,133,112,137,104,141))==cedillaWord,"cedilha inferior preservada no caractere original");
                var distantSymbol=Spatial.Append(question,Trace(190,135,194,138));
                Check(distantSymbol!=hook,"ponto distante não altera símbolo anterior");
                var coverage=new InkDocument();coverage.Regions.Add(new Region {Text=CharacterCoverage.Target});
                Check(CharacterCoverage.PrintableAscii.Length==95&&Storage.Decode(Storage.Encode(coverage)).Regions[0].Text==CharacterCoverage.Target,"95 ASCII imprimíveis e acentos pt-BR persistem sem perda");
                var snapshots=DocumentSnapshots.Copy(page);var sourceCount=page.Regions[0].Traces.Count;
                snapshots.Regions[0].Text="editado";snapshots.Regions[0].Traces.RemoveAt(0);
                Check(page.Regions[0].Text=="casa"&&page.Regions[0].Traces.Count==sourceCount,"snapshots isolam texto e listas sem copiar todos os pontos");
                var boundsBefore=word.Box;Spatial.Append(page,Trace(108,80,112,82));
                Check(word.Box.Y<boundsBefore.Y,"bounds atualizam após inclusão de acento");
                var lineA=new Region {AnchorX=100,AnchorY=100,Text="texto"};lineA.Traces.Add(Trace(100,100,120,130));
                var lineB=new Region {AnchorX=180,AnchorY=104,Text="contínuo"};lineB.Traces.Add(Trace(180,104,225,133));
                var lineC=new Region {AnchorX=100,AnchorY=200,Text="outra"};lineC.Traces.Add(Trace(100,200,140,230));
                var metrics=new[]{new TextWordMetrics{Region=lineA,Width=60,Height=30,Baseline=23},new TextWordMetrics{Region=lineB,Width=90,Height=30,Baseline=23},new TextWordMetrics{Region=lineC,Width=70,Height=30,Baseline=23}};
                var composed=TextLineLayout.Compose(metrics,true,6);
                Check(composed[lineA.Id].Y==composed[lineB.Id].Y,"palavras da mesma linha compartilham baseline");
                Check(composed[lineB.Id].X==composed[lineA.Id].X+60+6,"texto contínuo usa espaço tipográfico em vez de lacunas do desenho");
                Check(composed[lineA.Id].Width==60&&composed[lineB.Id].Width==90,"largura da caligrafia não comprime glifos");
                Check(composed[lineC.Id].Y==200,"linhas separadas mantêm sua região vertical");
                var positions=TextLineLayout.Compose(metrics,false,6);
                Check(positions[lineB.Id].X==180&&positions[lineB.Id].Width==90,"modo posição original também preserva tamanho tipográfico");
                var protectedDoc=new InkDocument();var baseStroke=Trace(100,100,120,130);baseStroke.Points.ForEach(p=>p.Time+=1000);
                var protectedWord=Spatial.Append(protectedDoc,baseStroke);protectedWord.Text="equação";protectedWord.RecognizedCount=1;protectedWord.Closed=true;
                int protectedRevision=protectedWord.Revision;
                var annotation=Trace(95,95,125,135,95,95);annotation.Points.ForEach(p=>p.Time+=20000);
                var preservedAnnotation=Spatial.Append(protectedDoc,annotation);
                Check(preservedAnnotation.InkOnly&&protectedWord.Text=="equação"&&protectedWord.Revision==protectedRevision,"traço tardio sobre palavra pronta vira anotação sem reinterpretação");
                var recentAccent=Trace(105,94,110,90);recentAccent.Points.ForEach(p=>p.Time+=3000);
                Check(Spatial.Append(protectedDoc,recentAccent)==protectedWord,"janela recente ainda aceita acento tardio");
                var inkRoundtrip=Storage.Decode(Storage.Encode(protectedDoc));
                Check(inkRoundtrip.Layout=="lines"&&inkRoundtrip.Regions[1].InkOnly,"layout e anotação livre persistem no documento");
                var cursive=new InkDocument();var cursiveWord=Spatial.Append(cursive,Trace(100,100,150,125));
                foreach(int dx in new[]{0,5,10,15,20})Spatial.Append(cursive,Trace(110+dx,113,113+dx,121));
                Check(Spatial.Append(cursive,Trace(135,88,143,99))==cursiveWord,"traços curtos não isolam til de palavra cursiva");
                var timing=new RecognitionTiming();timing.Started(0);timing.Ended(100);
                Check(!timing.Ready(260,false,false),"pausa de 160 ms não converte palavra incompleta");
                timing.Started(400);timing.Ended(550);
                Check(!timing.Ready(1000,false,false),"novo traço reinicia espera de conversão");
                Check(!timing.Ready(1600,false,true)&&timing.Ready(2200,false,true),"caneta próxima ganha tempo para acentos");
                Check(timing.Ready(1200,true,false),"bloco encerrado pode reconhecer após afastamento");
                timing.Started(2300);Check(!timing.Ready(10000,true,false)&&!timing.CanSave(10000),"contato ativo bloqueia conversão e captura de autosave");
                var persistenceStarted=new ManualResetEvent(false);var allowPersistence=new ManualResetEvent(false);var savedTexts=new List<string>();
                using(var writer=new BackgroundPersistence(snapshot=>{persistenceStarted.Set();if(!allowPersistence.WaitOne(3000))throw new TimeoutException();lock(savedTexts)savedTexts.Add(snapshot.Font);})){
                    writer.Enqueue(new InkDocument {Font="first"});Check(persistenceStarted.WaitOne(3000),"autosave começa fora da thread chamadora");
                    writer.Enqueue(new InkDocument {Font="intermediate"});writer.Enqueue(new InkDocument {Font="latest"});
                    allowPersistence.Set();Check(writer.Flush(3000),"autosave aguarda gravação consistente no fechamento");
                    lock(savedTexts)Check(savedTexts.SequenceEqual(new[]{"first","latest"}),"autosave coalesce versões pendentes");
                }
                using(var failedWriter=new BackgroundPersistence(snapshot=>{throw new IOException("simulated write failure");})){
                    failedWriter.Enqueue(new InkDocument());bool reported=false;
                    try{failedWriter.Flush(3000);}catch(InvalidOperationException){reported=true;}
                    Check(reported&&failedWriter.LastError!=null,"falha de autosave é reportada sem simular sucesso");
                }
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
                var pausedEngine=new BlockingEngine();pausedEngine.Release.Set();var resumed=new ManualResetEvent(false);
                using(var queue=new RecognitionScheduler(pausedEngine,c=>resumed.Set())){
                    queue.SetPaused(true);queue.Enqueue(new Work{Ticket=new Ticket {Region="obsolete"},Traces=new List<InkTrace>{isolated,accent}});queue.Cancel("obsolete");
                    queue.Enqueue(new Work{Ticket=new Ticket {Region="paused"},Traces=new List<InkTrace>{isolated}});
                    Check(!pausedEngine.Started.WaitOne(60),"fila não inicia motor enquanto caneta escreve");
                    queue.SetPaused(false);Check(resumed.WaitOne(3000),"fila retoma depois do contato");
                    lock(pausedEngine.Counts)Check(pausedEngine.Counts.SequenceEqual(new[]{1}),"revisão cancelada não consome chamada do motor");
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
            if(args.Length==2&&args[0]=="--evaluate-symbols")return SymbolEvaluation.Run(args[1]);
            if(args.Length==2&&args[0]=="--profile")return PerformanceEvaluation.Run(args[1]);
            return Launch();
        }
        static int Launch(){var app=new System.Windows.Application();return app.Run(new Editor());}
    }
}
