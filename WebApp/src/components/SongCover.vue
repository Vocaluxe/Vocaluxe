<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { coverUrl } from '@/api/client'
import AppIcon from './AppIcon.vue'

const props = defineProps<{ songId: number; hasCover?: boolean; size?: number }>()
const failed = ref(false)
const loaded = ref(false)
const pixels = computed(() => props.size ?? 48)
// Request twice the displayed size for sharp images on high density screens.
const src = computed(() => coverUrl(props.songId, pixels.value * 2))

watch(src, () => {
  failed.value = false
  loaded.value = false
})
</script>

<template>
  <div class="cover" :style="{ width: `${pixels}px`, height: `${pixels}px` }">
    <img
      v-if="hasCover !== false && !failed"
      :src="src"
      :class="{ loaded }"
      alt=""
      loading="lazy"
      decoding="async"
      @load="loaded = true"
      @error="failed = true"
    />
    <AppIcon v-else name="music" :size="Math.round(pixels / 2.4)" />
  </div>
</template>

<style scoped>
.cover {
  flex: none;
  display: grid;
  place-items: center;
  border-radius: 8px;
  overflow: hidden;
  background: linear-gradient(135deg, var(--surface-raised), var(--surface-hover));
  color: var(--text-muted);
}

.cover img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  opacity: 0;
  transition: opacity 0.2s ease;
}

.cover img.loaded {
  opacity: 1;
}
</style>
