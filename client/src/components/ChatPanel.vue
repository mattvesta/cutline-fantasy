<script setup lang="ts">
import { ref, computed, nextTick, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { HubConnectionBuilder } from '@microsoft/signalr'
import { chatApi } from '../api/chat'
import { useLeagueStore } from '../stores/league'
import { useAuthStore } from '../stores/auth'
import GifPicker from './GifPicker.vue'
import type { ChatMessage } from '../api/types'

const route    = useRoute()
const router   = useRouter()
const leagueId = route.params.leagueId as string
const store    = useLeagueStore()
const auth     = useAuthStore()

// ── Panel open state ──────────────────────────────────────────────────────────
const open      = ref(false)
const hasUnread = ref(false)

function toggleOpen() {
  open.value = !open.value
}

watch(open, (val) => {
  if (val) {
    hasUnread.value = false
    nextTick(() => scrollToBottom(true))
  }
})

// ── Identity from auth store ──────────────────────────────────────────────────
const managerId = computed(() => auth.managerId)

const myName = computed(() =>
  store.current?.leagueManagers.find(lm => lm.managerId === managerId.value)?.manager.displayName
  ?? auth.manager?.displayName
  ?? ''
)

const allManagers = computed(() =>
  (store.current?.leagueManagers ?? []).map(lm => lm.manager)
)

// ── Chat state ────────────────────────────────────────────────────────────────
const messages    = ref<ChatMessage[]>([])
const isLoading   = ref(false)
const loadingMore = ref(false)
const hasMore     = ref(true)
const sendError   = ref<string | null>(null)
const isSending   = ref(false)
const connected   = ref(false)

const messageText   = ref('')
const showGifPicker = ref(false)
const inputRef      = ref<HTMLTextAreaElement | null>(null)
const scrollRef     = ref<HTMLDivElement | null>(null)

// ── Mention autocomplete ──────────────────────────────────────────────────────
const mention      = ref<{ query: string; start: number } | null>(null)
const mentionIndex = ref(0)

const otherManagers = computed(() =>
  allManagers.value.filter(m => m && m.id !== managerId.value)
)

const filteredManagers = computed(() => {
  if (!mention.value) return []
  const q = mention.value.query.toLowerCase()
  return otherManagers.value.filter(m => m.displayName.toLowerCase().includes(q)).slice(0, 6)
})

const managerNames = computed(() =>
  (store.current?.leagueManagers ?? []).map(lm => lm.manager.displayName)
)

function onTextInput(e: Event) {
  const ta  = e.target as HTMLTextAreaElement
  const pos = ta.selectionStart ?? 0
  const before = ta.value.slice(0, pos)
  const match  = before.match(/@([A-Za-z][A-Za-z0-9 _.-]*)$/)
  if (match) {
    mention.value      = { query: match[1], start: pos - match[0].length }
    mentionIndex.value = 0
  } else {
    mention.value = null
  }
  autoGrow(ta)
}

function dismissMentionDelayed() { setTimeout(() => { mention.value = null }, 150) }

function onKeyDown(e: KeyboardEvent) {
  if (!mention.value || filteredManagers.value.length === 0) return
  if (e.key === 'ArrowDown')  { e.preventDefault(); mentionIndex.value = (mentionIndex.value + 1) % filteredManagers.value.length }
  if (e.key === 'ArrowUp')    { e.preventDefault(); mentionIndex.value = (mentionIndex.value - 1 + filteredManagers.value.length) % filteredManagers.value.length }
  if (e.key === 'Enter' || e.key === 'Tab') {
    if (filteredManagers.value.length > 0) { e.preventDefault(); insertMention(filteredManagers.value[mentionIndex.value].displayName) }
  }
  if (e.key === 'Escape') mention.value = null
}

function insertMention(displayName: string) {
  if (!mention.value || !inputRef.value) return
  const ta     = inputRef.value
  const before = messageText.value.slice(0, mention.value.start)
  const after  = messageText.value.slice(ta.selectionStart ?? mention.value.start)
  messageText.value = `${before}@${displayName} ${after}`
  mention.value = null
  nextTick(() => {
    ta.focus()
    const pos = before.length + displayName.length + 2
    ta.setSelectionRange(pos, pos)
    autoGrow(ta)
  })
}

function autoGrow(ta: HTMLTextAreaElement) {
  ta.style.height = 'auto'
  ta.style.height = Math.min(ta.scrollHeight, 120) + 'px'
}

// ── Message rendering ─────────────────────────────────────────────────────────
function renderContent(content: string): string {
  if (!content) return ''
  const esc = content
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
  const linked = esc.replace(
    /(https?:\/\/[^\s&]+)/g,
    '<a href="$1" target="_blank" rel="noopener noreferrer" class="text-blue-400 hover:underline break-all">$1</a>',
  )
  const names = managerNames.value
  const withMentions = linked.replace(/@([A-Za-z][A-Za-z0-9 _.-]*)/g, (match, name) =>
    names.includes(name)
      ? `<span style="color:var(--accent);font-weight:600">${match}</span>`
      : match,
  )
  return withMentions.replace(/\n/g, '<br>')
}

// ── Avatar helpers ────────────────────────────────────────────────────────────
const AVATAR_COLORS = [
  '#e31e24','#f97316','#eab308','#22c55e','#3b82f6','#a855f7','#ec4899','#06b6d4',
]
function avatarColor(name: string) {
  let h = 0
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) >>> 0
  return AVATAR_COLORS[h % AVATAR_COLORS.length]
}
function initials(name: string) {
  return name.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase()
}

function isGrouped(i: number) {
  if (i === 0) return false
  const cur  = messages.value[i]
  const prev = messages.value[i - 1]
  if (cur.managerId !== prev.managerId) return false
  return new Date(cur.sentAt).getTime() - new Date(prev.sentAt).getTime() < 5 * 60_000
}

function formatTime(iso: string) {
  const d = new Date(iso)
  const now = new Date()
  const diff = (now.getTime() - d.getTime()) / 1000
  if (diff < 60)    return 'just now'
  if (diff < 3600)  return `${Math.floor(diff / 60)}m ago`
  if (diff < 86400) return d.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })
  return d.toLocaleDateString([], { month: 'short', day: 'numeric' })
}

// ── Scroll ────────────────────────────────────────────────────────────────────
function isNearBottom() {
  const el = scrollRef.value
  if (!el) return true
  return el.scrollHeight - el.scrollTop - el.clientHeight < 80
}

function scrollToBottom(force = false) {
  const el = scrollRef.value
  if (!el) return
  if (force || isNearBottom()) {
    nextTick(() => el.scrollTo({ top: el.scrollHeight, behavior: force ? 'instant' : 'smooth' }))
  }
}

// ── Load ──────────────────────────────────────────────────────────────────────
async function loadHistory() {
  isLoading.value = true
  try {
    const batch = await chatApi.getHistory(leagueId, { limit: 50 })
    messages.value = batch
    hasMore.value  = batch.length >= 50
    if (open.value) nextTick(() => scrollToBottom(true))
  } finally {
    isLoading.value = false
  }
}

async function loadMore() {
  if (!hasMore.value || loadingMore.value || messages.value.length === 0) return
  loadingMore.value = true
  const oldest = messages.value[0].sentAt
  const batch  = await chatApi.getHistory(leagueId, { before: oldest, limit: 50 })
  messages.value = [...batch, ...messages.value]
  hasMore.value  = batch.length >= 50
  loadingMore.value = false
}

// ── Send ──────────────────────────────────────────────────────────────────────
async function sendMessage(gifUrl?: string) {
  if (!managerId.value) return
  const text = messageText.value.trim()
  if (!text && !gifUrl) return
  isSending.value = true
  sendError.value = null
  showGifPicker.value = false
  try {
    await chatApi.send(leagueId, text, gifUrl)
    messageText.value = ''
    if (inputRef.value) inputRef.value.style.height = 'auto'
    mention.value = null
  } catch (e) {
    sendError.value = e instanceof Error ? e.message : 'Failed to send.'
  } finally {
    isSending.value = false
  }
}

function onGifSelected(url: string) { sendMessage(url) }

function onEnterKey(e: KeyboardEvent) {
  if (mention.value && filteredManagers.value.length > 0) return
  if (!e.shiftKey) {
    e.preventDefault()
    sendMessage()
  }
}

// ── SignalR ────────────────────────────────────────────────────────────────────
const connection = new HubConnectionBuilder()
  .withUrl('/hubs/chat')
  .withAutomaticReconnect()
  .build()

connection.on('NewMessage', (msg: ChatMessage) => {
  messages.value.push(msg)
  if (open.value) {
    scrollToBottom()
  } else {
    hasUnread.value = true
  }
})

connection.onreconnected(() => { connected.value = true })
connection.onclose(() => { connected.value = false })

// ── Lifecycle ──────────────────────────────────────────────────────────────────
onMounted(async () => {
  await store.fetchLeague(leagueId)
  await loadHistory()
  try {
    await connection.start()
    connected.value = true
    await connection.invoke('JoinLeague', leagueId)
  } catch {}
})

onUnmounted(async () => {
  try {
    await connection.invoke('LeaveLeague', leagueId)
    await connection.stop()
  } catch {}
})
</script>

<template>
  <!-- Floating toggle button -->
  <button
    class="fixed bottom-6 right-6 w-14 h-14 rounded-full shadow-2xl flex items-center justify-center transition-all"
    style="z-index: 49; background: var(--surface-raised); border: 1px solid var(--border)"
    :style="open ? 'background: var(--accent); border-color: var(--accent)' : ''"
    title="League Chat"
    @click="toggleOpen"
  >
    <!-- Unread dot -->
    <span
      v-if="hasUnread && !open"
      class="absolute top-1.5 right-1.5 w-3 h-3 rounded-full"
      style="background: var(--accent); border: 2px solid var(--bg)"
    />
    <!-- Chat icon -->
    <svg v-if="!open" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="w-6 h-6" style="color: var(--text-secondary)">
      <path fill-rule="evenodd" d="M4.804 21.644A6.707 6.707 0 006 21.75a6.721 6.721 0 003.583-1.029c.774.182 1.584.279 2.417.279 5.322 0 9.75-3.97 9.75-9 0-5.03-4.428-9-9.75-9s-9.75 3.97-9.75 9c0 2.409 1.025 4.587 2.674 6.192.232.226.277.428.254.543a3.73 3.73 0 01-.814 1.686.75.75 0 00.44 1.223 3.677 3.677 0 00-.47 0z" clip-rule="evenodd" />
    </svg>
    <!-- Close icon -->
    <svg v-else xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="w-6 h-6 text-white">
      <path fill-rule="evenodd" d="M5.47 5.47a.75.75 0 011.06 0L12 10.94l5.47-5.47a.75.75 0 111.06 1.06L13.06 12l5.47 5.47a.75.75 0 11-1.06 1.06L12 13.06l-5.47 5.47a.75.75 0 01-1.06-1.06L10.94 12 5.47 6.53a.75.75 0 010-1.06z" clip-rule="evenodd" />
    </svg>
  </button>

  <!-- Panel -->
  <Transition
    enter-active-class="transition-transform duration-200 ease-out"
    enter-from-class="translate-x-full"
    enter-to-class="translate-x-0"
    leave-active-class="transition-transform duration-150 ease-in"
    leave-from-class="translate-x-0"
    leave-to-class="translate-x-full"
  >
    <div
      v-if="open"
      class="fixed inset-y-0 right-0 w-full sm:w-96 flex flex-col"
      style="z-index: 50; background: var(--surface-raised); border-left: 1px solid var(--border); box-shadow: -8px 0 40px rgba(0,0,0,0.5)"
    >
      <!-- Panel header -->
      <div
        class="shrink-0 flex items-center gap-3 px-4 py-3"
        style="border-bottom: 1px solid var(--border)"
      >
        <div class="flex-1 min-w-0">
          <div class="flex items-center gap-2">
            <span class="font-bold text-sm truncate" style="letter-spacing: -0.02em">
              {{ store.current?.name ?? 'League' }}
            </span>
            <span
              class="w-1.5 h-1.5 rounded-full shrink-0"
              :style="connected ? 'background: var(--green, #22c55e)' : 'background: var(--text-muted)'"
              :title="connected ? 'Connected' : 'Disconnected'"
            />
          </div>
          <p v-if="myName" class="text-xs" style="color: var(--text-muted)">
            Chat · as <span style="color: var(--accent)">{{ myName }}</span>
          </p>
        </div>
        <button
          class="shrink-0 text-xl leading-none transition-colors"
          style="color: var(--text-muted)"
          @click="open = false"
        >×</button>
      </div>

      <!-- Login prompt (not authenticated) -->
      <div v-if="!managerId" class="flex-1 flex flex-col items-center justify-center p-6 text-center">
        <p class="text-2xl mb-3">💬</p>
        <p class="text-sm font-semibold mb-1">Sign in to chat</p>
        <p class="text-xs mb-5" style="color: var(--text-muted)">You need an account to send messages.</p>
        <button
          class="btn btn-primary text-sm px-5 py-2"
          @click="open = false; router.push('/login')"
        >Sign in</button>
      </div>

      <template v-else>
        <!-- Loading -->
        <div v-if="isLoading" class="flex-1 flex items-center justify-center">
          <span class="text-sm animate-pulse" style="color: var(--text-muted)">Loading chat…</span>
        </div>

        <template v-else>
          <!-- Messages -->
          <div ref="scrollRef" class="flex-1 overflow-y-auto py-3 space-y-0.5">
            <!-- Load more -->
            <div class="flex justify-center mb-3">
              <button
                v-if="hasMore"
                class="text-xs px-4 py-1.5 rounded-full transition-colors"
                style="background: var(--surface); color: var(--text-muted)"
                :disabled="loadingMore"
                @click="loadMore"
              >
                {{ loadingMore ? 'Loading…' : '↑ Load older' }}
              </button>
            </div>

            <!-- Empty state -->
            <div v-if="messages.length === 0" class="flex flex-col items-center justify-center py-12 text-center">
              <p class="text-2xl mb-2">👋</p>
              <p class="text-sm font-medium">No messages yet.</p>
              <p class="text-xs mt-1" style="color: var(--text-muted)">Be the first to say something.</p>
            </div>

            <!-- Message rows -->
            <div
              v-for="(msg, i) in messages"
              :key="msg.id"
              class="flex gap-2.5 px-3"
              :class="isGrouped(i) ? 'mt-0.5' : 'mt-3'"
            >
              <!-- Avatar -->
              <div class="shrink-0 w-7">
                <div
                  v-if="!isGrouped(i)"
                  class="w-7 h-7 rounded-full flex items-center justify-center text-[10px] font-bold text-white"
                  :style="{ background: avatarColor(msg.managerName) }"
                >
                  {{ initials(msg.managerName) }}
                </div>
              </div>

              <!-- Content -->
              <div class="flex-1 min-w-0">
                <div v-if="!isGrouped(i)" class="flex items-baseline gap-2 mb-0.5">
                  <span class="text-xs font-semibold">{{ msg.managerName }}</span>
                  <span class="text-[10px]" style="color: var(--text-muted)">{{ formatTime(msg.sentAt) }}</span>
                  <span
                    v-if="msg.managerId === managerId"
                    class="text-[9px] font-medium px-1 py-0.5 rounded-full"
                    style="background: var(--accent-dim); color: var(--accent)"
                  >you</span>
                </div>

                <img
                  v-if="msg.gifUrl"
                  :src="msg.gifUrl"
                  alt="GIF"
                  class="rounded-lg max-h-40 mb-1"
                  style="max-width: 240px"
                  loading="lazy"
                />

                <p
                  v-if="msg.content"
                  class="text-sm leading-relaxed"
                  style="color: var(--text-secondary)"
                  v-html="renderContent(msg.content)"
                />
              </div>
            </div>
          </div>

          <!-- GIF Picker -->
          <div v-if="showGifPicker" class="shrink-0 px-3 pb-2">
            <GifPicker :show="showGifPicker" @select="onGifSelected" />
          </div>

          <!-- @mention dropdown -->
          <div
            v-if="mention && filteredManagers.length > 0"
            class="shrink-0 mx-3 rounded-xl overflow-hidden mb-1"
            style="background: var(--surface-raised); border: 1px solid var(--border)"
          >
            <button
              v-for="(mgr, i) in filteredManagers"
              :key="mgr.id"
              class="w-full flex items-center gap-2.5 px-4 py-2 text-sm text-left transition-colors"
              :style="i === mentionIndex
                ? 'background: var(--accent-dim); color: var(--accent)'
                : 'color: var(--text-secondary)'"
              @mouseenter="mentionIndex = i"
              @click="insertMention(mgr.displayName)"
            >
              <div
                class="w-5 h-5 rounded-full flex items-center justify-center text-[9px] font-bold text-white shrink-0"
                :style="{ background: avatarColor(mgr.displayName) }"
              >
                {{ initials(mgr.displayName) }}
              </div>
              {{ mgr.displayName }}
            </button>
          </div>

          <!-- Input bar -->
          <div class="shrink-0 px-3 py-3" style="border-top: 1px solid var(--border)">
            <div v-if="sendError" class="text-xs text-red-400 mb-2">{{ sendError }}</div>
            <div
              class="flex items-end gap-2 rounded-xl px-3 py-2"
              style="background: var(--surface); border: 1px solid var(--border)"
            >
              <button
                class="shrink-0 text-xs font-bold transition-colors mb-0.5 px-2 py-1 rounded"
                :style="showGifPicker
                  ? 'background: var(--accent-dim); color: var(--accent)'
                  : 'color: var(--text-muted)'"
                title="Insert GIF"
                @click="showGifPicker = !showGifPicker"
              >
                GIF
              </button>

              <textarea
                ref="inputRef"
                v-model="messageText"
                class="flex-1 bg-transparent outline-none resize-none text-sm leading-relaxed"
                style="color: var(--text); min-height: 24px; max-height: 120px; overflow-y: auto"
                placeholder="Message the league…"
                rows="1"
                @input="onTextInput"
                @keydown.enter="onEnterKey"
                @keydown="onKeyDown"
                @blur="dismissMentionDelayed()"
              />

              <button
                class="shrink-0 btn btn-primary text-xs px-3 py-1.5"
                :disabled="isSending || (!messageText.trim() && !showGifPicker)"
                @click="sendMessage()"
              >
                {{ isSending ? '…' : 'Send' }}
              </button>
            </div>
            <p class="text-[10px] mt-1.5 px-1" style="color: var(--text-muted)">
              @name to mention · Enter to send · Shift+Enter for new line
            </p>
          </div>
        </template>
      </template>
    </div>
  </Transition>
</template>
