import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

/** shadcn klasik: Tailwind class'larini birlesik birlestirir + son sozu tekille verir. */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs))
}
