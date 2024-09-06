using Microsoft.Data.Sqlite;

namespace Fls.AcesysConversion.Helpers.Database
{
    public partial class DbHelper
    {
        public static void UpdateMappingDetails(string name, string xml)
        {
            using SqliteConnection conn = new(connectionString);
            using SqliteCommand cmd = conn.CreateCommand();
            conn.Open();

            cmd.CommandText = $"UPDATE MAPPING_DETAIL SET Standard='{xml}' WHERE To_Object='{name}'";
            _ = cmd.ExecuteNonQuery();
        }

        public static void UpdateMappingDetailsFacePlateDecoratedData(string name, string xml)
        {
            using SqliteConnection conn = new(connectionString);
            using SqliteCommand cmd = conn.CreateCommand();
            conn.Open();

            cmd.CommandText = $"UPDATE MAPPING_DETAIL SET Faceplate_Decorated_Data='{xml}' WHERE To_Object='{name}'";
            _ = cmd.ExecuteNonQuery();
        }

        public static void UpdateMappingDetailsAddOnDecoratedData(string name, string xml)
        {
            using SqliteConnection conn = new(connectionString);
            using SqliteCommand cmd = conn.CreateCommand();
            conn.Open();

            cmd.CommandText = $"UPDATE MAPPING_DETAIL SET Addon_Decorated_Data='{xml}' WHERE To_Object='{name}'";
            _ = cmd.ExecuteNonQuery();
        }

        public static void UpdateMappingDetailsHmiTags(string name, string xml)
        {
            using SqliteConnection conn = new(connectionString);
            using SqliteCommand cmd = conn.CreateCommand();
            conn.Open();

            cmd.CommandText = $"UPDATE MAPPING_DETAIL SET Standard='{xml}' WHERE To_Object='{name}'";
            _ = cmd.ExecuteNonQuery();
        }
    }
}
