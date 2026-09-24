import { Crepe } from '@milkdown/crepe'
import '@milkdown/crepe/theme/common/style.css'
import '@milkdown/crepe/theme/frame.css'
import { replaceAll } from '@milkdown/kit/utils'
import mermaid from 'mermaid'
import './style.css'

type EditorMode = 'rich' | 'source' | 'split'

interface EditorTheme {
  id: string
  isDark: boolean
  monospaceInterface: boolean
  background: string
  surface: string
  muted: string
  foreground: string
  mutedForeground: string
  border: string
  accent: string
  accentForeground: string
  selection: string
  sidebar: string
  code: string
  destructive: string
}

interface HostMessage {
  type: 'load' | 'mode' | 'focus' | 'theme'
  documentId?: string
  markdown?: string
  mode?: EditorMode
  theme?: EditorTheme
}

interface WebViewHost {
  postMessage(message: unknown): void
  addEventListener(type: 'message', listener: (event: MessageEvent<HostMessage>) => void): void
}

declare global {
  interface Window {
    chrome?: {
      webview?: WebViewHost
    }
  }
}

const sourceEditor = document.querySelector<HTMLTextAreaElement>('#source-editor')
if (!sourceEditor) throw new Error('Source editor element was not found.')

let suppressChange = false
let sourceUpdateTimer: number | undefined
let currentDocumentId = ''

const postToHost = (message: unknown) => window.chrome?.webview?.postMessage(message)

mermaid.initialize({
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
    clusterBorder: '#d8e0e7',
  },
  flowchart: {
    curve: 'linear',
    htmlLabels: true,
    nodeSpacing: 48,
    rankSpacing: 46,
    padding: 16,
    useMaxWidth: true,
  },
})

let mermaidRenderSequence = 0

const renderDiagramPreview = (
  language: string,
  content: string,
  applyPreview: (value: null | string | HTMLElement) => void,
) => {
  if (language.trim().toLowerCase() !== 'mermaid') return null

  const renderId = `markdown-editor-mermaid-${++mermaidRenderSequence}`
  void mermaid
    .render(renderId, content)
    .then(({ svg }) => {
      const diagram = document.createElement('div')
      diagram.className = 'mermaid-diagram'
      diagram.innerHTML = svg
      applyPreview(diagram)
    })
    .catch((error: unknown) => {
      const message = document.createElement('div')
      message.className = 'mermaid-error'
      message.textContent =
        error instanceof Error ? `Mermaid: ${error.message}` : 'Unable to render this Mermaid diagram.'
      applyPreview(message)
    })

  return undefined
}

const crepe = new Crepe({
  root: '#rich-editor',
  defaultValue: '',
  features: {
    [Crepe.Feature.AI]: false,
    [Crepe.Feature.Latex]: false,
    [Crepe.Feature.TopBar]: true,
  },
  featureConfigs: {
    [Crepe.Feature.CodeMirror]: {
      renderPreview: renderDiagramPreview,
      previewOnlyByDefault: true,
      previewLoading: '<span class="mermaid-loading">Rendering diagram…</span>',
    },
  },
})

crepe.on((listener) => {
  listener.markdownUpdated((_context, markdown, previousMarkdown) => {
    if (suppressChange || markdown === previousMarkdown) return

    sourceEditor.value = markdown
    postToHost({ type: 'changed', documentId: currentDocumentId, markdown })
  })
})

await crepe.create()

const loadMarkdown = (markdown: string, documentId: string) => {
  suppressChange = true
  currentDocumentId = documentId
  sourceEditor.value = markdown
  crepe.editor.action(replaceAll(markdown, true))
  queueMicrotask(() => {
    suppressChange = false
  })
}

const setMode = (mode: EditorMode) => {
  document.body.className = `mode-${mode}`
  if (mode === 'source') sourceEditor.focus()
}

const applyTheme = (theme: EditorTheme) => {
  const root = document.documentElement
  root.dataset.theme = theme.id
  root.style.colorScheme = theme.isDark ? 'dark' : 'light'
  const uiFont = theme.monospaceInterface
    ? '"Cascadia Mono", Consolas, monospace'
    : '"Segoe UI Variable Text", "Segoe UI", system-ui, sans-serif'

  const tokens: Record<string, string> = {
    '--app-bg': theme.background,
    '--surface': theme.surface,
    '--surface-muted': theme.muted,
    '--sidebar': theme.sidebar,
    '--text': theme.foreground,
    '--text-muted': theme.mutedForeground,
    '--border': theme.border,
    '--accent': theme.accent,
    '--accent-text': theme.accentForeground,
    '--selection': theme.selection,
    '--code-bg': theme.code,
    '--destructive': theme.destructive,
    '--font-ui': uiFont,
    '--crepe-color-background': theme.surface,
    '--crepe-color-on-background': theme.foreground,
    '--crepe-color-surface': theme.surface,
    '--crepe-color-surface-low': theme.muted,
    '--crepe-color-on-surface': theme.foreground,
    '--crepe-color-outline': theme.border,
    '--crepe-color-primary': theme.accent,
    '--crepe-color-secondary': theme.selection,
    '--crepe-color-on-secondary': theme.foreground,
    '--crepe-color-inverse': theme.foreground,
    '--crepe-color-on-inverse': theme.background,
    '--crepe-color-inline-code': theme.code,
    '--crepe-color-error': theme.destructive,
    '--crepe-color-hover': theme.muted,
    '--crepe-color-selected': theme.selection,
    '--crepe-color-inline-area': theme.muted,
  }

  const themedElements = [root, document.querySelector<HTMLElement>('.milkdown')].filter(
    (element): element is HTMLElement => element !== null,
  )
  themedElements.forEach((element) => {
    Object.entries(tokens).forEach(([name, value]) => element.style.setProperty(name, value))
  })
}

sourceEditor.addEventListener('input', () => {
  if (sourceUpdateTimer !== undefined) window.clearTimeout(sourceUpdateTimer)
  sourceUpdateTimer = window.setTimeout(() => {
    const markdown = sourceEditor.value
    suppressChange = true
    crepe.editor.action(replaceAll(markdown))
    queueMicrotask(() => {
      suppressChange = false
    })
    postToHost({ type: 'changed', documentId: currentDocumentId, markdown })
  }, 180)
})

window.chrome?.webview?.addEventListener('message', (event) => {
  const message = event.data
  switch (message.type) {
    case 'load':
      loadMarkdown(message.markdown ?? '', message.documentId ?? '')
      break
    case 'mode':
      setMode(message.mode ?? 'rich')
      break
    case 'focus':
      if (document.body.classList.contains('mode-source')) sourceEditor.focus()
      break
    case 'theme':
      if (message.theme) applyTheme(message.theme)
      break
  }
})

postToHost({ type: 'ready' })
