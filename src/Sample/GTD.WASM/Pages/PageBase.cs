using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GTD.ReadModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GTD.WASM.Pages
{
    public class PageBase<T> : ComponentBase where T : new()
    {
        protected EditContext EditContext { get; set; }

        protected T Model { get; set; } = new();
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            EditContext = new EditContext(Model);
        }

        public string GetError(Expression<Func<object>> fu)
        {
            return EditContext?.GetValidationMessages(fu).FirstOrDefault();
        }
    }
}
