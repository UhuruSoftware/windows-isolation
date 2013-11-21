using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Cmd
{
    class Options
    {
        public Options()
        {
            ListVerb = new ListSubOptions() { All = true };
            ListUsersVerb = new ListUsersSubOptions() { Filter = string.Empty };
        }

        [VerbOption("list", HelpText = "List prison containers on the current machine.")]
        public ListSubOptions ListVerb { get; set; }


        [VerbOption("list-users", HelpText = "List prison users on the current machine.")]
        public ListUsersSubOptions ListUsersVerb { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }


    class ListSubOptions
    {
        [Option('a', "all", HelpText = "List all prison containers, both healthy and orphans.")]
        public bool All { get; set; }

        [Option('o', "orphaned", HelpText = "List prison containers that are broken.")]
        public bool Orphaned { get; set; }
    }

    class ListUsersSubOptions
    {
        [Option('f', "filter", HelpText = "Filters the list of printed users based on their prefix.")]
        public string Filter { get; set; }
    }

    class DestroySubOptions
    {
    }

    class ListCellTypesSubOptions
    {
    }

    class CreateSubOptions
    {
    }
}
