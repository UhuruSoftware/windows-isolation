using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Prison.Utilities;

namespace Uhuru.Prison
{
    public class PrisonUser
    {
        public const string GlobalPrefix = "prison";
        public const char Separator = '_';

        private string usernamePrefix;
        private bool created = false;
        private string username = string.Empty;
        private string password = string.Empty;

        public string UsernamePrefix
        {
            get
            {
                return this.usernamePrefix;
            }
        }

        public string Username
        {
            get
            {
                return this.username;
            }
        }

        public string Password
        {
            get
            {
                return this.password;
            }
        }

        private static PrisonUser[] ListOrphanedUsers()
        {
            throw new NotImplementedException();
        }

        public static PrisonUser[] ListUsers()
        {
            List<PrisonUser> result = new List<PrisonUser>();

            string[] allUsers = WindowsUsersAndGroups.GetUsers();


            foreach (string user in allUsers)
            {
                if (user.StartsWith(PrisonUser.GlobalPrefix))
                {
                    string password = (string)Persistence.ReadValue("prison_users", user);

                    // If we can't find the user's password, ignore the account
                    if (!string.IsNullOrWhiteSpace(password))
                    {
                        result.Add(new PrisonUser(PrisonUser.GetUsernamePrefix(user), user, password, true));
                    }
                }
            }

            return result.ToArray();
        }

        public static PrisonUser[] ListUsers(string prefixFilter)
        {
            return ListUsers().Where(user => user.usernamePrefix == prefixFilter).ToArray();
        }

        private PrisonUser(string prefix, string username, string password, bool existing)
        {
            if (prefix.Length > 5)
            {
                throw new ArgumentException("The prefix length must be 5 characters or less.");
            }

            this.usernamePrefix = prefix;
            this.username = existing ? username : GenerateUsername(username);
            this.password = password;
            this.created = existing;
        }

        public PrisonUser() : this(string.Empty)
        {
        }

        public PrisonUser(string prefix) : this(prefix, Credentials.GenerateCredential(7), string.Format("Pr!5{0}", Credentials.GenerateCredential(10)), false)
        {
        }

        public void Create()
        {
            if (this.created)
            {
                throw new InvalidOperationException("This user has already been created.");
            }

            if (WindowsUsersAndGroups.ExistsUser(this.username))
            {
                throw new InvalidOperationException("This windows user already exists.");
            }

            WindowsUsersAndGroups.CreateUser(this.username, this.password);
            Persistence.SaveValue("prison_users", this.username, this.password);
            this.created = true;
        }

        public void Delete()
        {
            if (!this.created)
            {
                throw new InvalidOperationException("This user has not been created yet.");
            }

            if (!WindowsUsersAndGroups.ExistsUser(this.username))
            {
                throw new InvalidOperationException("Cannot find this windows user.");
            }

            WindowsUsersAndGroups.DeleteUser(this.username);
        }

        private static string GetUsernamePrefix(string username)
        {
            string[] pieces = username.Split(PrisonUser.Separator);
            if (pieces.Length != 3)
            {
                return string.Empty;
            }
            else
            {
                return pieces[1];
            }
        }

        private string GenerateUsername(string username)
        {
            List<string> usernamePieces = new List<string>();
            usernamePieces.Add(PrisonUser.GlobalPrefix);
            
            if (!string.IsNullOrWhiteSpace(this.usernamePrefix))
            {
                usernamePieces.Add(this.usernamePrefix);
            }

            usernamePieces.Add(username);

            return string.Join(PrisonUser.Separator.ToString(), usernamePieces.ToArray());
        }
    }
}
