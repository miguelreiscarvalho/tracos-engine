using System;
using System.Threading;

namespace Tracos {
    public sealed class BackgroundPersistence : IDisposable {
        readonly object gate=new object();
        readonly AutoResetEvent ready=new AutoResetEvent(false);
        readonly Action<InkDocument> save;
        readonly Thread worker;
        InkDocument pending;
        long requested,completed;
        bool stopped;
        Exception error;
        public Exception LastError {get{lock(gate)return error;}}
        public BackgroundPersistence(Action<InkDocument> save) {
            this.save=save;
            worker=new Thread(Run){IsBackground=true,Name="Salvamento do documento"};worker.Start();
        }
        public void Enqueue(InkDocument snapshot){lock(gate){if(stopped)throw new ObjectDisposedException("BackgroundPersistence");pending=snapshot;requested++;ready.Set();}}
        public bool Flush(int milliseconds) {
            var limit=DateTime.UtcNow.AddMilliseconds(milliseconds);
            lock(gate){
                while(completed<requested){var left=(int)(limit-DateTime.UtcNow).TotalMilliseconds;if(left<=0)return false;Monitor.Wait(gate,left);}
                if(error!=null)throw new InvalidOperationException("Não foi possível salvar o documento.",error);
                return true;
            }
        }
        void Run(){
            while(true){
                ready.WaitOne();
                while(true){
                    InkDocument snapshot;long version;
                    lock(gate){if(stopped)return;snapshot=pending;version=requested;pending=null;}
                    if(snapshot==null)break;
                    Exception failure=null;
                    try{save(snapshot);}catch(Exception e){failure=e;}
                    lock(gate){error=failure;completed=version;Monitor.PulseAll(gate);}
                }
            }
        }
        public void Dispose(){lock(gate){stopped=true;pending=null;ready.Set();}}
    }
}
