import * as signalR from '@microsoft/signalr';
import { logger } from '@tn-doc/shared';

export interface TagMessage {
  deviceName: string;
  tagName: string;
  tagValue: any;
}

type TagHandler = (message: TagMessage) => void;

export class MessagingService {
  private connection: signalR.HubConnection | null = null;
  private readonly handlers: Set<TagHandler> = new Set();

  constructor(private readonly url: string = 'http://localhost:5010/SignalRApp') {}

  on(handler: TagHandler) {
    this.handlers.add(handler);
  }

  off(handler: TagHandler) {
    this.handlers.delete(handler);
  }

  async start() {
    if (this.connection) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.url)
      .withAutomaticReconnect()
      .build();

    this.connection.on('Receive', (deviceName: string, tagName: string, tagValue: any) => {
      this.handlers.forEach((handler) => handler({ deviceName, tagName, tagValue }));
    });

    try {
      await this.connection.start();
      logger.debug('[MessagingService] SignalR connected');
    } catch (error: any) {
      logger.error('[MessagingService] Не удалось подключиться к SignalR', {
        error: error?.message || error?.toString()
      });
    }
  }

  async stop() {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}
