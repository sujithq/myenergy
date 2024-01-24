using June.Data.Commands;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sungrow.Data.Commands
{
    public class SungrowRunSettings : BaseCommandSettings
    {
        
    }

    
    public class SungrowRunCommand : Command<SungrowRunSettings>
    {
        public override int Execute(CommandContext context, SungrowRunSettings settings)
        {
            // Omitted
            return 0;
        }
    }

}
