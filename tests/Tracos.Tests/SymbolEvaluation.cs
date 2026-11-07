using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Tracos {
    [DataContract] public sealed class RecognitionSample {
        [DataMember] public string Name;
        [DataMember] public string Expected;
        [DataMember] public List<InkTrace> Traces;
    }
    public static class SymbolEvaluation {
        static string Csv(string value){return "\""+(value??"").Replace("\"","\"\"")+"\"";}
        public static int Run(string path) {
            List<RecognitionSample> samples;
            using(var input=File.OpenRead(path))samples=(List<RecognitionSample>)new DataContractJsonSerializer(typeof(List<RecognitionSample>)).ReadObject(input);
            Directory.CreateDirectory("local");
            using(var output=new StreamWriter("local/symbol-evaluation.csv",false,Encoding.UTF8)) {
                output.WriteLine("sample,policy,expected,actual,exact,cer,elapsed_ms");
                foreach(var sample in samples) {
                    if(sample.Traces==null||sample.Traces.Count==0||sample.Expected==null)throw new InvalidDataException("Amostra sem traços ou referência.");
                    foreach(string policy in new[]{"isolated-strokes","native-combined","local-grouped"}) {
                        var watch=Stopwatch.StartNew();string text;
                        if(policy=="isolated-strokes") {
                            var engine=new WindowsInkEngine(false);
                            text=String.Concat(sample.Traces.Select(t=>engine.Recognize(new List<InkTrace>{t},1046).Text));
                        } else if(policy=="native-combined")text=new WindowsInkEngine(false).Recognize(sample.Traces,1046).Text;
                        else {
                            var document=new InkDocument();
                            foreach(var trace in sample.Traces)Spatial.Append(document,trace);
                            var engine=new WindowsInkEngine();
                            text=String.Concat(document.Regions.Select(r=>engine.Recognize(r.Traces,1046).Text));
                        }
                        bool exact=sample.Expected.Normalize()==text.Normalize();
                        output.WriteLine(String.Join(",",new[]{Csv(sample.Name),Csv(policy),Csv(sample.Expected),Csv(text),exact.ToString(),Metrics.Cer(sample.Expected,text).ToString(System.Globalization.CultureInfo.InvariantCulture),watch.Elapsed.TotalMilliseconds.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)}));
                        Console.WriteLine(sample.Name+" | "+policy+" | expected="+sample.Expected+" actual="+text+" exact="+exact);
                    }
                }
            }
            return 0;
        }
    }
}
