import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { server } from '../mocks/server'

// jsdom thiếu matchMedia/getComputedStyle cho pseudo-element mà antd dùng
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }),
})
globalThis.ResizeObserver ??= class {
  observe() {}
  unobserve() {}
  disconnect() {}
}
const { getComputedStyle } = window
window.getComputedStyle = (element: Element) => getComputedStyle(element)

beforeAll(() => server.listen({ onUnhandledFrame: 'error' }))
afterEach(() => {
  server.resetHandlers()
  cleanup()
})
afterAll(() => server.close())
