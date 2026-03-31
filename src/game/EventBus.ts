import { Events } from 'phaser';

// Bus d'événements global pour la communication entre React et Phaser
export const EventBus = new Events.EventEmitter();
