using System.Collections.Generic;

namespace Tracos {
    public sealed class InkAnswer {
        public string Text = "";
        public string[] Alternatives = new string[0];
        public string Engine = "";
        // Native ranking is not a calibrated probability.
    }

    public interface IRecognitionEngine {
        InkAnswer Recognize(IList<InkTrace> traces, int language);
    }
}
