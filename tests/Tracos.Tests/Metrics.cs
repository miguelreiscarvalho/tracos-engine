using System;
using System.Linq;
using System.Text;

namespace Tracos {
    public static class Metrics {
        public static int Distance<T>(T[] expected,T[] actual) {
            var row=Enumerable.Range(0,actual.Length+1).ToArray();
            for(int i=1;i<=expected.Length;i++){
                int diagonal=row[0];row[0]=i;
                for(int j=1;j<=actual.Length;j++){
                    int previous=row[j];
                    row[j]=Math.Min(Math.Min(row[j]+1,row[j-1]+1),diagonal+(Object.Equals(expected[i-1],actual[j-1])?0:1));
                    diagonal=previous;
                }
            }
            return row[actual.Length];
        }
        public static double Cer(string expected,string actual){
            var a=expected.Normalize(NormalizationForm.FormC).ToCharArray();var b=actual.Normalize(NormalizationForm.FormC).ToCharArray();
            return (double)Distance(a,b)/Math.Max(1,a.Length);
        }
        public static double Wer(string expected,string actual){
            var a=expected.Normalize().Split((char[])null,StringSplitOptions.RemoveEmptyEntries);var b=actual.Normalize().Split((char[])null,StringSplitOptions.RemoveEmptyEntries);
            return (double)Distance(a,b)/Math.Max(1,a.Length);
        }
    }
}
