using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;
using StackExchange.Redis;
using NRedisStack.RedisStackCommands;
using NRedisStack.Json.DataTypes;

namespace NRedisStack.Tests;

public class JsonTests : AbstractNRedisStackTest, IDisposable
{
    // private readonly string _testName = "JSON_TESTS";
    public JsonTests(RedisFixture redisFixture) : base(redisFixture) { }

    [Fact]
    public void TestSetFromFile()
    {
        //arrange
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommands(db);
        var keys = CreateKeyNames(1);

        //creating json string:
        var obj = new Person { Name = "Shachar", Age = 23 };
        string json = JsonSerializer.Serialize(obj);

        var file = "testFile.json";

        //writing json to file:
        File.WriteAllText(file, json);

        Assert.True(commands.SetFromFile(keys[0], "$", file));
        var actual = commands.Get(keys[0]);

        Assert.Equal(json, actual.ToString());
        File.Delete(file);

        //test not existing file:
        Assert.Throws<FileNotFoundException>(() => commands.SetFromFile(keys[0], "$", "notExistingFile.json"));
    }

    [Fact]
    public void TestSetFromDirectory()
    {
        //arrange
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommands(db);

        //creating json string:
        object[] persons = new object[10];
        string[] jsons = new string[10];
        string[] notJsons = new string[10];
        for (int i = 1; i <= 10; i++)
        {
            persons[i - 1] = new Person { Name = $"Person{i}", Age = i * 10 };
            jsons[i - 1] = JsonSerializer.Serialize(persons[i - 1]);
            notJsons[i - 1] = $"notjson{i}";
        }

        //creating directorys:
        Directory.CreateDirectory(Path.Combine("BaseDir", "DirNumber2", "DirNumber3"));

        //creating files in directorys:
        for (int i = 1; i <= 10; i++)
        {
            string jsonPath;
            string notJsonPath;
            if (i <= 3)
            {
                jsonPath = Path.Combine("BaseDir", $"jsonFile{i}.json");
                notJsonPath = Path.Combine("BaseDir", $"notJsonFile{i}.txt");
            }
            else if (i <= 6)
            {
                jsonPath = Path.Combine("BaseDir", "DirNumber2", $"jsonFile{i}.json");
                notJsonPath = Path.Combine("BaseDir", "DirNumber2", $"notJsonFile{i}.txt");
            }
            else
            {
                jsonPath = Path.Combine("BaseDir", "DirNumber2", "DirNumber3", $"jsonFile{i}.json");
                notJsonPath = Path.Combine("BaseDir", "DirNumber2", "DirNumber3", $"notJsonFile{i}.txt");
            }
            File.WriteAllText(Path.GetFullPath(jsonPath), jsons[i - 1]);
            File.WriteAllText(Path.GetFullPath(notJsonPath), notJsons[i - 1]);
        }

        Assert.Equal(10, commands.SetFromDirectory("$", "BaseDir"));

        var actual = commands.Get(Path.Combine("BaseDir", "DirNumber2", "DirNumber3", $"jsonFile7"));
        Assert.Equal(jsons[6], actual.ToString());
        Directory.Delete("BaseDir", true);
    }

    [Fact]
    public void TestJsonSetNotExist()
    {
        //arrange
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommands(db);
        db.Execute("FLUSHALL");

        var obj = new Person { Name = "Shachar", Age = 23 };
        Assert.True(commands.Set("Person:Shachar", "$", obj, When.NotExists));
        Assert.False(commands.Set("Person:Shachar", "$", obj, When.NotExists));
        Assert.True(commands.Set("Person:Shachar", "$", obj, When.Exists));
    }

    [Fact]
    public async Task TestJsonSetNotExistAsync()
    {
        //arrange
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommands(db);
        db.Execute("FLUSHALL");

        var obj = new Person { Name = "Shachar", Age = 23 };
        Assert.True(await commands.SetAsync("Person:Shachar", "$", obj, When.NotExists));
        Assert.False(await commands.SetAsync("Person:Shachar", "$", obj, When.NotExists));
        Assert.True(await commands.SetAsync("Person:Shachar", "$", obj, When.Exists));
    }

    [Fact]
    public void TestModulePrefixs()
    {
        IDatabase db1 = redisFixture.Redis.GetDatabase();
        IDatabase db2 = redisFixture.Redis.GetDatabase();

        var json1 = db1.JSON();
        var json2 = db2.JSON();

        Assert.NotEqual(json1.GetHashCode(), json2.GetHashCode());
    }

    [Fact]
    public void TestResp()
    {
        //arrange
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommands(db);
        var keys = CreateKeyNames(1);

        var key = keys[0];
        commands.Set(key, "$", new { name = "Steve", age = 33 });

        //act
        var respResult = commands.Resp(key);

        //assert
        var i = 0;
        Assert.Equal("{", respResult[i++]!.ToString());
        Assert.Equal("name", respResult[i++]!.ToString());
        Assert.Equal("Steve", respResult[i++]!.ToString());
        Assert.Equal("age", respResult[i++]!.ToString());
        Assert.Equal(33, (long)respResult[i]!);
        conn.GetDatabase().KeyDelete(key);
    }

    [Fact]
    public async Task TestRespAsync()
    {
        //arrange
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommandsAsync(db);
        var keys = CreateKeyNames(1);

        var key = keys[0];
        await commands.SetAsync(key, "$", new { name = "Steve", age = 33 });

        //act
        var respResult = await commands.RespAsync(key);

        //assert
        var i = 0;
        Assert.Equal("{", respResult[i++]!.ToString());
        Assert.Equal("name", respResult[i++]!.ToString());
        Assert.Equal("Steve", respResult[i++]!.ToString());
        Assert.Equal("age", respResult[i++]!.ToString());
        Assert.Equal(33, (long)respResult[i]!);
        conn.GetDatabase().KeyDelete(key);
    }

    [Fact]
    public void TestStringAppend()
    {
        //arrange
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommands(db);
        var keys = CreateKeyNames(2);

        var key = keys[0];
        commands.Set(key, "$", new { name = "Steve", sibling = new { name = "christopher" }, age = 33 });
        var simpleStringKey = keys[1];
        commands.Set(simpleStringKey, "$", "\"foo\"");

        //act
        var nullResult = commands.StrAppend(key, " Lorello", "$.age");
        var keyResult = commands.StrAppend(key, " Lorello", "$..name");
        var simpleKeyResult = commands.StrAppend(simpleStringKey, "bar");

        //assert
        var i = 0;
        Assert.Equal(2, keyResult.Length);
        Assert.Equal(13, keyResult[i++]);
        Assert.Equal(19, keyResult[i]);
        Assert.Null(nullResult[0]);
        Assert.Equal(6, simpleKeyResult[0]);
    }

    [Fact]
    public async Task TestStringAppendAsync()
    {
        //arrange
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommandsAsync(db);
        var keys = CreateKeyNames(2);

        var key = keys[0];
        await commands.SetAsync(key, "$", new { name = "Steve", sibling = new { name = "christopher" }, age = 33 });
        var simpleStringKey = keys[1];
        await commands.SetAsync(simpleStringKey, "$", "\"foo\"");

        //act
        var nullResult = await commands.StrAppendAsync(key, " Lorello", "$.age");
        var keyResult = await commands.StrAppendAsync(key, " Lorello", "$..name");
        var simpleKeyResult = await commands.StrAppendAsync(simpleStringKey, "bar");

        //assert
        var i = 0;
        Assert.Equal(2, keyResult.Length);
        Assert.Equal(13, keyResult[i++]);
        Assert.Equal(19, keyResult[i]);
        Assert.Null(nullResult[0]);
        Assert.Equal(6, simpleKeyResult[0]);
    }

    [Fact]
    public void StringLength()
    {
        //arrange
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommands(db);
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleStringKey = keys[1];

        commands.Set(key, "$", new { name = "Steve", sibling = new { name = "christopher" }, age = 33 });
        commands.Set(simpleStringKey, "$", "\"foo\"");

        var normalResult = commands.StrLen(key, "$..name");
        var nullResult = commands.StrLen(key, "$.age");
        var simpleResult = commands.StrLen(simpleStringKey);

        var i = 0;
        Assert.Equal(5, normalResult[i++]);
        Assert.Equal(11, normalResult[i]);
        Assert.Null(nullResult[0]);
        Assert.Equal(3, simpleResult[0]);
    }

    [Fact]
    public async Task StringLengthAsync()
    {
        //arrange
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommandsAsync(db);
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleStringKey = keys[1];

        await commands.SetAsync(key, "$", new { name = "Steve", sibling = new { name = "christopher" }, age = 33 });
        await commands.SetAsync(simpleStringKey, "$", "\"foo\"");

        var normalResult = await commands.StrLenAsync(key, "$..name");
        var nullResult = await commands.StrLenAsync(key, "$.age");
        var simpleResult = await commands.StrLenAsync(simpleStringKey);

        var i = 0;
        Assert.Equal(5, normalResult[i++]);
        Assert.Equal(11, normalResult[i]);
        Assert.Null(nullResult[0]);
        Assert.Equal(3, simpleResult[0]);
    }

    [Fact]
    public void Toggle()
    {
        //arrange
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommands(db);
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];

        commands.Set(key, "$", new { @bool = true, other = new { @bool = false }, age = 33 });
        commands.Set(simpleKey, "$", true);

        var result = commands.Toggle(key, "$..bool");
        var simpleResult = commands.Toggle(simpleKey);

        Assert.False(result[0]);
        Assert.True(result[1]);
        Assert.False(simpleResult[0]);
    }

    [Fact]
    public async Task ToggleAsync()
    {
        //arrange
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommandsAsync(db);
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];

        await commands.SetAsync(key, "$", new { @bool = true, other = new { @bool = false }, age = 33 });
        await commands.SetAsync(simpleKey, "$", true);

        var result = await commands.ToggleAsync(key, "$..bool");
        var simpleResult = await commands.ToggleAsync(simpleKey);

        Assert.False(result[0]);
        Assert.True(result[1]);
        Assert.False(simpleResult[0]);
    }

    [Fact]
    public void Type()
    {
        //arrange
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommands(db);
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];
        commands.Set(key, "$", new { name = "Steve", sibling = new { name = "christopher" }, age = 33, aDouble = 3.5 });
        commands.Set(simpleKey, "$", "true");

        var result = commands.Type(key, "$..name");
        Assert.Equal(JsonType.STRING, result[0]);
        Assert.Equal(JsonType.STRING, result[1]);
        result = commands.Type(key, "$..age");
        Assert.Equal(JsonType.INTEGER, result[0]);
        result = commands.Type(key, "$..aDouble");
        Assert.Equal(JsonType.NUMBER, result[0]);
        result = commands.Type(simpleKey);
        Assert.Equal(JsonType.BOOLEAN, result[0]);
    }

    [Fact]
    public async Task TypeAsync()
    {
        //arrange
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommandsAsync(db);
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];
        await commands.SetAsync(key, "$", new { name = "Steve", sibling = new { name = "christopher" }, age = 33, aDouble = 3.5 });
        await commands.SetAsync(simpleKey, "$", "true");

        var result = await commands.TypeAsync(key, "$..name");
        Assert.Equal(JsonType.STRING, result[0]);
        Assert.Equal(JsonType.STRING, result[1]);
        result = await commands.TypeAsync(key, "$..age");
        Assert.Equal(JsonType.INTEGER, result[0]);
        result = await commands.TypeAsync(key, "$..aDouble");
        Assert.Equal(JsonType.NUMBER, result[0]);
        result = await commands.TypeAsync(simpleKey);
        Assert.Equal(JsonType.BOOLEAN, result[0]);
    }

    [Fact]
    public void ArrayAppend()
    {
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommands(db);
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var complexKey = keys[1];

        commands.Set(key, "$", new { name = "Elizabeth", nickNames = new[] { "Beth" } });
        commands.Set(complexKey, "$", new { name = "foo", people = new[] { new { name = "steve" } } });
        var result = commands.ArrAppend(key, "$.nickNames", "Elle", "Liz", "Betty");
        Assert.Equal(4, result[0]);
        result = commands.ArrAppend(complexKey, "$.people", new { name = "bob" });
        Assert.Equal(2, result[0]);
    }

    [Fact]
    public async Task ArrayAppendAsync()
    {
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommandsAsync(db);
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var complexKey = keys[1];

        await commands.SetAsync(key, "$", new { name = "Elizabeth", nickNames = new[] { "Beth" } });
        await commands.SetAsync(complexKey, "$", new { name = "foo", people = new[] { new { name = "steve" } } });
        var result = await commands.ArrAppendAsync(key, "$.nickNames", "Elle", "Liz", "Betty");
        Assert.Equal(4, result[0]);
        result = await commands.ArrAppendAsync(complexKey, "$.people", new { name = "bob" });
        Assert.Equal(2, result[0]);
    }

    [Fact]
    public void ArrayIndex()
    {
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommands(db);
        var keys = CreateKeyNames(1);
        var key = keys[0];
        commands.Set(key, "$", new { name = "Elizabeth", nicknames = new[] { "Beth", "Betty", "Liz" }, sibling = new { name = "Johnathan", nicknames = new[] { "Jon", "Johnny" } } });
        var res = commands.ArrIndex(key, "$..nicknames", "Betty", 0, 5);
        Assert.Equal(1, res[0]);
        Assert.Equal(-1, res[1]);
    }

    [Fact]
    public async Task ArrayIndexAsync()
    {
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommandsAsync(db);
        var keys = CreateKeyNames(1);
        var key = keys[0];
        await commands.SetAsync(key, "$", new { name = "Elizabeth", nicknames = new[] { "Beth", "Betty", "Liz" }, sibling = new { name = "Johnathan", nicknames = new[] { "Jon", "Johnny" } } });
        var res = await commands.ArrIndexAsync(key, "$..nicknames", "Betty", 0, 5);
        Assert.Equal(1, res[0]);
        Assert.Equal(-1, res[1]);
    }

    [Fact]
    public void ArrayInsert()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];

        commands.Set(key, "$", new { name = "Alice", nicknames = new[] { "Al", "Ali", "Ally" } });
        commands.Set(simpleKey, "$", new[] { "Al", "Ali", "Ally" });

        var result = commands.ArrInsert(key, $"$.nicknames", 1, "Lys");
        Assert.Equal(4, result[0]);
        result = commands.ArrInsert(simpleKey, "$", 1, "Lys");
        Assert.Equal(4, result[0]);
    }

    [Fact]
    public async Task ArrayInsertAsync()
    {
        var commands = new JsonCommandsAsync(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];

        await commands.SetAsync(key, "$", new { name = "Alice", nicknames = new[] { "Al", "Ali", "Ally" } });
        await commands.SetAsync(simpleKey, "$", new[] { "Al", "Ali", "Ally" });

        var result = await commands.ArrInsertAsync(key, $"$.nicknames", 1, "Lys");
        Assert.Equal(4, result[0]);
        result = await commands.ArrInsertAsync(simpleKey, "$", 1, "Lys");
        Assert.Equal(4, result[0]);
    }

    [Fact]
    public void ArrayLength()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];
        commands.Set(key, "$", new { name = "Alice", nicknames = new[] { "Al", "Ali", "Ally" } });
        commands.Set(simpleKey, "$", new[] { "Al", "Ali", "Ally" });

        var result = commands.ArrLen(key, $"$.nicknames");
        Assert.Equal(3, result[0]);
        result = commands.ArrLen(simpleKey);
        Assert.Equal(3, result[0]);
    }

    [Fact]
    public async Task ArrayLengthAsync()
    {
        var commands = new JsonCommandsAsync(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];
        await commands.SetAsync(key, "$", new { name = "Alice", nicknames = new[] { "Al", "Ali", "Ally" } });
        await commands.SetAsync(simpleKey, "$", new[] { "Al", "Ali", "Ally" });

        var result = await commands.ArrLenAsync(key, $"$.nicknames");
        Assert.Equal(3, result[0]);
        result = await commands.ArrLenAsync(simpleKey);
        Assert.Equal(3, result[0]);
    }

    [Fact]
    public void ArrayPop()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];
        commands.Set(key, "$", new { name = "Alice", nicknames = new[] { "Al", "Ali", "Ally" } });
        commands.Set(simpleKey, "$", new[] { "Al", "Ali", "Ally" });

        var result = commands.ArrPop(key, "$.nicknames", 1);
        Assert.Equal("\"Ali\"", result[0].ToString());
        result = commands.ArrPop(key, "$.nicknames");
        Assert.Equal("\"Ally\"", result[0].ToString());
        result = commands.ArrPop(simpleKey);
        Assert.Equal("\"Ally\"", result[0].ToString());
    }

    [Fact]
    public async Task ArrayPopAsync()
    {
        var commands = new JsonCommandsAsync(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];
        await commands.SetAsync(key, "$", new { name = "Alice", nicknames = new[] { "Al", "Ali", "Ally" } });
        await commands.SetAsync(simpleKey, "$", new[] { "Al", "Ali", "Ally" });

        var result = await commands.ArrPopAsync(key, "$.nicknames", 1);
        Assert.Equal("\"Ali\"", result[0].ToString());
        result = await commands.ArrPopAsync(key, "$.nicknames");
        Assert.Equal("\"Ally\"", result[0].ToString());
        result = await commands.ArrPopAsync(simpleKey);
        Assert.Equal("\"Ally\"", result[0].ToString());
    }

    [Fact]
    public void ArrayTrim()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];
        commands.Set(key, "$", new { name = "Alice", nicknames = new[] { "Al", "Ali", "Ally" } });
        commands.Set(simpleKey, "$", new[] { "Al", "Ali", "Ally" });

        var result = commands.ArrTrim(key, "$.nicknames", 0, 0);
        Assert.Equal(1, result[0]);
        result = commands.ArrTrim(simpleKey, "$", 0, 1);
        Assert.Equal(2, result[0]);
    }

    [Fact]
    public async Task ArrayTrimAsync()
    {
        var commands = new JsonCommandsAsync(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];
        await commands.SetAsync(key, "$", new { name = "Alice", nicknames = new[] { "Al", "Ali", "Ally" } });
        await commands.SetAsync(simpleKey, "$", new[] { "Al", "Ali", "Ally" });

        var result = await commands.ArrTrimAsync(key, "$.nicknames", 0, 0);
        Assert.Equal(1, result[0]);
        result = await commands.ArrTrimAsync(simpleKey, "$", 0, 1);
        Assert.Equal(2, result[0]);
    }

    [Fact]
    public void Clear()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];
        commands.Set(key, "$", new { name = "Alice", nicknames = new[] { "Al", "Ali", "Ally" } });
        commands.Set(simpleKey, "$", new[] { "Al", "Ali", "Ally" });

        var result = commands.Clear(key, "$.nicknames");
        Assert.Equal(1, result);
        result = commands.Clear(simpleKey);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ClearAsync()
    {
        var commands = new JsonCommandsAsync(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];
        await commands.SetAsync(key, "$", new { name = "Alice", nicknames = new[] { "Al", "Ali", "Ally" } });
        await commands.SetAsync(simpleKey, "$", new[] { "Al", "Ali", "Ally" });

        var result = await commands.ClearAsync(key, "$.nicknames");
        Assert.Equal(1, result);
        result = await commands.ClearAsync(simpleKey);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Del()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];
        commands.Set(key, "$", new { name = "Alice", nicknames = new[] { "Al", "Ali", "Ally" } });
        commands.Set(simpleKey, "$", new[] { "Al", "Ali", "Ally" });

        var result = commands.Del(key, "$.nicknames");
        Assert.Equal(1, result);
        result = commands.Del(simpleKey);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task DelAsync()
    {
        var commands = new JsonCommandsAsync(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];
        await commands.SetAsync(key, "$", new { name = "Alice", nicknames = new[] { "Al", "Ali", "Ally" } });
        await commands.SetAsync(simpleKey, "$", new[] { "Al", "Ali", "Ally" });

        var result = await commands.DelAsync(key, "$.nicknames");
        Assert.Equal(1, result);
        result = await commands.DelAsync(simpleKey);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Forget()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];
        commands.Set(key, "$", new { name = "Alice", nicknames = new[] { "Al", "Ali", "Ally" } });
        commands.Set(simpleKey, "$", new[] { "Al", "Ali", "Ally" });

        var result = commands.Forget(key, "$.nicknames");
        Assert.Equal(1, result);
        result = commands.Forget(simpleKey);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ForgetAsync()
    {
        var commands = new JsonCommandsAsync(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key = keys[0];
        var simpleKey = keys[1];
        await commands.SetAsync(key, "$", new { name = "Alice", nicknames = new[] { "Al", "Ali", "Ally" } });
        await commands.SetAsync(simpleKey, "$", new[] { "Al", "Ali", "Ally" });

        var result = await commands.ForgetAsync(key, "$.nicknames");
        Assert.Equal(1, result);
        result = await commands.ForgetAsync(simpleKey);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Get()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(3);
        var key = keys[0];
        var complexKey = keys[1];
        var caseInsensitiveKey = keys[2];
        commands.Set(key, "$", new Person() { Age = 35, Name = "Alice" });
        commands.Set(complexKey, "$", new { a = new Person() { Age = 35, Name = "Alice" }, b = new { a = new Person() { Age = 35, Name = "Alice" } } });
        commands.Set(caseInsensitiveKey, "$", new { name = "Alice", AGE = 35 });
        var result = commands.Get<Person>(key);
        Assert.Equal("Alice", result!.Name);
        Assert.Equal(35, result.Age);
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        result = commands.Get<Person>(caseInsensitiveKey, "$", jsonOptions);
        Assert.NotNull(result);
        Assert.Equal("Alice", result!.Name);
        Assert.Equal(35, result.Age);
        var people = commands.GetEnumerable<Person>(complexKey, "$..a").ToArray();
        Assert.Equal(2, people.Length);
        Assert.Equal("Alice", people[0]!.Name);
        Assert.Equal(35, people[0]!.Age);
        Assert.Equal("Alice", people[1]!.Name);
        Assert.Equal(35, people[1]!.Age);
    }

    [Fact]
    public async Task GetAsync()
    {
        var commands = new JsonCommandsAsync(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(3);
        var key = keys[0];
        var complexKey = keys[1];
        var caseInsensitiveKey = keys[2];
        await commands.SetAsync(key, "$", new Person() { Age = 35, Name = "Alice" });
        await commands.SetAsync(complexKey, "$", new { a = new Person() { Age = 35, Name = "Alice" }, b = new { a = new Person() { Age = 35, Name = "Alice" } } });
        await commands.SetAsync(caseInsensitiveKey, "$", new { name = "Alice", AGE = 35 });
        var result = await commands.GetAsync<Person>(key);
        Assert.Equal("Alice", result!.Name);
        Assert.Equal(35, result.Age);
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        result = await commands.GetAsync<Person>(caseInsensitiveKey, "$", jsonOptions);
        Assert.NotNull(result);
        Assert.Equal("Alice", result!.Name);
        Assert.Equal(35, result.Age);
        var people = (await commands.GetEnumerableAsync<Person>(complexKey, "$..a")).ToArray();
        Assert.Equal(2, people.Length);
        Assert.Equal("Alice", people[0]!.Name);
        Assert.Equal(35, people[0]!.Age);
        Assert.Equal("Alice", people[1]!.Name);
        Assert.Equal(35, people[1]!.Age);
    }

    [SkipIfRedis(Is.OSSCluster, Comparison.LessThan, "7.1.242")]
    public void MSet()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key1 = keys[0];
        var key2 = keys[1];

        KeyPathValue[] values = new[]
        {
            new KeyPathValue(key1, "$", new { a = "hello" }),
            new KeyPathValue(key2, "$", new { a = "world" })
        };
        commands.MSet(values);

        var result = commands.MGet(keys.Select(x => new RedisKey(x)).ToArray(), "$.a");

        Assert.Equal("[\"hello\"]", result[0].ToString());
        Assert.Equal("[\"world\"]", result[1].ToString());

        // test errors:
        Assert.Throws<ArgumentOutOfRangeException>(() => commands.MSet(new KeyPathValue[0]));
    }

    [SkipIfRedis(Is.OSSCluster, Comparison.LessThan, "7.1.242")]
    public async Task MSetAsync()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key1 = keys[0];
        var key2 = keys[1];
        KeyPathValue[] values = new[]
        {
            new KeyPathValue(key1, "$", new { a = "hello" }),
            new KeyPathValue(key2, "$", new { a = "world" })
        };
        await commands.MSetAsync(values);

        var result = await commands.MGetAsync(keys.Select(x => new RedisKey(x)).ToArray(), "$.a");

        Assert.Equal("[\"hello\"]", result[0].ToString());
        Assert.Equal("[\"world\"]", result[1].ToString());

        // test errors:
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await commands.MSetAsync(new KeyPathValue[0]));
    }

    [SkipIfRedis("7.1.242")]
    public void Merge()
    {
        // Create a connection to Redis
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());

        Assert.True(commands.Set("test_merge", "$", new { person = new { name = "John Doe", age = 25, address = new { home = "123 Main Street" }, phone = "123-456-7890" } }));
        Assert.True(commands.Merge("test_merge", "$", new { person = new { age = 30 } }));
        Assert.Equal("{\"person\":{\"name\":\"John Doe\",\"age\":30,\"address\":{\"home\":\"123 Main Street\"},\"phone\":\"123-456-7890\"}}", commands.Get("test_merge").ToString());

        // Test with root path path $.a.b
        Assert.True(commands.Merge("test_merge", "$.person.address", new { work = "Redis office" }));
        Assert.Equal("{\"person\":{\"name\":\"John Doe\",\"age\":30,\"address\":{\"home\":\"123 Main Street\",\"work\":\"Redis office\"},\"phone\":\"123-456-7890\"}}", commands.Get("test_merge").ToString());

        // Test with null value to delete a value
        Assert.True(commands.Merge("test_merge", "$.person", "{\"age\":null}"));
        Assert.Equal("{\"person\":{\"name\":\"John Doe\",\"phone\":\"123-456-7890\",\"address\":{\"home\":\"123 Main Street\",\"work\":\"Redis office\"}}}", commands.Get("test_merge").ToString());
    }

    [SkipIfRedis("7.1.242")]
    public async Task MergeAsync()
    {
        // Create a connection to Redis
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());

        Assert.True(await commands.SetAsync("test_merge", "$", new { person = new { name = "John Doe", age = 25, address = new { home = "123 Main Street" }, phone = "123-456-7890" } }));
        Assert.True(await commands.MergeAsync("test_merge", "$", new { person = new { age = 30 } }));
        Assert.Equal("{\"person\":{\"name\":\"John Doe\",\"age\":30,\"address\":{\"home\":\"123 Main Street\"},\"phone\":\"123-456-7890\"}}", (await commands.GetAsync("test_merge")).ToString());

        // Test with root path path $.a.b
        Assert.True(await commands.MergeAsync("test_merge", "$.person.address", new { work = "Redis office" }));
        Assert.Equal("{\"person\":{\"name\":\"John Doe\",\"age\":30,\"address\":{\"home\":\"123 Main Street\",\"work\":\"Redis office\"},\"phone\":\"123-456-7890\"}}", (await commands.GetAsync("test_merge")).ToString());

        // Test with null value to delete a value
        Assert.True(await commands.MergeAsync("test_merge", "$.person", "{\"age\":null}"));
        Assert.Equal("{\"person\":{\"name\":\"John Doe\",\"phone\":\"123-456-7890\",\"address\":{\"home\":\"123 Main Street\",\"work\":\"Redis office\"}}}", (await commands.GetAsync("test_merge")).ToString());
    }

    [SkipIfRedis(Is.OSSCluster)]
    public void MGet()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key1 = keys[0];
        var key2 = keys[1];
        commands.Set(key1, "$", new { a = "hello" });
        commands.Set(key2, "$", new { a = "world" });
        var result = commands.MGet(keys.Select(x => new RedisKey(x)).ToArray(), "$.a");

        Assert.Equal("[\"hello\"]", result[0].ToString());
        Assert.Equal("[\"world\"]", result[1].ToString());
    }

    [SkipIfRedis(Is.OSSCluster)]
    public async Task MGetAsync()
    {
        var commands = new JsonCommandsAsync(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(2);
        var key1 = keys[0];
        var key2 = keys[1];
        await commands.SetAsync(key1, "$", new { a = "hello" });
        await commands.SetAsync(key2, "$", new { a = "world" });
        var result = await commands.MGetAsync(keys.Select(x => new RedisKey(x)).ToArray(), "$.a");

        Assert.Equal("[\"hello\"]", result[0].ToString());
        Assert.Equal("[\"world\"]", result[1].ToString());
    }

    [Fact]
    public void NumIncrby()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(1);
        var key = keys[0];
        commands.Set(key, "$", new { age = 33, a = new { age = 34 }, b = new { age = "cat" } });
        var result = commands.NumIncrby(key, "$..age", 2);
        Assert.Equal(35, result[0]);
        Assert.Equal(36, result[1]);
        Assert.Null(result[2]);
    }

    [Fact]
    public async Task NumIncrbyAsync()
    {
        var commands = new JsonCommandsAsync(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(1);
        var key = keys[0];
        await commands.SetAsync(key, "$", new { age = 33, a = new { age = 34 }, b = new { age = "cat" } });
        var result = await commands.NumIncrbyAsync(key, "$..age", 2);
        Assert.Equal(35, result[0]);
        Assert.Equal(36, result[1]);
        Assert.Null(result[2]);
    }

    [Fact]
    public void ObjectKeys()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(3);
        var key = keys[0];
        commands.Set(key, "$", new { a = 5, b = 10, c = "hello", d = new { a = new { a = 6, b = "hello" }, b = 7 } });
        var result = commands.ObjKeys(key).ToArray();
        Assert.Contains("a", result[0]);
        Assert.Contains("b", result[0]);
        Assert.Contains("c", result[0]);
        Assert.Contains("d", result[0]);
        result = commands.ObjKeys(key, "$..a").ToArray();
        Assert.Empty(result[0]);
        Assert.Contains("a", result[1]);
        Assert.Contains("b", result[1]);
    }

    [Fact]
    public async Task ObjectKeysAsync()
    {
        var commands = new JsonCommandsAsync(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(3);
        var key = keys[0];
        await commands.SetAsync(key, "$", new { a = 5, b = 10, c = "hello", d = new { a = new { a = 6, b = "hello" }, b = 7 } });
        var result = (await commands.ObjKeysAsync(key)).ToArray();
        Assert.Contains("a", result[0]);
        Assert.Contains("b", result[0]);
        Assert.Contains("c", result[0]);
        Assert.Contains("d", result[0]);
        result = (await commands.ObjKeysAsync(key, "$..a")).ToArray();
        Assert.Empty(result[0]);
        Assert.Contains("a", result[1]);
        Assert.Contains("b", result[1]);
    }

    [Fact]
    public void ObjectLength()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(3);
        var key = keys[0];
        commands.Set(key, "$", new { a = 5, b = 10, c = "hello", d = new { a = new { a = 6, b = "hello" }, b = 7 } });
        var result = commands.ObjLen(key);
        Assert.Equal(4, result[0]);
        result = commands.ObjLen(key, $"$..a");
        Assert.Null(result[0]);
        Assert.Equal(2, result[1]);
        Assert.Null(result[2]);

    }

    [Fact]
    public async Task ObjectLengthAsync()
    {
        var commands = new JsonCommandsAsync(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(3);
        var key = keys[0];
        await commands.SetAsync(key, "$", new { a = 5, b = 10, c = "hello", d = new { a = new { a = 6, b = "hello" }, b = 7 } });
        var result = await commands.ObjLenAsync(key);
        Assert.Equal(4, result[0]);
        result = await commands.ObjLenAsync(key, $"$..a");
        Assert.Null(result[0]);
        Assert.Equal(2, result[1]);
        Assert.Null(result[2]);

    }

    [Fact]
    public void TestMultiPathGet()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(1);
        var key = keys[0];
        commands.Set(key, "$", new { a = "hello", b = new { a = "world" } });
        var res = commands.Get(key, new[] { "$..a", "$.b" }).ToString();
        var obj = JsonSerializer.Deserialize<JsonObject>(res!);
        Assert.True(obj!.ContainsKey("$..a"));
        Assert.True(obj.ContainsKey("$.b"));
        if (obj["$..a"] is JsonArray arr)
        {
            Assert.Equal("hello", arr[0]!.ToString());
            Assert.Equal("world", arr[1]!.ToString());
        }
        else
        {
            Assert.True(false, "$..a was not a json array");
        }

        Assert.True(obj["$.b"]![0]!["a"]!.ToString() == "world");
    }

    [Fact]
    public async Task TestMultiPathGetAsync()
    {
        var commands = new JsonCommandsAsync(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(1);
        var key = keys[0];
        await commands.SetAsync(key, "$", new { a = "hello", b = new { a = "world" } });
        var res = (await commands.GetAsync(key, new[] { "$..a", "$.b" })).ToString();
        var obj = JsonSerializer.Deserialize<JsonObject>(res!);
        Assert.True(obj!.ContainsKey("$..a"));
        Assert.True(obj.ContainsKey("$.b"));
        if (obj["$..a"] is JsonArray arr)
        {
            Assert.Equal("hello", arr[0]!.ToString());
            Assert.Equal("world", arr[1]!.ToString());
        }
        else
        {
            Assert.True(false, "$..a was not a json array");
        }

        Assert.True(obj["$.b"]![0]!["a"]!.ToString() == "world");
    }

    [Fact]
    public void Memory()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(1);
        var key = keys[0];

        commands.Set(key, "$", new { a = "hello", b = new { a = "world" } });
        var res = commands.DebugMemory(key);
        Assert.True(res > 20);
        res = commands.DebugMemory("non-existent key");
        Assert.Equal(0, res);
    }

    [Fact]
    public async Task MemoryAsync()
    {
        var commands = new JsonCommandsAsync(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(1);
        var key = keys[0];

        await commands.SetAsync(key, "$", new { a = "hello", b = new { a = "world" } });
        var res = await commands.DebugMemoryAsync(key);
        Assert.True(res > 20);
        res = await commands.DebugMemoryAsync("non-existent key");
        Assert.Equal(0, res);
    }

    [Fact]
    public async Task TestSetFromFileAsync()
    {
        //arrange
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommandsAsync(db);
        var keys = CreateKeyNames(1);

        //creating json string:
        var obj = new Person { Name = "Shachar", Age = 23 };
        string json = JsonSerializer.Serialize(obj);

        var file = "testFile.json";

        //writing json to file:
        File.WriteAllText(file, json);

        Assert.True(await commands.SetFromFileAsync(keys[0], "$", file));
        var actual = await commands.GetAsync(keys[0]);

        Assert.Equal(json, actual.ToString());
        File.Delete(file);

        //test not existing file:
        await Assert.ThrowsAsync<FileNotFoundException>(async () => await commands.SetFromFileAsync(keys[0], "$", "notExistingFile.json"));
    }

    [Fact]
    public async Task TestSetFromDirectoryAsync()
    {
        //arrange
        var conn = redisFixture.Redis;
        var db = conn.GetDatabase();
        var commands = new JsonCommandsAsync(db);

        //creating json string:
        object[] persons = new object[10];
        string[] jsons = new string[10];
        string[] notJsons = new string[10];
        for (int i = 1; i <= 10; i++)
        {
            persons[i - 1] = new Person { Name = $"Person{i}", Age = i * 10 };
            jsons[i - 1] = JsonSerializer.Serialize(persons[i - 1]);
            notJsons[i - 1] = $"notjson{i}";
        }

        //creating directorys:
        Directory.CreateDirectory(Path.Combine("BaseDir", "DirNumber2", "DirNumber3"));

        //creating files in directorys:
        for (int i = 1; i <= 10; i++)
        {
            string jsonPath;
            string notJsonPath;
            if (i <= 3)
            {
                jsonPath = Path.Combine("BaseDir", $"jsonFile{i}.json");
                notJsonPath = Path.Combine("BaseDir", $"notJsonFile{i}.txt");
            }
            else if (i <= 6)
            {
                jsonPath = Path.Combine("BaseDir", "DirNumber2", $"jsonFile{i}.json");
                notJsonPath = Path.Combine("BaseDir", "DirNumber2", $"notJsonFile{i}.txt");
            }
            else
            {
                jsonPath = Path.Combine("BaseDir", "DirNumber2", "DirNumber3", $"jsonFile{i}.json");
                notJsonPath = Path.Combine("BaseDir", "DirNumber2", "DirNumber3", $"notJsonFile{i}.txt");
            }
            File.WriteAllText(Path.GetFullPath(jsonPath), jsons[i - 1]);
            File.WriteAllText(Path.GetFullPath(notJsonPath), notJsons[i - 1]);
        }

        Assert.Equal(10, await commands.SetFromDirectoryAsync("$", "BaseDir"));

        var actual = await commands.GetAsync(Path.Combine("BaseDir", "DirNumber2", "DirNumber3", $"jsonFile7"));
        Assert.Equal(jsons[6], actual.ToString());
        Directory.Delete("BaseDir", true);
    }

    [Fact]
    public void TestJsonCommandBuilder()
    {
        var getBuild1 = JsonCommandBuilder.Get("key", "indent", "newline", "space", "path");
        var getBuild2 = JsonCommandBuilder.Get("key", new string[] { "path1", "path2", "path3" }, "indent", "newline", "space");
        var expectedArgs1 = new object[] { "key", "INDENT", "indent", "NEWLINE", "newline", "SPACE", "space", "path" };
        var expectedArgs2 = new object[] { "key", "INDENT", "indent", "NEWLINE", "newline", "SPACE", "space", "path1", "path2", "path3" };


        for (int i = 0; i < expectedArgs1.Length; i++)
        {
            Assert.Equal(expectedArgs1[i].ToString(), getBuild1.Args[i].ToString());
        }
        Assert.Equal("JSON.GET", getBuild1.Command);

        for (int i = 0; i < expectedArgs2.Length; i++)
        {
            Assert.Equal(expectedArgs2[i].ToString(), getBuild2.Args[i].ToString());
        }
        Assert.Equal("JSON.GET", getBuild2.Command);
    }

    [Fact]
    public void TestGetIssue198()
    {
        var commands = new JsonCommands(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(1);
        var key = keys[0];
        commands.Set(key, "$", new Person() { Age = 35, Name = "Alice" });

        // Path not found:
        var result = commands.Get<Person>(key, "$.a"); // returns "[]" because the path is not found (but the key is)
        // Key not found:
        var result2 = commands.Get<Person>("notExistKey", "$.a"); // returns (nil) because the key is not found

        Assert.Null(result);
        Assert.Null(result2);
    }

    [Fact]
    public async Task TestGetIssue198_Async()
    {
        var commands = new JsonCommandsAsync(redisFixture.Redis.GetDatabase());
        var keys = CreateKeyNames(1);
        var key = keys[0];
        await commands.SetAsync(key, "$", new Person() { Age = 35, Name = "Alice" });

        // Path not found:
        var result = await commands.GetAsync<Person>(key, "$.a"); // returns "[]" because the path is not found (but the key is)
        // Key not found:
        var result2 = await commands.GetAsync<Person>("notExistKey", "$.a"); // returns (nil) because the key is not found

        Assert.Null(result);
        Assert.Null(result2);
    }
}