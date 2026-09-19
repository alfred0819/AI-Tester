using System;

namespace Moba.Core
{
    /// <summary>Client → host input payload (logic-side, no Unity types).</summary>
    public struct PlayerCommand
    {
        public uint Seq;
        public float MoveX;
        public float MoveY;
        public bool AttackPressed;
        public byte SkillSlot;
        public float AimX;
        public float AimY;
        public uint TargetNetId;
        public ushort ShopBuyItemId;

        public static PlayerCommand Empty(uint seq)
        {
            return new PlayerCommand { Seq = seq };
        }

        public PlayerCommand ClampedMove()
        {
            var copy = this;
            var lenSq = MoveX * MoveX + MoveY * MoveY;
            if (lenSq > 1f)
            {
                var inv = 1f / MathF.Sqrt(lenSq);
                copy.MoveX *= inv;
                copy.MoveY *= inv;
            }

            return copy;
        }
    }
}
