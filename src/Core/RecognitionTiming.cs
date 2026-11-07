using System;
using System.Linq;
using System.Collections.Generic;

namespace Tracos {
    public sealed class RecognitionTiming {
        readonly Queue<long> gaps=new Queue<long>();
        long lastUp;
        bool hasUp;
        public bool Writing {get;private set;}
        public double QuietDelayMs {
            get {
                if(gaps.Count==0)return 1200;
                var ordered=gaps.OrderBy(g=>g).ToArray();
                return Math.Max(1000,Math.Min(2200,ordered[ordered.Length/2]*2+400));
            }
        }
        public void Started(long now) {
            if(hasUp&&now>=lastUp&&now-lastUp<=4000){gaps.Enqueue(now-lastUp);if(gaps.Count>12)gaps.Dequeue();}
            Writing=true;
        }
        public void Ended(long now){Writing=false;hasUp=true;lastUp=now;}
        public bool CanSave(long now){return !Writing&&(!hasUp||now-lastUp>=QuietDelayMs);}
        public bool Ready(long now,bool closed,bool penNear) {
            if(Writing||!hasUp)return false;
            double delay=closed?Math.Max(400,QuietDelayMs*.6):QuietDelayMs;
            if(penNear&&!closed)delay*=1.6;
            return now-lastUp>=delay;
        }
    }
}
