using IcsMonitor.Utils;
using Microsoft.Extensions.CommandLineUtils;
using System;

namespace IcsMonitor
{
    internal sealed partial class ConsoleService
    {
        /// <summary>
        /// Prints the profile information in YAML format.
        /// </summary>
        /// <param name="command">The command.</param>
        private void ShowProfileCommand(CommandLineApplication command)
        {
            command.Description = "Prints the stored profile in user readable format.";
            command.ExtendedHelpText = "This command loads the specified profile and prints it in readable format. The output format can be specified.";
            command.HelpOption("-?|-h|--help");


            var profileFileOption = command.Option("-p|--profile-file <value>",
                "A file name of stored profile. This option is required.",
                CommandOptionType.SingleValue);

            command.OnExecute(async () =>
            {
                if (!profileFileOption.TryGetValueOrError(() => Console.Error.WriteLine($"Error: '--{profileFileOption.LongName}' option is required!"), out var profileFile)) return -1;
                var textWriter = Console.Out;
                await _functions.PrintProfileAsync(_functions.LoadProfileFromFile(profileFile), textWriter);
                return 0;
            });
        }
    }
}
