using System;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Tracos {
    public sealed class InkSurface : FrameworkElement {
        public InkDocument Document;
        public InkTrace Active;
        public string Selected;
        public bool SelectMode;
        public bool FreeInk;
        public event Action<InkTrace> Collected;
        public event Action<Region> EditRequested;
        public event Action Began;
        public event Action Ended;
        public Point? HoverPoint;
        public string ActiveRegion;
        UIElement inputRoot;
        GeneralTransform inputTransform;
        readonly Stopwatch clock=Stopwatch.StartNew();
        readonly long timeOrigin=DateTime.UtcNow.Ticks/TimeSpan.TicksPerMillisecond;
        public bool Writing {get{return Active!=null;}}
        public double LastPaintMs;
        sealed class GlyphCache {public string Text,Font;public double Size,Dpi,Width,Height,Baseline;public Geometry Geometry;}
        readonly Dictionary<string,GlyphCache> glyphCache=new Dictionary<string,GlyphCache>();
        Dictionary<string,TextWordPlacement> textPositions=new Dictionary<string,TextWordPlacement>();
        bool layoutDirty=true;
        public void SetDocument(InkDocument document){Document=document;layoutDirty=true;InvalidateVisual();}
        sealed class TraceCache {public int Count;public Geometry Geometry;}
        readonly Dictionary<string,TraceCache> traceCache=new Dictionary<string,TraceCache>();
        static readonly Pen inkPen=MakePen(Brushes.SteelBlue,2.2);
        static readonly Pen linePen=MakePen(new SolidColorBrush(Color.FromRgb(231,236,241)),1);
        static readonly Brush textBrush=new SolidColorBrush(Color.FromRgb(26,39,53));
        static Pen MakePen(Brush brush,double width){var pen=new Pen(brush,width){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};pen.Freeze();return pen;}
        GlyphCache Glyphs(Region region) {
            double dpi=VisualTreeHelper.GetDpi(this).PixelsPerDip;
            GlyphCache cached;
            if(glyphCache.TryGetValue(region.Id,out cached)&&cached.Text==region.Text&&cached.Font==Document.Font&&cached.Size==Document.FontSize&&cached.Dpi==dpi)
                return cached;
            var text=new FormattedText(region.Text,System.Globalization.CultureInfo.GetCultureInfo("pt-BR"),FlowDirection.LeftToRight,new Typeface(Document.Font),Document.FontSize,Brushes.Black,dpi);
            var geometry=text.BuildGeometry(new Point(0,0));geometry.Freeze();
            cached=new GlyphCache{Text=region.Text,Font=Document.Font,Size=Document.FontSize,Dpi=dpi,Geometry=geometry,Width=text.WidthIncludingTrailingWhitespace,Height=text.Height,Baseline=text.Baseline};
            glyphCache[region.Id]=cached;return cached;
        }
        void EnsureLayout(){
            if(!layoutDirty||Document==null)return;
            var words=Document.Regions.Where(r=>!r.InkOnly&&!String.IsNullOrWhiteSpace(r.Text)).Select(r=>{
                var glyph=Glyphs(r);return new TextWordMetrics{Region=r,Width=glyph.Width,Height=glyph.Height,Baseline=glyph.Baseline};
            }).ToArray();
            double space=new FormattedText(" ",System.Globalization.CultureInfo.GetCultureInfo("pt-BR"),FlowDirection.LeftToRight,new Typeface(Document.Font),Document.FontSize,Brushes.Black,VisualTreeHelper.GetDpi(this).PixelsPerDip).WidthIncludingTrailingWhitespace;
            textPositions=TextLineLayout.Compose(words,Document.Layout!="positions",space);layoutDirty=false;
        }
        Bounds DisplayBox(Region region){TextWordPlacement placed;return textPositions.TryGetValue(region.Id,out placed)?placed.Box:region.Box;}
        public bool OverlapsProtectedText(InkTrace trace){
            EnsureLayout();
            return Document.Regions.Any(r=>!r.InkOnly&&!String.IsNullOrEmpty(r.Text)&&!Spatial.MayRevise(r,trace)&&Spatial.Intersects(DisplayBox(r),trace.Box));
        }
        public InkSurface(){
            Width=2400;Height=1800;Focusable=false;ClipToBounds=true;
            Stylus.SetIsFlicksEnabled(this,false);
            Stylus.SetIsPressAndHoldEnabled(this,false);
            Stylus.SetIsTapFeedbackEnabled(this,false);
            Stylus.SetIsTouchFeedbackEnabled(this,false);
            RequestBringIntoView+=(s,e)=>e.Handled=true;
        }
        Region Hit(Point p){EnsureLayout();return Document.Regions.LastOrDefault(r=>{var b=DisplayBox(r);return p.X>=b.X-8&&p.X<=b.Right+8&&p.Y>=b.Y-8&&p.Y<=b.Bottom+8;});}
        void Start(Point p,string device,int clicks){
            var region=Hit(p);
            if(SelectMode||(device=="mouse"&&clicks==2)){Selected=region==null?null:region.Id;InvalidateVisual();if(clicks==2&&region!=null&&EditRequested!=null)EditRequested(region);return;}
            Active=new InkTrace {Device=device};
            var probe=new InkTrace();probe.Points.Add(new InkPoint{X=p.X,Y=p.Y,Time=timeOrigin+clock.ElapsedMilliseconds});
            ActiveRegion=region!=null&&!FreeInk&&Spatial.MayRevise(region,probe)?region.Id:null;
            inputRoot=(UIElement)Window.GetWindow(this)??this;
            inputTransform=inputRoot==this?null:TransformToVisual(inputRoot).Inverse;
            if(Began!=null)Began();
        }
        Point ToDocument(Point p){return inputTransform==null?p:inputTransform.Transform(p);}
        void AddMouse(Point p){if(Active!=null){p=ToDocument(p);Active.Points.Add(new InkPoint {X=p.X,Y=p.Y,Time=timeOrigin+clock.ElapsedMilliseconds,Pressure=.5f});InvalidateVisual();}}
        void AddStylus(StylusEventArgs e){
            if(Active==null)return;
            foreach(var p in e.GetStylusPoints(inputRoot??this)) {
                var position=ToDocument(new Point(p.X,p.Y));
                Active.Points.Add(new InkPoint {X=position.X,Y=position.Y,Time=timeOrigin+clock.ElapsedMilliseconds,Pressure=p.PressureFactor,
                    TiltX=p.HasProperty(StylusPointProperties.XTiltOrientation)?(float?)p.GetPropertyValue(StylusPointProperties.XTiltOrientation):null,
                    TiltY=p.HasProperty(StylusPointProperties.YTiltOrientation)?(float?)p.GetPropertyValue(StylusPointProperties.YTiltOrientation):null});
            }
            InvalidateVisual();
        }
        void Finish(){var t=Active;Active=null;ActiveRegion=null;if(t!=null){if(t.Points.Count>0&&Collected!=null)Collected(t);if(Ended!=null)Ended();}InvalidateVisual();}
        public void CommitPending(){Finish();}
        protected override void OnStylusDown(StylusDownEventArgs e){Start(e.GetPosition(this),"stylus",e.TapCount);if(Active!=null){CaptureStylus();AddStylus(e);}e.Handled=true;}
        protected override void OnStylusMove(StylusEventArgs e){HoverPoint=e.GetPosition(this);if(Active!=null){if(e.StylusDevice.InAir){Finish();ReleaseStylusCapture();}else AddStylus(e);e.Handled=true;}}
        protected override void OnStylusLeave(StylusEventArgs e){HoverPoint=null;}
        protected override void OnStylusUp(StylusEventArgs e){if(Active!=null){AddStylus(e);Finish();ReleaseStylusCapture();}e.Handled=true;}
        protected override void OnLostStylusCapture(StylusEventArgs e){Finish();}
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e){if(e.StylusDevice!=null)return;Start(e.GetPosition(this),"mouse",e.ClickCount);if(Active!=null){CaptureMouse();AddMouse(e.GetPosition(inputRoot??this));}e.Handled=true;}
        protected override void OnMouseMove(MouseEventArgs e){if(e.StylusDevice==null&&Active!=null&&e.LeftButton==MouseButtonState.Pressed)AddMouse(e.GetPosition(inputRoot??this));}
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e){if(e.StylusDevice!=null)return;AddMouse(e.GetPosition(inputRoot??this));Finish();ReleaseMouseCapture();}
        protected override void OnLostMouseCapture(MouseEventArgs e){Finish();}
        protected override void OnTouchDown(TouchEventArgs e){e.Handled=true;}
        protected override void OnStylusSystemGesture(StylusSystemGestureEventArgs e){e.Handled=true;}
        void DrawTrace(DrawingContext dc,InkTrace trace){
            if(trace.Points.Count==0)return;
            var points=trace.Points;
            if(points.Count==1){dc.DrawEllipse(Brushes.SteelBlue,null,new Point(points[0].X,points[0].Y),1.5,1.5);return;}
            TraceCache cached;
            if(!traceCache.TryGetValue(trace.Id,out cached)||cached.Count!=points.Count){
                var geometry=new StreamGeometry();
                using(var g=geometry.Open()){g.BeginFigure(new Point(points[0].X,points[0].Y),false,false);g.PolyLineTo(points.Skip(1).Select(p=>new Point(p.X,p.Y)).ToArray(),true,false);}
                geometry.Freeze();cached=new TraceCache{Count=points.Count,Geometry=geometry};traceCache[trace.Id]=cached;
            }
            dc.DrawGeometry(null,inkPen,cached.Geometry);
        }
        protected override void OnRender(DrawingContext dc){
            var watch=Stopwatch.StartNew();dc.DrawRectangle(Brushes.White,null,new Rect(0,0,Width,Height));
            EnsureLayout();
            for(int y=96;y<Height;y+=64)dc.DrawLine(linePen,new Point(32,y),new Point(Width-32,y));
            if(Document!=null&&glyphCache.Count>Document.Regions.Count) {
                var ids=new HashSet<string>(Document.Regions.Select(r=>r.Id));
                foreach(var id in glyphCache.Keys.Where(id=>!ids.Contains(id)).ToArray())glyphCache.Remove(id);
            }
            if(Document!=null)foreach(var r in Document.Regions){
                bool showText=!r.InkOnly&&!String.IsNullOrEmpty(r.Text)&&(r.Manual||(r.RecognizedCount==r.Traces.Count&&r.Id!=ActiveRegion));
                if(showText) {
                    var glyphs=Glyphs(r);
                    TextWordPlacement placement;
                    if(textPositions.TryGetValue(r.Id,out placement)) {
                        dc.PushTransform(new TranslateTransform(placement.X,placement.Y));
                        dc.DrawGeometry(textBrush,null,glyphs.Geometry);dc.Pop();
                    }
                }
                foreach(var t in r.Traces.Skip(showText?r.RecognizedCount:0))DrawTrace(dc,t);
                if(Selected==r.Id){var b=DisplayBox(r);dc.DrawRectangle(null,new Pen(Brushes.DarkOrange,1),new Rect(b.X-5,b.Y-8,Math.Max(10,b.Right-b.X+10),Math.Max(10,b.Height+16)));}
            }
            if(Active!=null)DrawTrace(dc,Active);
            LastPaintMs=watch.Elapsed.TotalMilliseconds;
        }
    }
    public sealed class Editor : Window {
        InkDocument document=new InkDocument();
        History history=new History();
        string session=Guid.NewGuid().ToString();
        readonly InkSurface surface=new InkSurface();
        readonly TextBlock status=new TextBlock {Margin=new Thickness(12,8,12,8)};
        readonly RecognitionScheduler scheduler;
        readonly BackgroundPersistence persistence;
        readonly RecognitionTiming activity=new RecognitionTiming();
        readonly Stopwatch timeline=Stopwatch.StartNew();
        readonly Dictionary<string,Completion> deferred=new Dictionary<string,Completion>();
        string latestWrittenRegion;
        readonly DispatcherTimer timer=new DispatcherTimer();
        readonly Dictionary<string,DateTime> dirty=new Dictionary<string,DateTime>();
        readonly string recovery;
        readonly ScrollViewer viewer;
        double lockedHorizontal,lockedVertical;
        bool restoringViewport;
        Exception reportedSaveError;
        Task manualSave=Task.FromResult(0);
        bool closing,saveNeeded;
        ComboBox fontSelector,sizeSelector,layoutSelector;
        bool syncingControls;
        DateTime lastSave=DateTime.UtcNow;
        public Editor(){
            Title="Traços — escrita para texto";Width=1180;Height=820;MinWidth=760;MinHeight=500;
            recovery=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","local","recovery.json");Directory.CreateDirectory(Path.GetDirectoryName(recovery));
            if(File.Exists(recovery)){try{document=Storage.Decode(File.ReadAllBytes(recovery));}catch{try{document=Storage.Decode(File.ReadAllBytes(recovery+".bak"));}catch{status.Text="Não foi possível recuperar o documento; arquivos preservados.";}}}
            foreach(var region in document.Regions)region.Closed=true;
            surface.Document=document;
            var root=new DockPanel {Background=new SolidColorBrush(Color.FromRgb(244,247,250))};Content=root;
            var toolbar=new WrapPanel {Margin=new Thickness(8)};DockPanel.SetDock(toolbar,Dock.Top);root.Children.Add(toolbar);
            Add(toolbar,"Desfazer",()=>Restore(false));Add(toolbar,"Refazer",()=>Restore(true));
            Add(toolbar,"Salvar…",SaveAs);Add(toolbar,"Abrir…",Open);
            var select=new CheckBox {Content="Selecionar",Margin=new Thickness(12,8,8,8)};select.Checked+=(s,e)=>surface.SelectMode=true;select.Unchecked+=(s,e)=>surface.SelectMode=false;toolbar.Children.Add(select);
            Add(toolbar,"Editar",()=>EditSelected());Add(toolbar,"Apagar seleção",DeleteSelected);
            var fonts=new ComboBox {Width=150,Margin=new Thickness(8,2,4,2),ItemsSource=new[]{"Segoe UI","Arial","Georgia","Consolas"},SelectedItem=document.Font};
            fontSelector=fonts;
            fonts.SelectionChanged+=(s,e)=>{if(syncingControls||fonts.SelectedItem==null)return;history.Record(document);document.Font=fonts.SelectedItem.ToString();Changed();};toolbar.Children.Add(fonts);
            var sizes=new ComboBox {Width=65,Margin=new Thickness(4,2,4,2),ItemsSource=new double[]{18,22,26,32,40},SelectedItem=document.FontSize};
            sizeSelector=sizes;
            sizes.SelectionChanged+=(s,e)=>{if(syncingControls||sizes.SelectedItem==null)return;history.Record(document);document.FontSize=(double)sizes.SelectedItem;Changed();};toolbar.Children.Add(sizes);
            var layout=new ComboBox {Width=145,Margin=new Thickness(4,2,4,2),ItemsSource=new[]{"Texto em linhas","Posição original"},SelectedIndex=document.Layout=="positions"?1:0};
            layoutSelector=layout;
            layout.SelectionChanged+=(s,e)=>{if(syncingControls)return;history.Record(document);document.Layout=layout.SelectedIndex==1?"positions":"lines";Changed();};toolbar.Children.Add(layout);
            var ink=new CheckBox{Content="Tinta livre",Margin=new Thickness(8),ToolTip="Preserva fórmulas e desenhos sem converter em texto"};
            ink.Checked+=(s,e)=>surface.FreeInk=true;ink.Unchecked+=(s,e)=>surface.FreeInk=false;toolbar.Children.Add(ink);
            var zoom=new Slider {Minimum=.5,Maximum=2,Value=1,Width=100,Margin=new Thickness(8),ToolTip="Zoom"};zoom.ValueChanged+=(s,e)=>surface.LayoutTransform=new ScaleTransform(zoom.Value,zoom.Value);toolbar.Children.Add(zoom);
            DockPanel.SetDock(status,Dock.Bottom);root.Children.Add(status);
            var hint=new TextBlock {Text="Escreva livremente. A conversão aguarda uma pausa para completar letras, pontos e acentos. Duplo clique com mouse para corrigir.",Margin=new Thickness(14,2,10,10),TextWrapping=TextWrapping.Wrap};DockPanel.SetDock(hint,Dock.Top);root.Children.Add(hint);
            viewer=new ScrollViewer {Content=surface,PanningMode=PanningMode.None,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new Thickness(12,0,12,0)};
            root.Children.Add(viewer);
            viewer.PreviewMouseWheel+=(s,e)=>{if(surface.Writing)e.Handled=true;};
            viewer.ScrollChanged+=(s,e)=>{
                if(surface.Writing&&!restoringViewport&&(Math.Abs(viewer.HorizontalOffset-lockedHorizontal)>.01||Math.Abs(viewer.VerticalOffset-lockedVertical)>.01)){
                    restoringViewport=true;viewer.ScrollToHorizontalOffset(lockedHorizontal);viewer.ScrollToVerticalOffset(lockedVertical);restoringViewport=false;
                }
            };
            scheduler=new RecognitionScheduler(new WindowsInkEngine(),c=>{if(!closing&&!Dispatcher.HasShutdownStarted)Dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>Completed(c)));});
            persistence=new BackgroundPersistence(snapshot=>Storage.Save(recovery,snapshot));
            surface.Collected+=Collected;surface.EditRequested+=Edit;
            surface.Began+=()=>{lockedHorizontal=viewer.HorizontalOffset;lockedVertical=viewer.VerticalOffset;activity.Started(timeline.ElapsedMilliseconds);scheduler.SetPaused(true);status.Text="Escrevendo… aguardando completar os traços.";};
            surface.Ended+=()=>{activity.Ended(timeline.ElapsedMilliseconds);scheduler.SetPaused(false);};
            ResumePending();
            timer.Interval=TimeSpan.FromMilliseconds(80);timer.Tick+=(s,e)=>Tick();timer.Start();
            PreviewKeyDown+=(s,e)=>{if(Keyboard.Modifiers==ModifierKeys.Control&&e.Key==Key.Z){Restore(false);e.Handled=true;}else if(Keyboard.Modifiers==ModifierKeys.Control&&e.Key==Key.Y){Restore(true);e.Handled=true;}else if(e.Key==Key.Delete){DeleteSelected();e.Handled=true;}};
            Closing+=(s,e)=>{try{surface.CommitPending();if(!manualSave.Wait(5000))throw new TimeoutException("Exportação ainda em andamento.");persistence.Enqueue(DocumentSnapshots.Copy(document));if(!persistence.Flush(5000))throw new TimeoutException("Salvamento ainda em andamento.");}catch(Exception ex){MessageBox.Show("Não foi possível salvar a recuperação: "+ex.Message);e.Cancel=true;return;}closing=true;timer.Stop();scheduler.Dispose();persistence.Dispose();};
            if(String.IsNullOrEmpty(status.Text))status.Text="Pronto · português brasileiro · reconhecimento local";
        }
        static void Add(Panel panel,string label,Action action){var b=new Button {Content=label,Padding=new Thickness(9,6,9,6),Margin=new Thickness(2)};b.Click+=(s,e)=>action();panel.Children.Add(b);}
        void Changed(){
            syncingControls=true;
            try{fontSelector.SelectedItem=document.Font;sizeSelector.SelectedItem=document.FontSize;layoutSelector.SelectedIndex=document.Layout=="positions"?1:0;}
            finally{syncingControls=false;}
            surface.SetDocument(document);saveNeeded=true;
        }
        void InvalidateSession(){session=Guid.NewGuid().ToString();scheduler.Clear();dirty.Clear();deferred.Clear();latestWrittenRegion=null;}
        void ResumePending(){
            foreach(var r in document.Regions.Where(r=>!r.InkOnly&&!r.Manual&&!r.Suppressed&&(String.IsNullOrEmpty(r.Text)||r.RecognizedCount<r.Traces.Count)))dirty[r.Id]=DateTime.UtcNow;
            if(dirty.Count>0)activity.Ended(timeline.ElapsedMilliseconds);
        }
        void Collected(InkTrace trace){
            history.Record(document);
            var r=surface.FreeInk||surface.OverlapsProtectedText(trace)?Spatial.AppendInk(document,trace):Spatial.Append(document,trace);
            if(r.InkOnly){Changed();status.Text=surface.FreeInk?"Tinta livre preservada.":"Anotação preservada. Use Editar para alterar o texto pronto.";return;}
            latestWrittenRegion=r.Id;
            scheduler.Cancel(r.Id);deferred.Remove(r.Id);
            dirty[r.Id]=DateTime.UtcNow;Changed();
        }
        bool PenNear(Region r){
            if(!surface.HoverPoint.HasValue)return false;
            var p=surface.HoverPoint.Value;var b=r.Box;double margin=Math.Max(20,Spatial.WritingHeight(r));
            return p.X>=b.X-margin&&p.X<=b.Right+margin&&p.Y>=b.Y-margin&&p.Y<=b.Bottom+margin;
        }
        void Tick(){
            if(!surface.Writing)foreach(var pair in dirty.ToArray()){
                var r=document.Regions.FirstOrDefault(v=>v.Id==pair.Key);
                if(r==null||r.InkOnly||r.Manual||r.Suppressed){dirty.Remove(pair.Key);deferred.Remove(pair.Key);continue;}
                // Returning to an old word for accents is active writing too;
                // its persisted Closed flag must not shorten this pause.
                if(!activity.Ready(timeline.ElapsedMilliseconds,r.Closed&&r.Id!=latestWrittenRegion,PenNear(r)))continue;
                Completion answer;
                if(deferred.TryGetValue(r.Id,out answer)){
                    deferred.Remove(r.Id);
                    if(answer.Work.Ticket.Matches(session,document,r)){dirty.Remove(r.Id);Apply(answer,r);continue;}
                }
                // Only the target region's completed traces are handed off. No
                // JSON serialization or whole-document cloning on the UI path.
                scheduler.Enqueue(new Work {Ticket=new Ticket {Session=session,Document=document.Id,Page=document.Page,Layer=document.Layer,Region=r.Id,Revision=r.Revision},Traces=new List<InkTrace>(r.Traces)});
                dirty.Remove(pair.Key);status.Text="Reconhecendo após a pausa…";
            }
            if(saveNeeded&&activity.CanSave(timeline.ElapsedMilliseconds)&&(DateTime.UtcNow-lastSave).TotalSeconds>=3){
                persistence.Enqueue(DocumentSnapshots.Copy(document));saveNeeded=false;lastSave=DateTime.UtcNow;
            }
            if(persistence.LastError!=null&&persistence.LastError!=reportedSaveError){reportedSaveError=persistence.LastError;status.Text="Falha ao salvar: "+reportedSaveError.Message;saveNeeded=true;}
        }
        void Completed(Completion c){
            if(closing)return;
            var r=document.Regions.FirstOrDefault(v=>v.Id==c.Work.Ticket.Region);
            if(!c.Work.Ticket.Matches(session,document,r))return;
            // Keep an already computed answer until the writing is stable.
            // Do not repeatedly send the same revision back to the recognizer.
            deferred[r.Id]=c;dirty[r.Id]=DateTime.UtcNow;
        }
        void Apply(Completion c,Region r){
            if(c.Error!=null){status.Text=c.Error.Message+" · tinta preservada";return;}
            if(c.Answer==null||String.IsNullOrWhiteSpace(c.Answer.Text)){status.Text="Sem hipótese · tinta preservada";return;}
            if(r.Text==c.Answer.Text&&r.RecognizedCount==r.Traces.Count)return;
            history.Record(document);r.Text=c.Answer.Text;r.Alternatives=c.Answer.Alternatives;r.RecognizedCount=r.Traces.Count;
            Changed();status.Text=String.Format("{0:F0} ms no motor · desenho {1:F1} ms · hipótese revisável · salvamento automático ativo",c.Milliseconds,surface.LastPaintMs);
        }
        void Restore(bool redo){if(surface.Writing)return;InvalidateSession();document=redo?history.Redo(document):history.Undo(document);foreach(var r in document.Regions)r.Suppressed=true;Changed();status.Text="Histórico restaurado. Nova escrita permite rever a região.";}
        Region Selected(){return document.Regions.FirstOrDefault(r=>r.Id==surface.Selected);}
        void EditSelected(){var r=Selected();if(r!=null)Edit(r);}
        void Edit(Region r){
            InvalidateSession();var dialog=new Window {Title="Corrigir texto",Width=480,Height=230,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner};
            var panel=new DockPanel {Margin=new Thickness(12)};var input=new TextBox {Text=r.Text,FontSize=22,AcceptsReturn=true};var buttons=new StackPanel {Orientation=Orientation.Horizontal};DockPanel.SetDock(buttons,Dock.Bottom);panel.Children.Add(buttons);panel.Children.Add(input);dialog.Content=panel;
            if(r.Alternatives.Length>0){
                var alternatives=new ComboBox {ItemsSource=r.Alternatives.Distinct().ToArray(),SelectedItem=r.Text,Margin=new Thickness(0,0,0,8),ToolTip="Alternativas devolvidas pelo reconhecedor"};
                alternatives.SelectionChanged+=(s,e)=>{if(alternatives.SelectedItem!=null)input.Text=alternatives.SelectedItem.ToString();};
                panel.Children.Remove(input);DockPanel.SetDock(alternatives,Dock.Top);panel.Children.Add(alternatives);panel.Children.Add(input);
            }
            Add(buttons,"Aplicar",()=>{history.Record(document);r.Text=input.Text;r.InkOnly=false;r.Manual=true;r.Revision++;r.RecognizedCount=String.IsNullOrEmpty(r.Text)?0:r.Traces.Count;Changed();dialog.Close();});Add(buttons,"Cancelar",()=>dialog.Close());dialog.Loaded+=(s,e)=>input.Focus();dialog.ShowDialog();ResumePending();
        }
        void DeleteSelected(){var r=Selected();if(r==null||surface.Writing)return;history.Record(document);InvalidateSession();document.Regions.Remove(r);surface.Selected=null;Changed();}
        async void SaveAs(){var dlg=new SaveFileDialog {Filter="Documento Traços (*.json)|*.json",FileName="Anotação.json"};if(dlg.ShowDialog(this)==true)try{var snapshot=DocumentSnapshots.Copy(document);manualSave=Task.Run(()=>Storage.Save(dlg.FileName,snapshot));await manualSave;status.Text="Documento salvo.";}catch(Exception e){manualSave=Task.FromResult(0);MessageBox.Show(e.Message);}}
        void Open(){if(surface.Writing)return;var dlg=new OpenFileDialog {Filter="Documento Traços (*.json)|*.json"};if(dlg.ShowDialog(this)!=true)return;try{var next=Storage.Decode(File.ReadAllBytes(dlg.FileName));Storage.Save(recovery,document);InvalidateSession();document=next;foreach(var region in document.Regions)region.Closed=true;ResumePending();history=new History();surface.Selected=null;Changed();status.Text="Documento aberto; tinta e texto recuperados.";}catch(Exception e){MessageBox.Show(e.Message);}}
    }
}
