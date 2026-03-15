import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import LeagueLayout from '../views/LeagueLayout.vue'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: HomeView },
    { path: '/login',    component: () => import('../views/LoginView.vue') },
    { path: '/register', component: () => import('../views/RegisterView.vue') },
    { path: '/leagues/create', component: () => import('../views/CreateLeagueView.vue'), meta: { requiresAuth: true } },
    { path: '/admin',   component: () => import('../views/AdminView.vue'), meta: { requiresAuth: true, requiresAdmin: true } },
    { path: '/players', component: () => import('../views/PlayersView.vue') },
    { path: '/players/:playerId', component: () => import('../views/PlayerDetailView.vue') },
    { path: '/managers/:managerId', component: () => import('../views/ManagerProfileView.vue') },
    {
      path: '/leagues/:leagueId',
      component: LeagueLayout,
      meta: { requiresAuth: true },
      children: [
        { path: '',                          component: () => import('../views/LeagueView.vue') },
        { path: 'teams/:teamId',             component: () => import('../views/TeamView.vue') },
        { path: 'managers',                  component: () => import('../views/LeagueManagersView.vue') },
        { path: 'draft',                     component: () => import('../views/DraftView.vue') },
        { path: 'teams/:teamId/live',        component: () => import('../views/LiveTeamView.vue') },
        { path: 'matchup',                   component: () => import('../views/WeeklyMatchupView.vue') },
        { path: 'history',                   component: () => import('../views/EliminationHistoryView.vue') },
        { path: 'teams/:teamId/waivers',     component: () => import('../views/WaiverWireView.vue'),  meta: { requiresAuth: true } },
        { path: 'teams/:teamId/trades',      component: () => import('../views/TradesView.vue'),      meta: { requiresAuth: true } },
        { path: 'commissioner',              component: () => import('../views/CommissionerView.vue'), meta: { requiresAuth: true } },
      ],
    },
  ],
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth && !auth.token) {
    return { path: '/login', query: { redirect: to.fullPath } }
  }
  if (to.meta.requiresAdmin) {
    await auth.init()
    if (!auth.isAdmin) return { path: '/' }
  }
})

export default router
