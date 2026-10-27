<script setup lang="ts">
// Simple stroke icons (24x24). Kept inline so the WebApp needs no icon font.
const icons = {
  mic: 'M12 3a3 3 0 0 0-3 3v6a3 3 0 0 0 6 0V6a3 3 0 0 0-3-3z M5 11a7 7 0 0 0 14 0 M12 18v3 M8 21h8',
  music: 'M9 18V5l11-2v13 M9 18a3 3 0 1 1-6 0 3 3 0 0 1 6 0z M20 16a3 3 0 1 1-6 0 3 3 0 0 1 6 0z',
  queue: 'M3 6h13 M3 12h13 M3 18h9 M18 15l4 3-4 3z',
  users: 'M16 20v-1a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v1 M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z M22 20v-1a4 4 0 0 0-3-3.87 M16 3.13a4 4 0 0 1 0 7.75',
  more: 'M5 12h.01 M12 12h.01 M19 12h.01',
  search: 'M11 19a8 8 0 1 0 0-16 8 8 0 0 0 0 16z M21 21l-4.35-4.35',
  plus: 'M12 5v14 M5 12h14',
  trash: 'M3 6h18 M8 6V4h8v2 M6 6l1 14h10l1-14',
  up: 'M12 19V5 M5 12l7-7 7 7',
  down: 'M12 5v14 M19 12l-7 7-7-7',
  left: 'M15 18l-6-6 6-6',
  right: 'M9 18l6-6-6-6',
  chevronUp: 'M18 15l-6-6-6 6',
  chevronDown: 'M6 9l6 6 6-6',
  back: 'M19 12H5 M12 19l-7-7 7-7',
  keyboard: 'M2 6h20v12H2z M6 10h.01 M10 10h.01 M14 10h.01 M18 10h.01 M7 14h10',
  playlist: 'M3 6h12 M3 12h12 M3 18h8 M17 18V9l4-1',
  shield: 'M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z',
  logout: 'M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4 M16 17l5-5-5-5 M21 12H9',
  edit: 'M12 20h9 M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z',
  camera: 'M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z M12 17a4 4 0 1 0 0-8 4 4 0 0 0 0 8z',
  close: 'M18 6L6 18 M6 6l12 12',
  check: 'M20 6L9 17l-5-5',
  globe: 'M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M2 12h20 M12 2a15 15 0 0 1 0 20 M12 2a15 15 0 0 0 0 20',
  code: 'M16 18l6-6-6-6 M8 6l-6 6 6 6',
  lock: 'M5 11h14v10H5z M8 11V7a4 4 0 0 1 8 0v4',
  duet: 'M8 4a3 3 0 0 0-3 3v4a3 3 0 0 0 6 0V7a3 3 0 0 0-3-3z M16 4a3 3 0 0 0-3 3v4a3 3 0 0 0 6 0V7a3 3 0 0 0-3-3z M8 14v6 M16 14v6',
  filter: 'M22 3H2l8 9.46V19l4 2v-8.54z',
  play: 'M7 4l13 8-13 8z',
  stop: 'M6 6h12v12H6z',
  sort: 'M3 6h10 M3 12h7 M3 18h4 M17 4v16 M14 17l3 3 3-3',
  help: 'M12 22a10 10 0 1 0 0-20 10 10 0 0 0 0 20z M9.1 9a3 3 0 0 1 5.8 1c0 2-3 3-3 3 M12 17h.01'
} as const

export type IconName = keyof typeof icons

defineProps<{ name: IconName; size?: number }>()
</script>

<template>
  <svg
    :width="size ?? 24"
    :height="size ?? 24"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    stroke-width="2"
    stroke-linecap="round"
    stroke-linejoin="round"
    aria-hidden="true"
  >
    <path :d="icons[name]" />
  </svg>
</template>
