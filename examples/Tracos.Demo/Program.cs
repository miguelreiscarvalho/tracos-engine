using System;

namespace Tracos {
    public static class DemoProgram {
        [STAThread] public static int Main() {
            var app = new System.Windows.Application();
            return app.Run(new Editor());
        }
    }
}
