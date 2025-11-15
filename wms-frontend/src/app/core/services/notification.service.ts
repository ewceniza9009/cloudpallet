import { Injectable, effect, inject, signal } from '@angular/core';
import { SignalRService } from './signal-r.service';

export interface Notification {
  id: string;
  icon: string;
  text: string;
  time: Date;
  isRead: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private signalr = inject(SignalRService);
  private readonly NOTIFICATION_KEY = 'wms_notifications';
  private readonly MAX_NOTIFICATIONS = 20;

  notifications = signal<Notification[]>([]);
  unreadCount = signal(0);

  constructor() {
    this.loadFromStorage();
    this.listenForRealTimeEvents();

    effect(() => {
        const unread = this.notifications().filter(n => !n.isRead).length;
        this.unreadCount.set(unread);
    });
  }

  private listenForRealTimeEvents(): void {
    // We ONLY listen to the centralized notification hub now.
    this.signalr.notificationReceived$.subscribe(notificationDto => {
      this.addNotification({
        icon: notificationDto.icon,
        text: notificationDto.text,
      });
    });
  }

  addNotification(data: { icon: string; text: string }): void {
    const newNotification: Notification = {
      id: self.crypto.randomUUID(),
      time: new Date(),
      isRead: false,
      ...data
    };

    this.notifications.update(current => {
      const updated = [newNotification, ...current];
      if (updated.length > this.MAX_NOTIFICATIONS) {
        updated.pop();
      }
      return updated;
    });

    this.saveToStorage();
  }

  markAsRead(id: string): void {
    this.notifications.update(current =>
      current.map(n => n.id === id ? { ...n, isRead: true } : n)
    );
    this.saveToStorage();
  }

  clearAll(): void {
    this.notifications.set([]);
    this.saveToStorage();
  }

  private saveToStorage(): void {
    localStorage.setItem(this.NOTIFICATION_KEY, JSON.stringify(this.notifications()));
  }

  private loadFromStorage(): void {
    const stored = localStorage.getItem(this.NOTIFICATION_KEY);
    if (stored) {
      const parsed = JSON.parse(stored).map((n: any) => ({...n, time: new Date(n.time)}));
      this.notifications.set(parsed);
    }
  }
}
