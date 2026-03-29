import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class MobileService {
  private audioContext: AudioContext | null = null;

  constructor() { }

  /**
   * Triggers haptic feedback (vibration)
   */
  vibrate(pattern: number | number[] = 50): void {
    if ('vibrate' in navigator) {
      navigator.vibrate(pattern);
    }
  }

  hapticSuccess(): void {
    this.vibrate([50, 30, 50]);
  }

  hapticError(): void {
    this.vibrate([200, 100, 200]);
  }

  hapticWarning(): void {
    this.vibrate(100);
  }

  /**
   * Plays a synthesized beep for immediate feedback without external assets
   */
  playBeep(type: 'success' | 'error' | 'scan'): void {
    try {
      if (!this.audioContext) {
        this.audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
      }

      const oscillator = this.audioContext.createOscillator();
      const gainNode = this.audioContext.createGain();

      oscillator.connect(gainNode);
      gainNode.connect(this.audioContext.destination);

      const now = this.audioContext.currentTime;

      if (type === 'success') {
        oscillator.type = 'sine';
        oscillator.frequency.setValueAtTime(880, now);
        oscillator.frequency.exponentialRampToValueAtTime(1320, now + 0.1);
        gainNode.gain.setValueAtTime(0.1, now);
        gainNode.gain.exponentialRampToValueAtTime(0.01, now + 0.2);
        oscillator.start(now);
        oscillator.stop(now + 0.2);
      } else if (type === 'error') {
        oscillator.type = 'sawtooth';
        oscillator.frequency.setValueAtTime(220, now);
        oscillator.frequency.linearRampToValueAtTime(110, now + 0.3);
        gainNode.gain.setValueAtTime(0.1, now);
        gainNode.gain.linearRampToValueAtTime(0.01, now + 0.3);
        oscillator.start(now);
        oscillator.stop(now + 0.3);
      } else {
        // Simple scan beep
        oscillator.type = 'square';
        oscillator.frequency.setValueAtTime(1000, now);
        gainNode.gain.setValueAtTime(0.05, now);
        gainNode.gain.exponentialRampToValueAtTime(0.01, now + 0.1);
        oscillator.start(now);
        oscillator.stop(now + 0.1);
      }
    } catch (e) {
      console.warn('Audio feedback failed', e);
    }
  }

  /**
   * Combined feedback for warehouse actions
   */
  notifySuccess(): void {
    this.hapticSuccess();
    this.playBeep('success');
  }

  notifyError(): void {
    this.hapticError();
    this.playBeep('error');
  }

  notifyScan(): void {
    this.vibrate(30);
    this.playBeep('scan');
  }
}
