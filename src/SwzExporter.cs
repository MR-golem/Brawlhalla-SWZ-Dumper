// ===
// File:         SwzExporter.cs
// Purpose:      Serialize/deserialize decrypted SWZ entries to/from a single XML file
// Dependencies: System.Xml
// Notes:        XML entries are embedded as structured nodes.
//               CSV entries (first line = table name) are converted to <TableName type="csv"><Row .../></TableName>.
//               Unrecognized non-XML entries fall back to CDATA.
// ===

using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace BrawlhallaSWZTool;

public static class SwzExporter
{
    public static void Export(IReadOnlyList<string> entries, string outputDir, string swzName)
    {
        Directory.CreateDirectory(outputDir);

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false,
            Encoding = Encoding.UTF8,
        };

        var path = Path.Combine(outputDir, $"{swzName}.xml");

        using var writer = XmlWriter.Create(path, settings);
        writer.WriteStartDocument();
        writer.WriteStartElement("Entries");
        writer.WriteAttributeString("source", swzName);
        writer.WriteAttributeString("count", entries.Count.ToString());

        for (var i = 0; i < entries.Count; i++)
        {
            writer.WriteStartElement("Entry");
            writer.WriteAttributeString("index", i.ToString());
            WriteEntryContent(writer, entries[i], i);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();

        Console.WriteLine($"  [*] {path}  ({entries.Count} entries)");
    }

    public static string[] Import(string xmlPath)
    {
        var doc = new XmlDocument();
        doc.Load(xmlPath);

        var nodes = doc.SelectNodes("/Entries/Entry")!;
        var results = new string[nodes.Count];

        foreach (XmlNode node in nodes)
        {
            var idx = int.Parse(node.Attributes!["index"]!.Value);
            results[idx] = ReadEntryContent(node);
        }

        return results;
    }

    // -------------------------------------------------------------------------

    private static void WriteEntryContent(XmlWriter writer, string entry, int index)
    {
        var trimmed = entry.TrimStart();
        if (!trimmed.StartsWith('<'))
        {
            WriteCsv(writer, trimmed, index);
            return;
        }

        var tmp = new XmlDocument();
        try
        {
            tmp.LoadXml($"<_>{SanitizeXml(SanitizeComments(entry))}</_>");
            foreach (XmlNode child in tmp.DocumentElement!.ChildNodes)
                writer.WriteNode(new XmlNodeReader(child), defattr: false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  [WARN:{index:D4}] XML parse failed ({ex.Message[..Math.Min(120, ex.Message.Length)]}) -> CDATA");
            writer.WriteCData(entry);
        }
    }

    // First line  = table name
    // Second line = comma-separated column headers
    // Rest        = data rows
    // Output: <tableName type="csv"><Row Col1="v1" Col2="v2" /></tableName>
    private static void WriteCsv(XmlWriter writer, string entry, int index)
    {
        var lines = entry.Split('\n', StringSplitOptions.TrimEntries);
        var nonEmpty = lines.Where(l => l.Length > 0).ToArray();

        if (nonEmpty.Length < 2)
        {
            Console.Error.WriteLine($"  [CDATA:{index:D4}] Too few lines for CSV");
            writer.WriteCData(entry);
            return;
        }

        var rawName = nonEmpty[0].Trim();
        var headers = nonEmpty[1].Split(',');
        var elementName = Regex.Replace(rawName, @"[^\w]", "_");
        if (elementName.Length > 0 && char.IsDigit(elementName[0]))
            elementName = "_" + elementName;

        writer.WriteStartElement(elementName);
        writer.WriteAttributeString("type", "csv");

        for (var r = 2; r < nonEmpty.Length; r++)
        {
            var cols = nonEmpty[r].Split(',');
            writer.WriteStartElement("Row");
            for (var c = 0; c < headers.Length; c++)
            {
                var header = Regex.Replace(headers[c].Trim(), @"[^\w]", "_");
                var value = c < cols.Length ? cols[c].Trim() : "";
                if (header.Length > 0)
                    writer.WriteAttributeString(header, value);
            }
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }

    private static string ReadEntryContent(XmlNode entryNode)
    {
        if (entryNode.ChildNodes.Count == 1
            && entryNode.FirstChild is XmlCDataSection cdata)
            return cdata.Value!;

        if (entryNode.ChildNodes.Count == 1
            && entryNode.FirstChild is XmlElement csvRoot
            && csvRoot.GetAttribute("type") == "csv")
            return ReadCsv(csvRoot);

        using var sw = new StringWriter();
        using var writer = XmlWriter.Create(sw, new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = false,
            ConformanceLevel = ConformanceLevel.Fragment,
        });

        foreach (XmlNode child in entryNode.ChildNodes)
            writer.WriteNode(new XmlNodeReader(child), defattr: false);

        writer.Flush();
        return sw.ToString()
                 .Replace("- -", "--")
                 .Replace("&amp;", "&");
    }

    private static string ReadCsv(XmlElement root)
    {
        var rows = root.SelectNodes("Row")!;
        if (rows.Count == 0) return root.LocalName + "\n";

        var headers = rows[0]!.Attributes!.Cast<XmlAttribute>()
                               .Select(a => a.LocalName)
                               .ToArray();

        var lines = new List<string> { root.LocalName, string.Join(",", headers) };

        foreach (XmlNode row in rows)
        {
            var vals = headers.Select(h => row.Attributes?[h]?.Value ?? "");
            lines.Add(string.Join(",", vals));
        }

        return string.Join("\n", lines);
    }

    // -------------------------------------------------------------------------

    private static string SanitizeComments(string xml)
    {
        var sb = new StringBuilder(xml.Length);
        var i = 0;
        while (i < xml.Length)
        {
            var open = xml.IndexOf("<!--", i, StringComparison.Ordinal);
            if (open < 0) { sb.Append(xml, i, xml.Length - i); break; }

            sb.Append(xml, i, open - i);

            var close = xml.IndexOf("-->", open + 4, StringComparison.Ordinal);
            if (close < 0) { sb.Append(xml, open, xml.Length - open); break; }

            var body = xml.Substring(open + 4, close - open - 4).Replace("--", "- -");
            sb.Append("<!--").Append(body).Append("-->");
            i = close + 3;
        }
        return sb.ToString();
    }

    private static readonly Regex s_missingAttrSpace =
        new(@"""(\w+?\s*=)", RegexOptions.Compiled);

    private static readonly Regex s_bareAmpersand =
        new(@"&(?!(?:#\d+|#x[0-9a-fA-F]+|[a-zA-Z]\w*);)", RegexOptions.Compiled);

    private static string SanitizeXml(string xml)
    {
        xml = s_missingAttrSpace.Replace(xml, "\" $1");
        xml = s_bareAmpersand.Replace(xml, "&amp;");
        return xml;
    }
}