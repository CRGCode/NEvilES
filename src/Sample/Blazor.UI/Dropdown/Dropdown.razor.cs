using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Blazor.UI.Dropdown
{
    public partial class Dropdown<TValue> :ComponentBase
    {
        [Parameter]
        public IList<DropdownItem<TValue>> Items { get; set; }

        [Parameter] public DropdownItem<TValue> SelectedItem { get; set; }

        [Parameter] public EventCallback<DropdownItem<TValue>> SelectedItemChanged { get; set; }

        public async void OnItemClicked(DropdownItem<TValue> item)
        {
            SelectedItem = item;
            StateHasChanged();
            await SelectedItemChanged.InvokeAsync(item);
        }
    }
}
