<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { api, type PlaylistSummary } from '@/api/client'
import AppIcon from '@/components/AppIcon.vue'
import AppSheet from '@/components/AppSheet.vue'
import { session } from '@/stores/session'
import { showError } from '@/stores/toast'

const { t } = useI18n()
const router = useRouter()
const playlists = ref<PlaylistSummary[] | null>(null)
const creating = ref(false)
const name = ref('')
const busy = ref(false)

onMounted(async () => {
  playlists.value = await api.playlists().catch((e) => {
    showError(e)
    return []
  })
})

async function create() {
  if (!name.value.trim()) return
  busy.value = true
  try {
    const playlist = await api.createPlaylist(name.value.trim())
    creating.value = false
    name.value = ''
    router.push({ name: 'playlist', params: { id: playlist.id } })
  } catch (e) {
    showError(e)
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <div class="page">
    <header class="page-header">
      <h1>{{ t('playlists.title') }}</h1>
      <button v-if="session.can('EditPlaylists')" class="btn btn--primary" @click="creating = true"><AppIcon name="plus" /> {{ t('playlists.new') }}</button>
    </header>

    <p class="muted intro">{{ t('playlists.intro') }}</p>
    <p v-if="playlists === null" class="muted">{{ t('common.loading') }}</p>
    <div v-else-if="playlists.length === 0" class="empty">
      <AppIcon name="playlist" :size="40" />
      <p>{{ t('playlists.empty') }}</p>
    </div>
    <nav v-else class="list">
      <RouterLink v-for="p in playlists" :key="p.id" :to="{ name: 'playlist', params: { id: p.id } }" class="row">
        <AppIcon name="playlist" />
        <span class="row-text">
          <span class="row-title">{{ p.name }}</span>
          <span class="row-sub">{{ t('playlists.songs', p.songCount) }}</span>
        </span>
        <AppIcon name="right" class="muted" />
      </RouterLink>
    </nav>

    <AppSheet :open="creating" :title="t('playlists.new')" @close="creating = false">
      <form class="form" @submit.prevent="create">
        <label class="field">
          <span>{{ t('playlists.name') }}</span>
          <input v-model="name" class="input" maxlength="64" required autofocus />
        </label>
        <button class="btn btn--primary btn--block" :disabled="busy || !name.trim()">{{ t('playlists.create') }}</button>
      </form>
    </AppSheet>
  </div>
</template>

<style scoped>
.intro {
  margin: -6px 0 0;
  font-size: 0.875rem;
}

.form {
  display: grid;
  gap: 14px;
}
</style>
