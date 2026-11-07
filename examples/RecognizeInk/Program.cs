using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using Tracos;

// Minimal consumer: references the libraries, without the editor or WPF.
public static class RecognizeInk {
    [STAThread] public static int Main(string[] args) {
        if (args.Length != 1) {
            Console.Error.WriteLine("Usage: Tracos.RecognizeInk.exe <traces.json>");
            return 2;
        }
        try {
            List<InkTrace> traces;
            using (var input = File.OpenRead(args[0])) {
                traces = (List<InkTrace>)new DataContractJsonSerializer(typeof(List<InkTrace>)).ReadObject(input);
            }
            if (traces == null || traces.Count == 0) throw new InvalidDataException("No ink traces.");
            foreach (var trace in traces) {
                if (trace == null || trace.Points == null || trace.Points.Count == 0)
                    throw new InvalidDataException("Each trace must contain points.");
                foreach (var point in trace.Points)
                    if (point == null || Double.IsNaN(point.X) || Double.IsInfinity(point.X) || Double.IsNaN(point.Y) || Double.IsInfinity(point.Y))
                        throw new InvalidDataException("Coordinates must be finite.");
            }
            IRecognitionEngine engine = new WindowsInkEngine();
            var answer = engine.Recognize(traces, 1046);
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine(answer.Text);
            return 0;
        } catch (Exception error) {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }
}
