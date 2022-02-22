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
    public class SignInBase : PageBase<User.SignInModel>
    {
        protected string Day { get; } = DateTime.Now.DayOfWeek.ToString();

        [Inject]
        private NavigationManager NavigationManager { get; set; }

        protected void OnSubmit()
        {
            if(!EditContext.Validate())
                return;
        }
    }
}
