import Phaser from 'phaser';

export type GameEvents = {
  'fighter:ko': { winnerId: 'p1' | 'p2'; reason: 'ko' | 'time' };
  'fighter:hit': { attackerId: string; damage: number; heavy: boolean };
};

export class EventBus extends Phaser.Events.EventEmitter {
  emit<K extends keyof GameEvents>(event: K, ...args: GameEvents[K][]): boolean {
    return super.emit(event, ...args);
  }

  on<K extends keyof GameEvents>(
    event: K,
    fn: (payload: GameEvents[K]) => void,
    context?: object,
  ): this {
    return super.on(event, fn, context);
  }
}

export const gameEvents = new EventBus();
