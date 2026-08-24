import { Crepe } from '@milkdown/crepe'
import '@milkdown/crepe/theme/common/style.css'
import '@milkdown/crepe/theme/frame.css'
import { replaceAll } from '@milkdown/kit/utils'
import './style.css'

type EditorMode = 'rich' | 'source' | 'split'

interface HostMessage {
  type: 'load' | 'mode' | 'focus'
  documentId?: string
  markdown?: string
  mode?: EditorMode
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
  }
})

postToHost({ type: 'ready' })
