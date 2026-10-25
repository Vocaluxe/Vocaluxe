<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, type RemoteKey } from '@/api/client'
import AppIcon, { type IconName } from '@/components/AppIcon.vue'
import { showError } from '@/stores/toast'

const { t } = useI18n()
const text = ref('')

const pad: { key: RemoteKey; icon: IconName; area: string }[] = [
  { key: 'Up', icon: 'chevronUp', area: 'up' },
  { key: 'Left', icon: 'left', area: 'left' },
  { key: 'Right', icon: 'right', area: 'right' },
  { key: 'Down', icon: 'chevronDown', area: 'down' }
]
const extraKeys: RemoteKey[] = ['Escape', 'Tab', 'Backspace', 'Space', 'PageUp', 'PageDown']

function send(key: RemoteKey) {
  navigator.vibrate?.(10)
  api.sendKey(key).catch(showError)
}

async function sendText() {
  if (!text.value) return
  try {
    await api.sendText(text.value)
    text.value = ''
  } catch (e) {
    showError(e)
  }
}
</script>

<template>
  <div class="page">
    <header class="page-header">
      <h1>{{ t('remote.title') }}</h1>
    </header>

    <div class="pad">
      <button v-for="p in pad" :key="p.key" class="pad-btn" :style="{ gridArea: p.area }" :aria-label="p.key" @click="send(p.key)">
        <AppIcon :name="p.icon" :size="32" />
      </button>
      <button class="pad-btn pad-ok" style="grid-area: ok" @click="send('Enter')">{{ t('remote.keys.Enter') }}</button>
    </div>

    <div class="keys">
      <button v-for="k in extraKeys" :key="k" class="btn" @click="send(k)">{{ t(`remote.keys.${k}`) }}</button>
    </div>

    <form class="text" @submit.prevent="sendText">
      <label class="visually-hidden" for="remote-text">{{ t('remote.text') }}</label>
      <input id="remote-text" v-model="text" class="input" maxlength="200" :placeholder="t('remote.text')" autocomplete="off" />
      <button class="btn btn--primary" :disabled="!text">{{ t('remote.send') }}</button>
    </form>
  </div>
</template>

<style scoped>
.page {
  max-width: 480px;
}

.pad {
  display: grid;
  grid-template-areas:
    '. up .'
    'left ok right'
    '. down .';
  grid-template-columns: repeat(3, 1fr);
  gap: 10px;
  aspect-ratio: 1;
  max-width: 340px;
  width: 100%;
  margin: 0 auto;
}

.pad-btn {
  display: grid;
  place-items: center;
  border: 0;
  border-radius: 22px;
  background: var(--surface-raised);
  font-weight: 700;
  font-size: 1.1rem;
  touch-action: manipulation;
  user-select: none;
}

.pad-btn:active {
  background: var(--surface-hover);
  transform: scale(0.97);
}

.pad-ok {
  border-radius: 50%;
  background: var(--accent);
  color: #fff;
}

.pad-ok:active {
  background: var(--accent);
  filter: brightness(1.1);
}

.keys {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 8px;
}

.keys .btn {
  padding: 0 8px;
  border-radius: var(--radius-small);
}

.text {
  display: flex;
  gap: 8px;
}
</style>
