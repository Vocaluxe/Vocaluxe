<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, type PlaylistSummary, type Song } from '@/api/client'
import { setQueue } from '@/stores/live'
import { preview, togglePreview } from '@/stores/preview'
import { session } from '@/stores/session'
import { showError, toast } from '@/stores/toast'
import AppIcon from './AppIcon.vue'
import AppSheet from './AppSheet.vue'
import QueueOptions, { type QueueChoice } from './QueueOptions.vue'
import SongCover from './SongCover.vue'

const props = defineProps<{ song: Song | null }>()
const emit = defineEmits<{ close: [] }>()
const { t } = useI18n()

const playlists = ref<PlaylistSummary[] | null>(null)
const choosingPlaylist = ref(false)
const busy = ref(false)
const choice = ref<QueueChoice>({ gameMode: 'Normal', audioMode: 'Normal', playerIds: [] })

watch(
  () => props.song,
  (song) => {
    choosingPlaylist.value = false
    if (song) choice.value = { gameMode: song.gameModes[0] ?? 'Normal', audioMode: 'Normal', playerIds: [] }
  }
)

async function addToQueue() {
  if (!props.song) return
  busy.value = true
  try {
    setQueue(await api.enqueue(props.song.id, choice.value))
    toast(t('songs.addedToQueue', { title: props.song.title }))
    emit('close')
  } catch (e) {
    showError(e)
  } finally {
    busy.value = false
  }
}

async function choosePlaylist() {
  choosingPlaylist.value = true
  playlists.value = null
  playlists.value = await api.playlists().catch((e) => {
    showError(e)
    return []
  })
}

async function addToPlaylist(playlist: PlaylistSummary) {
  if (!props.song) return
  busy.value = true
  try {
    await api.addToPlaylist(playlist.id, props.song.id)
    toast(t('songs.addedToPlaylist', { name: playlist.name }))
    emit('close')
  } catch (e) {
    showError(e)
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <AppSheet :open="song !== null" :title="choosingPlaylist ? t('songs.addToPlaylist') : ''" @close="emit('close')">
    <template v-if="song && !choosingPlaylist">
      <div class="detail">
        <SongCover :song-id="song.id" :has-cover="song.hasCover" :size="104" />
        <div class="detail-text">
          <h2>{{ song.title }}</h2>
          <p class="muted">{{ song.artist }}</p>
          <p class="badges">
            <span v-if="song.year" class="badge">{{ song.year }}</span>
            <span v-if="song.language" class="badge">{{ song.language }}</span>
            <span v-if="song.genre" class="badge">{{ song.genre }}</span>
            <span v-if="song.isDuet" class="badge badge--accent">{{ t('songs.duet') }}</span>
          </p>
          <button v-if="song.hasAudio" class="btn preview" :class="{ playing: preview.songId === song.id }" @click="togglePreview(song)">
            <AppIcon :name="preview.songId === song.id ? 'stop' : 'play'" :size="18" />
            {{ preview.songId === song.id ? t('songs.stopPreview') : t('songs.preview') }}
          </button>
        </div>
      </div>

      <QueueOptions v-if="session.can('AddToQueue')" v-model="choice" :song="song" />

      <div class="actions">
        <button v-if="session.can('AddToQueue')" class="btn btn--primary btn--block" :disabled="busy" @click="addToQueue">
          <AppIcon name="queue" /> {{ t('songs.addToQueue') }}
        </button>
        <button v-if="session.can('EditPlaylists')" class="btn btn--block" :disabled="busy" @click="choosePlaylist">
          <AppIcon name="playlist" /> {{ t('songs.addToPlaylist') }}
        </button>
      </div>
    </template>

    <template v-else-if="song">
      <p v-if="playlists === null" class="muted">{{ t('common.loading') }}</p>
      <p v-else-if="playlists.length === 0" class="muted">{{ t('songs.noPlaylists') }}</p>
      <ul v-else class="list picker">
        <li v-for="p in playlists" :key="p.id">
          <button class="row" :disabled="busy" @click="addToPlaylist(p)">
            <AppIcon name="playlist" />
            <span class="row-text">
              <span class="row-title">{{ p.name }}</span>
              <span class="row-sub">{{ t('playlists.songs', p.songCount) }}</span>
            </span>
          </button>
        </li>
      </ul>
      <button class="btn" @click="choosingPlaylist = false"><AppIcon name="back" /> {{ t('common.back') }}</button>
    </template>
  </AppSheet>
</template>

<style scoped>
.detail {
  display: flex;
  gap: 16px;
  align-items: center;
}

.detail-text {
  min-width: 0;
  display: grid;
  gap: 4px;
  justify-items: start;
}

.detail-text p {
  margin: 0;
}

.badges {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.preview {
  min-height: 36px;
  margin-top: 4px;
  padding: 0 14px;
}

.preview.playing {
  background: var(--accent-soft);
  color: var(--accent);
}

.actions {
  display: grid;
  gap: 10px;
}

.picker {
  background: var(--surface-raised);
  max-height: 50dvh;
  overflow-y: auto;
}
</style>
