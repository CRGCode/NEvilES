using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Blazor.UI.Validation
{
    public class ValidationInputBase : ComponentBase
    {
        [Parameter] public EventCallback<string> ValueChanged { get; set; }

        [Parameter] public string Value { get; set; }

        [Parameter] public string Error { get; set; }

        [Parameter(CaptureUnmatchedValues = true) ] public Dictionary<string, object> InputAttributes { get; set; }



        protected async void HandleInputChanged(ChangeEventArgs eventArgs)
        {
            await ValueChanged.InvokeAsync(eventArgs.Value.ToString());
        }

    }
}
