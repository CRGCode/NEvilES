using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using Blazor.UI.Dropdown;
using GTD.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;

namespace GTD.WASM.Pages
{
    public class SignUpBase : PageBase<User.Details>
    {
        [Inject] 
        private NavigationManager NavigationManager { get; set; }

        protected IList<DropdownItem<GenderType>> GenderTypeDropdownItems { get; private set; } = new List<DropdownItem<GenderType>>();

        public DropdownItem<GenderType> SelectedGenderDropdownItem { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            GenderTypeDropdownItems = new List<DropdownItem<GenderType>>()
            {
                new()
                {
                    ItemObject = GenderType.Male,
                    DisplayText = "Male"
                },
                new()
                {
                    ItemObject = GenderType.Female,
                    DisplayText = "Female",
                },
                new()
                {
                    ItemObject = GenderType.Neutral,
                    DisplayText = "Neutral",
                }
            };

            SelectedGenderDropdownItem = GenderTypeDropdownItems[1];

            TryGetEmailFromUri();
        }

        protected void OnValidSubmit()
        {
            NavigationManager.NavigateTo("signin");
        }

        private void TryGetEmailFromUri()
        {
            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("email", out var sv))
            {
                Model.Email = sv;
            }
        }
    }

    public enum GenderType
    {
        Male,
        Female,
        Neutral
    }
}
