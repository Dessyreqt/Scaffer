﻿using System.Text;
using Dapper.Scaffold.Domain.Enums;

namespace Dapper.Scaffold.Domain;

public abstract class GeneratedObject
{
    protected StringBuilder _output;

    public int BaseIndentation { get; set; }

    protected void AppendLine(string text = "")
    {
        if (_output == null)
        {
            _output = new();
        }

        if (text == "")
        {
            _output.AppendLine();
        }
        else
        {
            _output.AppendLine($"{string.Empty.PadLeft(BaseIndentation)}{text}");
        }
    }

    protected string AccessText(Access modifier)
    {
        return modifier.ToString().ToLower();
    }
}
