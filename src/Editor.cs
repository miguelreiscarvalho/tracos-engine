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
using Microsoft.Win32;

namespace Tracos {
    public sealed class InkSurface : FrameworkElement {
        public InkDocument Document;
        public InkTrace Active;
        public string Selected;
        public bool SelectMode;
        public event Action<InkTrace> Collected;
        public event Action<Region> EditRequested;
        readonly Stopwatch clock=Stopwatch.StartNew();
        readonly long timeOrigin=DateTime.UtcNow.Ticks/TimeSpan.TicksPerMillisecond;
        public bool Writing {get{return Active!=null;}}
        public double LastPaintMs;
        sealed class GlyphCache {public string Text,Font;public double Size,Dpi;public Geometry Geometry;}
        readonly Dictionary<string,GlyphCache> glyphCache=new Dictionary<string,GlyphCache>();
        Geometry Glyphs(Region region) {
            double dpi=VisualTreeHelper.GetDpi(this).PixelsPerDip;
            GlyphCache cached;
            if(glyphCache.TryGetValue(region.Id,out cached)&&cached.Text==region.Text&&cached.Font==Document.Font&&cached.Size==Document.FontSize&&cached.Dpi==dpi)
                return cached.Geometry;
            var text=new FormattedText(region.Text,System.Globalization.CultureInfo.GetCultureInfo("pt-BR"),FlowDirection.LeftToRight,new Typeface(Document.Font),Document.FontSize,Brushes.Black,dpi);
            var geometry=text.BuildGeometry(new Point(0,0));geometry.Freeze();
            glyphCache[region.Id]=new GlyphCache{Text=region.Text,Font=Document.Font,Size=Document.FontSize,Dpi=dpi,Geometry=geometry};
            return geometry;
        }
        public InkSurface(){Width=2400;Height=1800;Focusable=true;ClipToBounds=true;}
        Region Hit(Point p){return Document.Regions.LastOrDefault(r=>{var b=r.Box;double right=r.Manual?Math.Max(b.Right,b.X+r.Text.Length*Document.FontSize*.65):b.Right;return p.X>=b.X-8&&p.X<=right+8&&p.Y>=b.Y-8&&p.Y<=Math.Max(b.Bottom,b.Y+Document.FontSize)+8;});}
        void Start(Point p,string device,int clicks){
            Focus();var region=Hit(p);
            if(SelectMode||(device=="mouse"&&clicks==2)){Selected=region==null?null:region.Id;InvalidateVisual();if(clicks==2&&region!=null&&EditRequested!=null)EditRequested(region);return;}
            Active=new InkTrace {Device=device};
        }
        void AddMouse(Point p){if(Active!=null){Active.Points.Add(new InkPoint {X=p.X,Y=p.Y,Time=timeOrigin+clock.ElapsedMilliseconds,Pressure=.5f});InvalidateVisual();}}
        void AddStylus(StylusEventArgs e){
            if(Active==null)return;
            foreach(var p in e.GetStylusPoints(this)) {
                Active.Points.Add(new InkPoint {X=p.X,Y=p.Y,Time=timeOrigin+clock.ElapsedMilliseconds,Pressure=p.PressureFactor,
                    TiltX=p.HasProperty(StylusPointProperties.XTiltOrientation)?(float?)p.GetPropertyValue(StylusPointProperties.XTiltOrientation):null,
                    TiltY=p.HasProperty(StylusPointProperties.YTiltOrientation)?(float?)p.GetPropertyValue(StylusPointProperties.YTiltOrientation):null});
            }
            InvalidateVisual();
        }
        void Finish(){var t=Active;Active=null;if(t!=null&&t.Points.Count>0&&Collected!=null)Collected(t);InvalidateVisual();}
        protected override void OnStylusDown(StylusDownEventArgs e){Start(e.GetPosition(this),"stylus",e.TapCount);if(Active!=null){CaptureStylus();AddStylus(e);}e.Handled=true;}
        protected override void OnStylusMove(StylusEventArgs e){if(Active!=null){AddStylus(e);e.Handled=true;}}
        protected override void OnStylusUp(StylusEventArgs e){if(Active!=null){AddStylus(e);Finish();ReleaseStylusCapture();}e.Handled=true;}
        protected override void OnLostStylusCapture(StylusEventArgs e){Finish();}
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e){if(e.StylusDevice!=null)return;Start(e.GetPosition(this),"mouse",e.ClickCount);if(Active!=null){CaptureMouse();AddMouse(e.GetPosition(this));}e.Handled=true;}
        protected override void OnMouseMove(MouseEventArgs e){if(e.StylusDevice==null&&Active!=null&&e.LeftButton==MouseButtonState.Pressed)AddMouse(e.GetPosition(this));}
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e){if(e.StylusDevice!=null)return;AddMouse(e.GetPosition(this));Finish();ReleaseMouseCapture();}
        protected override void OnLostMouseCapture(MouseEventArgs e){Finish();}
        protected override void OnTouchDown(TouchEventArgs e){e.Handled=true;}
        void DrawTrace(DrawingContext dc,InkTrace trace){
            if(trace.Points.Count==0)return;
            var points=trace.Points;
            if(points.Count==1){dc.DrawEllipse(Brushes.SteelBlue,null,new Point(points[0].X,points[0].Y),1.5,1.5);return;}
            var geometry=new StreamGeometry();
            using(var g=geometry.Open()){g.BeginFigure(new Point(points[0].X,points[0].Y),false,false);g.PolyLineTo(points.Skip(1).Select(p=>new Point(p.X,p.Y)).ToArray(),true,false);}
            geometry.Freeze();dc.DrawGeometry(null,new Pen(Brushes.SteelBlue,2.2){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},geometry);
        }
        protected override void OnRender(DrawingContext dc){
            var watch=Stopwatch.StartNew();dc.DrawRectangle(Brushes.White,null,new Rect(0,0,Width,Height));
            for(int y=96;y<Height;y+=64)dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(231,236,241)),1),new Point(32,y),new Point(Width-32,y));
            if(Document!=null&&glyphCache.Count>Document.Regions.Count) {
                var ids=new HashSet<string>(Document.Regions.Select(r=>r.Id));
                foreach(var id in glyphCache.Keys.Where(id=>!ids.Contains(id)).ToArray())glyphCache.Remove(id);
            }
            if(Document!=null)foreach(var r in Document.Regions){
                if(!String.IsNullOrEmpty(r.Text)) {
                    // Fit actual glyph ink, not the font's line box, to the
                    // source writing. No paragraph reflow or first-stroke anchor.
                    var glyphs=Glyphs(r);
                    var bounds=glyphs.Bounds;
                    if(!bounds.IsEmpty) {
                        var placement=TextPlacement.Fit(r.Box,bounds.Width,bounds.Height);
                        // Explicit keyboard edits may expand a short source mark
                        // into a sentence; keep those at the selected font size.
                        if(r.Manual){placement.ScaleX=1;placement.ScaleY=1;}
                        dc.PushTransform(new TranslateTransform(placement.X,placement.Y));
                        dc.PushTransform(new ScaleTransform(placement.ScaleX,placement.ScaleY));
                        dc.PushTransform(new TranslateTransform(-bounds.X,-bounds.Y));
                        dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(26,39,53)),null,glyphs);
                        dc.Pop();dc.Pop();dc.Pop();
                    }
                }
                foreach(var t in r.Traces.Skip(String.IsNullOrEmpty(r.Text)?0:r.RecognizedCount))DrawTrace(dc,t);
                if(Selected==r.Id){var b=r.Box;dc.DrawRectangle(null,new Pen(Brushes.DarkOrange,1),new Rect(b.X-5,b.Y-8,Math.Max(10,b.Right-b.X+10),Math.Max(10,b.Height+16)));}
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
        readonly DispatcherTimer timer=new DispatcherTimer();
        readonly Dictionary<string,DateTime> dirty=new Dictionary<string,DateTime>();
        readonly string recovery;
        bool closing,saveNeeded;
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
            fonts.SelectionChanged+=(s,e)=>{if(fonts.SelectedItem==null)return;history.Record(document);document.Font=fonts.SelectedItem.ToString();Changed();};toolbar.Children.Add(fonts);
            var sizes=new ComboBox {Width=65,Margin=new Thickness(4,2,4,2),ItemsSource=new double[]{18,22,26,32,40},SelectedItem=document.FontSize};
            sizes.SelectionChanged+=(s,e)=>{if(sizes.SelectedItem==null)return;history.Record(document);document.FontSize=(double)sizes.SelectedItem;Changed();};toolbar.Children.Add(sizes);
            var zoom=new Slider {Minimum=.5,Maximum=2,Value=1,Width=100,Margin=new Thickness(8),ToolTip="Zoom"};zoom.ValueChanged+=(s,e)=>surface.LayoutTransform=new ScaleTransform(zoom.Value,zoom.Value);toolbar.Children.Add(zoom);
            DockPanel.SetDock(status,Dock.Bottom);root.Children.Add(status);
            var hint=new TextBlock {Text="Escreva na folha. A conversão é automática e revisável. Duplo clique para corrigir. Os traços originais são preservados.",Margin=new Thickness(14,2,10,10),TextWrapping=TextWrapping.Wrap};DockPanel.SetDock(hint,Dock.Top);root.Children.Add(hint);
            root.Children.Add(new ScrollViewer {Content=surface,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new Thickness(12,0,12,0)});
            scheduler=new RecognitionScheduler(new WindowsInkEngine(),c=>{if(!closing)Dispatcher.BeginInvoke(new Action(()=>Completed(c)));});
            surface.Collected+=Collected;surface.EditRequested+=Edit;
            timer.Interval=TimeSpan.FromMilliseconds(80);timer.Tick+=(s,e)=>Tick();timer.Start();
            PreviewKeyDown+=(s,e)=>{if(Keyboard.Modifiers==ModifierKeys.Control&&e.Key==Key.Z){Restore(false);e.Handled=true;}else if(Keyboard.Modifiers==ModifierKeys.Control&&e.Key==Key.Y){Restore(true);e.Handled=true;}else if(e.Key==Key.Delete){DeleteSelected();e.Handled=true;}};
            Closing+=(s,e)=>{try{Storage.Save(recovery,document);}catch(Exception ex){MessageBox.Show("Não foi possível salvar a recuperação: "+ex.Message);e.Cancel=true;return;}closing=true;timer.Stop();scheduler.Dispose();};
            if(String.IsNullOrEmpty(status.Text))status.Text="Pronto · português brasileiro · reconhecimento local";
        }
        static void Add(Panel panel,string label,Action action){var b=new Button {Content=label,Padding=new Thickness(9,6,9,6),Margin=new Thickness(2)};b.Click+=(s,e)=>action();panel.Children.Add(b);}
        void Changed(){surface.Document=document;surface.InvalidateVisual();saveNeeded=true;}
        void InvalidateSession(){session=Guid.NewGuid().ToString();scheduler.Clear();dirty.Clear();}
        void Collected(InkTrace trace){
            history.Record(document);
            var r=Spatial.Append(document,trace);
            dirty[r.Id]=DateTime.UtcNow;Changed();
        }
        void Tick(){
            if(!surface.Writing)foreach(var pair in dirty.ToArray()){
                // Pen-up plus revision dirtiness and coalescing; timer alone never creates recognition work.
                if((DateTime.UtcNow-pair.Value).TotalMilliseconds<160)continue;
                var r=document.Regions.FirstOrDefault(v=>v.Id==pair.Key);dirty.Remove(pair.Key);if(r==null||r.Manual||r.Suppressed)continue;
                var snapshot=Storage.Decode(Storage.Encode(document)).Regions.First(v=>v.Id==r.Id);
                scheduler.Enqueue(new Work {Ticket=new Ticket {Session=session,Document=document.Id,Page=document.Page,Layer=document.Layer,Region=r.Id,Revision=r.Revision},Traces=snapshot.Traces});
                status.Text="Reconhecendo… pode continuar escrevendo.";
            }
            if(saveNeeded&&!surface.Writing&&(DateTime.UtcNow-lastSave).TotalSeconds>=2){try{Storage.Save(recovery,document);saveNeeded=false;lastSave=DateTime.UtcNow;}catch(Exception e){status.Text="Falha ao salvar: "+e.Message;lastSave=DateTime.UtcNow;}}
        }
        void Completed(Completion c){
            if(closing)return;
            var r=document.Regions.FirstOrDefault(v=>v.Id==c.Work.Ticket.Region);
            if(!c.Work.Ticket.Matches(session,document,r))return;
            // A pen-down can precede the revision increment at pen-up. Defer that snapshot.
            if(surface.Writing){dirty[r.Id]=DateTime.UtcNow;return;}
            if(c.Error!=null){status.Text=c.Error.Message+" · tinta preservada";return;}
            if(c.Answer==null||String.IsNullOrWhiteSpace(c.Answer.Text)){status.Text="Sem hipótese · tinta preservada";return;}
            history.Record(document);r.Text=c.Answer.Text;r.Alternatives=c.Answer.Alternatives;r.RecognizedCount=r.Traces.Count;
            Changed();status.Text=String.Format("{0:F0} ms no motor · desenho {1:F1} ms · hipótese revisável · salvamento automático ativo",c.Milliseconds,surface.LastPaintMs);
        }
        void Restore(bool redo){if(surface.Writing)return;InvalidateSession();document=redo?history.Redo(document):history.Undo(document);foreach(var r in document.Regions)r.Suppressed=true;Changed();status.Text="Histórico restaurado. Nova escrita permite rever a região.";}
        Region Selected(){return document.Regions.FirstOrDefault(r=>r.Id==surface.Selected);}
        void EditSelected(){var r=Selected();if(r!=null)Edit(r);}
        void Edit(Region r){
            InvalidateSession();var dialog=new Window {Title="Corrigir texto",Width=480,Height=180,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner};
            var panel=new DockPanel {Margin=new Thickness(12)};var input=new TextBox {Text=r.Text,FontSize=22,AcceptsReturn=true};var buttons=new StackPanel {Orientation=Orientation.Horizontal};DockPanel.SetDock(buttons,Dock.Bottom);panel.Children.Add(buttons);panel.Children.Add(input);dialog.Content=panel;
            Add(buttons,"Aplicar",()=>{history.Record(document);r.Text=input.Text;r.Manual=true;r.Revision++;r.RecognizedCount=String.IsNullOrEmpty(r.Text)?0:r.Traces.Count;Changed();dialog.Close();});Add(buttons,"Cancelar",()=>dialog.Close());dialog.Loaded+=(s,e)=>input.Focus();dialog.ShowDialog();
        }
        void DeleteSelected(){var r=Selected();if(r==null||surface.Writing)return;history.Record(document);InvalidateSession();document.Regions.Remove(r);surface.Selected=null;Changed();}
        void SaveAs(){var dlg=new SaveFileDialog {Filter="Documento Traços (*.json)|*.json",FileName="Anotação.json"};if(dlg.ShowDialog(this)==true)try{Storage.Save(dlg.FileName,document);status.Text="Documento salvo.";}catch(Exception e){MessageBox.Show(e.Message);}}
        void Open(){if(surface.Writing)return;var dlg=new OpenFileDialog {Filter="Documento Traços (*.json)|*.json"};if(dlg.ShowDialog(this)!=true)return;try{var next=Storage.Decode(File.ReadAllBytes(dlg.FileName));Storage.Save(recovery,document);InvalidateSession();document=next;foreach(var region in document.Regions)region.Closed=true;history=new History();surface.Selected=null;Changed();status.Text="Documento aberto; tinta e texto recuperados.";}catch(Exception e){MessageBox.Show(e.Message);}}
    }
}
