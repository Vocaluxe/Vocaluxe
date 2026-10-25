<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import AppIcon, { type IconName } from '@/components/AppIcon.vue'
import { session } from '@/stores/session'

const { t } = useI18n()
const queueName = computed(() => session.state.server?.queuePlaylistName ?? 'WebApp Queue')

const sections: { icon: IconName; key: string }[] = [
  { icon: 'queue', key: 'queue' },
  { icon: 'music', key: 'game' },
  { icon: 'mic', key: 'start' },
  { icon: 'playlist', key: 'playlists' },
  { icon: 'play', key: 'preview' },
  { icon: 'globe', key: 'connect' },
  { icon: 'lock', key: 'https' }
]
</script>

<template>
  <div class="page">
    <header class="page-header">
      <h1>{{ t('help.title') }}</h1>
    </header>
    <section v-for="s in sections" :key="s.key" class="card section">
      <h2><AppIcon :name="s.icon" :size="20" /> {{ t(`help.${s.key}Title`) }}</h2>
      <p>{{ t(`help.${s.key}Text`, { name: queueName }) }}</p>
    </section>
  </div>
</template>

<style scoped>
.section {
  display: grid;
  gap: 8px;
}

.section h2 {
  display: flex;
  align-items: center;
  gap: 10px;
}

.section p {
  margin: 0;
  color: var(--text-muted);
}
</style>
