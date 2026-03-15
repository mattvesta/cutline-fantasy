export interface AuthManager {
  id: string
  displayName: string
  email: string
  avatarUrl: string | null
  isAdmin: boolean
}

interface AuthResponse {
  token: string
  manager: AuthManager
}

async function req<T>(path: string, body?: unknown): Promise<T> {
  const res = await fetch(`/api/auth${path}`, {
    method: body !== undefined ? 'POST' : 'GET',
    headers: { 'Content-Type': 'application/json' },
    ...(body !== undefined && { body: JSON.stringify(body) }),
  })
  if (!res.ok) {
    const data = await res.json().catch(() => ({}))
    throw new Error(data.error ?? `Request failed (${res.status})`)
  }
  return res.json()
}

function authedGet<T>(path: string, token: string): Promise<T> {
  return fetch(`/api/auth${path}`, {
    headers: { Authorization: `Bearer ${token}` },
  }).then(async r => {
    if (!r.ok) throw new Error('Unauthorized')
    return r.json()
  })
}

export const authApi = {
  register: (displayName: string, email: string, password: string): Promise<AuthResponse> =>
    req('/register', { displayName, email, password }),

  login: (email: string, password: string): Promise<AuthResponse> =>
    req('/login', { email, password }),

  me: (token: string): Promise<AuthManager> =>
    authedGet('/me', token),
}
