import { describe, expect, test } from 'vitest'
import { cn } from './utils'

describe('cn()', () => {
  test('class string birlestirir', () => {
    expect(cn('a', 'b')).toBe('a b')
  })

  test('falsy degerleri atlar', () => {
    expect(cn('a', false, null, undefined, '', 'b')).toBe('a b')
  })

  test('tailwind conflict son class kazanir (twMerge)', () => {
    expect(cn('p-2', 'p-4')).toBe('p-4')
    expect(cn('text-red-500', 'text-blue-500')).toBe('text-blue-500')
  })
})
