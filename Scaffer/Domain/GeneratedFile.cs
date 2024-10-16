﻿using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scaffer.Domain;

public class GeneratedFile : GeneratedObject
{
    private readonly List<string> _usingNamespaces = new();

    private readonly IDatabaseReader _databaseReader;
    private GeneratedClass _class = new();

    public GeneratedFile(IDatabaseReader databaseReader)
    {
        _databaseReader = databaseReader;
    }

    public string FileNamespace { get; init; } = string.Empty;

    public static string GetAutoGeneratedComment()
    {
        var commentText = new StringBuilder();

        commentText.AppendLine("// <auto-generated>");
        commentText.AppendLine("// This code was generated by a tool.");
        commentText.AppendLine("// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.");
        commentText.AppendLine("// </auto-generated>");
        commentText.AppendLine();
        commentText.AppendLine("#nullable enable");
        commentText.AppendLine();

        return commentText.ToString();
    }

    public void AddClass(GeneratedClass generatedClass)
    {
        generatedClass.BaseIndentation = BaseIndentation;
        _class = generatedClass;
    }

    public override string ToString()
    {
        _output = new();

        _output.Append(GetAutoGeneratedComment());

        if (_usingNamespaces.Count > 0)
        {
            foreach (var usingNamespace in _usingNamespaces)
            {
                AppendLine($"using {usingNamespace};");
            }

            AppendLine(string.Empty);
        }

        AppendLine($"namespace {FileNamespace};");
        AppendLine();

        _output.Append(_class);

        return _output.ToString();
    }

    public string GetListMethod()
    {
        var methodText = new StringBuilder();
        var selectAllQuery = _databaseReader.GetSelectAllQuery(_class.TableName);
        var selectWhereQuery = _databaseReader.GetSelectWhereQuery(_class.TableName);

        methodText.AppendLine(
            $"    public static async Task<List<{_class.Name}>> Get{_class.Name}ListAsync(this IDbConnection connection, string? whereClause = null, object? param = null)");

        methodText.AppendLine("    {");
        methodText.AppendLine("        if (whereClause == null)");
        methodText.AppendLine("        {");
        methodText.AppendLine($"            var selectQuery = \"{selectAllQuery}\";");
        methodText.AppendLine($"            var response = (await connection.QueryAsync<{_class.Name}>(selectQuery)).ToList();");
        methodText.AppendLine();
        methodText.AppendLine("            return response;");
        methodText.AppendLine("        }");
        methodText.AppendLine("        else");
        methodText.AppendLine("        {");
        methodText.AppendLine($"            var selectQuery = $\"{selectWhereQuery}\";");
        methodText.AppendLine($"            var response = (await connection.QueryAsync<{_class.Name}>(selectQuery, param)).ToList();");
        methodText.AppendLine();
        methodText.AppendLine("            return response;");
        methodText.AppendLine("        }");
        methodText.AppendLine("    }");

        return methodText.ToString();
    }

    public string GetInsertMethod()
    {
        var writeColumns = _class.Properties.Where(p => p.Name != _class.IdentityColumn && !p.Readonly).ToList();
        var readColumns = _class.Properties.Where(p => p.Name == _class.IdentityColumn || p.Readonly).ToList();
        var defaultColumns = writeColumns.Where(x => x.HasDefault).ToList();

        if (!defaultColumns.Any())
        {
            return GetBasicInsertMethod(writeColumns, readColumns);
        }

        return GetAdvancedInsertMethod(writeColumns, readColumns, defaultColumns);
    }

    public string GetGetByIdMethod()
    {
        var identityColumn = _class.Properties.FirstOrDefault(p => p.Name == _class.IdentityColumn);

        if (identityColumn == null)
        {
            return string.Empty;
        }

        var selectByIdQuery = _databaseReader.GetSelectByIdQuery(_class.TableName, identityColumn.Name);

        var methodText = new StringBuilder();

        methodText.AppendLine($"    public static async Task<{_class.Name}?> Get{_class.Name}ByIdAsync(this IDbConnection connection, {identityColumn.Type} id)");

        methodText.AppendLine("    {");
        methodText.AppendLine($"        var selectQuery = \"{selectByIdQuery}\";");
        methodText.AppendLine($"        var response = await connection.QueryFirstOrDefaultAsync<{_class.Name}>(selectQuery, new {{ id }});");
        methodText.AppendLine();
        methodText.AppendLine("        return response;");
        methodText.AppendLine("    }");

        return methodText.ToString();
    }

    public string GetSaveMethod()
    {
        var identityColumn = _class.Properties.FirstOrDefault(p => p.Name == _class.IdentityColumn);

        if (identityColumn == null)
        {
            return string.Empty;
        }

        var methodText = new StringBuilder();

        methodText.AppendLine($"    public static async Task SaveAsync(this IDbConnection connection, {_class.Name} obj)");
        methodText.AppendLine("    {");
        methodText.AppendLine($"        if (obj.{identityColumn.Name} == default)");
        methodText.AppendLine("        {");
        methodText.AppendLine("            await connection.InsertAsync(obj);");
        methodText.AppendLine("        }");
        methodText.AppendLine("        else");
        methodText.AppendLine("        {");
        methodText.AppendLine("            await connection.UpdateAsync(obj);");
        methodText.AppendLine("        }");
        methodText.AppendLine("    }");

        return methodText.ToString();
    }

    public string GetUpdateMethod()
    {
        var identityColumn = _class.Properties.FirstOrDefault(p => p.Name == _class.IdentityColumn);

        if (identityColumn == null)
        {
            return string.Empty;
        }

        var writeColumns = _class.Properties.Where(p => p.Name != _class.IdentityColumn && !p.Readonly).ToList();
        var methodText = new StringBuilder();
        var updateQuery = _databaseReader.GetUpdateQuery(_class.TableName, identityColumn.Name, writeColumns.Select(column => column.Name));

        methodText.AppendLine($"    public static async Task UpdateAsync(this IDbConnection connection, {_class.Name} obj)");
        methodText.AppendLine("    {");

        methodText.AppendLine($"        var updateQuery = \"{updateQuery}\";");

        methodText.AppendLine("        var rowsAffected = await connection.ExecuteAsync(updateQuery, obj);");
        methodText.AppendLine();
        methodText.AppendLine("        if (rowsAffected == 0)");
        methodText.AppendLine("        {");
        methodText.AppendLine("            await connection.InsertAsync(obj);");
        methodText.AppendLine("        }");
        methodText.AppendLine("    }");

        return methodText.ToString();
    }

    public string GetDeleteMethod()
    {
        var identityColumn = _class.Properties.FirstOrDefault(property => property.Name == _class.IdentityColumn);

        if (identityColumn == null)
        {
            return string.Empty;
        }

        var deleteQuery = _databaseReader.GetDeleteQuery(_class.TableName, identityColumn.Name);
        var methodText = new StringBuilder();

        methodText.AppendLine($"    public static async Task DeleteAsync(this IDbConnection connection, {_class.Name} obj)");
        methodText.AppendLine("    {");
        methodText.AppendLine($"        var deleteQuery = \"{deleteQuery}\";");
        methodText.AppendLine();
        methodText.AppendLine("        await connection.ExecuteAsync(deleteQuery, obj);");
        methodText.AppendLine($"        obj.{identityColumn.Name} = default;");
        methodText.AppendLine("    }");

        return methodText.ToString();
    }

    public void AddNamespaceForType(string typeName)
    {
        var usingNamespace = GetUsingNamespaceForType(typeName);

        if (usingNamespace is null)
        {
            return;
        }

        if (!_usingNamespaces.Contains(usingNamespace))
        {
            _usingNamespaces.Add(usingNamespace);
        }
    }

    private string GetAdvancedInsertMethod(List<GeneratedProperty> writeColumns, List<GeneratedProperty> readColumns, List<GeneratedProperty> defaultColumns)
    {
        var nonDefaultColumns = writeColumns.Where(x => !x.HasDefault).ToList();
        var methodText = new StringBuilder();

        methodText.AppendLine($"    public static async Task InsertAsync(this IDbConnection connection, {_class.Name} obj)");
        methodText.AppendLine("    {");
        methodText.AppendLine($"        var insertColumns = new List<string> {{ {string.Join(", ", nonDefaultColumns.Select(x => $"\"{x.Name}\""))} }};");

        if (readColumns.Any())
        {
            methodText.AppendLine($"        var outputColumns = new List<string> {{ {string.Join(", ", readColumns.Select(x => $"\"{x.Name}\""))} }};");
        }
        else
        {
            methodText.AppendLine("        var outputColumns = new List<string>();");
        }

        methodText.AppendLine();

        foreach (var column in defaultColumns)
        {
            methodText.AppendLine($"        if (obj.{column.Name} != default) {{ insertColumns.Add(\"{column.Name}\"); }} else {{ outputColumns.Add(\"{column.Name}\"); }}");
        }

        if (defaultColumns.Any())
        {
            methodText.AppendLine();
        }

        var outputText = _databaseReader.GetAdvancedInsertQueryOutputText();

        methodText.AppendLine($"        var outputText = outputColumns.Any() ? $\"{outputText}\" : string.Empty;");

        var insertQuery = _databaseReader.GetAdvancedInsertQuery(_class.TableName);

        methodText.AppendLine($"        var insertQuery = $\"{insertQuery}\";");

        methodText.AppendLine($"        var insertedObj = await connection.QueryFirstOrDefaultAsync<{_class.Name}>(insertQuery, obj);");
        methodText.AppendLine();

        foreach (var readColumn in readColumns)
        {
            methodText.AppendLine($"        obj.{readColumn.Name} = insertedObj?.{readColumn.Name} ?? {(readColumn.Type == "string" ? "string.Empty" : "default")};");
        }

        foreach (var column in defaultColumns)
        {
            methodText.AppendLine(
                $"        if (obj.{column.Name} == default) {{ obj.{column.Name} = insertedObj?.{column.Name} ?? {(column.Type == "string" ? "string.Empty" : "default")}; }}");
        }

        methodText.AppendLine("    }");

        return methodText.ToString();
    }

    private string GetBasicInsertMethod(List<GeneratedProperty> writeColumns, List<GeneratedProperty> readColumns)
    {
        var methodText = new StringBuilder();
        var insertQuery = _databaseReader.GetBasicInsertQuery(_class.TableName, writeColumns.Select(x => x.Name), readColumns.Select(x => x.Name));

        methodText.AppendLine($"    public static async Task InsertAsync(this IDbConnection connection, {_class.Name} obj)");
        methodText.AppendLine("    {");

        methodText.AppendLine($"        var insertQuery = \"{insertQuery}\";");

        if (readColumns.Any())
        {
            methodText.AppendLine($"        var insertedObj = await connection.QueryFirstOrDefaultAsync<{_class.Name}>(insertQuery, obj);");
            methodText.AppendLine();

            foreach (var readColumn in readColumns)
            {
                methodText.AppendLine($"        obj.{readColumn.Name} = insertedObj?.{readColumn.Name} ?? {(readColumn.Type == "string" ? "string.Empty" : "default")};");
            }
        }
        else
        {
            methodText.AppendLine("        await connection.ExecuteAsync(insertQuery, obj);");
        }

        methodText.AppendLine("    }");

        return methodText.ToString();
    }

    private string? GetUsingNamespaceForType(string typeName)
    {
        var baseTypeName = typeName.Replace("?", string.Empty);

        switch (baseTypeName)
        {
            case "DateTime":
            case "DateTimeOffset":
            case "TimeSpan":
            case "Guid":
                return "System";
            case "XDocument":
                return "System.Xml.Linq";
        }

        return null;
    }
}
