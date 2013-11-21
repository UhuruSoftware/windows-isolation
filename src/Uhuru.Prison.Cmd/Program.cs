using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Cmd
{
    class Program
    {
        static void Main(string[] args)
        {
            string invokedVerb = null;
            object invokedVerbInstance = null;

            var options = new Options();
            if (!CommandLine.Parser.Default.ParseArguments(args, options,
              (verb, subOptions) =>
              {
                  // if parsing succeeds the verb name and correct instance
                  // will be passed to onVerbCommand delegate (string,object)
                  invokedVerb = verb;
                  invokedVerbInstance = subOptions;
              }))
            {
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }

            if (invokedVerb == "list")
            {
                var listSubOptions = (ListSubOptions)invokedVerbInstance;
                if (listSubOptions.Orphaned)
                {
                    Dictionary<CellType, CellInstanceInfo[]> instances = Prison.ListCellInstances();

                    foreach (CellType cellType in instances.Keys)
                    {
                        TableBuilder tb = new TableBuilder();
                        tb.AddRow(cellType.ToString(), "Info");
                        tb.AddRow(new string('-', cellType.ToString().Length), "----");

                        foreach (CellInstanceInfo cellInstance in instances[cellType])
                        {
                            tb.AddRow(cellInstance.Name, cellInstance.Info);
                        }

                        Console.Write(tb.Output());
                        Console.WriteLine();
                    }
                }
            }
            else if (invokedVerb == "list-users")
            {
                var listUsersSubOptions = (ListUsersSubOptions)invokedVerbInstance;

                if (string.IsNullOrWhiteSpace(listUsersSubOptions.Filter))
                {
                    PrisonUser[] users = PrisonUser.ListUsers();

                    TableBuilder tb = new TableBuilder();
                    tb.AddRow("Full Username", "Prefix");
                    tb.AddRow("-------------", "------");

                    foreach (PrisonUser user in users)
                    {
                        tb.AddRow(user.Username, user.UsernamePrefix);
                    }

                    Console.Write(tb.Output());
                    Console.WriteLine();
                }
            }
        }
    }
}
