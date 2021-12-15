using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SkiaSharp;
using SkiaSharp.Views.Blazor.Internal;

//mainly based on https://github.com/mono/SkiaSharp/tree/main/source/SkiaSharp.Views.Blazor/SkiaSharp.Views.Blazor/Internal as this class is marked internal.

namespace OxyPlot.SkiaSharp.Blazor
{
    internal class SizeWatcherInterop : IDisposable
    {
        private const string JsFilename = "./_content/SkiaSharp.Views.Blazor/SizeWatcher.js";
        private readonly Task<IJSUnmarshalledObjectReference> _moduleTask;
        private IJSUnmarshalledObjectReference? _module;

        private readonly ElementReference _htmlElement;
        private readonly string _htmlElementId;

        private readonly FloatFloatActionHelper callbackHelper;
        private DotNetObjectReference<FloatFloatActionHelper>? callbackReference;
        public SizeWatcherInterop(IJSRuntime js, ElementReference element, Action<SKSize> callback)
        {
            _moduleTask = js.InvokeAsync<IJSUnmarshalledObjectReference>("import", JsFilename).AsTask();
            _htmlElement = element;
            _htmlElementId = element.Id;
            callbackHelper = new FloatFloatActionHelper((x, y) => callback(new SKSize(x, y)));
        }

        public void Start()
        {
            if (callbackReference != null)
                return;

            callbackReference = DotNetObjectReference.Create(callbackHelper);

            Invoke("SizeWatcher.observe", _htmlElement, _htmlElementId, callbackReference);
        }

        public void Stop()
        {
            if (callbackReference == null)
                return;

            Invoke("SizeWatcher.unobserve", _htmlElementId);

            callbackReference?.Dispose();
            callbackReference = null;
        }

        public void Dispose()
        {
            Stop();
            _module?.Dispose();
        }

        public async Task ImportAsync() => _module = await _moduleTask;
        public static async Task<SizeWatcherInterop> ImportAsync(IJSRuntime js, ElementReference element, Action<SKSize> callback)
        {
            var interop = new SizeWatcherInterop(js, element, callback);
            await interop.ImportAsync();
            interop.Start();
            return interop;
        }

        protected void Invoke(string identifier, params object?[]? args) =>
            _module?.InvokeVoid(identifier, args);

        protected TValue Invoke<TValue>(string identifier, params object?[]? args) =>
            _module!.Invoke<TValue>(identifier, args);


    }
}
