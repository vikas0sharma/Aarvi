using A2A.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services
{
    public static class Extensions
    {
        public static string? ToText(this Message message)
        {
            ArgumentNullException.ThrowIfNull(message);
            if (message.Parts == null) return null;
            var textBuilder = new StringBuilder();
            foreach (var part in message.Parts) textBuilder.Append(part.ToText());
            return textBuilder.ToString();
        }

        public static string ToText(this Part part)
        {
            ArgumentNullException.ThrowIfNull(part);
            switch (part)
            {
                case TextPart textPart:
                    return textPart.Text;
                case FilePart filePart:
                    var fileContentBuilder = new StringBuilder();
                    fileContentBuilder.AppendLine("----- FILE -----");
                    if (!string.IsNullOrWhiteSpace(filePart.File.Name)) fileContentBuilder.AppendLine($"Name    : {filePart.File.Name}");
                    if (!string.IsNullOrWhiteSpace(filePart.File.MimeType)) fileContentBuilder.AppendLine($"MIME    : {filePart.File.MimeType}");
                    if (!string.IsNullOrWhiteSpace(filePart.File.Bytes)) fileContentBuilder.AppendLine($"Base64  : {filePart.File.Bytes}");
                    else if (filePart.File.Uri is not null) fileContentBuilder.AppendLine($"URI     : {filePart.File.Uri}");
                    fileContentBuilder.AppendLine("----------------");
                    return fileContentBuilder.ToString();
                case DataPart dataPart:
                    var jsonContentBuilder = new StringBuilder();
                    jsonContentBuilder.AppendLine("```json");
                    jsonContentBuilder.AppendLine(JsonSerializer.Serialize(dataPart.Data));
                    jsonContentBuilder.AppendLine("```");
                    return jsonContentBuilder.ToString();
                default:
                    throw new NotSupportedException($"The specified part type '{part.Kind ?? "None"}' is not supported");
            }
        }

        public static async IAsyncEnumerable<(T current, T? next)> PeekingAsync<T>(this IAsyncEnumerable<T> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await using var enumerator = source.GetAsyncEnumerator(cancellationToken);
            if (!await enumerator.MoveNextAsync()) yield break;
            var current = enumerator.Current;
            while (await enumerator.MoveNextAsync())
            {
                yield return (current, enumerator.Current);
                current = enumerator.Current;
            }
            yield return (current, default);
        }
    }
}
