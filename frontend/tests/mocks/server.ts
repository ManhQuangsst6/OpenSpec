import { setupServer } from 'msw/node'
import { catalogHandlers } from './catalogHandlers'
import { handlers } from './handlers'

export const server = setupServer(...handlers, ...catalogHandlers)
