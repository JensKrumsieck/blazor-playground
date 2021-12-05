using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using SkiaSharp;
using SkiaSharp.Views.Blazor;
using System.Numerics;

namespace OxyPlot.SkiaSharp.Blazor;
public partial class PlotView : ComponentBase, IPlotView
{
    private PlotModel _model;
    private readonly object modelLock = new object();
    private IPlotController _defaultController;
    protected IRenderContext _renderContext;
    private SKRenderContext SKRenderContext => (SKRenderContext)_renderContext;

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
        RenderOverride();
    }

    /// <summary>
    /// Renders the plot to SKCanvas
    /// </summary>
    protected virtual void RenderOverride()
    {
        ClearBackground();
        if (ActualModel == null) return;
        // round width and height to full device pixels
        //var width = ((int)(this.plotPresenter.ActualWidth * dpiScale)) / dpiScale;
        //var height = ((int)(this.plotPresenter.ActualHeight * dpiScale)) / dpiScale;
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
        builder.AddAttribute(3, "height", Height);
        builder.AddMultipleAttributes(4, UnmatchedParameters);
        builder.CloseComponent();
    }
    #endregion
}
