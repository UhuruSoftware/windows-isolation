using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Uhuru.Prison
{
    [Serializable]
    public class PersistenceRow
    {
        public string GroupKey;
        public string ValueKey;
        public object Value;

        public PersistenceRow()
        {
        }

        public PersistenceRow(string groupKey, string valueKey, object value)
        {
            this.GroupKey = groupKey;
            this.ValueKey = valueKey;
            this.Value = value;
        }
    }

    public static class Persistence
    {

        private static string location = @"c:\ProgramData\Uhuru\prisondb.xml";

        public static void SetLocation(string location)
        {
            Persistence.location = location;
        }

        public static object ReadValue(string groupKey, string valueKey)
        {
            Dictionary<string, Dictionary<string, object>> data = ReadData();

            if (data.ContainsKey(groupKey) && data[groupKey].ContainsKey(valueKey))
            {
                return data[groupKey][valueKey];
            }
            else
            {
                return null;
            }
        }

        private static Dictionary<string, Dictionary<string, object>> ReadData()
        {
            Dictionary<string, Dictionary<string, object>> result = new Dictionary<string, Dictionary<string, object>>();

            if (File.Exists(location))
            {

                List<PersistenceRow> values = null;

                XmlSerializer serializer = new XmlSerializer(typeof(List<PersistenceRow>));

                using (FileStream stream = File.OpenRead(Persistence.location))
                {
                    values = (List<PersistenceRow>)serializer.Deserialize(stream);
                }

                foreach (PersistenceRow value in values)
                {
                    if (!result.ContainsKey(value.GroupKey))
                    {
                        result[value.GroupKey] = new Dictionary<string, object>();
                    }

                    result[value.GroupKey][value.ValueKey] = value.Value;
                }
            }

            return result;
        }

        public static void SaveValue(string group, string key, object value)
        {
            Dictionary<string, Dictionary<string, object>> data = Persistence.ReadData();
            
            Dictionary<string, object> groupValues = null;

            if (value != null)
            {
                if (!data.TryGetValue(group, out groupValues))
                {
                    data[group] = new Dictionary<string, object>();
                }

                data[group][key] = value;
            }
            else
            {
                if (data.TryGetValue(group, out groupValues))
                {
                    data[group].Remove(key);
                    if (data[group].Count == 0)
                    {
                        data.Remove(group);
                    }
                }
            }

            XmlSerializer serializer = new XmlSerializer(typeof(List<PersistenceRow>));

            List<PersistenceRow> values = new List<PersistenceRow>();
            foreach (string groupKey in data.Keys)
            {
                foreach (string valueKey in data[groupKey].Keys)
                {
                    PersistenceRow dataRow = new PersistenceRow(groupKey, valueKey, data[groupKey][valueKey]);
                    values.Add(dataRow);
                }
            }

            string containerDir = Directory.GetParent(Persistence.location).FullName;

            if (!Directory.Exists(containerDir))
            {
                Directory.CreateDirectory(containerDir);
            }

            using (FileStream stream = File.Open(Persistence.location, FileMode.Create, FileAccess.Write))
            {
                serializer.Serialize(stream, values);
            }
        }
    }
}
