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

    #region Parameters
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object> UnmatchedParameters { get; set; }
    [Parameter] public IPlotController Controller { get; set; }
    [Parameter] public double Width { get; set; }
    [Parameter] public double Height { get; set; }
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

    public OxyRect ClientArea => new(0, 0, Width, Height);
    /// <inheritdoc/>
    public void HideTracker() { }
    /// <inheritdoc/>
    public void HideZoomRectangle() { }
    /// <inheritdoc/>
    public void ShowTracker(TrackerHitResult trackerHitResult) { }
    /// <inheritdoc/>
    public void ShowZoomRectangle(OxyRect rectangle) { }

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
        //// round width and height to full device pixels
        ////var width = ((int)(this.plotPresenter.ActualWidth * dpiScale)) / dpiScale;
        ////var height = ((int)(this.plotPresenter.ActualHeight * dpiScale)) / dpiScale;
        lock (ActualModel.SyncRoot) ((IPlotModel)ActualModel).Render(_renderContext, ClientArea);
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

    /// <summary>
    /// Adds SKCanvasElement to RenderTree
    /// </summary>
    /// <param name="builder"></param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<SKCanvasView>(0);
        builder.AddAttribute(1, "OnPaintSurface", OnPaintSurface);
        builder.AddAttribute(2, "width", Width);
        builder.AddAttribute(2, "height", Height);
        //builder.AddAttribute(2, "style", $"width: {Width}px; height: {Height}px");
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
        builder.AddMultipleAttributes(5, UnmatchedParameters);

        builder.AddComponentReferenceCapture(6, reference =>
        {
            _canvasView = (SKCanvasView)reference;
        });
        builder.CloseComponent();
    }   
    #endregion
}
