using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blazor.UI.Dropdown
{
    public class DropdownItem<TValue>
    {
        public string DisplayText { get; set; }

        public TValue ItemObject { get; set; }
    }
}
