import { Crepe } from '@milkdown/crepe'
import '@milkdown/crepe/theme/common/style.css'
import '@milkdown/crepe/theme/frame.css'
import { replaceAll } from '@milkdown/kit/utils'
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

const crepe = new Crepe({
  root: '#rich-editor',
  defaultValue: '',
  features: {
    [Crepe.Feature.AI]: false,
    [Crepe.Feature.Latex]: false,
    [Crepe.Feature.TopBar]: true,
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
