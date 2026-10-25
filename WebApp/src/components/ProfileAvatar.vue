<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { avatarUrl, type Profile } from '@/api/client'
import { session } from '@/stores/session'

const props = defineProps<{ profile: Pick<Profile, 'id' | 'name' | 'hasAvatar'>; size?: number }>()
const failed = ref(false)
const pixels = computed(() => props.size ?? 48)
const version = computed(() => (props.profile.id === session.profile.value?.id ? session.state.avatarVersion : ''))
const src = computed(() => avatarUrl(props.profile.id, pixels.value * 2, version.value))
const initials = computed(() =>
  props.profile.name
    .split(/\s+/)
    .map((w) => w[0])
    .join('')
    .slice(0, 2)
    .toUpperCase()
)
// A stable color per profile for the placeholder.
const hue = computed(() => [...props.profile.id].reduce((sum, c) => sum + c.charCodeAt(0), 0) % 360)

watch(src, () => (failed.value = false))
</script>

<template>
  <div
    class="avatar"
    :style="{ width: `${pixels}px`, height: `${pixels}px`, fontSize: `${pixels / 2.6}px`, background: `hsl(${hue} 55% 42%)` }"
  >
    <img v-if="profile.hasAvatar && !failed" :src="src" alt="" loading="lazy" @error="failed = true" />
    <span v-else>{{ initials }}</span>
  </div>
</template>

<style scoped>
.avatar {
  flex: none;
  display: grid;
  place-items: center;
  border-radius: 50%;
  overflow: hidden;
  color: #fff;
  font-weight: 700;
}

.avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
</style>
