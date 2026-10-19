import Phaser from 'phaser';

export interface ResultData {
  winner: 'p1' | 'p2' | 'draw';
  reason: string;
}

export class ResultScene extends Phaser.Scene {
  constructor() {
    super({ key: 'Result' });
  }

  create(data: ResultData): void {
    this.cameras.main.setBackgroundColor('#1a1a2e');

    const title =
      data.winner === 'draw'
        ? '平局'
        : data.winner === 'p1'
          ? '玩家胜利！'
          : 'CPU 胜利';

    this.add
      .text(640, 280, title, { fontSize: '48px', color: '#ffffff' })
      .setOrigin(0.5);

    this.add
      .text(640, 360, `原因：${data.reason}`, { fontSize: '24px', color: '#aaaaaa' })
      .setOrigin(0.5);

    this.add
      .text(640, 440, '按 Enter 再来一局', { fontSize: '22px', color: '#88ccff' })
      .setOrigin(0.5);

    this.input.keyboard!.once('keydown-ENTER', () => {
      this.scene.start('Fight');
    });
  }
}
