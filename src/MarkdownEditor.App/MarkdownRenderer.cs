using System.Net;
using Markdig;

namespace MarkdownEditor.App;

public sealed class MarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public string RenderDocument(string markdown, string title)
    {
        var body = Markdown.ToHtml(markdown, _pipeline);
        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>{{WebUtility.HtmlEncode(title)}}</title>
              <style>
                :root { font-family: "Segoe UI", Arial, sans-serif; color: #172033; background: white; }
                body { max-width: 8.25in; margin: 0 auto; padding: 0.45in 0.55in; font-size: 11pt; line-height: 1.55; }
                h1, h2, h3, h4 { color: #111827; line-height: 1.2; page-break-after: avoid; }
                h1 { font-size: 24pt; border-bottom: 1px solid #d1d5db; padding-bottom: 0.12in; }
                h2 { font-size: 18pt; margin-top: 0.28in; }
                h3 { font-size: 14pt; }
                p, li, blockquote { orphans: 3; widows: 3; }
                blockquote { border-left: 4px solid #9ca3af; margin-left: 0; padding-left: 0.18in; color: #4b5563; }
                code { font-family: "Cascadia Mono", Consolas, monospace; background: #f3f4f6; padding: 0.02in 0.05in; }
                pre { background: #f3f4f6; border: 1px solid #d1d5db; padding: 0.14in; overflow-wrap: anywhere; white-space: pre-wrap; page-break-inside: avoid; }
                table { width: 100%; border-collapse: collapse; margin: 0.16in 0; }
                th, td { border: 1px solid #9ca3af; padding: 0.06in 0.09in; text-align: left; }
                th { background: #f3f4f6; }
                img { max-width: 100%; height: auto; }
                a { color: #1d4ed8; text-decoration: underline; }
                @page { margin: 0.55in; }
                @media print { body { max-width: none; padding: 0; } a { color: black; } }
              </style>
            </head>
            <body>
            {{body}}
            </body>
            </html>
            """;
    }
}
