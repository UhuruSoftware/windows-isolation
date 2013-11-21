using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uhuru.Prison.Data
{
    /// <summary>
    /// This class provides a helper function to get to a PrisonEntities object that is connected to the database that sits next to the bits.
    /// </summary>
    public static class Connection
    {
        /// <summary>
        /// The path to the local SQLite database, relative to the location of the Uhuru.Prison.Data assembly.
        /// <remarks>
        /// We assume it's always next to the assemblies.
        /// </remarks>
        /// </summary>
        private const string DatabaseFile = @".\prison.s3db";

        /// <summary>
        /// An Entity Framework template connection string for the PrisonModel.
        /// </summary>
        private const string ConnectionString = @"metadata=res://*/PrisonModel.csdl|res://*/PrisonModel.ssdl|res://*/PrisonModel.msl;provider=System.Data.SQLite;provider connection string=""data source={0}""";


        /// <summary>
        /// Gets the a PrisonEntities object that is connected to the local SQLite database.
        /// </summary>
        /// <returns>
        /// A PrisonEntities object connected to the 'prison.s3db' database that exists next to the assembly.
        /// </returns>
        public static PrisonEntities GetPrisonEntities()
        {
            string assemblyLocation = Path.GetDirectoryName(typeof(Connection).Assembly.Location);
            string databaseLocation = Path.GetFullPath(Path.Combine(assemblyLocation, Connection.DatabaseFile));

            string connectionString = string.Format(Connection.ConnectionString, databaseLocation);
            
            System.Data.SQLite.SQLiteFactory.Instance.;

            return new PrisonEntities(connectionString);
        }
    }
}
