using BlazorThreeJS.Extension;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ChemSharp.Molecules.Blazor
{
    public class BlazorBond : ComponentBase
    {
        [Parameter]
        public Bond? Data { get; set; }

        [Inject]
        public IJSRuntime JS { get; set; }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (Data == null) return;
            var (loc, matrix) = Data.CalculateRotation();
            await JS.InvokeVoidAsync("chemsharpMolecules.addBond", loc.X, loc.Y, loc.Z, matrix.X, matrix.Y, matrix.Z, matrix.W, Data.Length);
        }
    }
}
