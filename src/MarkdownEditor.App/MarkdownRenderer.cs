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
                :root {
                  color-scheme: light;
                  font-family: "Segoe UI Variable Text", "Segoe UI", Arial, sans-serif;
                  color: #1f2328;
                  background: white;
                }
                *, *::before, *::after { box-sizing: border-box; }
                body {
                  width: min(100%, 900px);
                  margin: 0 auto;
                  padding: 26px 34px 88px;
                  font-size: 13px;
                  line-height: 1.54;
                }
                h1, h2, h3, h4 {
                  color: #1f2328;
                  font-weight: 700;
                  line-height: 1.3;
                  letter-spacing: -0.018em;
                  page-break-after: avoid;
                }
                h1 { margin: 0 0 12px; padding: 0; border: 0; font-size: 20px; }
                h2 { margin: 23px 0 7px; font-size: 15px; }
                h3 { margin: 18px 0 6px; font-size: 14px; }
                h4 { margin: 15px 0 5px; font-size: 13px; }
                p { margin: 0 0 9px; }
                strong { font-weight: 650; }
                ul, ol { margin: 5px 0 10px; padding-left: 22px; }
                li { margin: 3px 0; padding-left: 1px; }
                li > p { margin-bottom: 3px; }
                p, li, blockquote { orphans: 3; widows: 3; }
                blockquote {
                  margin: 10px 0 12px;
                  padding: 2px 0 2px 12px;
                  border-left: 2px solid #d0d7de;
                  color: #656d76;
                }
                blockquote > :last-child { margin-bottom: 0; }
                code {
                  padding: 2px 5px;
                  border: 1px solid #d0d7de;
                  border-radius: 4px;
                  color: #1f2328;
                  background: #f6f8fa;
                  font-family: "Cascadia Mono", Consolas, monospace;
                  font-size: 0.9em;
                }
                pre {
                  margin: 10px 0 12px;
                  padding: 12px 14px;
                  overflow-wrap: anywhere;
                  white-space: pre-wrap;
                  border: 1px solid #d0d7de;
                  border-radius: 7px;
                  color: #1f2328;
                  background: #f6f8fa;
                  page-break-inside: avoid;
                }
                pre code { padding: 0; border: 0; background: transparent; font-size: 12px; line-height: 1.5; }
                table {
                  width: 100%;
                  margin: 10px 0 14px;
                  overflow: hidden;
                  border: 1px solid #d0d7de;
                  border-collapse: separate;
                  border-spacing: 0;
                  border-radius: 7px;
                  font-size: 12.5px;
                  page-break-inside: avoid;
                }
                th, td { padding: 6px 8px; border: 1px solid #d0d7de; text-align: left; }
                th { background: #f6f8fa; }
                img { max-width: 100%; height: auto; border-radius: 7px; }
                hr { margin: 18px 0; border: 0; border-top: 1px solid #d0d7de; }
                a { color: #0053fd; text-decoration-thickness: 1px; text-underline-offset: 3px; }
                @page { margin: 0.42in 0.4in 0.42in 0.5in; }
                @media print {
                  body {
                    width: auto;
                    max-width: none;
                    padding: 0;
                    font-size: 12.5px;
                  }
                  h1 { font-size: 19.25px; }
                  h2 { font-size: 14.5px; }
                  h3 { font-size: 13.5px; }
                  h4 { font-size: 12.5px; }
                  pre code { font-size: 11.5px; }
                  table { font-size: 12px; }
                }
              </style>
            </head>
            <body>
            {{body}}
            </body>
            </html>
            """;
    }
}
