using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SkiaSharp;
using SkiaSharp.Views.Blazor;
using System.Numerics;

namespace OxyPlot.SkiaSharp.Blazor;
public class PlotView : ComponentBase, IPlotView
{
    private PlotModel _model;
    private readonly object modelLock = new object();
    private IPlotController _defaultController;
    protected IRenderContext _renderContext;
    private SKRenderContext SKRenderContext => (SKRenderContext)_renderContext;
    private SKCanvasView _canvasView;
    private ElementReference _wrapper;
    private SKSize _canvasSize;
    private SizeWatcherInterop sizeWatcher = null!;
    private TrackerHitResult _lastTrackerHitResult; 
    private OxyRect zoomRectangle;

    #region Parameters
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object> UnmatchedParameters { get; set; }
    [Parameter] public IPlotController Controller { get; set; }
    [Parameter]
    public PlotModel Model
    {
        get => _model;
        set
        {
            _model = value;
            OnModelChanged();
        }
    }

    [Inject]
    IJSRuntime JS { get; set; } = null!;
    #endregion

    /// <summary>
    /// Gets the actual Model
    /// </summary>
    public PlotModel ActualModel { get; private set; }
    /// <inheritdoc/>
    Model IView.ActualModel => ActualModel;

    /// <summary>
    /// Gets the actual Plot Controller
    /// </summary>
    public IPlotController ActualController => Controller ?? (_defaultController ??= new PlotController());
    /// <inheritdoc/>
    IController IView.ActualController => ActualController;

    public OxyRect ClientArea => CalculateBounds();
    /// <inheritdoc/>
    public void HideTracker()
    {
        _lastTrackerHitResult = null!;
        StateHasChanged();
    }
    /// <inheritdoc/>
    public void HideZoomRectangle() {
        zoomRectangle = new OxyRect(0, 0, 0, 0);
        StateHasChanged();
    }
    /// <inheritdoc/>
    public void ShowTracker(TrackerHitResult trackerHitResult)
    {
        if (trackerHitResult == null)
        {
            HideTracker();
            return;
        }
        _lastTrackerHitResult = trackerHitResult;
        StateHasChanged();
    }
    /// <inheritdoc/>
    public void ShowZoomRectangle(OxyRect rectangle) {
        zoomRectangle = rectangle;
        StateHasChanged();
    }

    /// <inheritdoc/>
    public void SetClipboardText(string text) { }
    /// <inheritdoc/>
    public void SetCursorType(CursorType cursorType) { }

    #region Controls
    /// <summary>
    /// Pans all axes.
    /// </summary>
    /// <param name="delta">The delta.</param>
    public void PanAllAxes(Vector2 delta)
    {
        if (ActualModel != null) ActualModel.PanAllAxes(delta.X, delta.Y);
        InvalidatePlot(false);
    }

    /// <summary>
    /// Resets all axes.
    /// </summary>
    public void ResetAllAxes()
    {
        if (ActualModel != null) ActualModel.ResetAllAxes();
        InvalidatePlot(false);
    }

    /// <summary>
    /// Zooms all axes.
    /// </summary>
    /// <param name="factor">The zoom factor.</param>
    public void ZoomAllAxes(double factor)
    {
        if (ActualModel != null) ActualModel.ZoomAllAxes(factor);
        InvalidatePlot(false);
    }
    #endregion

    protected void OnModelChanged()
    {
        lock (modelLock)
        {
            if (ActualModel != null)
            {
                ((IPlotModel)ActualModel).AttachPlotView(null);
                ActualModel = null;
            }

            if (Model != null)
            {
                ((IPlotModel)Model).AttachPlotView(this);
                ActualModel = Model;
            }
        }
        InvalidatePlot();
    }

    private void AddEventCallback<T>(RenderTreeBuilder builder, int seq, string name, Action<T> callback)
    {
        builder.AddEventPreventDefaultAttribute(seq, name, true);
        builder.AddEventPreventDefaultAttribute(seq, name, true);
        builder.AddAttribute(seq, name, EventCallback.Factory.Create(this, callback));
    }

    #region Rendering
    /// <inheritdoc/>
    protected static IRenderContext CreateRenderContext() => new SKRenderContext();

    /// <inheritdoc/>
    public void InvalidatePlot(bool updateData = true)
    {
        if (ActualModel == null) return;
        lock (ActualModel.SyncRoot) ((IPlotModel)ActualModel).Update(updateData);
        Render();
    }
    /// <summary>
    /// Renders the plot to SKCanvas
    /// </summary>
    protected void Render()
    {
        if (_renderContext == null) return;
        _canvasView.Invalidate();
    }

    /// <summary>
    /// Renders the plot to SKCanvas
    /// </summary>
    protected virtual void RenderOverride()
    {
        ClearBackground();
        if (ActualModel == null) return;
        lock (ActualModel.SyncRoot) ((IPlotModel)ActualModel).Render(_renderContext, CalculateBounds());
    }

    private OxyRect CalculateBounds()
    {
        //TODO: Better solution: https://github.com/mono/SkiaSharp/pull/1832
        var dpiFi = typeof(SKCanvasView).GetField("dpi", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var dpi = (double)dpiFi.GetValue(_canvasView);
        SKRenderContext.DpiScale = (float)dpi;
        return new OxyRect(0, 0, (int)(_canvasSize.Width), (int)(_canvasSize.Height));
    }
    protected void ClearBackground()
    {
        var color = ActualModel?.Background.IsVisible() == true
                    ? ActualModel.Background.ToSKColor()
                    : SKColors.Empty;

        SKRenderContext.SkCanvas.Clear(color);
    }
    /// <summary>
    /// Paints Plot to Canvas
    /// </summary>
    /// <param name="e"></param>
    private void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        SKRenderContext.SkCanvas = e.Surface.Canvas;
        RenderOverride();
        SKRenderContext.SkCanvas = null!;
    }
    #endregion
    #region Razor Component Methods
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _renderContext = CreateRenderContext();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        sizeWatcher = await SizeWatcherInterop.ImportAsync(JS, _wrapper, OnSizeChanged);
    }

    private void OnSizeChanged(SKSize size)
    {
        _canvasSize = size;
        StateHasChanged();
    }

    /// <summary>
    /// Adds SKCanvasElement to RenderTree
    /// </summary>
    /// <param name="builder"></param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, UnmatchedParameters);
        builder.AddAttribute(1, "class", $"oxyplotview {(UnmatchedParameters.ContainsKey("class") ? UnmatchedParameters["class"] : "")}");
        builder.AddAttribute(1, "style", $"position: relative; {(UnmatchedParameters.ContainsKey("style") ? UnmatchedParameters["style"] : "")}");
        builder.OpenComponent<SKCanvasView>(1);
        builder.AddAttribute(2, "OnPaintSurface", OnPaintSurface);
        builder.AddAttribute(2, "style", "width: 100%; height: inherit;"); //do not override!
        AddEventCallback<MouseEventArgs>(builder, 3, "onmousedown", e => ActualController.HandleMouseDown(this, e.OxyMouseEventArgs()));
        AddEventCallback<MouseEventArgs>(builder, 3, "onmousemove", e => ActualController.HandleMouseMove(this, e.OxyMouseEventArgs()));
        AddEventCallback<MouseEventArgs>(builder, 3, "onmouseup", e => ActualController.HandleMouseUp(this, e.OxyMouseEventArgs()));
        AddEventCallback<MouseEventArgs>(builder, 3, "onmousein", e => ActualController.HandleMouseEnter(this, e.OxyMouseEventArgs()));
        AddEventCallback<MouseEventArgs>(builder, 3, "onmouseout", e => ActualController.HandleMouseLeave(this, e.OxyMouseEventArgs()));

        builder.AddAttribute(4, "onmousewheel", EventCallback.Factory.Create<WheelEventArgs>(this, e => ActualController.HandleMouseWheel(this, e.OxyMouseWheelEventArgs())));
        //builder.AddEventPreventDefaultAttribute(6, "onmousewheel", true);
        builder.AddEventStopPropagationAttribute(4, "onmousewheel", true);

        builder.AddEventPreventDefaultAttribute(4, "oncontextmenu", true);
        builder.AddEventStopPropagationAttribute(4, "oncontextmenu", true);

        builder.AddComponentReferenceCapture(6, reference => _canvasView = (SKCanvasView)reference);
        builder.CloseComponent();
        builder.AddElementReferenceCapture(6, reference => _wrapper = reference);

        if (_lastTrackerHitResult != null)
        {
            builder.OpenElement(7, "div");
            builder.AddAttribute(7, "class", "oxyTracker");
            builder.AddAttribute(7, "style", $"position: absolute; left: {(int)_lastTrackerHitResult.Position.X}px; top: {(int)_lastTrackerHitResult.Position.Y}px; pointer-events: none; font-family: {Model.DefaultFont}; font-size: {Model.DefaultFontSize}px;");
            builder.AddContent(8, (MarkupString)_lastTrackerHitResult.Text);
            builder.CloseElement();
        }
        if(zoomRectangle.Width > 0 && zoomRectangle.Height > 0)
        {
            builder.OpenElement(9, "div");
            builder.AddAttribute(9, "class", "oxyZoomRectangle");
            builder.AddAttribute(7, "style", $"position: absolute; left: {zoomRectangle.Left}px; top: {zoomRectangle.Top}px; width: {zoomRectangle.Width}px; height: {zoomRectangle.Height}px; border: 1px solid #f0f0f0; background: rgba(0,255,0,.1); pointer-events: none;");
            builder.CloseElement();
        }

        builder.CloseElement();
    }
    #endregion
}
