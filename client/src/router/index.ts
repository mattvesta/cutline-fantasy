import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: HomeView },
    { path: '/leagues/:leagueId', component: () => import('../views/LeagueView.vue') },
    { path: '/leagues/:leagueId/draft', component: () => import('../views/DraftView.vue') },
  ],
})

export default router
