// Translations live in src/locales/<lang>.json as flat key/value files (Transifex format KEYVALUEJSON).
// en.json is the source; every file that Transifex adds there shows up as a language automatically.
import { createI18n } from 'vue-i18n'
import { storage } from '@/stores/storage'

const files = import.meta.glob<Record<string, string>>('../locales/*.json', { eager: true, import: 'default' })

const messages: Record<string, Record<string, string>> = {}
for (const [path, content] of Object.entries(files)) {
  const code = path.replace(/^.*\/(.+)\.json$/, '$1')
  messages[code] = content
}

export const languages = Object.keys(messages)
  .map((code) => ({ code, name: messages[code]['language.name'] ?? code }))
  .sort((a, b) => a.name.localeCompare(b.name))

const languageKey = 'vlx.language'

function initialLanguage(): string {
  const saved = storage.get(languageKey)
  if (saved && messages[saved]) return saved
  const browser = navigator.language.toLowerCase()
  return Object.keys(messages).find((code) => browser === code || browser.startsWith(`${code}-`)) ?? 'en'
}

export const i18n = createI18n({
  legacy: false,
  flatJson: true,
  locale: initialLanguage(),
  fallbackLocale: 'en',
  messages
})

document.documentElement.lang = i18n.global.locale.value

export function setLanguage(code: string) {
  i18n.global.locale.value = code
  document.documentElement.lang = code
  storage.set(languageKey, code)
}
