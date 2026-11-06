using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace Tracos {
    public sealed class Work {
        public Ticket Ticket;
        public List<InkTrace> Traces;
    }
    public sealed class Completion {
        public Work Work; public InkAnswer Answer; public Exception Error; public double Milliseconds;
    }
    public sealed class RecognitionScheduler : IDisposable {
        readonly Dictionary<string,Work> pending=new Dictionary<string,Work>();
        readonly AutoResetEvent ready=new AutoResetEvent(false);
        readonly Thread thread;
        readonly IRecognitionEngine engine;
        readonly Action<Completion> completed;
        bool stopped;
        public RecognitionScheduler(IRecognitionEngine engine,Action<Completion> completed) {
            this.engine=engine;this.completed=completed;
            thread=new Thread(Run){IsBackground=true,Name="Reconhecimento STA"};
            thread.SetApartmentState(ApartmentState.STA);thread.Start();
        }
        public void Enqueue(Work work){lock(pending){if(stopped)return;pending[work.Ticket.Region]=work;ready.Set();}}
        public void Clear(){lock(pending)pending.Clear();}
        void Run(){
            while(true){
                ready.WaitOne();
                while(true){
                    Work work=null;
                    lock(pending){if(stopped)return;foreach(var entry in pending){work=entry.Value;break;}if(work!=null)pending.Remove(work.Ticket.Region);}
                    if(work==null)break;
                    var done=new Completion {Work=work};var watch=Stopwatch.StartNew();
                    try{done.Answer=engine.Recognize(work.Traces,1046);}catch(Exception e){done.Error=e;}
                    done.Milliseconds=watch.Elapsed.TotalMilliseconds;
                    lock(pending){if(stopped)return;}
                    completed(done);
                }
            }
        }
        public void Dispose(){lock(pending){stopped=true;pending.Clear();ready.Set();}}
    }
}
