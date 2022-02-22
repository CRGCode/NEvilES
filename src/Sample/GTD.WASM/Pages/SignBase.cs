using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GTD.WASM.Pages
{
    public class SignBase : ComponentBase
    {
        protected EditContext EditContext { get; set; }

        public string GetError(Expression<Func<object>> fu)
        {
            return EditContext?.GetValidationMessages(fu).FirstOrDefault();
        }
    }
}