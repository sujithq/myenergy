using June.Data.Commands;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeteoStat.Data.Commands
{
    public class MeteoStatRunSettings : BaseCommandSettings
    {
        
    }

    
    public class MeteoStatRunCommand : Command<MeteoStatRunSettings>
    {
        public override int Execute(CommandContext context, MeteoStatRunSettings settings)
        {
            // Omitted
            return 0;
        }
    }

}
