import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

export interface DockStatusUpdate {
  dockId: string;
  isAvailable: boolean;
  appointmentId: string | null;
}

export interface NotificationDto {
  icon: string;
  text: string;
  time: string;
}

// Add warehouseId to the temperature alert payload ---
export interface TemperatureAlert {
    warehouseId: string;
    roomId: string;
    roomName: string;
    currentTemperature: number;
    threshold: number;
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private tempHubConnection!: signalR.HubConnection;
  private dockHubConnection!: signalR.HubConnection;
  private notificationHubConnection!: signalR.HubConnection;
  private authService = inject(AuthService);

  public temperatureAlerts$ = new Subject<TemperatureAlert>(); // MODIFIED
  public dockStatusUpdate$ = new Subject<DockStatusUpdate>();
  public notificationReceived$ = new Subject<NotificationDto>();

  public startConnections = (): Promise<any> => {
    const accessTokenFactory = () => this.authService.getToken()!;

    this.tempHubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubs.temperature, { accessTokenFactory })
      .withAutomaticReconnect().build();

    this.dockHubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubs.docks, { accessTokenFactory })
      .withAutomaticReconnect().build();

    this.notificationHubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubs.notifications, { accessTokenFactory })
      .withAutomaticReconnect().build();

    this.addTemperatureAlertListener();
    this.addDockStatusUpdateListener();
    this.addNotificationReceivedListener();

    return Promise.all([
        this.tempHubConnection.start().catch(err => console.error('Temperature Hub connection failed:', err)),
        this.dockHubConnection.start().catch(err => console.error('Dock Hub connection failed:', err)),
        this.notificationHubConnection.start().catch(err => console.error('Notification Hub connection failed:', err))
    ]);
  }

  public stopConnections = () => {
    if (this.tempHubConnection) {
      this.tempHubConnection.stop().catch(err => console.error('Error stopping Temperature Hub:', err));
    }
    if (this.dockHubConnection) {
      this.dockHubConnection.stop().catch(err => console.error('Error stopping Dock Hub:', err));
    }
    if (this.notificationHubConnection) {
        this.notificationHubConnection.stop().catch(err => console.error('Error stopping Notification Hub:', err));
    }
  }

  private addTemperatureAlertListener = () => {
    // Handle the warehouseId parameter ---
    this.tempHubConnection.on('ReceiveTemperatureAlert', (warehouseId, roomId, roomName, currentTemperature, threshold) => {
      this.temperatureAlerts$.next({ warehouseId, roomId, roomName, currentTemperature, threshold });
    });
  }

  private addDockStatusUpdateListener = () => {
    this.dockHubConnection.on('ReceiveDockStatusUpdate', (update: DockStatusUpdate) => {
      this.dockStatusUpdate$.next(update);
    });
  }

  private addNotificationReceivedListener = () => {
    this.notificationHubConnection.on('ReceiveNotification', (notification: NotificationDto) => {
      this.notificationReceived$.next(notification);
    });
  }
}
