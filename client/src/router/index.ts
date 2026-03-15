import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: HomeView },
    { path: '/leagues/create', component: () => import('../views/CreateLeagueView.vue') },
    { path: '/players', component: () => import('../views/PlayersView.vue') },
    { path: '/players/:playerId', component: () => import('../views/PlayerDetailView.vue') },
    { path: '/leagues/:leagueId', component: () => import('../views/LeagueView.vue') },
    { path: '/leagues/:leagueId/teams/:teamId', component: () => import('../views/TeamView.vue') },
    { path: '/leagues/:leagueId/managers', component: () => import('../views/LeagueManagersView.vue') },
    { path: '/leagues/:leagueId/draft', component: () => import('../views/DraftView.vue') },
    { path: '/leagues/:leagueId/teams/:teamId/live', component: () => import('../views/LiveTeamView.vue') },
    { path: '/leagues/:leagueId/matchup', component: () => import('../views/WeeklyMatchupView.vue') },
    { path: '/leagues/:leagueId/teams/:teamId/waivers', component: () => import('../views/WaiverWireView.vue') },
    { path: '/managers/:managerId', component: () => import('../views/ManagerProfileView.vue') },
  ],
})

export default router
