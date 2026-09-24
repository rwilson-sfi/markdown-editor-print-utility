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
                .mermaid-diagram {
                  min-width: 520px;
                  margin: 18px auto 24px;
                  overflow-x: auto;
                  color: #245aa5;
                  background: transparent;
                  text-align: center;
                }
                .mermaid-diagram svg {
                  display: block;
                  width: 100%;
                  max-width: 100% !important;
                  height: auto;
                  margin: 0 auto;
                  font-family: "Segoe UI Variable Text", "Segoe UI", Arial, sans-serif !important;
                }
                .mermaid-diagram .node rect,
                .mermaid-diagram .node polygon,
                .mermaid-diagram .node path { stroke-width: 1px !important; }
                .mermaid-diagram .node rect { rx: 13px; ry: 13px; }
                .mermaid-diagram .nodeLabel,
                .mermaid-diagram .nodeLabel p {
                  color: #245aa5 !important;
                  font-family: "Segoe UI Variable Text", "Segoe UI", Arial, sans-serif !important;
                  font-size: 13px !important;
                  font-weight: 600 !important;
                  line-height: 1.22 !important;
                }
                .mermaid-diagram .edgeLabel,
                .mermaid-diagram .edgeLabel p {
                  color: #656d76 !important;
                  background: white !important;
                  font: 12px/1.25 "Segoe UI Variable Text", "Segoe UI", Arial, sans-serif !important;
                }
                .mermaid-diagram .flowchart-link { stroke-width: 1px !important; }
                .mermaid-diagram marker path {
                  fill: #858b91 !important;
                  stroke: #858b91 !important;
                }
                .mermaid-error {
                  margin: 18px 0 24px;
                  padding: 18px;
                  border: 1px solid #d0d7de;
                  border-radius: 7px;
                  color: #a40e26;
                  background: #f6f8fa;
                  text-align: center;
                }
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
                  .mermaid-diagram {
                    min-width: 0;
                    margin: 12px auto 18px;
                    overflow: visible;
                    break-inside: avoid;
                    page-break-inside: avoid;
                  }
                }
              </style>
            </head>
            <body>
            {{body}}
            <script src="https://markdown-editor.local/mermaid.min.js"></script>
            <script>
              (() => {
                const diagrams = [];
                const notifyReady = () => window.chrome?.webview?.postMessage('preview-ready');

                const renderDiagrams = async () => {
                  const blocks = document.querySelectorAll('pre > code.language-mermaid');
                  if (blocks.length === 0) return;
                  if (!window.mermaid) throw new Error('The Mermaid renderer could not be loaded.');

                  window.mermaid.initialize({
                    startOnLoad: false,
                    securityLevel: 'strict',
                    suppressErrorRendering: true,
                    theme: 'base',
                    look: 'classic',
                    fontFamily: 'Segoe UI Variable Text, Segoe UI, Arial, sans-serif',
                    themeVariables: {
                      background: '#ffffff',
                      fontFamily: 'Segoe UI Variable Text, Segoe UI, Arial, sans-serif',
                      fontSize: '13px',
                      primaryColor: '#e7f2fc',
                      primaryTextColor: '#245aa5',
                      primaryBorderColor: '#cad8e4',
                      secondaryColor: '#e7f2fc',
                      secondaryTextColor: '#245aa5',
                      secondaryBorderColor: '#cad8e4',
                      tertiaryColor: '#f7fafc',
                      tertiaryTextColor: '#245aa5',
                      tertiaryBorderColor: '#cad8e4',
                      lineColor: '#858b91',
                      edgeLabelBackground: '#ffffff',
                      clusterBkg: '#f7fafc',
                      clusterBorder: '#d8e0e7'
                    },
                    flowchart: {
                      curve: 'linear',
                      htmlLabels: true,
                      nodeSpacing: 48,
                      rankSpacing: 46,
                      padding: 16,
                      useMaxWidth: true
                    }
                  });

                  blocks.forEach((code) => {
                    const diagram = document.createElement('div');
                    diagram.className = 'mermaid mermaid-diagram';
                    diagram.textContent = code.textContent ?? '';
                    code.parentElement?.replaceWith(diagram);
                    diagrams.push(diagram);
                  });

                  await window.mermaid.run({ nodes: diagrams });
                };

                renderDiagrams()
                  .catch(() => {
                    diagrams.forEach((diagram) => {
                      diagram.className = 'mermaid-error';
                      diagram.textContent = 'Unable to render this Mermaid diagram.';
                    });
                  })
                  .finally(notifyReady);
              })();
            </script>
            </body>
            </html>
            """;
    }
}
