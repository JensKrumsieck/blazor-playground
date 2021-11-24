using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ChemSharp.Molecules.Blazor
{
    public class BlazorAtom : ComponentBase
    {
        [Parameter]
        public Atom? Data { get; set; }

        [Inject]
        public IJSRuntime JS { get; set; }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (Data == null) return;
            await JS.InvokeVoidAsync("chemsharpMolecules.addAtom", Data.Title, Data.Symbol, Data.Location.X, Data.Location.Y, Data.Location.Z, Data.CovalentRadius ?? 100, Data.Color);
        }
    }
}
