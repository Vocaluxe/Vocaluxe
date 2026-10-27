// Audio preview of songs. Only one song plays at a time; it starts where the song menu of the game starts its preview.
import { reactive } from 'vue'
import { audioUrl, type Song } from '@/api/client'
import { i18n } from '@/i18n'
import { toast } from './toast'

const previewLength = 30

export const preview = reactive({
  songId: null as number | null,
  loading: false
})

let audio: HTMLAudioElement | null = null
let stopTimer: number | undefined

export function stopPreview() {
  window.clearTimeout(stopTimer)
  audio?.pause()
  preview.songId = null
  preview.loading = false
}

export function togglePreview(song: Song) {
  if (preview.songId === song.id) {
    stopPreview()
    return
  }
  stopPreview()
  audio ??= new Audio()
  const player = audio
  preview.songId = song.id
  preview.loading = true
  player.src = audioUrl(song.id)
  player.preload = 'auto'
  player.onloadedmetadata = () => {
    const length = Number.isFinite(player.duration) ? player.duration : 0
    let start = song.previewStart ?? length / 4
    if (length > 0 && start > length - 5) start = Math.max(0, Math.min(length / 4, length - 5))
    player.currentTime = Math.max(0, start)
  }
  player.onplaying = () => {
    preview.loading = false
    window.clearTimeout(stopTimer)
    stopTimer = window.setTimeout(stopPreview, previewLength * 1000)
  }
  player.onended = stopPreview
  player.onerror = () => {
    if (preview.songId === song.id) {
      stopPreview()
      toast(i18n.global.t('songs.previewUnavailable'), 'error')
    }
  }
  player.play().catch(() => stopPreview())
}
