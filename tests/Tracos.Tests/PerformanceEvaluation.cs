using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace Tracos {
    public static class PerformanceEvaluation {
        static void Measure(StreamWriter output,string name,int count,Action action){
            action();var samples=new List<double>();
            for(int i=0;i<count;i++){var watch=Stopwatch.StartNew();action();samples.Add(watch.Elapsed.TotalMilliseconds);}
            samples.Sort();
            string result=String.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2:F4},{3:F4}",name,count,samples[count/2],samples[(int)Math.Ceiling(count*.95)-1]);
            output.WriteLine(result);Console.WriteLine(result);
        }
        public static int Run(string path){
            var doc=Storage.Decode(File.ReadAllBytes(path));
            var region=doc.Regions.Last(r=>r.Traces.Count>0);
            var history=new History();
            Directory.CreateDirectory("local");
            using(var output=new StreamWriter("local/performance-stability.csv")){
                output.WriteLine("operation,n,p50_ms,p95_ms");
                Measure(output,"previous_history_json_encode",12,()=>{Storage.Encode(doc);});
                Measure(output,"previous_recognition_document_json_roundtrip",12,()=>{Storage.Decode(Storage.Encode(doc));});
                Measure(output,"current_history_metadata_snapshot",24,()=>{history.Record(doc);});
                Measure(output,"current_recognition_region_list_snapshot",24,()=>{new List<InkTrace>(region.Traces);});
                Measure(output,"current_autosave_snapshot_capture",24,()=>{DocumentSnapshots.Copy(doc);});
                Measure(output,"current_cached_document_bounds",24,()=>{foreach(var r in doc.Regions){var box=r.Box;if(box.Height<0)throw new Exception();}});
            }
            Console.WriteLine("Document: "+doc.Regions.Count+" regions, "+doc.Regions.Sum(r=>r.Traces.Count)+" strokes; file not modified.");
            return 0;
        }
    }
}
