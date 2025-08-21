using System.Security.Cryptography;
using System.Text;
using Core.Configurations;
using Core.Globals;
using Core.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using Serilog;
using XtremeWorlds.Server.Game.Network;
using static Core.Globals.Command;
using static Core.Globals.Type;
using File = System.IO.File;
using Path = System.IO.Path;
using Resource = XtremeWorlds.Server.Game.Objects.Resource;
using Task = System.Threading.Tasks.Task;
using Type = Core.Globals.Type;

namespace XtremeWorlds.Server.Game;

public class Database
{
    private static readonly int StatCount = Enum.GetValues<Stat>().Length;

    private static readonly SemaphoreSlim ConnectionSemaphore = new(SettingsManager.Instance.MaxSqlClients, SettingsManager.Instance.MaxSqlClients);

    public static string ConnectionString { get; set; } = string.Empty;

    public static async Task CreateDatabaseAsync(string databaseName)
    {
        await ConnectionSemaphore.WaitAsync();
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString.Replace("Database=mirage", "Database=postgres"));
            await connection.OpenAsync();

            await using var checkDatabaseCommand = connection.CreateCommand();
            
            checkDatabaseCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @DatabaseName";
            checkDatabaseCommand.Parameters.Add("DatabaseName", NpgsqlDbType.Varchar).Value = databaseName;
            
            var databaseExists = await checkDatabaseCommand.ExecuteScalarAsync() is not null;
            if (!databaseExists)
            {
                await using var createCommand = new NpgsqlCommand($"CREATE DATABASE {databaseName}", connection);
                
                createCommand.CommandText = $"CREATE DATABASE @DatabaseName";
                createCommand.Parameters.Add("DatabaseName", NpgsqlDbType.Varchar).Value = databaseName;
                
                await createCommand.ExecuteNonQueryAsync();

                await using var dbConnection = new NpgsqlConnection(ConnectionString);
                await dbConnection.CloseAsync();
            }
        }
        finally
        {
            ConnectionSemaphore.Release();
        }
    }

    public static async Task UpdateRowByColumnAsync(string columnName, long value, string targetColumn, string newValue, string tableName)
    {
        await ConnectionSemaphore.WaitAsync();
        try
        {
            var sql = $"UPDATE {tableName} SET {targetColumn} = @newValue::jsonb WHERE {columnName} = @value;";

            newValue = newValue.Replace(@"\u0000", "");

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@value", value);
            command.Parameters.AddWithValue("@newValue", newValue);

            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            ConnectionSemaphore.Release();
        }
    }

    public static async Task CreateTablesAsync()
    {
        await ConnectionSemaphore.WaitAsync();
        try
        {
            var dataTable = "id SERIAL PRIMARY KEY, data jsonb";
            var playerTable = "id BIGINT PRIMARY KEY, data jsonb, bank jsonb";

            for (int i = 1, loopTo = Core.Globals.Constant.MaxChars; i <= loopTo; i++)
                playerTable += $", character{i} jsonb";

            var tableNames = new[] {"job", "item", "map", "npc", "shop", "skill", "resource", "animation", "projectile", "moral"};

            var tasks = tableNames.Select(tableName => CreateTableAsync(tableName, dataTable));
            await Task.WhenAll(tasks);

            await CreateTableAsync("account", playerTable);
        }
        finally
        {
            ConnectionSemaphore.Release();
        }
    }

    public static async Task CreateTableAsync(string tableName, string layout)
    {
        await ConnectionSemaphore.WaitAsync();
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($"CREATE TABLE IF NOT EXISTS {tableName} ({layout});", conn);
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            ConnectionSemaphore.Release();
        }
    }

    public static async Task<List<long>> GetDataAsync(string tableName)
    {
        var ids = new List<long>();

        await ConnectionSemaphore.WaitAsync();
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            // Define a query
            var cmd = new NpgsqlCommand($"SELECT id FROM {tableName}", conn);

            // Execute a query
            await using var reader = await cmd.ExecuteReaderAsync();
            // Read all rows and output the first column in each row
            while (await reader.ReadAsync())
            {
                var id = await reader.GetFieldValueAsync<long>(0);
                ids.Add(id);
            }
        }
        finally
        {
            ConnectionSemaphore.Release();
        }

        return ids;
    }

    public static async Task<bool> RowExistsAsync(long id, string table)
    {
        await ConnectionSemaphore.WaitAsync();
        try
        {
            var sql = $"SELECT EXISTS (SELECT 1 FROM {table} WHERE id = @id);";

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetBoolean(0);
            }
            else
            {
                return false;
            }
        }
        finally
        {
            ConnectionSemaphore.Release();
        }
    }

    public static async Task InsertRowByColumnAsync(long id, string data, string tableName, string dataColumn, string idColumn)
    {
        await ConnectionSemaphore.WaitAsync();
        try
        {
            // Sanitize the data string
            data = data.Replace("\\u0000", "");

            var sql = $@"
                    INSERT INTO {tableName} ({idColumn}, {dataColumn}) 
                    VALUES (@id, @data::jsonb)
                    ON CONFLICT ({idColumn}) 
                    DO UPDATE SET {dataColumn} = @data::jsonb;";

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@data", data); // Ensure this is properly serialized JSON

            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            ConnectionSemaphore.Release();
        }
    }

    public static async Task<JObject?> SelectRowAsync(long id, string tableName, string columnName)
    {
        await ConnectionSemaphore.WaitAsync();
        try
        {
            var sql = $"SELECT {columnName} FROM {tableName} WHERE id = @id;";

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var jsonbData = reader.GetString(0);
                var jsonObject = JObject.Parse(jsonbData);
                return jsonObject;
            }
            else
            {
                return null;
            }
        }
        finally
        {
            ConnectionSemaphore.Release();
        }
    }

    public static async Task<JObject?> SelectRowByColumnAsync(string columnName, long value, string tableName, string dataColumn)
    {
        await ConnectionSemaphore.WaitAsync();
        try
        {
            var sql = $"SELECT {dataColumn} FROM {tableName} WHERE {columnName} = @value;";

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@value", Math.Abs(value));

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                // Check if the first column is not null
                if (!reader.IsDBNull(0))
                {
                    var jsonbData = reader.GetString(0);
                    var jsonObject = JObject.Parse(jsonbData);
                    return jsonObject;
                }
                else
                {
                    // Handle null value or return null JObject...
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        finally
        {
            ConnectionSemaphore.Release();
        }
    }

    public static bool RowExistsByColumn(string columnName, long value, string tableName)
    {
        var sql = $"SELECT EXISTS (SELECT 1 FROM {tableName} WHERE {columnName} = @value);";

        using var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@value", value);

        var exists = Convert.ToBoolean(command.ExecuteScalar());
        return exists;
    }

    public static void UpdateRow(long id, string data, string table, string columnName)
    {
        var sqlCheck = $"SELECT column_name FROM information_schema.columns WHERE table_name='{table}' AND column_name='{columnName}';";

        using var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();

        // Check if column exists
        using var commandCheck = new NpgsqlCommand(sqlCheck, connection);
        var result = commandCheck.ExecuteScalar();

        // If column exists, then proceed with update
        if (result is not null)
        {
            var sqlUpdate = $"UPDATE {table} SET {columnName} = @data WHERE id = @id;";

            using var commandUpdate = new NpgsqlCommand(sqlUpdate, connection);
            var jsonString = data;
            commandUpdate.Parameters.AddWithValue("@data", NpgsqlDbType.Jsonb, jsonString);
            commandUpdate.Parameters.AddWithValue("@id", id);

            commandUpdate.ExecuteNonQuery();
        }
        else
        {
            Console.WriteLine($"Column '{columnName}' does not exist in table {table}.");
        }
    }

    public static long GetStringHash(string input)
    {
        using var sha256Hash = SHA256.Create();
        // ComputeHash - returns byte array
        var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Convert byte array to a long
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        // Use only the first 8 bytes (64 bits) to fit a Long
        return Math.Abs((BitConverter.ToInt64(bytes, 0)));
    }

    public static void UpdateRowByColumn(string columnName, long value, string targetColumn, string newValue, string tableName)
    {
        var sql = $"UPDATE {tableName} SET {targetColumn} = @newValue::jsonb WHERE {columnName} = @value;";

        newValue = newValue.Replace(@"\u0000", "");

        using var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@value", value);
        command.Parameters.AddWithValue("@newValue", newValue);

        command.ExecuteNonQuery();
    }

    public static bool RowExists(long id, string table)
    {
        var sql = $"SELECT EXISTS (SELECT 1 FROM {table} WHERE id = @id);";

        using var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return reader.GetBoolean(0);
        }
        else
        {
            return false;
        }
    }

    public static void InsertRow(long id, string data, string tableName)
    {
        using var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();

        using var cmd = new NpgsqlCommand();
        cmd.Connection = conn;
        cmd.CommandText = $"INSERT INTO {tableName} (id, data) VALUES (@id, @data::jsonb);";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@data", data); // Convert JObject back to string

        cmd.ExecuteNonQuery();
    }

    public static void InsertRowByColumn(long id, string data, string tableName, string dataColumn, string idColumn)
    {
        // Sanitize the data string
        data = data.Replace("\\u0000", "");

        var sql = $@"
            INSERT INTO {tableName} ({idColumn}, {dataColumn}) 
            VALUES (@id, @data::jsonb)
            ON CONFLICT ({idColumn}) 
            DO UPDATE SET {dataColumn} = @data::jsonb;";

        using var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@data", data); // Ensure this is properly serialized JSON

        command.ExecuteNonQuery();
    }

    public static JObject? SelectRowByColumn(string columnName, long value, string tableName, string dataColumn)
    {
        var sql = $"SELECT {dataColumn} FROM {tableName} WHERE {columnName} = @value;";

        using var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();

        using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@value", Math.Abs(value));

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            // Check if the first column is not null
            if (!reader.IsDBNull(0))
            {
                var jsonbData = reader.GetString(0);
                var jsonObject = JObject.Parse(jsonbData);
                return jsonObject;
            }
            else
            {
                // Handle null value or return null JObject...
                return null;
            }
        }
        else
        {
            return null;
        }
    }
    
    public static string GetVar(string filePath, string section, string key)
    {
        var isInSection = false;

        foreach (var line in File.ReadAllLines(filePath))
        {
            if (line.Equals("[" + section + "]", StringComparison.OrdinalIgnoreCase))
            {
                isInSection = true;
            }
            else if (line.StartsWith("[") & line.EndsWith("]"))
            {
                isInSection = false;
            }
            else if (isInSection & line.Contains("="))
            {
                var parts = line.Split(new char[] {'='}, 2);
                if (parts[0].Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return parts[1];
                }
            }
        }

        return string.Empty; // Key not found
    }
    
    public static void ClearJob(int jobNum)
    {
        var statCount = Enum.GetValues<Stat>().Length;
        Data.Job[jobNum].Stat = new int[statCount];
        Data.Job[jobNum].StartItem = new int[Core.Globals.Constant.MaxStartItems];
        Data.Job[jobNum].StartValue = new int[Core.Globals.Constant.MaxStartItems];

        Data.Job[jobNum].Name = "";
        Data.Job[jobNum].Desc = "";
        Data.Job[jobNum].StartMap = 1;
        Data.Job[jobNum].MaleSprite = 0;
        Data.Job[jobNum].FemaleSprite = 0;
    }

    public static async Task LoadJobAsync(int jobNum)
    {
        var data = await SelectRowAsync(jobNum, "job", "data");

        if (data is null)
        {
            ClearJob(jobNum);
            return;
        }

        var jobData = JObject.FromObject(data).ToObject<Job>();
        Data.Job[jobNum] = jobData;
    }

    public static async Task LoadJobsAsync()
    {
        var tasks = Enumerable.Range(0, Core.Globals.Constant.MaxJobs).Select(i => Task.Run(() => LoadJobAsync(i)));
        await Task.WhenAll(tasks);
    }

    public static void SaveJob(int jobNum)
    {
        var json = JsonConvert.SerializeObject(Data.Job[jobNum]);

        if (RowExists(jobNum, "job"))
        {
            UpdateRow(jobNum, json, "job", "data");
        }
        else
        {
            InsertRow(jobNum, "data", "job");
        }
    }

    public static void ClearMap(int mapNum)
    {
        int x;

        Data.Map[mapNum].Tileset = 1;
        Data.Map[mapNum].Name = "";
        Data.Map[mapNum].MaxX = Core.Globals.Constant.MaxMapx;
        Data.Map[mapNum].MaxY = Core.Globals.Constant.MaxMapy;
        Data.Map[mapNum].Npc = new int[Core.Globals.Constant.MaxMapNpcs];
        Data.Map[mapNum].Tile = new Tile[(Data.Map[mapNum].MaxX), (Data.Map[mapNum].MaxY)];

        var loopTo = Data.Map[mapNum].MaxX;
        for (x = 0; x < loopTo; x++)
        {
            var loopTo1 = Data.Map[mapNum].MaxY;
            for (var y = 0; y < loopTo1; y++)
                Data.Map[mapNum].Tile[x, y].Layer = new Layer[Enum.GetValues<MapLayer>().Length];
        }

        var loopTo2 = Core.Globals.Constant.MaxMapNpcs;
        for (x = 0; x < loopTo2; x++)
        {
            Data.Map[mapNum].Npc[x] = -1;
        }

        Data.Map[mapNum].EventCount = 0;
        Data.Map[mapNum].Event = new Type.Event[1];

        // Reset the values for if a player is on the map or not
        Data.Map[mapNum].Name = "";
        Data.Map[mapNum].Music = "";
    }

    public static void SaveMap(int mapNum)
    {
        var json = JsonConvert.SerializeObject(Data.Map[mapNum]);

        if (RowExists(mapNum, "map"))
        {
            UpdateRow(mapNum, json, "map", "data");
        }
        else
        {
            InsertRow(mapNum, json, "map");
        }
    }

    public static async Task LoadMapsAsync()
    {
        var tasks = Enumerable.Range(0, Core.Globals.Constant.MaxMaps).Select(i => Task.Run(() => LoadMapAsync(i)));
        await Task.WhenAll(tasks);
    }

    public static async Task LoadNpcsAsync()
    {
        var tasks = Enumerable.Range(0, Core.Globals.Constant.MaxNpcs).Select(i => Task.Run(() => LoadNpcAsync(i)));
        await Task.WhenAll(tasks);
    }

    public static async Task LoadShopsAsync()
    {
        var tasks = Enumerable.Range(0, Core.Globals.Constant.MaxShops).Select(i => Task.Run(() => LoadShopAsync(i)));
        await Task.WhenAll(tasks);
    }

    public static async Task LoadSkillsAsync()
    {
        var tasks = Enumerable.Range(0, Core.Globals.Constant.MaxSkills).Select(i => Task.Run(() => LoadSkillAsync(i)));
        await Task.WhenAll(tasks);
    }

    public static async Task LoadMapAsync(int mapNum)
    {
        var data = await SelectRowAsync(mapNum, "map", "data");
        if (data is null)
        {
            ClearMap(mapNum);
            return;
        }

        var mapData = JObject.FromObject(data).ToObject<Map>();
        
        Data.Map[mapNum] = mapData;

        Resource.CacheResources(mapNum);
    }

    public static void ClearMapItem(int index, int mapNum)
    {
        Data.MapItem[mapNum, index].PlayerName = "";
        Data.MapItem[mapNum, index].Num = -1;
    }

    public static void SaveNpc(int npcNum)
    {
        var json = JsonConvert.SerializeObject(Data.Npc[npcNum]);

        if (RowExists(npcNum, "npc"))
        {
            UpdateRow(npcNum, json, "npc", "data");
        }
        else
        {
            InsertRow(npcNum, json, "npc");
        }
    }

    public static async Task LoadNpcAsync(int npcNum)
    {
        var data = await SelectRowAsync(npcNum, "npc", "data");
        if (data is null)
        {
            ClearNpc(npcNum);
            return;
        }

        var npcData = JObject.FromObject(data).ToObject<Type.Npc>();
        Data.Npc[npcNum] = npcData;
    }

    public static void ClearMapNpc(int index, int mapNum)
    {
        var count = Enum.GetValues<Vital>().Length;
        Data.MapNpc[mapNum].Npc[index].Vital = new int[count];
        Data.MapNpc[mapNum].Npc[index].SkillCd = new int[Core.Globals.Constant.MaxNpcSkills];
        Data.MapNpc[mapNum].Npc[index].Num = -1;
        Data.MapNpc[mapNum].Npc[index].SkillBuffer = -1;
    }

    public static void ClearNpc(int index)
    {
        Data.Npc[index].Name = "";
        Data.Npc[index].AttackSay = "";
        var statCount = Enum.GetValues<Stat>().Length;
        Data.Npc[index].Stat = new byte[statCount];

        for (int i = 0, loopTo = Core.Globals.Constant.MaxDropItems; i < loopTo; i++)
        {
            Data.Npc[index].DropChance = new int[Core.Globals.Constant.MaxDropItems];
            Data.Npc[index].DropItem = new int[Core.Globals.Constant.MaxDropItems];
            Data.Npc[index].DropItemValue = new int[Core.Globals.Constant.MaxDropItems];
            Data.Npc[index].Skill = new byte[Core.Globals.Constant.MaxNpcSkills];
        }
    }
    
    public static void SaveShop(int shopNum)
    {
        var json = JsonConvert.SerializeObject(Data.Shop[shopNum]);

        if (RowExists(shopNum, "shop"))
        {
            UpdateRow(shopNum, json, "shop", "data");
        }
        else
        {
            InsertRow(shopNum, json, "shop");
        }
    }

    public static async Task LoadShopAsync(int shopNum)
    {
        var data = await SelectRowAsync(shopNum, "shop", "data");

        if (data is null)
        {
            ClearShop(shopNum);
            return;
        }

        var shopData = JObject.FromObject(data).ToObject<Shop>();
        Data.Shop[shopNum] = shopData;
    }

    public static void ClearShop(int index)
    {
        Data.Shop[index] = default;
        Data.Shop[index].Name = "";

        Data.Shop[index].TradeItem = new TradeItem[Core.Globals.Constant.MaxTrades];
        for (int i = 0, loopTo = Core.Globals.Constant.MaxTrades; i < loopTo; i++)
        {
            Data.Shop[index].TradeItem[i].Item = -1;
            Data.Shop[index].TradeItem[i].CostItem = -1;
        }
    }
    
    public static void SaveSkill(int skillNum)
    {
        var json = JsonConvert.SerializeObject(Data.Skill[skillNum]);

        if (RowExists(skillNum, "skill"))
        {
            UpdateRow(skillNum, json, "skill", "data");
        }
        else
        {
            InsertRow(skillNum, json, "skill");
        }
    }

    public static async Task LoadSkillAsync(int skillNum)
    {
        var data = await SelectRowAsync(skillNum, "skill", "data");

        if (data is null)
        {
            ClearSkill(skillNum);
            return;
        }

        var skillData = JObject.FromObject(data).ToObject<Skill>();
        Data.Skill[skillNum] = skillData;
    }

    public static void ClearSkill(int index)
    {
        Data.Skill[index].Name = "";
        Data.Skill[index].LevelReq = 0;
    }
    
    public static async Task SaveAllPlayersOnlineAsync()
    {
        foreach (var i in PlayerService.Instance.PlayerIds)
        {
            if (!NetworkConfig.IsPlaying(i))
                continue;

            await SaveCharacterAsync(i, Data.TempPlayer[i].Slot);
            
            SaveBank(i);
        }
    }

    public static async Task SaveCharacterAsync(int index, int slot)
    {
        await Task.Run(() => SaveCharacter(index, slot));
    }

    public static async Task SaveAccountAsync(int index)
    {
        var json = JsonConvert.SerializeObject(Data.Account[index]);
        var username = GetAccountLogin(index);
        var id = GetStringHash(username);

        if (await RowExistsAsync(id, "account"))
        {
            await UpdateRowByColumnAsync("id", id, "data", json, "account");
        }
        else
        {
            await InsertRowByColumnAsync(id, json, "account", "data", "id");
        }
    }

    public static void RegisterAccount(int playerId, string accountName, string password)
    {
        SetPlayerLogin(playerId, accountName);
        SetPlayerPassword(playerId, password);

        var json = JsonConvert.SerializeObject(Data.Account[playerId]);
        var id = GetStringHash(accountName);

        InsertRowByColumn(id, json, "account", "data", "id");
    }

    public static bool LoadAccount(int playerId, string accountName)
    {
        var data = SelectRowByColumn("id", GetStringHash(accountName), "account", "data");
        if (data is null)
        {
            return false;
        }

        var accountData = JObject.FromObject(data).ToObject<Account>();
        
        Data.Account[playerId] = accountData;
        
        return true;
    }

    public static void ClearAccount(int index)
    {
        SetPlayerLogin(index, "");
        SetPlayerPassword(index, "");
    }

    public static void ClearPlayer(int index)
    {
        ClearAccount(index);
        ClearBank(index);

        Data.TempPlayer[index].SkillCd = new int[Core.Globals.Constant.MaxPlayerSkills];
        Data.TempPlayer[index].TradeOffer = new PlayerInv[Core.Globals.Constant.MaxInv];

        Data.TempPlayer[index].SkillCd = new int[Core.Globals.Constant.MaxPlayerSkills];
        Data.TempPlayer[index].Editor = EditorType.None;
        Data.TempPlayer[index].SkillBuffer = -1;
        Data.TempPlayer[index].InShop = -1;
        Data.TempPlayer[index].InTrade = -1;
        Data.TempPlayer[index].InParty = -1;

        for (int i = 0, loopTo = Data.TempPlayer[index].EventProcessingCount; i < loopTo; i++)
            Data.TempPlayer[index].EventProcessing[i].EventId = -1;

        ClearCharacter(index);
    }
    
    public static void LoadBank(int index)
    {
        var data = SelectRowByColumn("id", GetStringHash(GetAccountLogin(index)), "account", "bank");

        if (data is null)
        {
            ClearBank(index);
            return;
        }

        var bankData = JObject.FromObject(data).ToObject<Bank>();
        Data.Bank[index] = bankData;
    }

    public static void SaveBank(int index)
    {
        var json = JsonConvert.SerializeObject(Data.Bank[index]);
        var username = GetAccountLogin(index);
        var id = GetStringHash(username);

        if (RowExistsByColumn("id", id, "account"))
        {
            UpdateRowByColumn("id", id, "bank", json, "account");
        }
        else
        {
            InsertRowByColumn(id, json, "account", "bank", "id");
        }
    }

    public static void ClearBank(int index)
    {
        Data.Bank[index].Item = new PlayerInv[Core.Globals.Constant.MaxBank + 1];
        for (var i = 0; i < Core.Globals.Constant.MaxBank; i++)
        {
            Data.Bank[index].Item[i].Num = -1;
            Data.Bank[index].Item[i].Value = 0;
        }
    }

    public static void ClearCharacter(int index)
    {
        Data.Player[index].Name = "";
        Data.Player[index].Job = 0;
        Data.Player[index].Dir = 0;
        Data.Player[index].Access = (byte) AccessLevel.Player;

        Data.Player[index].Equipment = new int[Enum.GetValues<Equipment>().Length];
        for (int i = 0, loopTo = Enum.GetValues<Equipment>().Length; i < loopTo; i++)
            Data.Player[index].Equipment[i] = -1;

        Data.Player[index].Inv = new PlayerInv[Core.Globals.Constant.MaxInv];
        for (int i = 0, loopTo1 = Core.Globals.Constant.MaxInv; i < loopTo1; i++)
        {
            Data.Player[index].Inv[i].Num = -1;
            Data.Player[index].Inv[i].Value = 0;
        }

        Data.Player[index].Exp = 0;
        Data.Player[index].Level = 0;
        Data.Player[index].Map = 0;
        Data.Player[index].Name = "";
        Data.Player[index].Pk = false;
        Data.Player[index].Points = 0;
        Data.Player[index].Sex = 0;

        Data.Player[index].Skill = new PlayerSkill[Core.Globals.Constant.MaxPlayerSkills];
        for (int i = 0, loopTo2 = Core.Globals.Constant.MaxPlayerSkills; i < loopTo2; i++)
        {
            Data.Player[index].Skill[i].Num = -1;
            Data.Player[index].Skill[i].Cd = 0;
        }

        Data.Player[index].Sprite = 0;

        Data.Player[index].Stat = new byte[Enum.GetValues<Stat>().Length];
        for (int i = 0, loopTo3 = Enum.GetValues<Stat>().Length; i < loopTo3; i++)
            Data.Player[index].Stat[i] = 0;

        var count = Enum.GetValues<Vital>().Length;
        Data.Player[index].Vital = new int[count];
        for (int i = 0, loopTo4 = count; i < loopTo4; i++)
            Data.Player[index].Vital[i] = 0;

        Data.Player[index].X = 0;
        Data.Player[index].Y = 0;

        Data.Player[index].Hotbar = new Hotbar[Core.Globals.Constant.MaxHotbar];
        for (int i = 0, loopTo5 = Core.Globals.Constant.MaxHotbar; i < loopTo5; i++)
        {
            Data.Player[index].Hotbar[i].Slot = -1;
            Data.Player[index].Hotbar[i].SlotType = 0;
        }

        Data.Player[index].Switches = new byte[Core.Globals.Constant.MaxSwitches];
        for (int i = 0, loopTo6 = Core.Globals.Constant.MaxSwitches; i < loopTo6; i++)
            Data.Player[index].Switches[i] = 0;

        Data.Player[index].Variables = new int[Core.Globals.Constant.MaxVariables];
        for (int i = 0, loopTo7 = Core.Globals.Constant.MaxVariables; i < loopTo7; i++)
            Data.Player[index].Variables[i] = 0;

        var resoruceCount = Enum.GetValues<ResourceSkill>().Length;
        Data.Player[index].GatherSkills = new ResourceType[resoruceCount];
        for (int i = 0, loopTo8 = resoruceCount; i < loopTo8; i++)
        {
            Data.Player[index].GatherSkills[i].SkillLevel = 0;
            Data.Player[index].GatherSkills[i].SkillCurExp = 0;
            SetPlayerGatherSkillMaxExp(index, i, GetSkillNextLevel(index, i));
        }

        for (int i = 0, loopTo9 = Enum.GetValues<Equipment>().Length; i < loopTo9; i++)
            Data.Player[index].Equipment[i] = -1;
    }

    public static bool LoadCharacter(int index, int charNum)
    {
        var data = SelectRowByColumn("id", GetStringHash(GetAccountLogin(index)), "account", "character" + charNum);

        if (data is null)
        {
            return false;
        }

        var characterData = data.ToObject<Type.Player>();

        if (characterData.Name == "")
        {
            return false;
        }

        Data.Player[index] = characterData;
        Data.TempPlayer[index].Slot = (byte) charNum;
        return true;
    }

    public static void SaveCharacter(int index, int slot)
    {
        var json = JsonConvert.SerializeObject(Data.Player[index]);
        var id = GetStringHash(GetAccountLogin(index));

        if (slot < 1 | slot > Core.Globals.Constant.MaxChars)
            return;

        if (RowExistsByColumn("id", id, "account"))
        {
            UpdateRowByColumn("id", id, "character" + slot, json, "account");
        }
        else
        {
            InsertRowByColumn(id, json, "account", "character" + slot, "id");
        }
    }

    public static void AddChar(int index, int slot, string name, byte sex, byte jobNum, int sprite)
    {
        int n;
        int i;

        if (Data.Player[index].Name == "")
        {
            Data.Player[index].Name = name;
            Data.Player[index].Sex = sex;
            Data.Player[index].Job = jobNum;
            Data.Player[index].Sprite = sprite;
            Data.Player[index].Level = 1;

            var statCount = Enum.GetValues<Stat>().Length;
            for (n = 0; n < statCount; n++)
                Data.Player[index].Stat[n] = (byte) Data.Job[jobNum].Stat[n];

            Data.Player[index].Dir = (byte) Direction.Down;
            Data.Player[index].Map = Data.Job[jobNum].StartMap;

            if (Data.Player[index].Map == 0)
                Data.Player[index].Map = 1;

            Data.Player[index].X = Data.Job[jobNum].StartX;
            Data.Player[index].Y = Data.Job[jobNum].StartY;
            Data.Player[index].Dir = (byte) Direction.Down;

            var vitalCount = Enum.GetValues<Vital>().Length;
            for (i = 0; i < vitalCount; i++)
                SetPlayerVital(index, (Vital) i, GetPlayerMaxVital(index, (Vital) i));

            // set starter equipment
            for (n = 0; n < Core.Globals.Constant.MaxStartItems; n++)
            {
                if (Data.Job[jobNum].StartItem[n] > 0)
                {
                    Data.Player[index].Inv[n].Num = Data.Job[jobNum].StartItem[n];
                    Data.Player[index].Inv[n].Value = Data.Job[jobNum].StartValue[n];
                }
            }

            // set skills
            var resourceCount = Enum.GetValues<ResourceSkill>().Length;
            for (i = 0; i < resourceCount; i++)
            {
                Data.Player[index].GatherSkills[i].SkillLevel = 0;
                Data.Player[index].GatherSkills[i].SkillCurExp = 0;
                SetPlayerGatherSkillMaxExp(index, i, GetSkillNextLevel(index, i));
            }

            SaveCharacter(index, slot);
        }
    }

    public static bool IsBanned(int index, string ip)
    {
        var isBanned = false;

        for (var i = ip.Length; i >= 0; i -= 1)
        {
            if (ip.Substring(i - 1, 1) == ".")
            {
                ip = ip.Substring(i - 1, 1);
                break;
            }
        }

        var filename = Path.Combine(DataPath.Database, "banlist.txt");

        // Check if file exists
        if (!File.Exists(filename))
        {
            return false;
        }

        var sr = new StreamReader(filename);

        while (sr.Peek() >= 0)
        {
            // Is banned?
            var line = sr.ReadLine();
            if ((line?.ToLower() ?? "") == (ip.Substring(0, Math.Min(line?.Length ?? 0, ip.Length)).ToLower() ?? ""))
            {
                isBanned = true;
            }
        }

        sr.Close();

        if (Data.Account[index].Banned)
        {
            isBanned = true;
        }

        return isBanned;
    }

    public static void BanPlayer(int banPlayerIndex, int bannedByIndex)
    {
        var filename = Path.Combine(DataPath.Database, "banlist.txt");
        int i;

        // Make sure the file exists
        if (!File.Exists(filename))
            File.Create(filename).Dispose();

        // Cut off last portion of ip
        var ip = PlayerService.Instance.ClientIp(banPlayerIndex);

        for (i = ip.Length; i >= 0; i -= 1)
        {
            if (ip.Substring(i - 1, 1) == ".")
            {
                break;
            }
        }

        Data.Account[banPlayerIndex].Banned = true;

        ip = ip.Substring(0, i);

        File.AppendAllText(filename, ip + "\n");

        NetworkSend.GlobalMsg(GetPlayerName(banPlayerIndex) + " has been banned from " + SettingsManager.Instance.GameName + " by " + GetPlayerName(bannedByIndex) + "!");

        Log.Information("{PlayerName} has banned {BannedPlayerName}", GetPlayerName(bannedByIndex), GetPlayerName(banPlayerIndex));

        var task = Objects.Player.LeftGame(banPlayerIndex);
        task.Wait();
    }

    public static void WriteJobDataToPacket(int jobNum, PacketWriter packetWriter)
    {
        packetWriter.WriteString(Data.Job[jobNum].Name);
        packetWriter.WriteString(Data.Job[jobNum].Desc);
        packetWriter.WriteInt32(Data.Job[jobNum].MaleSprite);
        packetWriter.WriteInt32(Data.Job[jobNum].FemaleSprite);

        for (var i = 0; i < StatCount; i++)
        {
            packetWriter.WriteInt32(Data.Job[jobNum].Stat[i]);
        }

        for (var q = 0; q < Core.Globals.Constant.MaxStartItems; q++)
        {
            packetWriter.WriteInt32(Data.Job[jobNum].StartItem[q]);
            packetWriter.WriteInt32(Data.Job[jobNum].StartValue[q]);
        }

        packetWriter.WriteInt32(Data.Job[jobNum].StartMap);
        packetWriter.WriteByte(Data.Job[jobNum].StartX);
        packetWriter.WriteByte(Data.Job[jobNum].StartY);
        packetWriter.WriteInt32(Data.Job[jobNum].BaseExp);
    }
}