<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, type Song, type SongFacets, type SongSort } from '@/api/client'
import AppIcon from '@/components/AppIcon.vue'
import SongCover from '@/components/SongCover.vue'
import SongSheet from '@/components/SongSheet.vue'
import { preview, stopPreview, togglePreview } from '@/stores/preview'
import { storage } from '@/stores/storage'
import { showError } from '@/stores/toast'

const { t } = useI18n()
const sorts: SongSort[] = ['Artist', 'Title', 'Year']
const pageSize = 50
const sortKey = 'vlx.songSort'

const query = ref('')
const sort = ref<SongSort>(sorts.find((s) => s === storage.get(sortKey)) ?? 'Artist')
const filters = reactive({ language: '', genre: '', decade: '', duet: false })
const songs = ref<Song[]>([])
const total = ref(0)
const loading = ref(false)
const facets = ref<SongFacets | null>(null)
const selected = ref<Song | null>(null)
const sentinel = ref<HTMLElement>()
let page = 0
let generation = 0
let debounce: number | undefined
let observer: IntersectionObserver | undefined

const activeFilters = computed(() => {
  const list: { key: keyof typeof filters; label: string }[] = []
  if (filters.language) list.push({ key: 'language', label: filters.language })
  if (filters.genre) list.push({ key: 'genre', label: filters.genre })
  if (filters.decade) list.push({ key: 'decade', label: t('songs.decade', { decade: filters.decade }) })
  if (filters.duet) list.push({ key: 'duet', label: t('songs.onlyDuets') })
  return list
})

function resetFilters() {
  Object.assign(filters, { language: '', genre: '', decade: '', duet: false })
}

async function load(reset: boolean) {
  if (reset) {
    page = 0
    generation++
  } else if (loading.value || songs.value.length >= total.value) {
    return
  }
  const current = generation
  loading.value = true
  try {
    const result = await api.songs({
      query: query.value || undefined,
      language: filters.language || undefined,
      genre: filters.genre || undefined,
      decade: filters.decade ? Number(filters.decade) : undefined,
      duet: filters.duet || undefined,
      sort: sort.value,
      page,
      pageSize
    })
    if (current !== generation) return
    songs.value = reset ? result.items : [...songs.value, ...result.items]
    total.value = result.total
    page++
  } catch (e) {
    showError(e)
  } finally {
    if (current === generation) loading.value = false
  }
}

watch(query, () => {
  window.clearTimeout(debounce)
  debounce = window.setTimeout(() => load(true), 250)
})
watch(filters, () => load(true))
watch(sort, () => {
  storage.set(sortKey, sort.value)
  load(true)
})

onMounted(() => {
  load(true)
  api
    .facets()
    .then((f) => (facets.value = f))
    .catch(showError)
  observer = new IntersectionObserver((e) => e[0]?.isIntersecting && load(false), { rootMargin: '600px' })
  if (sentinel.value) observer.observe(sentinel.value)
})
onUnmounted(() => {
  observer?.disconnect()
  window.clearTimeout(debounce)
  stopPreview()
})
</script>

<template>
  <div class="page">
    <header class="page-header">
      <h1>{{ t('songs.title') }}</h1>
      <span class="muted">{{ t('songs.count', total) }}</span>
    </header>

    <div class="toolbar">
      <label class="search">
        <AppIcon name="search" class="search-icon" />
        <span class="visually-hidden">{{ t('songs.search') }}</span>
        <input v-model="query" class="input" type="search" :placeholder="t('songs.search')" enterkeyhint="search" />
      </label>

      <div class="option-row">
        <span class="muted option-label"><AppIcon name="filter" :size="16" /> {{ t('songs.filterLabel') }}</span>
        <select v-model="filters.language" class="pill" :class="{ active: filters.language }" :aria-label="t('songs.language')">
          <option value="">{{ t('songs.allLanguages') }}</option>
          <option v-for="f in facets?.languages" :key="f.value" :value="f.value">{{ f.value }} ({{ f.count }})</option>
        </select>
        <select v-model="filters.genre" class="pill" :class="{ active: filters.genre }" :aria-label="t('songs.genre')">
          <option value="">{{ t('songs.allGenres') }}</option>
          <option v-for="f in facets?.genres" :key="f.value" :value="f.value">{{ f.value }} ({{ f.count }})</option>
        </select>
        <select v-model="filters.decade" class="pill" :class="{ active: filters.decade }" :aria-label="t('songs.decadeLabel')">
          <option value="">{{ t('songs.allDecades') }}</option>
          <option v-for="f in facets?.decades" :key="f.value" :value="f.value">{{ t('songs.decade', { decade: f.value }) }} ({{ f.count }})</option>
        </select>
        <label class="pill toggle" :class="{ active: filters.duet }">
          <input v-model="filters.duet" type="checkbox" class="visually-hidden" />
          <AppIcon name="duet" :size="16" /> {{ t('songs.onlyDuets') }}
        </label>
        <button v-if="activeFilters.length" class="pill reset" :aria-label="t('songs.clearFilters')" @click="resetFilters">
          <AppIcon name="close" :size="16" />
        </button>
      </div>

      <div class="option-row">
        <span class="muted option-label"><AppIcon name="sort" :size="16" /> {{ t('songs.sortLabel') }}</span>
        <div class="segments" role="radiogroup" :aria-label="t('songs.sortLabel')">
          <label v-for="s in sorts" :key="s" class="segment" :class="{ active: sort === s }">
            <input v-model="sort" type="radio" :value="s" class="visually-hidden" />
            {{ t(`songs.sortBy.${s}`) }}
          </label>
        </div>
      </div>
    </div>

    <ul v-if="songs.length" class="list">
      <li v-for="song in songs" :key="song.id" class="song">
        <button class="row" @click="selected = song">
          <SongCover :song-id="song.id" :has-cover="song.hasCover" />
          <span class="row-text">
            <span class="row-title">{{ song.title }}</span>
            <span class="row-sub">{{ song.artist }}<template v-if="song.year"> · {{ song.year }}</template></span>
          </span>
          <span v-if="song.isDuet" class="badge badge--accent">{{ t('songs.duet') }}</span>
        </button>
        <button
          v-if="song.hasAudio"
          class="icon-btn play"
          :class="{ playing: preview.songId === song.id }"
          :aria-label="preview.songId === song.id ? t('songs.stopPreview') : t('songs.preview')"
          @click="togglePreview(song)"
        >
          <AppIcon :name="preview.songId === song.id ? 'stop' : 'play'" :size="20" />
        </button>
      </li>
    </ul>
    <div v-else-if="!loading" class="empty">
      <AppIcon name="music" :size="40" />
      <p>{{ t('songs.empty') }}</p>
    </div>
    <p v-if="loading" class="muted loading">{{ t('common.loading') }}</p>
    <div ref="sentinel" aria-hidden="true"></div>

    <SongSheet :song="selected" @close="selected = null" />

  </div>
</template>

<style scoped>
.toolbar {
  display: grid;
  gap: 10px;
  position: sticky;
  top: 0;
  z-index: 5;
  margin: 0 calc(var(--gutter) * -1);
  padding: 8px var(--gutter);
  background: var(--bg);
}

.search {
  position: relative;
}

.search .input {
  padding-left: 44px;
  border-radius: 999px;
}

.search-icon {
  position: absolute;
  left: 14px;
  top: 50%;
  transform: translateY(-50%);
  color: var(--text-muted);
  pointer-events: none;
}

.option-row {
  display: flex;
  align-items: center;
  gap: 8px;
  overflow-x: auto;
  scrollbar-width: none;
}

.option-label {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  flex: none;
  min-width: 118px;
  font-size: 0.875rem;
}

.pill {
  position: relative;
  flex: none;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  width: auto;
  min-height: 36px;
  padding: 0 14px;
  border: 1px solid var(--border);
  border-radius: 999px;
  background: var(--surface);
  color: var(--text);
  font: inherit;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
}

select.pill {
  appearance: none;
  padding-right: 30px;
  background-image: linear-gradient(45deg, transparent 50%, currentColor 50%), linear-gradient(135deg, currentColor 50%, transparent 50%);
  background-position:
    calc(100% - 16px) 52%,
    calc(100% - 11px) 52%;
  background-size: 5px 5px;
  background-repeat: no-repeat;
}

.pill.active {
  border-color: var(--accent);
  background-color: var(--accent-soft);
  color: var(--accent);
}

.pill.reset {
  padding: 0 10px;
  color: var(--text-muted);
}

.segments {
  display: inline-flex;
  flex: none;
  padding: 3px;
  border-radius: 999px;
  background: var(--surface);
}

.segment {
  position: relative;
  display: inline-flex;
  align-items: center;
  min-height: 30px;
  padding: 0 14px;
  border-radius: 999px;
  cursor: pointer;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-muted);
}

.segment.active {
  background: var(--surface-raised);
  color: var(--text);
}

.song {
  display: flex;
  align-items: center;
  border-bottom: 1px solid var(--border);
}

.song:last-child {
  border-bottom: 0;
}

.song .row {
  border-bottom: 0;
  flex: 1;
  min-width: 0;
}

.play {
  margin-right: 6px;
  color: var(--text-muted);
}

.play.playing {
  color: var(--accent);
  background: var(--accent-soft);
}

.loading {
  text-align: center;
}

</style>
