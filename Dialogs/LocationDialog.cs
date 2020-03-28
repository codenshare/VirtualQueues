using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MultiDialogsBot.Dialogs
{
    public class LocationDialog : IDialog<object>
    {
        Task IDialog<object>.StartAsync(IDialogContext context)
        {
            throw new NotImplementedException();
        }
    }
}